namespace Drangit.Web.Repositories;

/// <summary>
/// Ce qu'un « ls » écrit d'emblée : le début de la liste, pas le catalogue entier.
/// </summary>
/// <remarks>
/// Quarante-sept lignes déversées d'un coup ne se lisent pas, et un terminal ne le fait pas
/// non plus. La coupe est un choix d'affichage, pas un critère : elle ne trie rien et ne
/// retire rien du catalogue — la suite est à une commande, ou à un lien.
/// </remarks>
public static class RepositoryListing
{
    /// <summary>Nombre de dépôts écrits avant qu'on en demande davantage.</summary>
    public const int DefaultPageSize = 12;

    /// <summary>
    /// Début de la liste, dans l'ordre reçu.
    /// </summary>
    /// <param name="repositories">Dépôts à écrire, déjà filtrés et ordonnés.</param>
    /// <param name="size">
    /// Nombre de lignes voulu. Une valeur nulle ou négative rend la liste entière : une page
    /// vide se lirait comme un catalogue vide, ce qui serait faux.
    /// </param>
    public static IReadOnlyList<Repository> FirstPageOf(IReadOnlyList<Repository> repositories, int size)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        return size > 0 && repositories.Count > size ? [.. repositories.Take(size)] : repositories;
    }
}
