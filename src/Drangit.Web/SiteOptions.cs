using System.ComponentModel.DataAnnotations;

namespace Drangit.Web;

/// <summary>Identité du site et liens affichés dans le pied de page.</summary>
public sealed class SiteOptions
{
    /// <summary>Section de configuration correspondante.</summary>
    public const string SectionName = "Site";

    /// <summary>Nom affiché dans l'en-tête et le titre des pages.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Name { get; init; } = "Drangit";

    /// <summary>Profil GitHub du compte exposé.</summary>
    [Required(AllowEmptyStrings = false)]
    [Url]
    public string GitHubUrl { get; init; } = "https://github.com/drangoht";

    /// <summary>Page itch.io du même auteur, facultative.</summary>
    /// <remarks>
    /// Le déploiement écrit toutes les clés du modèle dans l'environnement du conteneur,
    /// renseignées ou non : une valeur absente arrive donc en chaîne vide, que
    /// <see cref="UrlAttribute"/> refuse. La normaliser ici fait dire à « facultatif » ce
    /// qu'il annonce, au lieu d'empêcher le démarrage.
    /// </remarks>
    [Url]
    public string? ItchProfileUrl
    {
        get => _itchProfileUrl;
        init => _itchProfileUrl = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private readonly string? _itchProfileUrl;
}
