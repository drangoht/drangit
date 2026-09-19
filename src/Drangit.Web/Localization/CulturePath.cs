using Microsoft.AspNetCore.Http;

namespace Drangit.Web.Localization;

/// <summary>La langue telle qu'elle s'écrit dans le chemin d'une URL (ADR 0006).</summary>
public static class CulturePath
{
    /// <summary>
    /// Détache le préfixe de langue d'un chemin, quand il en porte un.
    /// </summary>
    /// <param name="path">Chemin demandé, préfixe compris.</param>
    /// <param name="culture">Langue lue, normalisée comme le site l'écrit.</param>
    /// <param name="rest">Ce qui reste du chemin, toujours non vide.</param>
    /// <returns><see langword="true"/> si le chemin commençait par une langue publiée.</returns>
    public static bool TryDetach(PathString path, out string culture, out PathString rest)
    {
        foreach (var candidate in SupportedCultures.All)
        {
            // StartsWithSegments compare des segments entiers : « /french » ne commence pas
            // par « /fr », alors qu'une comparaison de chaînes le prétendrait.
            if (path.StartsWithSegments(
                    new PathString($"/{candidate}"),
                    StringComparison.OrdinalIgnoreCase,
                    out var remaining))
            {
                culture = candidate;

                // « /fr » ne laisse rien derrière lui : c'est la racine qui est demandée,
                // et un chemin vide ne s'apparie à aucune route.
                rest = remaining.HasValue ? remaining : new PathString("/");
                return true;
            }
        }

        culture = string.Empty;
        rest = path;
        return false;
    }

    /// <summary>
    /// Racine du site, préfixe de langue ôté.
    /// </summary>
    /// <remarks>
    /// La base d'une page porte déjà sa langue : les adresses des autres langues se
    /// construisent depuis la racine, sans quoi elles s'empileraient (<c>/fr/en/…</c>).
    /// </remarks>
    public static Uri SiteRootOf(Uri baseUri)
    {
        ArgumentNullException.ThrowIfNull(baseUri);

        return TryDetach(new PathString(baseUri.AbsolutePath), out _, out var rest)
            ? new Uri(baseUri, rest.Value ?? "/")
            : baseUri;
    }
}
