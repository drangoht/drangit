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
    public async Task Accueil_EstUneSeuleFenetreDeTerminal()
    {
        var html = await CreateClient().GetStringAsync("/en/", CancellationToken.None);

        // La page entière tient dans le cadre : barre de titre, écran, prompt.
        html.ShouldContain("class=\"window\"");
        html.ShouldContain("class=\"window__bar\"");
        html.ShouldContain("class=\"window__screen\"");
    }

    [Fact]
    public async Task Accueil_NOffreNiBarreDeFiltresNiVitrine()
    {
        var html = await CreateClient().GetStringAsync("/en/", CancellationToken.None);

        // Tout passe par le prompt (ADR 0010) : plus de critères cliquables au-dessus de la
        // liste, plus de bloc de vitrine avant elle.
        html.ShouldNotContain("class=\"filters\"");
        html.ShouldNotContain("class=\"chip");
        html.ShouldNotContain("hero__title");
    }

    [Fact]
    public async Task Accueil_PlaceLeDepotDesigneEnTeteDeListe()
    {
        var html = await CreateClient().GetStringAsync("/en/", CancellationToken.None);

        // La vitrine a disparu, pas la désignation : le dépôt mis en avant par le fichier
        // éditorial ouvre la liste.
        var designe = html.IndexOf("Algorithme-de-Huffman", StringComparison.Ordinal);
        var autre = html.IndexOf("ASM-Gameboy-FirstProg", StringComparison.Ordinal);

        designe.ShouldBeGreaterThan(-1);
        autre.ShouldBeGreaterThan(designe);
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

        // C'est une ligne de commande décorative : elle annonce ce que la liste affiche,
        // comme la sortie d'un programme le ferait, et reflète les critères posés.
        html.ShouldContain("class=\"cli__line\"");
        html.ShouldContain("repos --list");
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
    public async Task Prompt_UneCommandeDeFiltre_MeneAuMemeEndroitQueLeLienCorrespondant()
    {
        // La condition posée par l'ADR 0008 : sans JavaScript, taper la commande et cliquer
        // le critère aboutissent à la même adresse, partageable.
        var reponse = await CreateClient().GetAsync(
            new Uri("/en/?c=--topic%3Dalgorithms", UriKind.Relative), CancellationToken.None);

        reponse.StatusCode.ShouldBe(HttpStatusCode.Found);
        reponse.Headers.Location!.PathAndQuery.ShouldBe("/en/?topic=algorithms");
    }

    [Fact]
    public async Task Prompt_UneRechercheLibre_DevientLeCritereDeRecherche()
    {
        var reponse = await CreateClient().GetAsync(
            new Uri("/en/?c=huffman", UriKind.Relative), CancellationToken.None);

        reponse.StatusCode.ShouldBe(HttpStatusCode.Found);
        reponse.Headers.Location!.PathAndQuery.ShouldBe("/en/?q=huffman");
    }

    [Fact]
    public async Task Prompt_open_MeneALaFicheDuDepot()
    {
        var reponse = await CreateClient().GetAsync(
            new Uri("/en/?c=open%20ASM-Gameboy-FirstProg", UriKind.Relative), CancellationToken.None);

        reponse.StatusCode.ShouldBe(HttpStatusCode.Found);
        reponse.Headers.Location!.PathAndQuery.ShouldBe("/en/repos/asm-gameboy-firstprog");
    }

    [Fact]
    public async Task Prompt_cd_about_MeneALaPageAPropos()
    {
        var reponse = await CreateClient().GetAsync(
            new Uri("/en/?c=cd%20about", UriKind.Relative), CancellationToken.None);

        reponse.StatusCode.ShouldBe(HttpStatusCode.Found);
        reponse.Headers.Location!.PathAndQuery.ShouldBe("/en/about");
    }

    [Fact]
    public async Task Prompt_UneCommandeVide_RevientALAccueilSansCritere()
    {
        var reponse = await CreateClient().GetAsync(
            new Uri("/en/?c=", UriKind.Relative), CancellationToken.None);

        // Valider un prompt vide ne doit pas boucler ni laisser « c= » dans l'adresse.
        reponse.StatusCode.ShouldBe(HttpStatusCode.Found);
        reponse.Headers.Location!.PathAndQuery.ShouldBe("/en/");
    }

    [Fact]
    public async Task Prompt_SansJavaScript_UneCommandeDInformationEstRendueParLeServeur()
    {
        var reponse = await CreateClient().GetAsync(
            new Uri("/en/?c=topics", UriKind.Relative), CancellationToken.None);

        // Les critères cliquables ayant disparu (ADR 0010), « topics » est le seul endroit où
        // l'on découvre les sujets : il doit répondre sans JavaScript, donc sans redirection.
        reponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await reponse.Content.ReadAsStringAsync(CancellationToken.None);

        html.ShouldNotContain("class=\"cli__outputs\" hidden");
        html.ShouldContain("algorithms");
    }

    [Fact]
    public async Task Accueil_OffreLePromptCommeFormulaire()
    {
        var html = await CreateClient().GetStringAsync("/en/", CancellationToken.None);

        // Un vrai formulaire GET : le prompt fonctionne sans JavaScript (ADR 0008).
        html.ShouldContain("class=\"cli__form\"");
        html.ShouldContain("name=\"c\"");
    }

    [Fact]
    public async Task Accueil_FournitLesSortiesDuPromptDejaRedigees()
    {
        var html = await CreateClient().GetStringAsync("/en/", CancellationToken.None);

        // Le script client ne connaît aucun texte ni aucune règle (ADR 0008) : le serveur
        // rend ces blocs, traduits et chiffrés, et le script ne fait que les montrer.
        html.ShouldContain("data-output=\"help\"");
        html.ShouldContain("data-output=\"whoami\"");
        html.ShouldContain("data-output=\"stats\"");
        html.ShouldContain("data-output=\"topics\"");
    }

    [Fact]
    public async Task Accueil_LesSortiesDuPromptSontMasqueesTantQuOnNeLesDemandePas()
    {
        var html = await CreateClient().GetStringAsync("/en/", CancellationToken.None);

        // Sans JavaScript, elles ne doivent pas s'ajouter au bas de la page.
        html.ShouldContain("class=\"cli__outputs\" hidden");
    }

    [Fact]
    public async Task Accueil_LaSortieStats_CompteLesDepotsDuCatalogue()
    {
        var html = await CreateClient().GetStringAsync("/en/", CancellationToken.None);

        // Le jeu de test compte deux dépôts, dont un archivé et un avec démonstration.
        html.ShouldContain("<span data-stat=\"repositories\">2</span>");
        html.ShouldContain("<span data-stat=\"archived\">1</span>");
        html.ShouldContain("<span data-stat=\"demos\">1</span>");
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
