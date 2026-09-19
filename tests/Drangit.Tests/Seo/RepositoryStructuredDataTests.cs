using System.Text.Json;
using Drangit.Tests.Builders;
using Drangit.Web.Seo;
using Shouldly;

namespace Drangit.Tests.Seo;

public sealed class RepositoryStructuredDataTests
{
    private static readonly Uri PageUrl = new("https://drangit.example/fr/repos/katas");

    [Fact]
    public void ToJsonLd_DecritLeDepotCommeDuCodeSource()
    {
        var depot = new RepositoryBuilder().WithName("katas").WithLanguage("C#").Build();

        var document = Parse(RepositoryStructuredData.ToJsonLd(depot, PageUrl, "Recueil de katas"));

        document.GetProperty("@type").GetString().ShouldBe("SoftwareSourceCode");
        document.GetProperty("name").GetString().ShouldBe("katas");
        document.GetProperty("url").GetString().ShouldBe(PageUrl.ToString());
        document.GetProperty("codeRepository").GetString().ShouldBe("https://github.com/drangoht/katas");
        document.GetProperty("programmingLanguage").GetString().ShouldBe("C#");
        document.GetProperty("description").GetString().ShouldBe("Recueil de katas");
    }

    [Fact]
    public void ToJsonLd_OmetLesProprietesQueLeDepotNeRenseignePas()
    {
        var depot = new RepositoryBuilder().WithLanguage(null).WithLicense(null).WithPushedAt(null).Build();

        var document = Parse(RepositoryStructuredData.ToJsonLd(depot, PageUrl, description: null));

        // Une propriété vide affirmerait que le dépôt n'a ni langage ni licence.
        document.TryGetProperty("programmingLanguage", out _).ShouldBeFalse();
        document.TryGetProperty("license", out _).ShouldBeFalse();
        document.TryGetProperty("dateModified", out _).ShouldBeFalse();
        document.TryGetProperty("description", out _).ShouldBeFalse();
    }

    [Fact]
    public void ToJsonLd_DesigneLaLicenceParSonAdresseSpdx()
    {
        var depot = new RepositoryBuilder().WithLicense("MIT").Build();

        Parse(RepositoryStructuredData.ToJsonLd(depot, PageUrl, null))
            .GetProperty("license").GetString().ShouldBe("https://spdx.org/licenses/MIT");
    }

    [Fact]
    public void ToJsonLd_EchappeLesChevronsDUnNomDeDepot()
    {
        // Le document est inséré dans une balise <script> : un nom malicieux ne doit pas
        // pouvoir la refermer.
        var depot = new RepositoryBuilder().WithName("</script><b>").Build();

        RepositoryStructuredData.ToJsonLd(depot, PageUrl, null).ShouldNotContain("</script>");
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
