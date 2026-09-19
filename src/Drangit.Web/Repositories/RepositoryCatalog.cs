using System.Text.Json;
using Drangit.Web.Repositories.Editorial;
using Drangit.Web.Repositories.GitHub;
using Drangit.Web.Repositories.Snapshots;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Drangit.Web.Repositories;

/// <summary>
/// Fournit les dépôts du site : données GitHub mises en cache, complétées par le contenu
/// éditorial local, avec repli sur le dernier instantané connu quand l'API est indisponible.
/// </summary>
/// <remarks>
/// Le contenu éditorial est appliqué <em>en sortie</em>, jamais avant la mise en cache ni
/// avant l'écriture de l'instantané : corriger une description, mettre un dépôt en vitrine
/// ou en retirer un prend ainsi effet immédiatement, même si GitHub est injoignable.
/// </remarks>
public sealed partial class RepositoryCatalog : IRepositoryCatalog, IDisposable
{
    private const string CacheKey = "repositories:public";

    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private bool _orphansReported;
    private readonly IGitHubClient _client;
    private readonly EditorialCatalog _editorial;
    private readonly RepositoryCatalogSnapshotStore _snapshotStore;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration;
    private readonly ILogger<RepositoryCatalog> _logger;

    /// <summary>Construit le catalogue.</summary>
    public RepositoryCatalog(
        IGitHubClient client,
        EditorialCatalog editorial,
        RepositoryCatalogSnapshotStore snapshotStore,
        IMemoryCache cache,
        IOptions<GitHubOptions> options,
        ILogger<RepositoryCatalog> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(editorial);
        ArgumentNullException.ThrowIfNull(snapshotStore);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _client = client;
        _editorial = editorial;
        _snapshotStore = snapshotStore;
        _cache = cache;
        _cacheDuration = options.Value.CacheDuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Repository>> GetRepositoriesAsync(CancellationToken cancellationToken)
    {
        var repositories = await GetOrRefreshAsync(cancellationToken).ConfigureAwait(false);

        return
        [
            .. repositories
                .Where(repository => !_editorial.IsHidden(repository))
                .Select(_editorial.Apply),
        ];
    }

    /// <inheritdoc />
    public void Dispose() => _refreshGate.Dispose();

    private async Task<IReadOnlyList<Repository>> GetOrRefreshAsync(CancellationToken cancellationToken)
    {
        if (TryGetCached(out var cached))
        {
            return cached;
        }

        // Un démarrage à froid sous trafic déclencherait autant d'appels à GitHub que de
        // requêtes simultanées : une seule les rafraîchit, les autres attendent le résultat.
        // Le quota d'API rend cette précaution plus qu'une optimisation.
        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (TryGetCached(out cached))
            {
                return cached;
            }

            return await FetchAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private bool TryGetCached(out IReadOnlyList<Repository> repositories)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<Repository>? cached) && cached is not null)
        {
            repositories = cached;
            return true;
        }

        repositories = [];
        return false;
    }

    private async Task<IReadOnlyList<Repository>> FetchAsync(CancellationToken cancellationToken)
    {
        try
        {
            var repositories = await _client.GetPublicRepositoriesAsync(cancellationToken).ConfigureAwait(false);

            _cache.Set(CacheKey, repositories, _cacheDuration);

            if (repositories.Count > 0)
            {
                ReportOrphanEditorialEntries(repositories);

                await _snapshotStore.SaveAsync(repositories, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Défense en profondeur : un catalogue vide ne remplace pas un instantané
                // peuplé. Le repli est le dernier filet du site quand GitHub est en panne.
                LogEmptyCatalogNotSnapshotted(_logger);
            }

            return repositories;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Le visiteur est parti : ce n'est pas une panne de GitHub.
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException
                                              or OperationCanceledException
                                              or JsonException
                                              or GitHubRateLimitException)
        {
            LogFetchFailed(_logger, exception);

            // L'échec n'est délibérément pas mis en cache : le figer pour toute la durée du
            // TTL transformerait une indisponibilité passagère en panne de trente minutes.
            var snapshot = await _snapshotStore.LoadAsync(cancellationToken).ConfigureAwait(false);

            if (snapshot is null)
            {
                LogNoSnapshotAvailable(_logger);
                return [];
            }

            LogServedFromSnapshot(_logger, snapshot.Count);
            return snapshot;
        }
    }

    /// <summary>
    /// Signale les entrées éditoriales qui ne correspondent à aucun dépôt public.
    /// </summary>
    /// <remarks>
    /// Une seule fois, au premier catalogue obtenu : c'est le premier moment où la liste des
    /// dépôts existe, et le répéter à chaque rafraîchissement noierait le journal. Sans ce
    /// signalement, une faute de frappe dans un slug produit un enrichissement silencieusement
    /// ignoré — ou pire, un dépôt qu'on croyait masqué et qui reste affiché.
    /// </remarks>
    private void ReportOrphanEditorialEntries(IReadOnlyList<Repository> repositories)
    {
        if (_orphansReported)
        {
            return;
        }

        _orphansReported = true;

        if (_editorial.FindOrphanEntries(repositories) is { Count: > 0 } orphans)
        {
            LogOrphanEditorialEntries(_logger, string.Join(", ", orphans));
        }
    }

    [LoggerMessage(EventId = 3004, Level = LogLevel.Warning,
        Message = "Entrées du fichier éditorial sans dépôt correspondant : {Slugs}. "
                  + "Un « hidden » posé sur l'une d'elles ne masque rien.")]
    private static partial void LogOrphanEditorialEntries(ILogger logger, string slugs);

    [LoggerMessage(EventId = 3000, Level = LogLevel.Error,
        Message = "Échec de la récupération des dépôts depuis GitHub.")]
    private static partial void LogFetchFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Warning,
        Message = "GitHub est indisponible : {Count} dépôts servis depuis le dernier instantané.")]
    private static partial void LogServedFromSnapshot(ILogger logger, int count);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Warning,
        Message = "GitHub n'a renvoyé aucun dépôt : l'instantané précédent est conservé.")]
    private static partial void LogEmptyCatalogNotSnapshotted(ILogger logger);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Critical,
        Message = "GitHub est indisponible et aucun instantané n'existe : le catalogue est vide.")]
    private static partial void LogNoSnapshotAvailable(ILogger logger);
}
