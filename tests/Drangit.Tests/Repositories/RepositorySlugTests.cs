using Drangit.Web.Repositories;
using Shouldly;

namespace Drangit.Tests.Repositories;

public sealed class RepositorySlugTests
{
    [Fact]
    public void FromName_NormaliseLaCasse()
    {
        // Sans normalisation, /repos/AdventOfCode et /repos/adventofcode seraient deux
        // adresses pour la même page — donc deux pages indexées pour un seul dépôt.
        RepositorySlug.FromName("AdventOfCode", 1).Value.ShouldBe("adventofcode");
    }

    [Fact]
    public void FromName_ConserveLesTiretsEtLesPoints()
    {
        RepositorySlug.FromName("Algorithme-de-Huffman", 1).Value.ShouldBe("algorithme-de-huffman");
        RepositorySlug.FromName("Benchmark.Practice", 1).Value.ShouldBe("benchmark.practice");
    }

    [Fact]
    public void FromName_IgnoreLesEspacesDeBordure()
    {
        RepositorySlug.FromName("  katas  ", 1).Value.ShouldBe("katas");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromName_QuandLeNomEstAbsent_RetombeSurLIdentifiant(string? nom)
    {
        // Une route moins jolie vaut mieux qu'un dépôt inatteignable.
        RepositorySlug.FromName(nom, 4242).Value.ShouldBe("4242");
    }

    [Fact]
    public void ToString_RendLaValeurTextuelle()
    {
        RepositorySlug.FromName("Drangit", 1).ToString().ShouldBe("drangit");
    }
}
