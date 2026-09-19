using System.Globalization;

namespace Drangit.Web.Repositories;

/// <summary>
/// Identifiant lisible d'un dépôt dans les URLs du site, dérivé de son nom GitHub.
/// </summary>
public readonly record struct RepositorySlug
{
    private RepositorySlug(string value) => Value = value;

    /// <summary>Valeur textuelle du slug, toujours en minuscules.</summary>
    public string Value { get; }

    /// <summary>
    /// Dérive le slug du nom du dépôt.
    /// </summary>
    /// <remarks>
    /// GitHub garantit l'unicité du nom par compte et n'y accepte que des caractères déjà
    /// valides dans un chemin. La casse, en revanche, est libre : on la normalise, sans quoi
    /// <c>/repos/AdventOfCode</c> et <c>/repos/adventofcode</c> seraient deux adresses pour
    /// la même page. Un nom vide — donnée tronquée côté API — retombe sur l'identifiant
    /// numérique : une route moins jolie vaut mieux qu'un dépôt inatteignable.
    /// </remarks>
    public static RepositorySlug FromName(string? name, long repositoryId) =>
        string.IsNullOrWhiteSpace(name)
            ? new RepositorySlug(repositoryId.ToString(CultureInfo.InvariantCulture))
            : new RepositorySlug(name.Trim().ToLowerInvariant());

    /// <inheritdoc />
    public override string ToString() => Value;
}
