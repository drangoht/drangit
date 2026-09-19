using Drangit.Web.Repositories;

namespace Drangit.Web.Seo;

/// <summary>Ce que le site publie à l'intention des robots d'indexation.</summary>
public static class SeoEndpoints
{
    /// <summary>Enregistre <c>/robots.txt</c> et <c>/sitemap.xml</c>.</summary>
    public static IEndpointRouteBuilder MapSeoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/robots.txt", (HttpContext context) =>
        {
            var plan = new Uri(BaseUriOf(context), "sitemap.xml");

            return Results.Text(
                $"""
                 User-agent: *
                 Allow: /

                 Sitemap: {plan}

                 """,
                "text/plain");
        }).WithName("Robots");

        endpoints.MapGet("/sitemap.xml", async (HttpContext context, IRepositoryCatalog catalog) =>
        {
            var repositories = await catalog.GetRepositoriesAsync(context.RequestAborted).ConfigureAwait(false);

            return Results.Text(Sitemap.Build(repositories, BaseUriOf(context)), "application/xml");
        }).WithName("Sitemap");

        return endpoints;
    }

    /// <summary>
    /// Adresse publique du site, telle que le visiteur l'a demandée.
    /// </summary>
    /// <remarks>
    /// Reconstruite depuis la requête plutôt que configurée : le site tourne derrière un
    /// reverse proxy dont <c>UseForwardedHeaders</c> a déjà rétabli le schéma et l'hôte.
    /// Une option de configuration ferait un second endroit à tenir à jour au moindre
    /// changement de domaine.
    /// </remarks>
    private static Uri BaseUriOf(HttpContext context) =>
        new($"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}/");
}
