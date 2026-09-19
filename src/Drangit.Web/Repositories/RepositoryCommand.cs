namespace Drangit.Web.Repositories;

/// <summary>Pages du site qu'une commande peut demander.</summary>
public enum SitePage
{
    /// <summary>La liste des dépôts.</summary>
    Home,

    /// <summary>La page de présentation du site.</summary>
    About,
}

/// <summary>
/// Ce qu'une ligne saisie dans le prompt demande au site.
/// </summary>
/// <remarks>
/// <para>
/// Le prompt double la barre de filtres, il ne la remplace pas (ADR 0008) : chaque commande
/// aboutit à une page que le visiteur aurait pu atteindre en cliquant. D'où trois issues
/// seulement — filtrer la liste, ouvrir une fiche, changer de page — et aucune notion
/// d'erreur : ce qui n'est pas une commande connue est ce qu'on cherche.
/// </para>
/// <para>
/// Le type ne connaît aucune adresse. Le préfixe de langue vit dans le chemin (ADR 0006) et
/// c'est la page qui compose le lien ; ici, on ne dit que l'intention.
/// </para>
/// </remarks>
public abstract record RepositoryCommand
{
    private RepositoryCommand()
    {
    }

    /// <summary>Filtrer la liste des dépôts.</summary>
    public sealed record Filter(RepositoryFilter Value) : RepositoryCommand;

    /// <summary>Ouvrir la fiche d'un dépôt désigné sans ambiguïté.</summary>
    public sealed record Open(RepositorySlug Slug) : RepositoryCommand;

    /// <summary>Aller sur une page du site.</summary>
    public sealed record GoToPage(SitePage Page) : RepositoryCommand;

    /// <summary>
    /// Lit une ligne saisie et en déduit ce qu'elle demande.
    /// </summary>
    /// <param name="line">Ligne telle que saisie, drapeaux compris.</param>
    /// <param name="catalogue">
    /// Dépôts connus, nécessaires pour résoudre le nom donné à <c>open</c> ou à <c>cd</c>.
    /// </param>
    public static RepositoryCommand Parse(string? line, IReadOnlyList<Repository> catalogue)
    {
        ArgumentNullException.ThrowIfNull(catalogue);

        var tokens = Tokenize(line);

        if (tokens.Count == 0)
        {
            return new Filter(RepositoryFilter.Empty);
        }

        var verb = tokens[0];
        var arguments = tokens.Skip(1).ToList();

        return verb.ToLowerInvariant() switch
        {
            // « repos » et « ls » sont le verbe par défaut : les drapeaux qui suivent sont
            // les mêmes que sans eux.
            "repos" or "ls" or "ll" or "dir" => new Filter(ToFilter(arguments)),
            "find" or "search" => new Filter(new RepositoryFilter { SearchTerm = Join(arguments) }),
            "open" or "cat" => Resolve(arguments, catalogue),
            "cd" => ChangeDirectory(arguments, catalogue),
            _ => new Filter(ToFilter(tokens)),
        };
    }

    /// <summary>
    /// Ouvre le dépôt désigné, ou cherche le terme quand il n'en désigne pas exactement un.
    /// </summary>
    /// <remarks>
    /// Un nom ambigu — « huffman », que deux dépôts portent — ne produit pas d'erreur : la
    /// recherche montre les candidats, et le visiteur choisit dans la liste, ce qu'il aurait
    /// fait de toute façon.
    /// </remarks>
    private static RepositoryCommand Resolve(List<string> arguments, IReadOnlyList<Repository> catalogue)
    {
        var name = Join(arguments);

        if (string.IsNullOrWhiteSpace(name))
        {
            return new Filter(RepositoryFilter.Empty);
        }

        var exact = catalogue.FirstOrDefault(
            repository => string.Equals(repository.Name, name, StringComparison.OrdinalIgnoreCase));

        if (exact is not null)
        {
            return new Open(exact.Slug);
        }

        var matches = catalogue
            .Where(repository => repository.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return matches.Count == 1
            ? new Open(matches[0].Slug)
            : new Filter(new RepositoryFilter { SearchTerm = name });
    }

    private static RepositoryCommand ChangeDirectory(List<string> arguments, IReadOnlyList<Repository> catalogue)
    {
        var target = Join(arguments);

        return target switch
        {
            "" or "/" or "~" or ".." => new GoToPage(SitePage.Home),
            _ when IsAboutPage(target) => new GoToPage(SitePage.About),
            _ => Resolve(arguments, catalogue),
        };
    }

    private static bool IsAboutPage(string target) =>
        target.Equals("about", StringComparison.OrdinalIgnoreCase)
        || target.Equals("a-propos", StringComparison.OrdinalIgnoreCase);

    /// <summary>Traduit une suite de jetons en critères, l'inverse de <see cref="RepositoryFilter.ToCommandLine"/>.</summary>
    private static RepositoryFilter ToFilter(List<string> tokens)
    {
        string? topic = null;
        string? language = null;
        var withDemo = false;
        var hideArchived = false;
        var terms = new List<string>();

        foreach (var token in tokens)
        {
            if (ValueOf(token, "--topic=") is { } topicValue)
            {
                topic = topicValue;
            }
            else if (ValueOf(token, "--lang=") is { } languageValue)
            {
                language = languageValue;
            }
            else if (token.Equals("--demo", StringComparison.OrdinalIgnoreCase))
            {
                withDemo = true;
            }
            else if (token.Equals("--no-archived", StringComparison.OrdinalIgnoreCase)
                || token.Equals("--active", StringComparison.OrdinalIgnoreCase))
            {
                hideArchived = true;
            }
            else if (!token.StartsWith('-'))
            {
                // Tout ce qui n'est pas un drapeau est un terme de recherche : « --list » et
                // les drapeaux inconnus sont ignorés plutôt que cherchés, sans quoi une faute
                // de frappe sur un drapeau renverrait zéro dépôt sans dire pourquoi.
                terms.Add(token);
            }
        }

        return new RepositoryFilter
        {
            Topic = topic,
            Language = language,
            SearchTerm = terms.Count > 0 ? string.Join(' ', terms) : null,
            WithDemo = withDemo,
            HideArchived = hideArchived,
        };
    }

    private static string? ValueOf(string token, string flag) =>
        token.StartsWith(flag, StringComparison.OrdinalIgnoreCase) && token.Length > flag.Length
            ? token[flag.Length..]
            : null;

    private static string Join(List<string> arguments) => string.Join(' ', arguments);

    /// <summary>
    /// Découpe la ligne en jetons, en gardant d'un seul tenant ce qui est entre guillemets.
    /// </summary>
    /// <remarks>
    /// Sans cela, <c>"game dev"</c> deviendrait deux termes combinés par ET, et la ligne
    /// affichée au-dessus de la barre de filtres ne serait plus celle qu'on peut retaper.
    /// </remarks>
    private static List<string> Tokenize(string? line)
    {
        var tokens = new List<string>();

        if (string.IsNullOrWhiteSpace(line))
        {
            return tokens;
        }

        var current = new System.Text.StringBuilder();
        var quoted = false;

        foreach (var character in line)
        {
            if (character == '"')
            {
                quoted = !quoted;
            }
            else if (char.IsWhiteSpace(character) && !quoted)
            {
                Flush(tokens, current);
            }
            else
            {
                current.Append(character);
            }
        }

        Flush(tokens, current);

        return tokens;
    }

    private static void Flush(List<string> tokens, System.Text.StringBuilder current)
    {
        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
            current.Clear();
        }
    }
}
