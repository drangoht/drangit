using System.ComponentModel.DataAnnotations;

namespace Drangit.Web.Repositories.GitHub;

/// <summary>Configuration de l'accès à l'API GitHub.</summary>
public sealed class GitHubOptions
{
    /// <summary>Section de configuration correspondante.</summary>
    public const string SectionName = "GitHub";

    /// <summary>Compte dont les dépôts publics sont exposés.</summary>
    [Required(AllowEmptyStrings = false)]
    [RegularExpression(
        "^[A-Za-z0-9](?:[A-Za-z0-9]|-(?=[A-Za-z0-9])){0,38}$",
        ErrorMessage = "Le compte doit être un identifiant GitHub valide.")]
    public string Login { get; init; } = "drangoht";

    /// <summary>
    /// Jeton d'accès personnel, facultatif (ADR 0003).
    /// </summary>
    /// <remarks>
    /// C'est un secret : il est fourni par variable d'environnement (<c>GitHub__Token</c>)
    /// ou par les user-secrets en développement, jamais par <c>appsettings.json</c>. Absent,
    /// le site interroge l'API en anonyme — 60 requêtes par heure et par adresse IP, ce que
    /// le cache et l'instantané de repli suffisent à absorber. Présent, il porte le quota à
    /// 5000. Aucune portée n'est requise : le site ne lit que des dépôts publics.
    /// </remarks>
    public string? Token
    {
        get => _token;
        // Le déploiement écrit toutes les clés du modèle dans l'environnement du conteneur,
        // renseignées ou non : un jeton absent arrive donc en chaîne vide, qui produirait un
        // en-tête « Bearer » vide — refusé par GitHub avec un 401 au lieu d'un appel anonyme.
        init => _token = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>Durée pendant laquelle la réponse de l'API est réutilisée sans nouvel appel.</summary>
    [Range(typeof(TimeSpan), "00:00:30", "24:00:00")]
    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>Délai maximal accordé à un appel à l'API, tentatives de reprise comprises.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:02:00")]
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Nombre maximal de pages demandées à l'API en une récupération.
    /// </summary>
    /// <remarks>
    /// Une borne, pas un réglage : elle existe pour qu'une pagination qui ne se terminerait
    /// pas — réponse inattendue, changement de contrat — ne tourne pas en boucle sur le
    /// quota d'API. Cent dépôts par page suffisent largement au compte visé.
    /// </remarks>
    [Range(1, 20)]
    public int MaxPages { get; init; } = 5;

    /// <summary>
    /// Indique que les bifurcations sont listées comme les autres dépôts.
    /// </summary>
    /// <remarks>
    /// Écartées par défaut : une vitrine montre ce qu'on a écrit, et un compte qui a
    /// bifurqué vingt dépôts pour y corriger une virgule les verrait noyer son propre
    /// travail.
    /// </remarks>
    public bool IncludeForks { get; init; }

    private readonly string? _token;
}
