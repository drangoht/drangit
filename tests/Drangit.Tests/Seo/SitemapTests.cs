using Drangit.Tests.Builders;
using Drangit.Web.Seo;
using Shouldly;

namespace Drangit.Tests.Seo;

public sealed class SitemapTests
{
    private static readonly Uri BaseUri = new("https://drangit.example/");

    [Fact]
    public void Build_AnnonceChaquePageDansChaqueLangue()
    {
        var depot = new RepositoryBuilder().WithName("katas").Build();

        var plan = Sitemap.Build([depot], BaseUri);

        // Une version non annoncée n'est explorée qu'au hasard des liens (ADR 0006).
        plan.ShouldContain("https://drangit.example/en/");
        plan.ShouldContain("https://drangit.example/fr/");
        plan.ShouldContain("https://drangit.example/en/about");
        plan.ShouldContain("https://drangit.example/fr/about");
        plan.ShouldContain("https://drangit.example/en/repos/katas");
        plan.ShouldContain("https://drangit.example/fr/repos/katas");
    }

    [Fact]
    public void Build_DateChaqueFicheDeSonDernierCommit()
    {
        var depot = new RepositoryBuilder()
            .WithName("katas")
            .WithPushedAt(new DateTimeOffset(2024, 3, 17, 12, 0, 0, TimeSpan.Zero))
            .Build();

        Sitemap.Build([depot], BaseUri).ShouldContain("<lastmod>2024-03-17</lastmod>");
    }

    [Fact]
    public void Build_QuandLeDepotNAJamaisRecuDeCommit_NAnnonceAucuneDate()
    {
        var depot = new RepositoryBuilder().WithName("vide").WithPushedAt(null).Build();

        // Une date inventée vaut moins que pas de date du tout.
        Sitemap.Build([depot], BaseUri).ShouldNotContain("<lastmod>");
    }

    [Fact]
    public void Build_QuandLeCatalogueEstVide_AnnonceQuandMemeLesPagesFixes()
    {
        var plan = Sitemap.Build([], BaseUri);

        plan.ShouldContain("https://drangit.example/en/about");
        plan.ShouldNotContain("/repos/");
    }

    [Fact]
    public void Build_ProduitUnDocumentXmlValide()
    {
        var plan = Sitemap.Build([new RepositoryBuilder().Build()], BaseUri);

        Should.NotThrow(() => System.Xml.Linq.XDocument.Parse(plan));
        plan.ShouldContain("http://www.sitemaps.org/schemas/sitemap/0.9");
    }
}
