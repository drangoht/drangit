using Drangit.Tests.Builders;
using Drangit.Web.Components;
using Shouldly;

namespace Drangit.Tests.Components;

public sealed class RepositoryVisualTests
{
    [Fact]
    public void AccentOf_ReprendLaCouleurDuLangage()
    {
        RepositoryVisual.AccentOf("C#").ShouldBe("#178600");
        RepositoryVisual.AccentOf("c#").ShouldBe("#178600");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Brainfuck")]
    public void AccentOf_QuandLeLangageEstInconnuOuAbsent_RetombeSurLaTeinteNeutre(string? langage)
    {
        // Un langage hors table n'est pas un bug : un dépôt vide n'en a aucun.
        RepositoryVisual.AccentOf(langage).ShouldBe("#8b96a8");
    }

    [Fact]
    public void AngleOf_EstStableDUnDemarrageALAutre()
    {
        // string.GetHashCode est rendu aléatoire à chaque démarrage du processus : s'en
        // servir ferait changer l'aspect de chaque vignette à chaque redéploiement, y
        // compris dans les caches des navigateurs. Cette valeur est donc figée ici exprès.
        RepositoryVisual.AngleOf("BankingKata").ShouldBe(RepositoryVisual.AngleOf("BankingKata"));
        RepositoryVisual.AngleOf("BankingKata").ShouldBe(14);
    }

    [Fact]
    public void AngleOf_DistingueDeuxDepotsDuMemeLangage()
    {
        RepositoryVisual.AngleOf("BankingKata").ShouldNotBe(RepositoryVisual.AngleOf("SocialNetworkKata"));
    }

    [Theory]
    [InlineData("AdventOfCode")]
    [InlineData("go-training")]
    [InlineData("")]
    public void AngleOf_ResteUnAngleValide(string nom)
    {
        RepositoryVisual.AngleOf(nom).ShouldBeInRange(0, 359);
    }

    [Fact]
    public void InkOf_QuandLaTeinteDuLangageSeLitSurLaVignette_LaConserve()
    {
        // La vignette est sombre : une teinte assez claire y sert telle quelle, à l'écrit
        // comme à la pastille.
        RepositoryVisual.InkOf("C#").ShouldBe("#178600");
        RepositoryVisual.InkOf("Go").ShouldBe("#00ADD8");
    }

    [Theory]
    [InlineData("PowerShell", "#8091aa")]
    [InlineData("Lua", "#7f7fbf")]
    [InlineData("C", "#aaaaaa")]
    public void InkOf_QuandLaTeinteEstTropSombrePourLaVignette_LEclaircit(string langage, string attendu)
    {
        // #012456 sur un fond presque noir ne se voit pas : le prompt disparaît et la
        // pastille du langage avec lui. La teinte garde sa dominante, éclaircie de moitié.
        RepositoryVisual.InkOf(langage).ShouldBe(attendu);
    }

    [Fact]
    public void InkOf_EclairciAssezPourResterLisible()
    {
        // Le pire cas possible : la teinte la plus sombre qui soit.
        RepositoryVisual.InkOf("Lua").ShouldNotBe(RepositoryVisual.AccentOf("Lua"));
    }

    [Fact]
    public void StyleOf_PoseLesTroisVariablesAttenduesParLaFeuilleDeStyle()
    {
        var depot = new RepositoryBuilder().WithName("BankingKata").WithLanguage("C#").Build();

        var style = RepositoryVisual.StyleOf(depot);

        style.ShouldBe("--cover-accent: #178600; --cover-ink: #178600; --cover-angle: 14deg");
    }

    [Fact]
    public void StyleOf_QuandLeLangageEstSombre_LaTeinteEcriteDiffereDeLaTeinteDuFond()
    {
        var depot = new RepositoryBuilder().WithName("BankingKata").WithLanguage("PowerShell").Build();

        var style = RepositoryVisual.StyleOf(depot);

        style.ShouldBe("--cover-accent: #012456; --cover-ink: #8091aa; --cover-angle: 14deg");
    }
}
