using Shouldly;

namespace Drangit.Tests.Integration;

/// <summary>
/// Ce que le site présente aux moteurs de recherche et aux aperçus de partage.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SeoTests : IClassFixture<SiteFactoryFixture>
{
    private readonly SiteFactory _factory;

    public SeoTests(SiteFactoryFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _factory = fixture.Factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    [Fact]
    public async Task RobotsTxt_AutoriseLExplorationEtDesigneLePlanDuSite()
    {
        using var response = await CreateClient().GetAsync("/robots.txt", CancellationToken.None);

        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/plain");

        var texte = await response.Content.ReadAsStringAsync(CancellationToken.None);
        texte.ShouldContain("User-agent: *");
        texte.ShouldContain("Sitemap: http://localhost/sitemap.xml");
    }

    [Fact]
    public async Task SitemapXml_ListeLesPagesIndexablesDuSite()
    {
        using var response = await CreateClient().GetAsync("/sitemap.xml", CancellationToken.None);

        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/xml");

        var xml = await response.Content.ReadAsStringAsync(CancellationToken.None);

        // Chaque page indexable existe dans les deux langues, et le plan les annonce toutes :
        // une version absente du plan n'est explorée que par hasard, au gré des liens.
        xml.ShouldContain("http://localhost/en/repos/algorithme-de-huffman");
        xml.ShouldContain("http://localhost/fr/repos/algorithme-de-huffman");
        xml.ShouldContain("http://localhost/en/about");
        xml.ShouldContain("http://localhost/fr/about");
    }

    [Fact]
    public async Task FicheDepot_DesigneSonAdresseCanonique()
    {
        var html = await CreateClient()
            .GetStringAsync("/fr/repos/algorithme-de-huffman", CancellationToken.None);

        html.ShouldContain(
            "<link rel=\"canonical\" href=\"http://localhost/fr/repos/algorithme-de-huffman\"");
    }

    [Fact]
    public async Task FicheDepot_DeclareSesVersionsDansLesDeuxLangues()
    {
        var html = await CreateClient()
            .GetStringAsync("/en/repos/algorithme-de-huffman", CancellationToken.None);

        // La réciprocité est la condition pour que le groupe soit pris en compte, et son
        // absence ne se signale nulle part.
        html.ShouldContain("hreflang=\"en\" href=\"http://localhost/en/repos/algorithme-de-huffman\"");
        html.ShouldContain("hreflang=\"fr\" href=\"http://localhost/fr/repos/algorithme-de-huffman\"");
        html.ShouldContain("hreflang=\"x-default\"");
    }

    [Fact]
    public async Task Accueil_SousFiltre_DesigneLAccueilNuCommeAdresseCanonique()
    {
        // Sans cela, chaque combinaison de critères produirait une copie indexée.
        var html = await CreateClient().GetStringAsync("/en/?topic=algorithms", CancellationToken.None);

        html.ShouldContain("<link rel=\"canonical\" href=\"http://localhost/en/\"");
    }

    [Fact]
    public async Task FicheDepot_PorteSaFicheSchemaOrg()
    {
        var html = await CreateClient()
            .GetStringAsync("/en/repos/algorithme-de-huffman", CancellationToken.None);

        // Blazor encode le « + » du type MIME en « &#x2B; » : c'est du HTML valide, que
        // l'analyseur du navigateur comme celui d'un robot redécodent. On cherche donc le
        // document lui-même, pas la forme exacte de l'attribut.
        html.ShouldContain("<script type=\"application/ld");
        html.ShouldContain("SoftwareSourceCode");
        html.ShouldContain("https://github.com/drangoht/Algorithme-de-Huffman");
    }

    [Fact]
    public async Task FicheDepot_QuandUneCaptureEstDeclaree_LaProposeEnApercuDePartage()
    {
        var html = await CreateClient()
            .GetStringAsync("/en/repos/algorithme-de-huffman", CancellationToken.None);

        html.ShouldContain("<meta property=\"og:image\" content=\"https://exemple.test/huffman.png\"");
        html.ShouldContain("twitter:card");
    }

    [Theory]
    [InlineData("/en/")]
    [InlineData("/en/about")]
    [InlineData("/en/repos/asm-gameboy-firstprog")]
    public async Task LesPages_NeParagentJamaisLaCarteDeGitHub(string chemin)
    {
        // Elle incrusterait l'avatar du compte dans chaque aperçu. Dans une page du site on
        // la recadre ; un aperçu, lui, s'affiche chez autrui, où rien ne la recadre.
        var html = await CreateClient().GetStringAsync(chemin, CancellationToken.None);

        var tete = html[..html.IndexOf("</head>", StringComparison.Ordinal)];

        tete.ShouldNotContain("opengraph.githubassets.com");
        tete.ShouldContain("og-share");
    }

    [Fact]
    public async Task LImageDePartageDuSite_EstReellementServie()
    {
        // Une adresse d'aperçu qui répond 404 ne se voit nulle part : l'aperçu s'affiche
        // simplement sans image, chez celui qui a partagé le lien.
        var client = CreateClient();
        var html = await client.GetStringAsync("/en/", CancellationToken.None);

        var marqueur = "<meta property=\"og:image\" content=\"";
        var debut = html.IndexOf(marqueur, StringComparison.Ordinal) + marqueur.Length;
        var adresse = html[debut..html.IndexOf('"', debut)];

        using var response = await client.GetAsync(adresse, CancellationToken.None);

        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
    }
}
