namespace Drangit.Web.Repositories;

/// <summary>
/// Un dépôt tel qu'il est présenté sur le site.
/// </summary>
/// <remarks>
/// Ce type est le <em>seul</em> contrat entre le catalogue et les vues. Il ne porte
/// délibérément aucune des données que l'API GitHub attache à un dépôt privé ou au compte
/// lui-même (<c>private</c>, <c>visibility</c>, <c>permissions</c>, <c>owner</c>) : ce qui
/// n'existe pas dans le modèle ne peut pas fuiter dans une page publique. Un test
/// d'architecture verrouille cette propriété.
/// </remarks>
public sealed record Repository
{
    /// <summary>Identifiant GitHub du dépôt.</summary>
    public required long Id { get; init; }

    /// <summary>Identifiant du dépôt dans les URLs du site.</summary>
    public required RepositorySlug Slug { get; init; }

    /// <summary>Nom du dépôt, tel que GitHub l'affiche.</summary>
    public required string Name { get; init; }

    /// <summary>Nom complet, <c>compte/dépôt</c>.</summary>
    public required string FullName { get; init; }

    /// <summary>Adresse de la page GitHub du dépôt.</summary>
    public required Uri HtmlUrl { get; init; }

    /// <summary>Description courte, traduisible par le fichier éditorial.</summary>
    public LocalizedText Description { get; init; } = LocalizedText.Empty;

    /// <summary>Texte long rédigé localement, absent pour la plupart des dépôts.</summary>
    public LocalizedText LongDescription { get; init; } = LocalizedText.Empty;

    /// <summary>Langage principal détecté par GitHub, absent pour un dépôt vide.</summary>
    public string? Language { get; init; }

    /// <summary>Sujets déclarés sur le dépôt.</summary>
    public IReadOnlyList<string> Topics { get; init; } = [];

    /// <summary>Nombre d'étoiles.</summary>
    public int Stars { get; init; }

    /// <summary>Nombre de bifurcations.</summary>
    public int Forks { get; init; }

    /// <summary>Date du dernier commit poussé, absente pour un dépôt vide.</summary>
    public DateTimeOffset? PushedAt { get; init; }

    /// <summary>Date de création du dépôt.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>Licence déclarée (identifiant SPDX), absente si le dépôt n'en publie pas.</summary>
    public string? License { get; init; }

    /// <summary>Démonstration en ligne, quand le dépôt en déclare une.</summary>
    public Uri? HomepageUrl { get; init; }

    /// <summary>Indique que le dépôt est archivé : lecture seule, plus maintenu.</summary>
    public bool IsArchived { get; init; }

    /// <summary>Indique que le dépôt est une bifurcation d'un autre.</summary>
    public bool IsFork { get; init; }

    /// <summary>Captures d'écran, alimentées par le fichier éditorial.</summary>
    public IReadOnlyList<Screenshot> Screenshots { get; init; } = [];

    /// <summary>
    /// Indique que le dépôt est désigné pour la vitrine de l'accueil.
    /// </summary>
    /// <remarks>
    /// Alimenté par le fichier éditorial : GitHub ne connaît pas cette notion, qui relève
    /// d'un choix de présentation.
    /// </remarks>
    public bool IsFeatured { get; init; }

    /// <summary>Indique qu'une démonstration en ligne est accessible.</summary>
    public bool HasDemo => HomepageUrl is not null;

    /// <summary>
    /// Illustration propre au dépôt, exploitable hors du site.
    /// </summary>
    /// <remarks>
    /// Seule une capture du fichier éditorial en est une. La carte de GitHub en est exclue :
    /// elle porte l'avatar du compte, et un aperçu de partage s'affiche chez autrui, là où
    /// aucun recadrage ne s'applique. Sans capture, c'est au site de fournir son image —
    /// <c>SeoHead</c> s'en charge.
    /// </remarks>
    public Uri? ShowcaseImageUrl => Screenshots.Count > 0 ? Screenshots[0].Url : null;
}
