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

    /// <summary>Couleur d'accent d'un langage, ou la teinte neutre s'il est inconnu.</summary>
    public static string AccentOf(string? language) =>
        language is not null && AccentsByLanguage.TryGetValue(language, out var accent)
            ? accent
            : NeutralAccent;

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
            $"--cover-accent: {AccentOf(repository.Language)}; --cover-angle: {AngleOf(repository.Name)}deg");
    }
}
