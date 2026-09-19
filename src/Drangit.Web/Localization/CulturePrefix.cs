using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace Drangit.Web.Localization;

/// <summary>
/// Le préfixe de langue du chemin, détaché à l'entrée du pipeline (ADR 0006).
/// </summary>
public static class CulturePrefix
{
    /// <summary>
    /// Adresses qui portent une langue, et qui sont donc redirigées quand elles arrivent
    /// sans préfixe. Ce sont exactement les pages que le plan du site publie.
    /// </summary>
    /// <remarks>
    /// Tout le reste — sonde de vivacité, ressources statiques, <c>robots.txt</c>,
    /// <c>sitemap.xml</c> — répond sans préfixe et ne doit pas être redirigé.
    /// </remarks>
    private static readonly string[] TranslatedPaths = ["/about", "/repos"];

    /// <summary>
    /// Détache le préfixe de langue du chemin et le porte en <c>PathBase</c>, de sorte que
    /// les routes des pages n'aient pas à le connaître.
    /// </summary>
    public static IApplicationBuilder UseCulturePathPrefix(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            if (CulturePath.TryDetach(context.Request.Path, out var culture, out var rest))
            {
                // La langue rejoint la base de l'application : `NavigationManager` la reprend
                // dans son `BaseUri`, donc dans le <base href> de la page et dans tous les
                // liens relatifs qui s'y résolvent.
                context.Request.PathBase = context.Request.PathBase.Add($"/{culture}");
                context.Request.Path = rest;
            }

            await next(context).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Renvoie vers son adresse préfixée toute page traduite demandée sans préfixe.
    /// </summary>
    /// <remarks>
    /// À placer <b>après</b> <c>UseRequestLocalization</c> : la racine oriente le visiteur
    /// vers la langue négociée, qui n'est connue qu'une fois la localisation résolue.
    /// </remarks>
    public static IApplicationBuilder UseCulturePathRedirects(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            var request = context.Request;

            if (CarriesCulture(request.PathBase) || !IsTranslated(request.Path))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            if (request.Path == "/")
            {
                // La racine est la seule adresse dont la destination dépend du visiteur :
                // une redirection permanente la figerait dans son navigateur, et le
                // francophone qui l'a visitée une fois n'atteindrait plus jamais l'anglais.
                var negotiated = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var language = SupportedCultures.IsSupported(negotiated)
                    ? negotiated
                    : SupportedCultures.Default;

                context.Response.Redirect($"{request.PathBase}/{language}/{request.QueryString}");
                return;
            }

            // Les autres adresses désignent une page précise : leur destination ne dépend
            // pas du visiteur, la redirection est donc permanente. Elle existe pour les
            // liens écrits à la main et pour les adresses devinées, pas pour le site
            // lui-même, dont tous les liens portent déjà un préfixe.
            context.Response.Redirect(
                $"{request.PathBase}/{SupportedCultures.Default}{request.Path}{request.QueryString}",
                permanent: true);
        });
    }

    /// <summary>Choisit la langue d'après le préfixe déjà détaché du chemin.</summary>
    /// <remarks>
    /// Enregistré en tête des fournisseurs : le chemin prime sur l'en-tête du navigateur,
    /// sans quoi une adresse française servirait de l'anglais à un anglophone.
    /// </remarks>
    public static RequestCultureProvider FromPath { get; } = new CustomRequestCultureProvider(
        context =>
        {
            ArgumentNullException.ThrowIfNull(context);

            var culture = CarriesCulture(context.Request.PathBase, out var language)
                ? new ProviderCultureResult(language)
                : null;

            return Task.FromResult<ProviderCultureResult?>(culture);
        });

    private static bool IsTranslated(PathString path) =>
        path == "/"
        || TranslatedPaths.Any(translated =>
            path.StartsWithSegments(translated, StringComparison.OrdinalIgnoreCase));

    private static bool CarriesCulture(PathString pathBase) =>
        CarriesCulture(pathBase, out _);

    private static bool CarriesCulture(PathString pathBase, out string language) =>
        CulturePath.TryDetach(pathBase, out language, out _);
}
