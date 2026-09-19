using System.Globalization;
using Drangit.Web.Repositories;

// Volontairement hors du dossier Shared : « Shared » est un mot réservé de Visual Basic,
// que l'analyseur CA1716 refuse dans un nom d'espace de noms. Les composants .razor du
// dossier voisin, eux, sont du code généré et échappent à la règle.
namespace Drangit.Web.Components;

/// <summary>
/// De quoi dessiner une couverture pour un dépôt qui n'a pas d'illustration.
/// </summary>
/// <remarks>
/// <para>
/// GitHub sait produire une image par dépôt (<c>opengraph.githubassets.com</c>), et le site
/// s'en sert — mais pour les <em>aperçus de partage</em>, ce à quoi elle est destinée. En
/// vignette, elle ne vaut rien : c'est une carte blanche qui réécrit le nom, la description
/// et les compteurs, déjà affichés juste en dessous. Quarante-cinq d'entre elles font un mur
/// de rectangles identiques, et chacune coûte une requête vers un tiers.
/// </para>
/// <para>
/// On dessine donc la vignette nous-mêmes, à partir de ce qui distingue vraiment un dépôt
/// d'un autre : son langage — d'où la couleur — et son nom — d'où les initiales et
/// l'inclinaison. Aucun octet ne traverse le réseau, et la grille se lit d'un coup d'œil.
/// </para>
/// </remarks>
public static class RepositoryVisual
{
    /// <summary>Teinte des dépôts dont GitHub n'a détecté aucun langage.</summary>
    private const string NeutralAccent = "#8b96a8";

    /// <summary>
    /// Couleurs officielles de GitHub pour les langages présents sur le compte.
    /// </summary>
    /// <remarks>
    /// Reprises de <c>github/linguist</c>. Un langage absent de cette table n'est pas un
    /// bug : il prend la teinte neutre, et l'ajouter ici est un geste d'une ligne.
    /// </remarks>
    private static readonly Dictionary<string, string> AccentsByLanguage =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Assembly"] = "#6E4C13",
            ["C"] = "#555555",
            ["C#"] = "#178600",
            ["C++"] = "#f34b7d",
            ["CSS"] = "#563d7c",
            ["Dart"] = "#00B4AB",
            ["Dockerfile"] = "#384d54",
            ["Go"] = "#00ADD8",
            ["HTML"] = "#e34c26",
            ["Java"] = "#b07219",
            ["JavaScript"] = "#f1e05a",
            ["Jupyter Notebook"] = "#DA5B0B",
            ["Kotlin"] = "#A97BFF",
            ["Lua"] = "#000080",
            ["Makefile"] = "#427819",
            ["PHP"] = "#4F5D95",
            ["PowerShell"] = "#012456",
            ["Python"] = "#3572A5",
            ["Ruby"] = "#701516",
            ["Rust"] = "#dea584",
            ["Shell"] = "#89e051",
            ["SQL"] = "#e38c00",
            ["Svelte"] = "#ff3e00",
            ["TSQL"] = "#e38c00",
            ["TypeScript"] = "#3178c6",
            ["Vue"] = "#41b883",
        };

    /// <summary>
    /// Luminance relative en deçà de laquelle une teinte ne se lit plus sur la vignette.
    /// </summary>
    /// <remarks>
    /// Seuil constaté à l'œil sur les langages du compte, pas une valeur normative : au-dessus,
    /// le vert de C# et le bleu de TypeScript se détachent encore ; en dessous, le bleu nuit de
    /// PowerShell et celui de Lua se confondent avec le fond.
    /// </remarks>
    private const double MinimumInkLuminance = 0.12;

    /// <summary>Couleur d'accent d'un langage, ou la teinte neutre s'il est inconnu.</summary>
    public static string AccentOf(string? language) =>
        language is not null && AccentsByLanguage.TryGetValue(language, out var accent)
            ? accent
            : NeutralAccent;

    /// <summary>
    /// Teinte du langage telle qu'elle s'écrit sur la vignette : la couleur officielle si elle
    /// s'y lit, sa version éclaircie sinon.
    /// </summary>
    /// <remarks>
    /// Les couleurs de <c>linguist</c> sont faites pour une pastille sur le fond clair de
    /// GitHub ; la vignette, elle, est sombre, et les plus foncées y disparaissent — le prompt
    /// et la pastille avec. Un mélange à parts égales avec le blanc suffit dans tous les cas,
    /// le noir pur compris, et garde la dominante : c'est tout ce qu'on demande à une pastille.
    /// </remarks>
    public static string InkOf(string? language)
    {
        var accent = AccentOf(language);

        return RelativeLuminanceOf(accent) >= MinimumInkLuminance ? accent : Lighten(accent);
    }

    /// <summary>Luminance relative d'une couleur écrite <c>#rrggbb</c>, au sens WCAG.</summary>
    private static double RelativeLuminanceOf(string color)
    {
        var (red, green, blue) = ChannelsOf(color);

        return (0.2126 * Linearized(red)) + (0.7152 * Linearized(green)) + (0.0722 * Linearized(blue));
    }

    /// <summary>Composante sRGB ramenée à l'échelle linéaire de la formule de luminance.</summary>
    private static double Linearized(int channel)
    {
        var ratio = channel / 255d;

        return ratio <= 0.04045 ? ratio / 12.92 : Math.Pow((ratio + 0.055) / 1.055, 2.4);
    }

    /// <summary>Mélange une couleur à parts égales avec le blanc.</summary>
    private static string Lighten(string color)
    {
        var (red, green, blue) = ChannelsOf(color);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"#{(red + 255) / 2:x2}{(green + 255) / 2:x2}{(blue + 255) / 2:x2}");
    }

    /// <summary>Composantes d'une couleur écrite <c>#rrggbb</c>.</summary>
    private static (int Red, int Green, int Blue) ChannelsOf(string color) =>
    (
        int.Parse(color.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
        int.Parse(color.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
        int.Parse(color.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
    );

    /// <summary>
    /// Inclinaison du dégradé, entre 0 et 359 degrés, dérivée du nom.
    /// </summary>
    /// <remarks>
    /// Sans elle, deux dépôts du même langage auraient exactement la même vignette. Le
    /// calcul est un FNV-1a et non <c>string.GetHashCode</c> : ce dernier est rendu
    /// aléatoire à chaque démarrage du processus, et la vignette d'un dépôt changerait
    /// d'aspect à chaque redéploiement — y compris dans les caches des navigateurs.
    /// </remarks>
    public static int AngleOf(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;

        foreach (var character in name)
        {
            hash = (hash ^ character) * prime;
        }

        return (int)(hash % 360);
    }

    /// <summary>
    /// Variables CSS de la couverture d'un dépôt, à poser sur son élément.
    /// </summary>
    public static string StyleOf(Repository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"--cover-accent: {AccentOf(repository.Language)}; --cover-ink: {InkOf(repository.Language)}; --cover-angle: {AngleOf(repository.Name)}deg");
    }
}
