using Drangit.Tests.Builders;
using Drangit.Web.Repositories;
using Shouldly;

namespace Drangit.Tests.Repositories;

public sealed class RepositoryListingTests
{
    private static IReadOnlyList<Repository> Catalogue(int count) =>
        [.. Enumerable.Range(1, count).Select(index => new RepositoryBuilder().WithName($"depot-{index}").Build())];

    [Fact]
    public void FirstPageOf_QuandLeCatalogueTientDansLaPage_LeRendEntier()
    {
        var catalogue = Catalogue(3);

        RepositoryListing.FirstPageOf(catalogue, 12).Count.ShouldBe(3);
    }

    [Fact]
    public void FirstPageOf_QuandLeCatalogueDepasse_NeRendQueLeDebut()
    {
        var catalogue = Catalogue(47);

        var page = RepositoryListing.FirstPageOf(catalogue, 12);

        page.Count.ShouldBe(12);
        page[0].Name.ShouldBe("depot-1");
        page[^1].Name.ShouldBe("depot-12");
    }

    [Fact]
    public void FirstPageOf_NeTrieRien()
    {
        // L'ordre est décidé ailleurs — le dépôt désigné ouvre la liste. Couper ne doit pas
        // le défaire.
        var catalogue = Catalogue(20);

        RepositoryListing.FirstPageOf(catalogue, 5)
            .Select(repository => repository.Name)
            .ShouldBe(["depot-1", "depot-2", "depot-3", "depot-4", "depot-5"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FirstPageOf_QuandLaTailleNEnEstPasUne_RendToutPlutotQueRien(int size)
    {
        // Une taille absurde ne doit pas produire une page vide : une liste vide passerait
        // pour un catalogue vide, ce qui n'est pas le cas.
        RepositoryListing.FirstPageOf(Catalogue(4), size).Count.ShouldBe(4);
    }
}
