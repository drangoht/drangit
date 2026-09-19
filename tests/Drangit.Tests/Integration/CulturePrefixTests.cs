using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Drangit.Tests.Integration;

/// <summary>
/// La langue vit dans le chemin (ADR 0006) : ce que devient chaque forme d'adresse.
/// </summary>
[Trait("Category", "Integration")]
public sealed class CulturePrefixTests : IClassFixture<SiteFactoryFixture>
{
    private readonly SiteFactory _factory;

    public CulturePrefixTests(SiteFactoryFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _factory = fixture.Factory;
    }

    private HttpClient CreateClient(string acceptLanguage = "en")
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        client.DefaultRequestHeaders.Add("Accept-Language", acceptLanguage);
        return client;
    }

    [Theory]
    [InlineData("/fr/", "fr")]
    [InlineData("/en/", "en")]
    [InlineData("/fr/about", "fr")]
    [InlineData("/en/about", "en")]
    [InlineData("/fr/repos/algorithme-de-huffman", "fr")]
    [InlineData("/en/repos/algorithme-de-huffman", "en")]
    public async Task PagePrefixee_EstServieDansLaLangueDeSonChemin(string chemin, string langue)
    {
        // L'en-tête du navigateur dit l'inverse du chemin : c'est le chemin qui doit gagner.
        var contraire = langue == "fr" ? "en" : "fr";

        using var response = await CreateClient(contraire).GetAsync(chemin, CancellationToken.None);

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(CancellationToken.None);
        html.ShouldContain($"lang=\"{langue}\"");
    }

    [Theory]
    [InlineData("/repos/algorithme-de-huffman", "/en/repos/algorithme-de-huffman")]
    [InlineData("/about", "/en/about")]
    public async Task AdresseSansPrefixe_RedirigeDefinitivementVersLAnglais(string ancienne, string attendue)
    {
        // Ces adresses désignent une page précise : leur destination ne dépend pas du
        // visiteur, la redirection est donc permanente.
        using var response = await CreateClient("fr").GetAsync(ancienne, CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.ToString().ShouldBe(attendue);
    }

    [Theory]
    [InlineData("fr-FR,fr;q=0.9", "/fr/")]
    [InlineData("de-DE", "/en/")]
    [InlineData("", "/en/")]
    public async Task Racine_OrienteLeVisiteurVersSaLangue(string acceptLanguage, string attendue)
    {
        // La racine est la seule adresse dont la destination dépend du visiteur : la
        // redirection y est temporaire, sans quoi le navigateur la figerait pour tous.
        using var response = await CreateClient(acceptLanguage).GetAsync("/", CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location!.ToString().ShouldBe(attendue);
    }

    [Fact]
    public async Task AdresseSansPrefixe_ConserveSaChaineDeRequete()
    {
        using var response = await CreateClient().GetAsync("/?topic=algorithms", CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location!.ToString().ShouldBe("/en/?topic=algorithms");
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/robots.txt")]
    [InlineData("/sitemap.xml")]
    public async Task RessourceSansLangue_ResteAccessibleSansPrefixe(string chemin)
    {
        // Ce que ces adresses servent ne dépend d'aucune langue, et des tiers les
        // connaissent déjà — les préfixer les casserait sans rien apporter.
        using var response = await CreateClient().GetAsync(chemin, CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task LangueInconnueDansLeChemin_NeSeFaitPasPasserPourUneLangue()
    {
        // « /de/about » n'est pas une page : le site ne publie pas l'allemand, et cette
        // adresse ne doit pas répondre en anglais comme si de rien n'était.
        using var response = await CreateClient().GetAsync("/de/about", CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/en/")]
    [InlineData("/fr/")]
    public async Task PagePrefixee_SertChaqueFeuilleQuelleReference(string chemin)
    {
        // Deux défauts d'un coup, qu'aucun test de contenu ne verrait : une feuille
        // référencée mais absente, et une feuille que le préfixe de langue rendrait
        // introuvable — les liens étant relatifs à <base>, elle est demandée préfixée.
        // Dans les deux cas le site s'afficherait sans style, en répondant 200.
        var client = CreateClient();
        var html = await client.GetStringAsync(chemin, CancellationToken.None);
        var feuilles = StylesheetHrefs(html).ToList();

        feuilles.ShouldNotBeEmpty();

        foreach (var feuille in feuilles)
        {
            using var response = await client.GetAsync(
                new Uri(new Uri(new Uri("http://localhost"), chemin), feuille),
                CancellationToken.None);

            response.StatusCode.ShouldBe(HttpStatusCode.OK, feuille);
        }
    }

    [Theory]
    [InlineData("/fr/repos/algorithme-de-huffman")]
    [InlineData("/en/about")]
    [InlineData("/fr/")]
    public async Task LienDEvitement_ResteSurLaPageAuLieuDeRamenerALAccueil(string chemin)
    {
        // Piège de <base href> : une référence réduite à un fragment se résout contre la
        // base, pas contre l'adresse courante. Un « #main » nu ferait quitter la page —
        // le lien d'évitement, qui n'existe que pour les lecteurs d'écran et le clavier,
        // renverrait à l'accueil sans que personne ne s'en aperçoive.
        var html = await CreateClient().GetStringAsync(chemin, CancellationToken.None);

        html.ShouldContain($"class=\"skip-link\" href=\"{chemin}#main\"");
        html.ShouldNotContain("href=\"#main\"");
    }

    [Fact]
    public async Task SelecteurDeLangue_RenvoieVersLaMemePageDansLAutreLangue()
    {
        var html = await CreateClient()
            .GetStringAsync("/fr/repos/algorithme-de-huffman", CancellationToken.None);

        // Changer de langue, c'est changer de base : ces liens-là sont absolus, et mènent
        // à la page équivalente plutôt qu'à l'accueil.
        html.ShouldContain("href=\"/en/repos/algorithme-de-huffman\"");
    }

    [Fact]
    public async Task SelecteurDeLangue_DepuisLAccueilFiltre_ConserveLesCriteres()
    {
        var html = await CreateClient().GetStringAsync("/fr/?topic=algorithms", CancellationToken.None);

        html.ShouldContain("href=\"/en/?topic=algorithms\"");
    }

    [Fact]
    public async Task PagePrefixee_PorteDesLiensInternesQuiRestentDansSaLangue()
    {
        var html = await CreateClient("en")
            .GetStringAsync("/fr/repos/algorithme-de-huffman", CancellationToken.None);

        // Le préfixe est porté par <base>, donc les liens internes sont relatifs. Un
        // « href="/… » absolu échapperait à la base et ramènerait le visiteur en anglais.
        html.ShouldContain("<base href=\"/fr/\"");
        html.ShouldNotContain("href=\"/repos/");
        html.ShouldNotContain("href=\"/about\"");
    }

    private static IEnumerable<string> StylesheetHrefs(string html)
    {
        const string marker = "<link rel=\"stylesheet\" href=\"";

        for (var start = html.IndexOf(marker, StringComparison.Ordinal);
             start >= 0;
             start = html.IndexOf(marker, start + 1, StringComparison.Ordinal))
        {
            var value = start + marker.Length;
            yield return html[value..html.IndexOf('"', value)];
        }
    }
}
