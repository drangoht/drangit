namespace Drangit.Web.Repositories;

/// <summary>Choix du dépôt mis en vitrine sur l'accueil.</summary>
public static class FeaturedRepository
{
    /// <summary>
    /// Désigne le dépôt de la vitrine, ou <c>null</c> si le catalogue est vide.
    /// </summary>
    /// <remarks>
    /// L'ordre des critères est une règle de présentation, pas un détail : la mise en avant
    /// éditoriale prime, puis une démonstration jouable — c'est ce qui se montre le mieux —,
    /// puis la popularité, et enfin la fraîcheur. Un dépôt archivé ne prend la vitrine que
    /// s'il n'y a rien d'autre : la page d'accueil ne doit pas donner l'impression d'un
    /// compte à l'abandon.
    /// </remarks>
    public static Repository? Of(IEnumerable<Repository> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        return repositories
            .OrderByDescending(repository => repository.IsFeatured)
            .ThenBy(repository => repository.IsArchived)
            .ThenByDescending(repository => repository.HasDemo)
            .ThenByDescending(repository => repository.Stars)
            .ThenByDescending(repository => repository.PushedAt)
            .FirstOrDefault();
    }
}
