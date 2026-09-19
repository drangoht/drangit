namespace Drangit.Web.Localization;

/// <summary>Langues dans lesquelles le site est publié.</summary>
public static class SupportedCultures
{
    /// <summary>Langue servie quand aucune préférence n'est exprimée.</summary>
    /// <remarks>Le code, les noms de dépôts et les descriptions GitHub sont en anglais.</remarks>
    public const string Default = "en";

    /// <summary>Codes de culture acceptés, dans l'ordre d'affichage du sélecteur.</summary>
    public static IReadOnlyList<string> All { get; } = ["en", "fr"];

    /// <summary>Nom de la langue tel qu'on l'affiche dans le sélecteur.</summary>
    public static string DisplayNameOf(string culture) => culture switch
    {
        "fr" => "Français",
        "en" => "English",
        _ => culture,
    };

    /// <summary>Indique que le site sait servir cette culture.</summary>
    public static bool IsSupported(string? culture) =>
        culture is not null && All.Contains(culture, StringComparer.OrdinalIgnoreCase);
}
