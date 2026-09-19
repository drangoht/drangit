using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Drangit.Web.Repositories.Snapshots;

/// <summary>Configuration de l'instantané de repli.</summary>
public sealed class SnapshotOptions
{
    /// <summary>Section de configuration correspondante.</summary>
    public const string SectionName = "Snapshot";

    /// <summary>
    /// Répertoire dans lequel le dernier catalogue réussi est conservé.
    /// </summary>
    /// <remarks>
    /// En conteneur, ce chemin doit pointer vers un volume : sinon l'instantané disparaît à
    /// chaque redéploiement, précisément quand il serait le plus utile — le cache mémoire
    /// est vide au démarrage, et c'est là qu'un quota d'API épuisé se paierait par un site
    /// vide.
    /// </remarks>
    [Required(AllowEmptyStrings = false)]
    public string Directory { get; init; } = "/var/lib/drangit";
}

/// <summary>
/// Conserve sur disque le dernier catalogue obtenu de GitHub, pour pouvoir servir le site
/// quand l'API est indisponible ou que le quota d'appels est épuisé.
/// </summary>
/// <remarks>
/// Aucune opération de ce magasin ne lève : un instantané est un confort. Le perdre ne doit
/// jamais dégrader une requête qui, elle, s'est bien passée.
/// </remarks>
public sealed partial class RepositoryCatalogSnapshotStore
{
    private const string FileName = "repositories-snapshot.json";

    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    private readonly string _filePath;
    private readonly string _directory;
    private readonly ILogger<RepositoryCatalogSnapshotStore> _logger;

    /// <summary>Construit le magasin sur le répertoire configuré.</summary>
    public RepositoryCatalogSnapshotStore(
        IOptions<SnapshotOptions> options,
        ILogger<RepositoryCatalogSnapshotStore> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _directory = options.Value.Directory;
        _filePath = Path.Combine(_directory, FileName);
        _logger = logger;
    }

    /// <summary>Enregistre le catalogue comme instantané de repli.</summary>
    public async Task SaveAsync(IReadOnlyList<Repository> repositories, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repositories);

        try
        {
            Directory.CreateDirectory(_directory);

            // Écriture puis remplacement : une coupure en cours d'écriture laisserait
            // sinon un instantané tronqué, donc inutilisable au moment critique.
            var temporaryPath = _filePath + ".tmp";

            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer
                    .SerializeAsync(stream, repositories, SerializerOptions, cancellationToken)
                    .ConfigureAwait(false);
            }

            File.Move(temporaryPath, _filePath, overwrite: true);

            LogSnapshotSaved(_logger, repositories.Count, _filePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                                              or ArgumentException or NotSupportedException)
        {
            LogSnapshotSaveFailed(_logger, exception, _filePath);
        }
    }

    /// <summary>
    /// Relit l'instantané, ou retourne <c>null</c> s'il est absent ou illisible.
    /// </summary>
    public async Task<IReadOnlyList<Repository>?> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);

            var repositories = await JsonSerializer
                .DeserializeAsync<List<Repository>>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            return repositories;
        }
        catch (Exception exception) when (exception is JsonException or IOException
                                              or UnauthorizedAccessException or UriFormatException)
        {
            LogSnapshotUnreadable(_logger, exception, _filePath);
            return null;
        }
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new RepositorySlugJsonConverter());
        return options;
    }

    [LoggerMessage(EventId = 2000, Level = LogLevel.Debug,
        Message = "Instantané de {Count} dépôts écrit dans {Path}.")]
    private static partial void LogSnapshotSaved(ILogger logger, int count, string path);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning,
        Message = "Impossible d'écrire l'instantané dans {Path} : le site continue sans repli.")]
    private static partial void LogSnapshotSaveFailed(ILogger logger, Exception exception, string path);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning,
        Message = "Instantané {Path} illisible : il sera reconstruit au prochain appel réussi.")]
    private static partial void LogSnapshotUnreadable(ILogger logger, Exception exception, string path);
}
