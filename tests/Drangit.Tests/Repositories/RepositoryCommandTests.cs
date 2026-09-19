using Drangit.Tests.Builders;
using Drangit.Web.Repositories;
using Shouldly;

namespace Drangit.Tests.Repositories;

public sealed class RepositoryCommandTests
{
    private static readonly IReadOnlyList<Repository> Catalogue =
    [
        new RepositoryBuilder().WithName("Algorithme-de-Huffman").WithLanguage("C#").Build(),
        new RepositoryBuilder().WithName("huffman-algorithm-flutter").WithLanguage("Dart").Build(),
        new RepositoryBuilder().WithName("ASM-Gameboy-FirstProg").WithLanguage("Assembly").Build(),
    ];

    private static RepositoryCommand Parse(string ligne) => RepositoryCommand.Parse(ligne, Catalogue);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ls")]
    [InlineData("repos --list")]
    public void Parse_SansCritere_RendLeCatalogueEntier(string ligne)
    {
        var commande = Parse(ligne).ShouldBeOfType<RepositoryCommand.Filter>();

        commande.Value.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Parse_UnDrapeauParCritere_LesCombineTous()
    {
        var commande = Parse("ls --topic=unity --lang=C# --demo --no-archived")
            .ShouldBeOfType<RepositoryCommand.Filter>();

        commande.Value.Topic.ShouldBe("unity");
        commande.Value.Language.ShouldBe("C#");
        commande.Value.WithDemo.ShouldBeTrue();
        commande.Value.HideArchived.ShouldBeTrue();
    }

    [Fact]
    public void Parse_UnMotQuiNEstPasUneCommande_ChercheDansLeCatalogue()
    {
        // Le prompt double la barre de recherche : ce qui n'est pas une commande est ce
        // qu'on cherche.
        var commande = Parse("tetris").ShouldBeOfType<RepositoryCommand.Filter>();

        commande.Value.SearchTerm.ShouldBe("tetris");
    }

    [Fact]
    public void Parse_find_ChercheToutCeQuiSuit()
    {
        var commande = Parse("find codage de huffman").ShouldBeOfType<RepositoryCommand.Filter>();

        commande.Value.SearchTerm.ShouldBe("codage de huffman");
    }

    [Fact]
    public void Parse_UnTermeEntreGuillemets_LeReconstitueEnUnSeulCritere()
    {
        var commande = Parse("\"game dev\"").ShouldBeOfType<RepositoryCommand.Filter>();

        commande.Value.SearchTerm.ShouldBe("game dev");
    }

    [Fact]
    public void Parse_open_QuandLeNomNeDesigneQuUnDepot_OuvreSaFiche()
    {
        var commande = Parse("open asm").ShouldBeOfType<RepositoryCommand.Open>();

        commande.Slug.Value.ShouldBe("asm-gameboy-firstprog");
    }

    [Fact]
    public void Parse_open_QuandLeNomEstExact_LEmporteSurUneCorrespondancePartielle()
    {
        // « huffman-algorithm-flutter » contient aussi le terme : sans cette règle, un nom
        // écrit en entier resterait ambigu.
        var commande = Parse("open huffman-algorithm-flutter").ShouldBeOfType<RepositoryCommand.Open>();

        commande.Slug.Value.ShouldBe("huffman-algorithm-flutter");
    }

    [Fact]
    public void Parse_open_QuandLeNomDesignePlusieursDepots_BasculeEnRecherche()
    {
        // Deux dépôts portent « huffman ». Plutôt qu'une erreur, on montre les candidats :
        // le visiteur choisit dans la liste, ce qu'il aurait fait de toute façon.
        var commande = Parse("open huffman").ShouldBeOfType<RepositoryCommand.Filter>();

        commande.Value.SearchTerm.ShouldBe("huffman");
    }

    [Fact]
    public void Parse_open_QuandAucunDepotNeCorrespond_BasculeEnRecherche()
    {
        var commande = Parse("open inexistant").ShouldBeOfType<RepositoryCommand.Filter>();

        commande.Value.SearchTerm.ShouldBe("inexistant");
    }

    [Theory]
    [InlineData("cd about")]
    [InlineData("cd a-propos")]
    public void Parse_cd_VersLaPageAPropos_YEnvoie(string ligne)
    {
        Parse(ligne).ShouldBeOfType<RepositoryCommand.GoToPage>().Page.ShouldBe(SitePage.About);
    }

    [Theory]
    [InlineData("cd /")]
    [InlineData("cd ~")]
    [InlineData("cd ..")]
    public void Parse_cd_VersLaRacine_RevientALAccueil(string ligne)
    {
        Parse(ligne).ShouldBeOfType<RepositoryCommand.GoToPage>().Page.ShouldBe(SitePage.Home);
    }

    [Fact]
    public void Parse_cd_VersUnDepot_OuvreSaFiche()
    {
        // « cd » sur un dossier de dépôt fait ce qu'on en attend.
        Parse("cd asm").ShouldBeOfType<RepositoryCommand.Open>()
            .Slug.Value.ShouldBe("asm-gameboy-firstprog");
    }

    [Fact]
    public void Parse_EstLInverseDeToCommandLine()
    {
        // Les deux sens doivent rester d'accord : la ligne affichée au-dessus de la barre de
        // filtres est exactement celle qu'on pourrait retaper.
        var filtre = new RepositoryFilter
        {
            Topic = "unity",
            Language = "C#",
            SearchTerm = "game dev",
            WithDemo = true,
            HideArchived = true,
        };

        var relu = RepositoryCommand.Parse(filtre.ToCommandLine(), Catalogue)
            .ShouldBeOfType<RepositoryCommand.Filter>();

        relu.Value.ShouldBe(filtre);
    }
}
