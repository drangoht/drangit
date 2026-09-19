using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Refit;

namespace Drangit.Web.Repositories.GitHub;

/// <summary>
/// Traduit l'API GitHub en dépôts du site.
/// </summary>
/// <remarks>
/// C'est la seule classe du projet qui connaît le vocabulaire de GitHub. Elle constitue la
/// couche anti-corruption : au-delà, on ne manipule plus que des <see cref="Repository"/>.
/// Le transport est délégué à <see cref="IGitHubApi"/> — ici, plus aucun appel réseau, que
/// des décisions.
/// </remarks>
internal sealed partial class GitHubClient : IGitHubClient
{
    /// <summary>Taille de page demandée : c'est le maximum accepté par GitHub.</summary>
    private const int PageSize = 100;

    /// <summary>
    /// GitHub produit pour chaque dépôt une image de partage. Le premier segment n'est
    /// qu'un cache-buster : sa valeur n'a pas de sens, seule sa présence compte.
    /// </summary>
    private const string PreviewImageBaseUrl = "https://opengraph.githubassets.com/1/";

    /// <summary>
    /// GitHub renvoie cet identifiant quand il reconnaît un fichier de licence sans pouvoir
    /// l'attribuer. L'afficher tel quel ne dirait rien au visiteur.
    /// </summary>
    private const string UnknownSpdxId = "NOASSERTION";

    private readonly IGitHubApi _api;
    private readonly GitHubOptions _options;
    private readonly ILogger<GitHubClient> _logger;

    /// <summary>Construit le traducteur sur le contrat HTTP de GitHub.</summary>
    public GitHubClient(IGitHubApi api, IOptions<GitHubOptions> options, ILogger<GitHubClient> logger)
    {
        ArgumentNullException.ThrowIfNull(api);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _api = api;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Repository>> GetPublicRepositoriesAsync(CancellationToken cancellationToken)
    {
        var payload = await FetchAllPagesAsync(cancellationToken).ConfigureAwait(false);

        var repositories = payload
            .Where(IsPubliclyListable)
            .Select(ToRepository)
            .OrderByDescending(repository => repository.PushedAt)
            .ToArray();

        LogRepositoriesFetched(_logger, _options.Login, payload.Count, repositories.Length);

        return repositories;
    }

    /// <summary>
    /// Parcourt la pagination jusqu'à la dernière page.
    /// </summary>
    /// <remarks>
    /// Une page incomplète signe la fin : c'est la seule marque que donne ce point d'accès
    /// sans lire l'en-tête <c>Link</c>, et elle suffit. <c>MaxPages</c> borne malgré tout la
    /// boucle — une réponse inattendue ne doit pas consommer le quota d'API en entier.
    /// </remarks>
    private async Task<IReadOnlyList<GitHubRepositoryDto>> FetchAllPagesAsync(CancellationToken cancellationToken)
    {
        var all = new List<GitHubRepositoryDto>();

        for (var page = 1; page <= _options.MaxPages; page++)
        {
            var batch = await FetchPageAsync(page, cancellationToken).ConfigureAwait(false);

            all.AddRange(batch);

            if (batch.Count < PageSize)
            {
                return all;
            }
        }

        LogPaginationTruncated(_logger, _options.Login, _options.MaxPages);

        return all;
    }

    private async Task<IReadOnlyList<GitHubRepositoryDto>> FetchPageAsync(
        int page,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _api
                .GetPublicRepositoriesAsync(_options.Login, PageSize, page, AuthorizationHeader(), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ApiException exception)
        {
            if (RateLimitOf(exception) is { } rateLimit)
            {
                LogRateLimited(_logger, rateLimit.ResetsAt?.ToString("u", CultureInfo.InvariantCulture) ?? "inconnue");
                throw rateLimit;
            }

            if (exception.StatusCode == HttpStatusCode.NotFound)
            {
                // Le compte n'existe pas — une faute de frappe dans la configuration, pas une
                // panne. Sans ce message, le site se replierait indéfiniment sur son
                // instantané en laissant croire à une indisponibilité de GitHub.
                LogAccountNotFound(_logger, _options.Login);
            }

            // Refit signale les codes HTTP d'erreur par son propre type, qui ne dérive pas de
            // HttpRequestException. Le laisser remonter ferait sortir le SDK de ce dossier et
            // priverait le catalogue de son repli sur instantané, qui guette ce type-là.
            throw new HttpRequestException(exception.Message, exception, exception.StatusCode);
        }
    }

    private string? AuthorizationHeader() =>
        _options.Token is { } token ? $"Bearer {token}" : null;

    /// <summary>
    /// Reconnaît un refus dû au quota, qui ne se traite pas comme une panne.
    /// </summary>
    /// <remarks>
    /// Un 403 de GitHub ne veut pas dire « quota épuisé » à lui seul — il couvre aussi les
    /// requêtes sans agent utilisateur et les comptes bloqués. C'est
    /// <c>x-ratelimit-remaining: 0</c> qui tranche. Un 429, lui, est toujours un abus de
    /// débit, avec ou sans cet en-tête.
    /// </remarks>
    private static GitHubRateLimitException? RateLimitOf(ApiException exception)
    {
        if (exception.StatusCode is not (HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests))
        {
            return null;
        }

        var exhausted = HeaderValue(exception.Headers, "x-ratelimit-remaining") is "0";

        if (!exhausted && exception.StatusCode != HttpStatusCode.TooManyRequests)
        {
            return null;
        }

        var resetsAt = long.TryParse(
            HeaderValue(exception.Headers, "x-ratelimit-reset"),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var epochSeconds)
            ? DateTimeOffset.FromUnixTimeSeconds(epochSeconds)
            : (DateTimeOffset?)null;

        return new GitHubRateLimitException(resetsAt);
    }

    private static string? HeaderValue(HttpHeaders? headers, string name) =>
        headers is not null && headers.TryGetValues(name, out var values)
            ? values.FirstOrDefault()
            : null;

    /// <summary>
    /// Un dépôt ne rejoint le site que s'il est public et exploitable : sans nom ni adresse,
    /// il n'y a ni carte à afficher ni route vers laquelle pointer.
    /// </summary>
    /// <remarks>
    /// Le contrôle de confidentialité est une défense en profondeur : <c>/users/{login}/repos</c>
    /// ne renvoie que du public. C'est précisément parce que cette garantie est extérieure
    /// au projet qu'on ne s'y fie pas seule.
    /// </remarks>
    private bool IsPubliclyListable(GitHubRepositoryDto dto) =>
        !dto.Private
        && (dto.Visibility is null || string.Equals(dto.Visibility, "public", StringComparison.OrdinalIgnoreCase))
        && (_options.IncludeForks || !dto.Fork)
        && !string.IsNullOrWhiteSpace(dto.Name)
        && Uri.TryCreate(dto.HtmlUrl, UriKind.Absolute, out _);

    private Repository ToRepository(GitHubRepositoryDto dto)
    {
        var fullName = string.IsNullOrWhiteSpace(dto.FullName)
            ? $"{_options.Login}/{dto.Name}"
            : dto.FullName;

        return new Repository
        {
            Id = dto.Id,
            Slug = RepositorySlug.FromName(dto.Name, dto.Id),
            Name = dto.Name!,
            FullName = fullName,
            HtmlUrl = new Uri(dto.HtmlUrl!, UriKind.Absolute),
            Description = LocalizedText.FromEnglish(dto.Description),
            Language = string.IsNullOrWhiteSpace(dto.Language) ? null : dto.Language,
            Topics = [.. dto.Topics.Where(topic => !string.IsNullOrWhiteSpace(topic)).Select(topic => topic.Trim())],
            Stars = dto.StargazersCount,
            Forks = dto.ForksCount,
            PushedAt = dto.PushedAt,
            CreatedAt = dto.CreatedAt,
            License = ToLicense(dto.License),
            HomepageUrl = ToWebUri(dto.Homepage),
            PreviewImageUrl = new Uri(PreviewImageBaseUrl + fullName, UriKind.Absolute),
            IsArchived = dto.Archived,
            IsFork = dto.Fork,
        };
    }

    private static string? ToLicense(GitHubLicenseDto? license) =>
        license?.SpdxId is { } spdxId
        && !string.IsNullOrWhiteSpace(spdxId)
        && !string.Equals(spdxId, UnknownSpdxId, StringComparison.OrdinalIgnoreCase)
            ? spdxId
            : null;

    /// <summary>
    /// Le champ « homepage » est saisi à la main sur GitHub : il contient aussi bien une URL
    /// qu'un fragment de texte. Seule une adresse web absolue devient un lien.
    /// </summary>
    private static Uri? ToWebUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri
            : null;
}
