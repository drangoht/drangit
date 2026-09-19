using Refit;

namespace Drangit.Web.Repositories.GitHub;

/// <summary>
/// Contrat HTTP de l'API GitHub, tel que Refit l'implémente.
/// </summary>
/// <remarks>
/// Ce type décrit le protocole — route, en-têtes, forme de la réponse — et rien d'autre :
/// aucune décision ne s'y prend. La traduction en <see cref="Repository"/> revient à
/// <see cref="GitHubClient"/>, seul à porter les règles du site.
/// </remarks>
internal interface IGitHubApi
{
    /// <summary>
    /// Appelle <c>GET /users/{login}/repos</c>, qui liste les dépôts <em>publics</em> d'un
    /// compte — y compris authentifié, ce point d'accès n'en expose jamais d'autres.
    /// </summary>
    /// <param name="login">Compte dont on liste les dépôts.</param>
    /// <param name="perPage">Taille de page demandée ; GitHub plafonne à 100.</param>
    /// <param name="page">Numéro de page, à partir de 1.</param>
    /// <param name="authorization">
    /// En-tête d'autorisation complet (<c>Bearer …</c>), ou <c>null</c> pour un appel
    /// anonyme — Refit omet alors l'en-tête, là où une valeur vide vaudrait un 401.
    /// </param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    [Get("/users/{login}/repos?type=owner&sort=pushed&direction=desc")]
    [Headers("Accept: application/vnd.github+json", "X-GitHub-Api-Version: 2022-11-28")]
    Task<IReadOnlyList<GitHubRepositoryDto>> GetPublicRepositoriesAsync(
        string login,
        // AliasAs, et non Query : c'est lui qui renomme le paramètre. Avec Query, Refit
        // envoie « perPage », que GitHub ignore — la page retombe alors à sa taille par
        // défaut de 30, et la pagination s'arrête sur la première page en croyant l'avoir
        // vue incomplète.
        [AliasAs("per_page")] int perPage,
        int page,
        [Header("Authorization")] string? authorization,
        CancellationToken cancellationToken);
}
