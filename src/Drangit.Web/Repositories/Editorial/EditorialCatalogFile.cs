using System.ComponentModel.DataAnnotations;

namespace Drangit.Web.Repositories.Editorial;

/// <summary>Configuration du fichier de contenu éditorial.</summary>
public sealed class EditorialOptions
{
    /// <summary>Section de configuration correspondante.</summary>
    public const string SectionName = "Editorial";

    /// <summary>Chemin du fichier JSON, relatif au répertoire de l'application.</summary>
    [Required(AllowEmptyStrings = false)]
    public string FilePath { get; init; } = "data/repositories.json";
}

/// <summary>
/// Charge le contenu éditorial depuis le disque.
/// </summary>
/// <remarks>
/// Objet humble : il lit un fichier et délègue toute l'interprétation à
/// <see cref="EditorialCatalog"/>, qui reste testable sans I/O.
/// </remarks>
public static partial class EditorialCatalogFile
{
    /// <summary>
    /// Lit le fichier s'il existe, ou retourne un catalogue vide.
    /// </summary>
    /// <exception cref="System.Text.Json.JsonException">
    /// Le fichier existe mais son contenu est invalide. On échoue au démarrage plutôt que de
    /// servir un site silencieusement privé de ses descriptions — ou, plus grave, montrant
    /// un dépôt que ce fichier était seul à masquer.
    /// </exception>
    public static EditorialCatalog Load(string filePath, ILogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(logger);

        var fullPath = Path.IsPathRooted(filePath)
            ? filePath
            : Path.Combine(AppContext.BaseDirectory, filePath);

        if (!File.Exists(fullPath))
        {
            LogEditorialFileMissing(logger, fullPath);
            return EditorialCatalog.Empty;
        }

        var catalog = EditorialCatalog.FromJson(File.ReadAllText(fullPath));
        LogEditorialFileLoaded(logger, fullPath);

        return catalog;
    }

    [LoggerMessage(EventId = 4000, Level = LogLevel.Warning,
        Message = "Aucun contenu éditorial trouvé en {Path} : les dépôts s'afficheront avec les seules données GitHub.")]
    private static partial void LogEditorialFileMissing(ILogger logger, string path);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Information,
        Message = "Contenu éditorial chargé depuis {Path}.")]
    private static partial void LogEditorialFileLoaded(ILogger logger, string path);
}
