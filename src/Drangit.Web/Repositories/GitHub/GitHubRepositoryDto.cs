namespace Drangit.Web.Repositories.GitHub;

/// <summary>
/// Un dépôt tel que l'API GitHub le décrit.
/// </summary>
/// <remarks>
/// La réponse de GitHub compte une centaine de champs, dont les URL de chacun de ses points
/// d'accès et les permissions du porteur du jeton. On ne désérialise que ce que le site
/// affiche, plus <see cref="Private"/> et <see cref="Visibility"/>, qui servent uniquement à
/// refuser ce qui ne serait pas public.
/// </remarks>
internal sealed record GitHubRepositoryDto
{
    public long Id { get; init; }

    public string? Name { get; init; }

    public string? FullName { get; init; }

    public string? HtmlUrl { get; init; }

    public string? Description { get; init; }

    public string? Homepage { get; init; }

    public string? Language { get; init; }

    public IReadOnlyList<string> Topics { get; init; } = [];

    public int StargazersCount { get; init; }

    public int ForksCount { get; init; }

    public DateTimeOffset? PushedAt { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public GitHubLicenseDto? License { get; init; }

    public bool Archived { get; init; }

    public bool Fork { get; init; }

    /// <summary>Drapeau de confidentialité, lu pour pouvoir écarter le dépôt.</summary>
    public bool Private { get; init; }

    /// <summary>Visibilité déclarée (<c>public</c>, <c>private</c>, <c>internal</c>).</summary>
    public string? Visibility { get; init; }
}

/// <summary>Licence d'un dépôt, telle que GitHub la reconnaît.</summary>
internal sealed record GitHubLicenseDto
{
    /// <summary>Identifiant SPDX, ou <c>NOASSERTION</c> quand GitHub n'a pas su trancher.</summary>
    public string? SpdxId { get; init; }

    public string? Name { get; init; }
}
