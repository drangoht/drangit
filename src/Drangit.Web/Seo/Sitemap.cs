using System.Globalization;
using System.Xml.Linq;
using Drangit.Web.Localization;
using Drangit.Web.Repositories;

namespace Drangit.Web.Seo;

/// <summary>Plan du site soumis aux moteurs de recherche.</summary>
public static class Sitemap
{
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    /// <summary>Construit le document listant les pages indexables du site.</summary>
    public static string Build(IReadOnlyList<Repository> repositories, Uri baseUri)
    {
        ArgumentNullException.ThrowIfNull(repositories);
        ArgumentNullException.ThrowIfNull(baseUri);

        // Chaque page existe dans chaque langue et chacune a sa propre adresse (ADR 0006) :
        // le plan les annonce toutes, faute de quoi une version ne serait explorée qu'au
        // hasard des liens.
        var urlset = new XElement(
            Ns + "urlset",
            SupportedCultures.All.SelectMany(culture => new[]
            {
                Url(baseUri, $"{culture}/"),
                Url(baseUri, $"{culture}/about"),
            }.Concat(repositories.Select(repository =>
                Url(baseUri, $"{culture}/repos/{repository.Slug.Value}", repository.PushedAt)))));

        return new XDocument(new XDeclaration("1.0", "utf-8", null), urlset).ToString();
    }

    private static XElement Url(Uri baseUri, string path, DateTimeOffset? lastModified = null) =>
        new(
            Ns + "url",
            new XElement(Ns + "loc", new Uri(baseUri, path)),
            lastModified is { } date
                ? new XElement(Ns + "lastmod", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                : null);
}
