using Drangit.Web;
using Drangit.Web.Repositories.GitHub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Drangit.Tests.Configuration;

/// <summary>
/// Le déploiement transmet toutes les clés du modèle, renseignées ou non. Une clé
/// facultative laissée vide ne doit pas empêcher le site de démarrer ; une clé erronée,
/// elle, doit le faire échouer tout de suite plutôt qu'à la première visite.
/// </summary>
public sealed class OptionsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ItchProfileUrl_QuandLaConfigurationEstVide_EstConsidereAbsentSansEmpecherLeDemarrage(string valeur)
    {
        var options = Construire<SiteOptions>(SiteOptions.SectionName, ("Site:ItchProfileUrl", valeur));

        Should.NotThrow(() => options.Value);
        options.Value.ItchProfileUrl.ShouldBeNull();
    }

    [Fact]
    public void ItchProfileUrl_QuandLAdresseEstInvalide_FaitEchouerLeDemarrage()
    {
        var options = Construire<SiteOptions>(SiteOptions.SectionName, ("Site:ItchProfileUrl", "pas-une-adresse"));

        Should.Throw<OptionsValidationException>(() => options.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Token_QuandLeJetonEstVide_EstConsidereAbsent(string valeur)
    {
        // Un en-tête « Bearer » vide vaudrait un 401 ; l'absence de jeton vaut un appel
        // anonyme parfaitement valide (ADR 0003).
        var options = Construire<GitHubOptions>(GitHubOptions.SectionName, ("GitHub:Token", valeur));

        options.Value.Token.ShouldBeNull();
    }

    [Fact]
    public void Token_EstDebarrasseDesEspacesDeBordure()
    {
        // Un jeton copié-collé traîne souvent un saut de ligne : dans un en-tête HTTP, il
        // ferait échouer la requête avant même d'atteindre GitHub.
        var options = Construire<GitHubOptions>(GitHubOptions.SectionName, ("GitHub:Token", " jeton \n"));

        options.Value.Token.ShouldBe("jeton");
    }

    [Theory]
    [InlineData("drangoht")]
    [InlineData("a")]
    [InlineData("nom-avec-tirets")]
    public void Login_AccepteUnIdentifiantGitHubValide(string login)
    {
        var options = Construire<GitHubOptions>(GitHubOptions.SectionName, ("GitHub:Login", login));

        Should.NotThrow(() => options.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-commence-par-un-tiret")]
    [InlineData("finit-par-un-tiret-")]
    [InlineData("deux--tirets")]
    [InlineData("espace dedans")]
    public void Login_QuandLIdentifiantEstImpossible_FaitEchouerLeDemarrage(string login)
    {
        // GitHub répondrait 404 : mieux vaut un conteneur qui refuse de démarrer qu'une
        // vitrine vide servie en vert pendant des semaines.
        var options = Construire<GitHubOptions>(GitHubOptions.SectionName, ("GitHub:Login", login));

        Should.Throw<OptionsValidationException>(() => options.Value);
    }

    [Fact]
    public void CacheDuration_QuandLaDureeEstAberrante_FaitEchouerLeDemarrage()
    {
        // Un cache d'une seconde épuiserait le quota d'API en quelques minutes.
        var options = Construire<GitHubOptions>(GitHubOptions.SectionName, ("GitHub:CacheDuration", "00:00:01"));

        Should.Throw<OptionsValidationException>(() => options.Value);
    }

    private static IOptions<TOptions> Construire<TOptions>(
        string section,
        params (string Cle, string Valeur)[] reglages)
        where TOptions : class
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(reglages.Select(r => new KeyValuePair<string, string?>(r.Cle, r.Valeur)))
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(section))
            .ValidateDataAnnotations();

        return services.BuildServiceProvider().GetRequiredService<IOptions<TOptions>>();
    }
}
