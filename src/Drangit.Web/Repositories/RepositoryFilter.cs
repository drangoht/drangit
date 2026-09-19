using System.Globalization;
using System.Text;

namespace Drangit.Web.Repositories;

/// <summary>
/// Critères de sélection appliqués au catalogue : sujet, langage, démonstration, état
/// d'archivage et recherche textuelle, combinés par ET.
/// </summary>
/// <remarks>
/// Le filtrage est calculé en mémoire sur le catalogue déjà chargé : à cette volumétrie,
/// une requête vers GitHub par changement de filtre coûterait bien plus que le parcours —
/// et consommerait le quota d'API pour rien.
/// </remarks>
public sealed record RepositoryFilter
{
    /// <summary>Filtre sans aucun critère : tout le catalogue passe.</summary>
    public static RepositoryFilter Empty { get; } = new();

    /// <summary>Sujet exigé, insensible à la casse.</summary>
    public string? Topic { get; init; }

    /// <summary>Langage principal exigé, insensible à la casse.</summary>
    public string? Language { get; init; }

    /// <summary>Terme recherché dans le nom, les descriptions, les sujets et le langage.</summary>
    public string? SearchTerm { get; init; }

    /// <summary>Ne retient que les dépôts qui publient une démonstration en ligne.</summary>
    public bool WithDemo { get; init; }

    /// <summary>Écarte les dépôts archivés.</summary>
    public bool HideArchived { get; init; }

    /// <summary>Indique qu'au moins un critère exploitable est posé.</summary>
    public bool IsActive =>
        !string.IsNullOrWhiteSpace(Topic)
        || !string.IsNullOrWhiteSpace(Language)
        || !string.IsNullOrWhiteSpace(SearchTerm)
        || WithDemo
        || HideArchived;

    /// <summary>
    /// Écrit les critères posés comme la ligne de commande qui les produirait.
    /// </summary>
    /// <remarks>
    /// L'accueil l'affiche au-dessus de la barre de filtres : cliquer un critère et taper la
    /// commande mènent au même endroit, et la ligne le montre. L'ordre suit celui de la barre,
    /// le terme recherché venant en dernier — c'est le seul argument qui n'est pas un drapeau.
    /// </remarks>
    public string ToCommandLine()
    {
        var flags = new List<string>(5);

        if (!string.IsNullOrWhiteSpace(Topic))
        {
            flags.Add($"--topic={Topic}");
        }

        if (!string.IsNullOrWhiteSpace(Language))
        {
            flags.Add($"--lang={Language}");
        }

        if (WithDemo)
        {
            flags.Add("--demo");
        }

        if (HideArchived)
        {
            flags.Add("--no-archived");
        }

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            flags.Add(Quote(SearchTerm));
        }

        return flags.Count > 0 ? string.Join(' ', flags) : "--list";
    }

    /// <summary>Met un terme entre guillemets s'il ne tiendrait pas en un seul mot.</summary>
    private static string Quote(string term) =>
        term.Any(char.IsWhiteSpace) ? $"\"{term}\"" : term;

    /// <summary>Applique les critères en conservant l'ordre du catalogue.</summary>
    public IReadOnlyList<Repository> Apply(IEnumerable<Repository> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        var searchTerm = Normalize(SearchTerm);

        return
        [
            .. repositories.Where(repository =>
                MatchesTopic(repository)
                && MatchesLanguage(repository)
                && MatchesDemo(repository)
                && MatchesArchived(repository)
                && MatchesSearch(repository, searchTerm)),
        ];
    }

    /// <summary>Sujets présents dans le catalogue, du plus fréquent au plus rare.</summary>
    /// <remarks>
    /// Le tri alphabétique conviendrait à une liste courte ; celle-ci suit le nombre de
    /// dépôts, car la barre de sujets est tronquée à l'affichage et doit montrer d'abord ce
    /// qui caractérise le compte. À fréquence égale, l'ordre alphabétique départage, sans
    /// quoi la page changerait d'un rafraîchissement à l'autre.
    /// </remarks>
    public static IReadOnlyList<RepositoryFacet> AvailableTopicsOf(IEnumerable<Repository> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        return
        [
            .. repositories.SelectMany(repository => repository.Topics)
                .GroupBy(topic => topic, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.CurrentCulture)
                .Select(group => new RepositoryFacet(group.Key, group.Count())),
        ];
    }

    /// <summary>Langages présents dans le catalogue, triés alphabétiquement.</summary>
    /// <remarks>
    /// Alphabétique et non par fréquence : la liste est déroulante, donc entière sous les
    /// yeux, et on y cherche un langage qu'on a déjà en tête.
    /// </remarks>
    public static IReadOnlyList<RepositoryFacet> AvailableLanguagesOf(IEnumerable<Repository> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        return
        [
            .. repositories.Select(repository => repository.Language)
                .OfType<string>()
                .GroupBy(language => language, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.CurrentCulture)
                .Select(group => new RepositoryFacet(group.Key, group.Count())),
        ];
    }

    /// <summary>Nombre de dépôts qui publient une démonstration en ligne.</summary>
    public static int CountWithDemo(IEnumerable<Repository> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        return repositories.Count(repository => repository.HasDemo);
    }

    /// <summary>Nombre de dépôts archivés.</summary>
    public static int CountArchived(IEnumerable<Repository> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        return repositories.Count(repository => repository.IsArchived);
    }

    private bool MatchesTopic(Repository repository) =>
        string.IsNullOrWhiteSpace(Topic)
        || repository.Topics.Contains(Topic, StringComparer.OrdinalIgnoreCase);

    private bool MatchesLanguage(Repository repository) =>
        string.IsNullOrWhiteSpace(Language)
        || string.Equals(repository.Language, Language, StringComparison.OrdinalIgnoreCase);

    private bool MatchesDemo(Repository repository) => !WithDemo || repository.HasDemo;

    private bool MatchesArchived(Repository repository) => !HideArchived || !repository.IsArchived;

    private static bool MatchesSearch(Repository repository, string? searchTerm)
    {
        if (searchTerm is null)
        {
            return true;
        }

        return Contains(repository.Name, searchTerm)
            || Contains(repository.Language, searchTerm)
            || Contains(repository.Description.English, searchTerm)
            || Contains(repository.Description.French, searchTerm)
            || repository.Topics.Any(topic => Contains(topic, searchTerm));
    }

    private static bool Contains(string? haystack, string needle) =>
        Normalize(haystack) is { } normalized && normalized.Contains(needle, StringComparison.Ordinal);

    /// <summary>
    /// Réduit un texte à une forme comparable : minuscules et sans signes diacritiques.
    /// </summary>
    /// <remarks>
    /// Sans cette normalisation, « générateur » ne trouverait pas « generateur » saisi sans
    /// accents, et inversement — cas courant sur un site bilingue.
    /// </remarks>
    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
