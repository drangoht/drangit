using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Drangit.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class SiteTests : IClassFixture<SiteFactoryFixture>
{
    private readonly SiteFactory _factory;

    public SiteTests(SiteFactoryFixture fixture)
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

        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);
        return client;
    }

    [Fact]
    public async Task Accueil_ListeLesDepotsDuCatalogue()
    {
        var html = await CreateClient().GetStringAsync("/en/", CancellationToken.None);

        html.ShouldContain("ASM-Gameboy-FirstProg");
        html.ShouldContain("href=\"repos/asm-gameboy-firstprog\"");
    }

    [Fact]
    public async Task Accueil_MetEnVitrineLeDepotDesigne()
    {
        var html = await CreateClient().GetStringAsync("/en/", CancellationToken.None);

        html.ShouldContain("hero__title");
        html.ShouldContain("Algorithme-de-Huffman");
    }

    [Fact]
    public async Task Accueil_NAfficheJamaisLaCarteDeGitHub()
    {
        var html = await CreateClient().GetStringAsync("/en/", CancellationToken.None);

        // La carte que produit GitHub réécrit le nom, la description et les compteurs, tous
        // affichés juste en dessous. Quarante-cinq d'entre elles font un mur de rectangles
        // clairs identiques, et chacune coûte une requête vers un tiers. La vignette est
        // donc dessinée ici, à partir du langage et du nom.
        html.ShouldNotContain("opengraph.githubassets.com");
    }

    [Fact]
    public async Task Accueil_SAnnonceParLaBanniereDuSite()
    {
        var html = await CreateClient().GetStringAsync("/en/", CancellationToken.None);

        // La bannière tient lieu de titre d'affichage : la peau n'a pas de fonte
        // d'affichage, c'est le dessin en caractères qui en fait office.
        html.ShouldContain("class=\"cli__banner\"");
        html.ShouldContain("aria-label=\"Drangit\"");
    }

    [Fact]
    public async Task Accueil_EcritChaqueSectionCommeUneCommandeSaisie()
    {
        var html = await CreateClient().GetStringAsync("/en/", CancellationToken.None);

        // Ce sont des lignes de commande décoratives : elles annoncent ce que la section
        // affiche, comme la sortie d'un programme le ferait.
        html.ShouldContain("class=\"cli__line\"");
        html.ShouldContain("repos --featured");
    }

    [Fact]
    public async Task Accueil_ListeLesDepotsEnColonnesEtNonEnVignettes()
    {
        var html = await CreateClient().GetStringAsync("/en/", CancellationToken.None);

        // Quarante-sept dépôts se lisent en colonnes alignées, pas en mur de tuiles.
        html.ShouldContain("class=\"listing\"");
        html.ShouldNotContain("class=\"grid\"");
    }

    [Fact]
    public async Task Accueil_NAfficheAucuneImage()
    {
        var html = await CreateClient().GetStringAsync("/en/", CancellationToken.None);

        // Une sortie de terminal ne montre pas d'images — pas même les captures du fichier
        // éditorial, qui restent sur la fiche du dépôt.
        html.ShouldNotContain("<img");
        html.ShouldNotContain("https://exemple.test/huffman.png");
    }

    [Fact]
    public async Task Fiche_QuandUneCaptureEstDeclaree_ElleSAfficheParDessusLaVignette()
    {
        var html = await CreateClient().GetStringAsync("/en/repos/algorithme-de-huffman", CancellationToken.None);

        // Une capture du fichier éditorial montre le projet lui-même : elle, elle vaut
        // la place qu'elle prend.
        html.ShouldContain("https://exemple.test/huffman.png");
        html.ShouldContain("onerror=\"this.remove()\"");
    }

    [Fact]
    public async Task Fiche_DessineUneVignettePourUnDepotSansCapture()
    {
        var html = await CreateClient().GetStringAsync("/en/repos/asm-gameboy-firstprog", CancellationToken.None);

        // Sans elle, une fiche sans capture n'aurait aucune illustration.
        html.ShouldContain("--cover-accent:");
        html.ShouldContain("<span class=\"cover__owner\">drangoht/</span>ASM-Gameboy-FirstProg");
    }

    [Fact]
    public async Task Accueil_QuandUnFiltreEstActif_EffaceLaVitrine()
    {
        // Filtrer, c'est chercher : la vitrine deviendrait un doublon au-dessus des résultats.
        var html = await CreateClient().GetStringAsync("/en/?topic=algorithms", CancellationToken.None);

        html.ShouldNotContain("hero__title");
    }

    [Fact]
    public async Task Accueil_FiltreParSujetDepuisLaChaineDeRequete()
    {
        var client = CreateClient();

        (await client.GetStringAsync("/en/?topic=algorithms", CancellationToken.None))
            .ShouldContain("Algorithme-de-Huffman");
        (await client.GetStringAsync("/en/?topic=metroidvania", CancellationToken.None))
            .ShouldContain("No repository matches these filters");
    }

    [Fact]
    public async Task Accueil_NeGardeQueLesDepotsAvecDemoQuandLUrlLeDemande()
    {
        var html = await CreateClient().GetStringAsync("/en/?demo=true", CancellationToken.None);

        html.ShouldContain("Algorithme-de-Huffman");
        html.ShouldNotContain("ASM-Gameboy-FirstProg");
    }

    [Fact]
    public async Task Accueil_EcarteLesDepotsArchivesQuandLUrlLeDemande()
    {
        var html = await CreateClient().GetStringAsync("/en/?active=true", CancellationToken.None);

        html.ShouldNotContain("ASM-Gameboy-FirstProg");
    }

    [Fact]
    public async Task Accueil_QuandUnCritereDeLUrlEstInvalide_NEchouePas()
    {
        // Le paramètre vient du visiteur, pas de nous.
        using var response = await CreateClient().GetAsync("/en/?lang=nawak&demo=nawak", CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FicheDepot_AfficheLeDepotEtRenvoieVersGitHub()
    {
        var html = await CreateClient().GetStringAsync("/en/repos/algorithme-de-huffman", CancellationToken.None);

        html.ShouldContain("Algorithme-de-Huffman");
        html.ShouldContain("https://github.com/drangoht/Algorithme-de-Huffman");
        html.ShouldContain("git clone https://github.com/drangoht/Algorithme-de-Huffman.git");
    }

    [Fact]
    public async Task FicheDepot_QuandLeDepotDeclareUneDemo_LaPropose()
    {
        var html = await CreateClient().GetStringAsync("/en/repos/algorithme-de-huffman", CancellationToken.None);

        html.ShouldContain("https://huffman.example/");
    }

    [Fact]
    public async Task FicheDepot_QuandLeDepotEstArchive_LeDitAuVisiteur()
    {
        var html = await CreateClient().GetStringAsync("/en/repos/asm-gameboy-firstprog", CancellationToken.None);

        html.ShouldContain("no longer maintained");
    }

    [Fact]
    public async Task FicheDepot_QuandLeSlugEstInconnu_Repond404()
    {
        using var response = await CreateClient().GetAsync("/en/repos/depot-fantome", CancellationToken.None);

        // Un 200 ferait indexer des pages fantômes par les moteurs de recherche.
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FicheDepot_EstAtteignableQuelleQueSoitLaCasseDuSlug()
    {
        using var response = await CreateClient().GetAsync("/en/repos/Algorithme-de-Huffman", CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RouteInconnue_Repond404()
    {
        using var response = await CreateClient().GetAsync("/nimporte-quoi", CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Health_RepondSansDependreDeGitHub()
    {
        using var response = await CreateClient().GetAsync("/health", CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task APropos_EstAccessible()
    {
        using var response = await CreateClient().GetAsync("/en/about", CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reponses_PortentLesEntetesDeSecuriteAttendus()
    {
        using var response = await CreateClient().GetAsync("/en/", CancellationToken.None);

        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        response.Headers.Contains("Referrer-Policy").ShouldBeTrue();
        // Aucune page du site n'est encadrée, pas même par lui-même.
        response.Headers.GetValues("X-Frame-Options").ShouldContain("DENY");
    }

    [Theory]
    [InlineData("/en/")]
    [InlineData("/en/repos/algorithme-de-huffman")]
    [InlineData("/en/about")]
    public async Task LesPages_RepondentAUneRequeteHead(string chemin)
    {
        // Les services de supervision sondent en HEAD : une page qui n'y répond pas
        // passe pour hors ligne alors qu'elle est servie normalement en GET.
        using var requete = new HttpRequestMessage(HttpMethod.Head, chemin);

        using var response = await CreateClient().SendAsync(requete, CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

/// <summary>Partage une seule instance du site entre les tests de la classe.</summary>
public sealed class SiteFactoryFixture : IDisposable
{
    internal SiteFactory Factory { get; } = new();

    public void Dispose() => Factory.Dispose();
}
