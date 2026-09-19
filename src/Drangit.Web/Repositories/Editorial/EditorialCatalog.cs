using System.Text.Json;

namespace Drangit.Web.Repositories.Editorial;

/// <summary>
/// Contenu rédigé à la main qui complète ce que l'API GitHub ne publie pas : mise en
/// vitrine, description longue, captures d'écran, traductions françaises, et la liste des
/// dépôts que le site ne montre pas.
/// </summary>
/// <remarks>
/// Le catalogue est indexé par slug de dépôt — le nom que l'on lit dans l'URL GitHub, donc
/// celui qu'un humain reconnaît en éditant le fichier. Une entrée absente n'est pas une
/// erreur : le dépôt reste affiché avec les seules données de l'API.
/// </remarks>
public sealed class EditorialCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly IReadOnlyDictionary<string, EditorialEntry> _entriesBySlug;

    private EditorialCatalog(IReadOnlyDictionary<string, EditorialEntry> entriesBySlug) =>
        _entriesBySlug = entriesBySlug;

    /// <summary>Catalogue sans aucune entrée : tous les dépôts restent tels que GitHub les décrit.</summary>
    public static EditorialCatalog Empty { get; } =
        new(new Dictionary<string, EditorialEntry>(StringComparer.OrdinalIgnoreCase));

    /// <summary>Indique qu'aucun dépôt n'est enrichi.</summary>
    public bool IsEmpty => _entriesBySlug.Count == 0;

    /// <summary>
    /// Charge le contenu éditorial depuis son JSON.
    /// </summary>
    /// <exception cref="JsonException">
    /// Le fichier est syntaxiquement invalide. Il est versionné avec le code : mieux vaut un
    /// démarrage en échec qu'un site amputé de ses descriptions sans que personne ne le voie.
    /// </exception>
    public static EditorialCatalog FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var file = JsonSerializer.Deserialize<EditorialFile>(json, SerializerOptions);

        if (file is null)
        {
            return Empty;
        }

        var entries = new Dictionary<string, EditorialEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var (slug, entry) in file.Repositories)
        {
            entries[slug.Trim()] = entry;
        }

        return new EditorialCatalog(entries);
    }

    /// <summary>
    /// Complète un dépôt avec son entrée éditoriale, s'il en a une.
    /// </summary>
    public Repository Apply(Repository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (!_entriesBySlug.TryGetValue(repository.Slug.Value, out var entry))
        {
            return repository;
        }

        return repository with
        {
            Topics = MergeTopics(repository.Topics, entry.Topics),
            Description = repository.Description.OverlaidWith(entry.Summary?.ToLocalizedText()),
            LongDescription = repository.LongDescription.OverlaidWith(entry.Description?.ToLocalizedText()),
            Screenshots = ToScreenshots(entry.Screenshots) is { Count: > 0 } screenshots
                ? screenshots
                : repository.Screenshots,
            // La démonstration déclarée localement ne s'impose que si le dépôt n'en porte
            // pas : « homepage » se corrige sur GitHub, ce fichier ne doit pas le doubler.
            HomepageUrl = repository.HomepageUrl ?? ToUri(entry.DemoUrl),
            IsFeatured = entry.Featured,
        };
    }

    /// <summary>
    /// Indique que le dépôt est explicitement retiré du site.
    /// </summary>
    /// <remarks>
    /// Un dépôt public qu'on ne souhaite pas exposer ici — bac à sable, dépôt de
    /// configuration, suivi de tutoriel — reste public sur GitHub. C'est une décision de
    /// présentation, pas de confidentialité.
    /// </remarks>
    public bool IsHidden(Repository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        return _entriesBySlug.TryGetValue(repository.Slug.Value, out var entry) && entry.Hidden;
    }

    /// <summary>
    /// Retourne les slugs du fichier éditorial qui ne correspondent à aucun dépôt public.
    /// </summary>
    /// <remarks>
    /// Une faute de frappe dans le fichier produit sinon un enrichissement silencieusement
    /// ignoré — ou pire, un dépôt qu'on croyait masqué et qui reste affiché. On les
    /// journalise au démarrage.
    /// </remarks>
    public IReadOnlyList<string> FindOrphanEntries(IEnumerable<Repository> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        var knownSlugs = repositories
            .Select(repository => repository.Slug.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return [.. _entriesBySlug.Keys.Where(slug => !knownSlugs.Contains(slug)).Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Réunit les sujets du dépôt et ceux du fichier éditorial, sans doublon.
    /// </summary>
    /// <remarks>
    /// Ceux de GitHub d'abord : ce sont ceux que le dépôt affiche déjà là-bas, et un
    /// visiteur venu de GitHub doit les retrouver en tête.
    /// </remarks>
    private static IReadOnlyList<string> MergeTopics(
        IReadOnlyList<string> fromGitHub,
        IReadOnlyList<string> fromEditorial)
    {
        if (fromEditorial.Count == 0)
        {
            return fromGitHub;
        }

        return
        [
            .. fromGitHub
                .Concat(fromEditorial.Where(topic => !string.IsNullOrWhiteSpace(topic)).Select(topic => topic.Trim()))
                .Distinct(StringComparer.OrdinalIgnoreCase),
        ];
    }

    private static IReadOnlyList<Screenshot> ToScreenshots(IReadOnlyList<EditorialScreenshot> screenshots) =>
    [
        .. screenshots
            .Select(screenshot => new
            {
                Uri = ToUri(screenshot.Url),
                screenshot.Caption,
            })
            .Where(screenshot => screenshot.Uri is not null)
            .Select(screenshot => new Screenshot(
                screenshot.Uri!,
                screenshot.Caption?.ToLocalizedText() ?? LocalizedText.Empty)),
    ];

    private static Uri? ToUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
}
