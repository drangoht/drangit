using Drangit.Tests.Builders;
using Drangit.Web.Repositories;
using Shouldly;

namespace Drangit.Tests.Repositories;

public sealed class RepositoryFilterTests
{
    private static readonly Repository Huffman = new RepositoryBuilder()
        .WithName("Algorithme-de-Huffman")
        .WithTopics("algorithms", "web")
        .WithLanguage("HTML")
        .WithDemo()
        .WithDescription(new LocalizedText("A tough compression exercise", "Un exercice de compression exigeant"))
        .Build();

    private static readonly Repository Katas = new RepositoryBuilder()
        .WithName("BankingKata")
        .WithTopics("katas", "tdd")
        .WithLanguage("C#")
        .WithDescription(LocalizedText.FromEnglish("Banking kata, test-driven"))
        .Build();

    private static readonly Repository Vieux = new RepositoryBuilder()
        .WithName("ASM-Gameboy-FirstProg")
        .WithTopics("retro")
        .WithLanguage("Assembly")
        .Archived()
        .Build();

    private static readonly Repository[] Tous = [Huffman, Katas, Vieux];

    [Fact]
    public void Apply_QuandAucunCritereNEstPose_RetourneToutLeCatalogue()
    {
        RepositoryFilter.Empty.Apply(Tous).ShouldBe(Tous);
    }

    [Fact]
    public void Apply_FiltreParSujetSansTenirCompteDeLaCasse()
    {
        var filtre = RepositoryFilter.Empty with { Topic = "ALGORITHMS" };

        filtre.Apply(Tous).ShouldHaveSingleItem().Name.ShouldBe("Algorithme-de-Huffman");
    }

    [Fact]
    public void Apply_FiltreParLangage()
    {
        var filtre = RepositoryFilter.Empty with { Language = "c#" };

        filtre.Apply(Tous).ShouldHaveSingleItem().Name.ShouldBe("BankingKata");
    }

    [Fact]
    public void Apply_ChercheDansLeNom()
    {
        var filtre = RepositoryFilter.Empty with { SearchTerm = "gameboy" };

        filtre.Apply(Tous).ShouldHaveSingleItem().Name.ShouldBe("ASM-Gameboy-FirstProg");
    }

    [Fact]
    public void Apply_ChercheAussiDansLaDescriptionDansLesDeuxLangues()
    {
        (RepositoryFilter.Empty with { SearchTerm = "compression" }).Apply(Tous)
            .ShouldHaveSingleItem().Name.ShouldBe("Algorithme-de-Huffman");

        (RepositoryFilter.Empty with { SearchTerm = "test-driven" }).Apply(Tous)
            .ShouldHaveSingleItem().Name.ShouldBe("BankingKata");
    }

    [Fact]
    public void Apply_ChercheSansTenirCompteDesAccents()
    {
        // Un visiteur qui tape « exigeant » depuis un clavier sans accents doit trouver.
        var filtre = RepositoryFilter.Empty with { SearchTerm = "exigeant" };

        filtre.Apply(Tous).ShouldHaveSingleItem().Name.ShouldBe("Algorithme-de-Huffman");
    }

    [Fact]
    public void Apply_ChercheDansLesSujets()
    {
        (RepositoryFilter.Empty with { SearchTerm = "tdd" }).Apply(Tous)
            .ShouldHaveSingleItem().Name.ShouldBe("BankingKata");
    }

    [Fact]
    public void Apply_NeGardeQueLesDepotsAvecDemoQuandCEstDemande()
    {
        var filtre = RepositoryFilter.Empty with { WithDemo = true };

        filtre.Apply(Tous).ShouldBe([Huffman]);
    }

    [Fact]
    public void Apply_EcarteLesDepotsArchivesQuandCEstDemande()
    {
        var filtre = RepositoryFilter.Empty with { HideArchived = true };

        filtre.Apply(Tous).ShouldBe([Huffman, Katas]);
    }

    [Fact]
    public void Apply_CombineLesCriteresParEt()
    {
        var filtre = RepositoryFilter.Empty with { Topic = "web", Language = "C#" };

        filtre.Apply(Tous).ShouldBeEmpty();
    }

    [Fact]
    public void Apply_QuandLeTermeEstFaitDEspaces_LeTraiteCommeAbsent()
    {
        (RepositoryFilter.Empty with { SearchTerm = "   " }).Apply(Tous).ShouldBe(Tous);
    }

    [Fact]
    public void Apply_PreserveLOrdreDuCatalogue()
    {
        var filtre = RepositoryFilter.Empty with { HideArchived = true };

        filtre.Apply(Tous).ShouldBe([Huffman, Katas]);
    }

    [Fact]
    public void IsActive_DistingueUnFiltrePoseDUnFiltreVide()
    {
        RepositoryFilter.Empty.IsActive.ShouldBeFalse();
        (RepositoryFilter.Empty with { Topic = "katas" }).IsActive.ShouldBeTrue();
        (RepositoryFilter.Empty with { WithDemo = true }).IsActive.ShouldBeTrue();
        (RepositoryFilter.Empty with { HideArchived = true }).IsActive.ShouldBeTrue();
        (RepositoryFilter.Empty with { SearchTerm = " " }).IsActive.ShouldBeFalse();
    }

    [Fact]
    public void AvailableTopicsOf_ClasseLesSujetsDuPlusFrequentAuPlusRare()
    {
        var catalogue = new[]
        {
            new RepositoryBuilder().WithName("a").WithTopics("csharp", "tdd").Build(),
            new RepositoryBuilder().WithName("b").WithTopics("csharp").Build(),
            new RepositoryBuilder().WithName("c").WithTopics("rust").Build(),
        };

        // « rust » et « tdd » sont à égalité : l'ordre alphabétique départage, sans quoi la
        // barre de filtres changerait d'un rafraîchissement à l'autre.
        RepositoryFilter.AvailableTopicsOf(catalogue)
            .Select(facet => facet.Value)
            .ShouldBe(["csharp", "rust", "tdd"]);
    }

    [Fact]
    public void AvailableTopicsOf_CompteLesDepotsDeChaqueSujet()
    {
        var catalogue = new[]
        {
            new RepositoryBuilder().WithName("a").WithTopics("csharp", "tdd").Build(),
            new RepositoryBuilder().WithName("b").WithTopics("csharp").Build(),
        };

        // Sans ce compte affiché, on clique sur un critère sans savoir s'il rendra vingt
        // dépôts ou un seul — et un catalogue peu étiqueté passe pour un filtre cassé.
        RepositoryFilter.AvailableTopicsOf(catalogue).ShouldBe(
        [
            new RepositoryFacet("csharp", 2),
            new RepositoryFacet("tdd", 1),
        ]);
    }

    [Fact]
    public void AvailableTopicsOf_NeCompteQuUneFoisUnSujetEcritDeDeuxFacons()
    {
        var catalogue = new[]
        {
            new RepositoryBuilder().WithName("a").WithTopics("DotNet").Build(),
            new RepositoryBuilder().WithName("b").WithTopics("dotnet").Build(),
        };

        // La casse ne crée pas deux sujets : le compte doit dire deux dépôts, pas deux fois un.
        RepositoryFilter.AvailableTopicsOf(catalogue)
            .ShouldHaveSingleItem()
            .Count.ShouldBe(2);
    }

    [Fact]
    public void AvailableLanguagesOf_ListeLesLangagesTriesAvecLeurCompte()
    {
        RepositoryFilter.AvailableLanguagesOf(Tous).ShouldBe(
        [
            new RepositoryFacet("Assembly", 1),
            new RepositoryFacet("C#", 1),
            new RepositoryFacet("HTML", 1),
        ]);
    }

    [Fact]
    public void AvailableLanguagesOf_IgnoreLesDepotsSansLangage()
    {
        var catalogue = new[] { new RepositoryBuilder().WithLanguage(null).Build() };

        RepositoryFilter.AvailableLanguagesOf(catalogue).ShouldBeEmpty();
    }

    [Fact]
    public void CountWithDemo_EtCountArchived_DisentSiLaBasculeSertAQuelqueChose()
    {
        // Une bascule qui retiendrait tout le catalogue, ou rien, n'est pas proposée.
        RepositoryFilter.CountWithDemo(Tous).ShouldBe(1);
        RepositoryFilter.CountArchived(Tous).ShouldBe(1);
        RepositoryFilter.CountArchived([]).ShouldBe(0);
    }

    [Fact]
    public void ToCommandLine_SansCritere_ListeToutLeCatalogue()
    {
        RepositoryFilter.Empty.ToCommandLine().ShouldBe("--list");
    }

    [Fact]
    public void ToCommandLine_EcritUnDrapeauParCritere()
    {
        var filtre = new RepositoryFilter
        {
            Topic = "unity",
            Language = "C#",
            WithDemo = true,
            HideArchived = true,
        };

        // L'ordre est celui de la barre de filtres, pour que la ligne se lise comme ce que
        // le visiteur vient de cliquer.
        filtre.ToCommandLine().ShouldBe("--topic=unity --lang=C# --demo --no-archived");
    }

    [Fact]
    public void ToCommandLine_QuandLeTermeContientUneEspace_LeMetEntreGuillemets()
    {
        var filtre = new RepositoryFilter { SearchTerm = "game dev" };

        // Sans guillemets, la ligne affichée ne serait pas celle qu'on pourrait retaper.
        filtre.ToCommandLine().ShouldBe("\"game dev\"");
    }

    [Fact]
    public void ToCommandLine_EcritLeTermeDeRechercheEnDernier()
    {
        var filtre = new RepositoryFilter { SearchTerm = "huffman", Topic = "algorithms" };

        filtre.ToCommandLine().ShouldBe("--topic=algorithms huffman");
    }
}
