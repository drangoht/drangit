using System.Globalization;
using System.Text.Json;
using Drangit.Tests.Builders;
using Drangit.Web.Repositories;
using Drangit.Web.Repositories.Editorial;
using Shouldly;

namespace Drangit.Tests.Repositories;

public sealed class EditorialCatalogTests
{
    private static readonly CultureInfo Francais = new("fr");

    [Fact]
    public void FromJson_QuandLeFichierEstVide_NEnrichitRien()
    {
        var catalogue = EditorialCatalog.FromJson("""{ "repositories": {} }""");

        catalogue.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Apply_QuandLeDepotNAPasDEntree_LeLaisseIntact()
    {
        var depot = new RepositoryBuilder().WithName("sans-entree").Build();
        var catalogue = EditorialCatalog.FromJson("""{ "repositories": { "autre": { "featured": true } } }""");

        catalogue.Apply(depot).ShouldBe(depot);
    }

    [Fact]
    public void Apply_SuperposeLaTraductionFrancaiseSansEffacerLAnglaisDeGitHub()
    {
        var depot = new RepositoryBuilder()
            .WithName("katas")
            .WithDescription(LocalizedText.FromEnglish("Kata collection"))
            .Build();

        var catalogue = EditorialCatalog.FromJson(
            """{ "repositories": { "katas": { "summary": { "fr": "Recueil de katas" } } } }""");

        var enrichi = catalogue.Apply(depot);

        enrichi.Description.For(Francais).ShouldBe("Recueil de katas");
        enrichi.Description.English.ShouldBe("Kata collection");
    }

    [Fact]
    public void Apply_AjouteLesSujetsLocauxSansEffacerCeuxDeGitHub()
    {
        var depot = new RepositoryBuilder().WithName("katas").WithTopics("tdd").Build();

        var catalogue = EditorialCatalog.FromJson(
            """{ "repositories": { "katas": { "topics": ["csharp", "TDD"] } } }""");

        // « TDD » est déjà là sous une autre casse : il ne doit pas apparaître deux fois.
        catalogue.Apply(depot).Topics.ShouldBe(["tdd", "csharp"]);
    }

    [Fact]
    public void Apply_NAjouteLaDemoQueSiLeDepotNEnDeclarePas()
    {
        var sansDemo = new RepositoryBuilder().WithName("sans-demo").Build();
        var avecDemo = new RepositoryBuilder().WithName("avec-demo").WithDemo("https://deja.example/").Build();

        var catalogue = EditorialCatalog.FromJson(
            """
            {
              "repositories": {
                "sans-demo": { "demoUrl": "https://local.example/" },
                "avec-demo": { "demoUrl": "https://local.example/" }
              }
            }
            """);

        catalogue.Apply(sansDemo).HomepageUrl!.ToString().ShouldBe("https://local.example/");
        // « homepage » se corrige sur GitHub : le fichier éditorial ne doit pas le doubler.
        catalogue.Apply(avecDemo).HomepageUrl!.ToString().ShouldBe("https://deja.example/");
    }

    [Fact]
    public void Apply_IgnoreUneCaptureDontLAdresseEstInvalide()
    {
        var depot = new RepositoryBuilder().WithName("katas").Build();

        var catalogue = EditorialCatalog.FromJson(
            """
            {
              "repositories": {
                "katas": {
                  "screenshots": [
                    { "url": "pas-une-adresse" },
                    { "url": "https://exemple.test/capture.png", "caption": { "fr": "Écran principal" } }
                  ]
                }
              }
            }
            """);

        var capture = catalogue.Apply(depot).Screenshots.ShouldHaveSingleItem();
        capture.Url.ToString().ShouldBe("https://exemple.test/capture.png");
        capture.Caption.For(Francais).ShouldBe("Écran principal");
    }

    [Fact]
    public void Apply_RetientLaMiseEnVitrine()
    {
        var depot = new RepositoryBuilder().WithName("vitrine").Build();
        var catalogue = EditorialCatalog.FromJson("""{ "repositories": { "vitrine": { "featured": true } } }""");

        catalogue.Apply(depot).IsFeatured.ShouldBeTrue();
    }

    [Fact]
    public void IsHidden_ReconnaitUnDepotRetireDuSite()
    {
        var masque = new RepositoryBuilder().WithName("drangoht").Build();
        var visible = new RepositoryBuilder().WithName("katas").Build();

        var catalogue = EditorialCatalog.FromJson(
            """{ "repositories": { "drangoht": { "hidden": true }, "katas": { "featured": true } } }""");

        catalogue.IsHidden(masque).ShouldBeTrue();
        catalogue.IsHidden(visible).ShouldBeFalse();
    }

    [Fact]
    public void FromJson_ReconnaitLeSlugQuelleQueSoitLaCasseDuNomDuDepot()
    {
        var depot = new RepositoryBuilder().WithName("Algorithme-de-Huffman").Build();

        var catalogue = EditorialCatalog.FromJson(
            """{ "repositories": { "Algorithme-de-Huffman": { "featured": true } } }""");

        catalogue.Apply(depot).IsFeatured.ShouldBeTrue();
    }

    [Fact]
    public void FindOrphanEntries_SignaleUneEntreeQuiNeCorrespondANucunDepot()
    {
        var catalogue = EditorialCatalog.FromJson(
            """{ "repositories": { "katas": {}, "depot-renomme": {} } }""");

        var orphelins = catalogue.FindOrphanEntries([new RepositoryBuilder().WithName("katas").Build()]);

        orphelins.ShouldBe(["depot-renomme"]);
    }

    [Fact]
    public void FromJson_QuandLeJsonEstInvalide_Leve()
    {
        // Le fichier est versionné avec le code : mieux vaut un démarrage en échec qu'un
        // site qui montrerait un dépôt que ce fichier était seul à masquer.
        Should.Throw<JsonException>(() => EditorialCatalog.FromJson("{ pas du json"));
    }

    [Fact]
    public void FromJson_ToleLesCommentairesEtLesVirgulesFinales()
    {
        // Le fichier est écrit à la main : ces deux tolérances évitent un démarrage en
        // échec pour une virgule.
        var catalogue = EditorialCatalog.FromJson(
            """
            {
              // le dépôt de la vitrine
              "repositories": { "katas": { "featured": true }, }
            }
            """);

        catalogue.IsEmpty.ShouldBeFalse();
    }
}
