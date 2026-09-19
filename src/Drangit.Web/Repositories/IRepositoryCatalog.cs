namespace Drangit.Web.Repositories;

/// <summary>
/// Source des dépôts affichés par le site, du point de vue des pages.
/// </summary>
public interface IRepositoryCatalog
{
    /// <summary>
    /// Retourne les dépôts publics du compte, du plus récemment mis à jour au plus ancien.
    /// </summary>
    /// <remarks>Ne lève pas quand la source distante est indisponible : voir les implémentations.</remarks>
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync(CancellationToken cancellationToken);
}
