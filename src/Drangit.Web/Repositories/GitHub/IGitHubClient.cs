namespace Drangit.Web.Repositories.GitHub;

/// <summary>
/// Accès à l'API GitHub, du point de vue du catalogue.
/// </summary>
/// <remarks>
/// Cette abstraction isole un appel réseau : elle coûte une interface et un enregistrement
/// dans le conteneur, et le paie aujourd'hui en permettant de tester le cache, le repli sur
/// instantané et la fusion des métadonnées sans monter de serveur HTTP.
/// </remarks>
public interface IGitHubClient
{
    /// <summary>
    /// Retourne les dépôts publics du compte configuré, du plus récemment poussé au plus ancien.
    /// </summary>
    /// <exception cref="HttpRequestException">L'API est injoignable ou refuse la requête.</exception>
    /// <exception cref="GitHubRateLimitException">Le quota d'appels est épuisé.</exception>
    Task<IReadOnlyList<Repository>> GetPublicRepositoriesAsync(CancellationToken cancellationToken);
}
