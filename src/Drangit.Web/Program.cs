using System.Globalization;
using Drangit.Web;
using Drangit.Web.Components;
using Drangit.Web.Localization;
using Drangit.Web.Repositories;
using Drangit.Web.Repositories.Editorial;
using Drangit.Web.Repositories.GitHub;
using Drangit.Web.Repositories.Snapshots;
using Drangit.Web.Seo;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration fortement typée, validée au démarrage ---------------------------------
// Un compte mal orthographié doit empêcher le conteneur de démarrer, pas produire une
// vitrine vide à la première visite.
builder.Services.AddOptions<GitHubOptions>()
    .Bind(builder.Configuration.GetSection(GitHubOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<SnapshotOptions>()
    .Bind(builder.Configuration.GetSection(SnapshotOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<EditorialOptions>()
    .Bind(builder.Configuration.GetSection(EditorialOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<SiteOptions>()
    .Bind(builder.Configuration.GetSection(SiteOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// --- Accès à GitHub -----------------------------------------------------------------------
// Le détail du transport (route, en-têtes, sérialisation, résilience) reste dans
// Repositories/GitHub : le composition root n'a pas à connaître la convention de nommage
// d'un fournisseur tiers.
builder.Services.AddGitHubCatalog();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<RepositoryCatalogSnapshotStore>();
builder.Services.AddSingleton(provider =>
{
    var options = provider.GetRequiredService<IOptions<EditorialOptions>>().Value;
    var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(EditorialCatalogFile));

    return EditorialCatalogFile.Load(options.FilePath, logger);
});

builder.Services.AddSingleton<IRepositoryCatalog, RepositoryCatalog>();

// --- Localisation -------------------------------------------------------------------------
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = SupportedCultures.All
        .Select(culture => new CultureInfo(culture))
        .ToArray();

    options.DefaultRequestCulture = new RequestCulture(SupportedCultures.Default);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.ApplyCurrentCultureToResponseHeaders = true;

    // Le chemin fait foi : une adresse française sert du français, quelle que soit la
    // préférence du navigateur. L'en-tête ne décide plus que de la destination de la racine.
    options.RequestCultureProviders.Insert(0, CulturePrefix.FromPath);
});

builder.Services.AddRazorComponents();
builder.Services.AddHealthChecks();

// Le site tourne derrière un reverse proxy : sans cela, les URL générées et les journaux
// portent l'adresse du conteneur au lieu de celle du visiteur.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    // DENY, et non SAMEORIGIN : aucune page du site n'est encadrée, pas même par lui-même.
    headers["X-Frame-Options"] = "DENY";
    await next().ConfigureAwait(false);
});

// Ré-exécution réservée à la navigation. Sur un POST, elle rejouerait tout le pipeline —
// dont la validation antiforgery, qui échouerait une seconde fois en levant cette fois une
// exception : un refus net se transformerait en erreur serveur.
app.UseWhen(
    static context => HttpMethods.IsGet(context.Request.Method)
                      || HttpMethods.IsHead(context.Request.Method),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

// La langue est portée par le chemin (ADR 0006). Le préfixe est détaché avant la
// localisation, qui le lit ; les adresses non préfixées sont redirigées après elle, la
// racine ayant besoin de connaître la langue négociée pour choisir sa destination.
app.UseCulturePathPrefix();

// Le routage est appelé ici explicitement : à défaut, ASP.NET Core l'insère en tête du
// pipeline, où il apparierait le chemin avant que son préfixe de langue en soit détaché —
// et aucune route ne correspondrait.
app.UseRouting();

app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
app.UseCulturePathRedirects();
app.UseAntiforgery();

app.MapStaticAssets();

// Les pages ne déclarent que GET et POST : une requête HEAD y récolte un 404, alors que
// les services de supervision sondent avec cette méthode et signaleraient le site à terre.
// Kestrel se charge d'omettre le corps de la réponse.
app.MapRazorComponents<App>().Add(static endpoint =>
{
    var methodes = endpoint.Metadata.OfType<HttpMethodMetadata>().LastOrDefault();

    if (methodes is not null
        && methodes.HttpMethods.Contains(HttpMethods.Get)
        && !methodes.HttpMethods.Contains(HttpMethods.Head))
    {
        endpoint.Metadata.Add(new HttpMethodMetadata([.. methodes.HttpMethods, HttpMethods.Head]));
    }
});

// Sonde de vivacité : volontairement indépendante de GitHub. Marquer le conteneur malsain
// parce qu'une API tierce est tombée le ferait redémarrer en boucle sans rien réparer.
app.MapHealthChecks("/health").AllowAnonymous();

app.MapSeoEndpoints();

await app.RunAsync().ConfigureAwait(false);

/// <summary>Point d'entrée, rendu visible pour les tests d'intégration.</summary>
public partial class Program;
