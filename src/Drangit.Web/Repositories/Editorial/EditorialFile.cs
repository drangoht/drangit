using System.Text.Json.Serialization;

namespace Drangit.Web.Repositories.Editorial;

/// <summary>Fichier de contenu éditorial, indexé par slug de dépôt.</summary>
internal sealed record EditorialFile
{
    [JsonPropertyName("repositories")]
    public Dictionary<string, EditorialEntry> Repositories { get; init; } = [];
}

/// <summary>
/// Ce que l'API GitHub ne fournit pas : mise en vitrine, description longue, captures,
/// traductions françaises, et le droit de ne pas figurer sur le site.
/// </summary>
/// <remarks>
/// Toutes les propriétés sont facultatives. Une entrée partielle complète les données
/// GitHub sans jamais les effacer.
/// </remarks>
internal sealed record EditorialEntry
{
    [JsonPropertyName("summary")]
    public EditorialText? Summary { get; init; }

    [JsonPropertyName("description")]
    public EditorialText? Description { get; init; }

    /// <summary>
    /// Sujets ajoutés à ceux que le dépôt déclare sur GitHub.
    /// </summary>
    /// <remarks>
    /// Ajoutés, jamais substitués : GitHub reste la source de vérité de ce qu'un dépôt dit
    /// de lui-même. Ce champ existe pour classer sans avoir à retourner sur github.com — et
    /// pour les dépôts anciens, qui n'ont jamais reçu de sujet.
    /// </remarks>
    [JsonPropertyName("topics")]
    public IReadOnlyList<string> Topics { get; init; } = [];

    [JsonPropertyName("screenshots")]
    public IReadOnlyList<EditorialScreenshot> Screenshots { get; init; } = [];

    /// <summary>
    /// Démonstration en ligne, quand le champ « homepage » du dépôt ne la porte pas.
    /// </summary>
    [JsonPropertyName("demoUrl")]
    public string? DemoUrl { get; init; }

    [JsonPropertyName("featured")]
    public bool Featured { get; init; }

    /// <summary>Retire le dépôt du site, sans toucher à sa visibilité sur GitHub.</summary>
    [JsonPropertyName("hidden")]
    public bool Hidden { get; init; }
}

/// <summary>Texte bilingue tel qu'il est saisi dans le fichier éditorial.</summary>
internal sealed record EditorialText
{
    [JsonPropertyName("en")]
    public string? English { get; init; }

    [JsonPropertyName("fr")]
    public string? French { get; init; }

    public LocalizedText ToLocalizedText() => new(English, French);
}

/// <summary>Capture d'écran telle qu'elle est saisie dans le fichier éditorial.</summary>
internal sealed record EditorialScreenshot
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("caption")]
    public EditorialText? Caption { get; init; }
}
