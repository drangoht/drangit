using Drangit.Tests.Builders;
using Drangit.Web.Repositories;
using Shouldly;

namespace Drangit.Tests.Repositories;

public sealed class FeaturedRepositoryTests
{
    [Fact]
    public void Of_QuandLeCatalogueEstVide_NeDesigneRien()
    {
        FeaturedRepository.Of([]).ShouldBeNull();
    }

    [Fact]
    public void Of_LaDesignationEditorialePrimeSurToutLeReste()
    {
        var designe = new RepositoryBuilder().WithName("choisi").Featured().Build();
        var populaire = new RepositoryBuilder().WithName("populaire").WithStars(500).WithDemo().Build();

        FeaturedRepository.Of([populaire, designe]).ShouldBe(designe);
    }

    [Fact]
    public void Of_ADefautDeDesignation_PrefereUnDepotAvecDemonstration()
    {
        var avecDemo = new RepositoryBuilder().WithName("avec-demo").WithDemo().Build();
        var sansDemo = new RepositoryBuilder().WithName("sans-demo").Build();

        FeaturedRepository.Of([sansDemo, avecDemo]).ShouldBe(avecDemo);
    }

    [Fact]
    public void Of_ADemonstrationEgale_PrefereLePlusEtoile()
    {
        var discret = new RepositoryBuilder().WithName("discret").WithStars(1).Build();
        var populaire = new RepositoryBuilder().WithName("populaire").WithStars(9).Build();

        FeaturedRepository.Of([discret, populaire]).ShouldBe(populaire);
    }

    [Fact]
    public void Of_NeMetEnVitrineUnDepotArchiveQueSILNYARienDAutre()
    {
        var archive = new RepositoryBuilder().WithName("archive").WithStars(99).WithDemo().Archived().Build();
        var actif = new RepositoryBuilder().WithName("actif").Build();

        // La page d'accueil ne doit pas donner l'impression d'un compte à l'abandon.
        FeaturedRepository.Of([archive, actif]).ShouldBe(actif);
        FeaturedRepository.Of([archive]).ShouldBe(archive);
    }
}
