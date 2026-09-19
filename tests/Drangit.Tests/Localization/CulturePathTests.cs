using Drangit.Web.Localization;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace Drangit.Tests.Localization;

/// <summary>La langue telle qu'elle se lit — ou non — au début d'un chemin.</summary>
public sealed class CulturePathTests
{
    [Theory]
    [InlineData("/fr/repos/katas", "fr", "/repos/katas")]
    [InlineData("/en/about", "en", "/about")]
    [InlineData("/fr", "fr", "/")]
    [InlineData("/fr/", "fr", "/")]
    public void TryDetach_QuandLeCheminCommenceParUneLangue_LaDetacheDuReste(
        string chemin,
        string langueAttendue,
        string resteAttendu)
    {
        var detache = CulturePath.TryDetach(new PathString(chemin), out var langue, out var reste);

        detache.ShouldBeTrue();
        langue.ShouldBe(langueAttendue);
        reste.Value.ShouldBe(resteAttendu);
    }

    [Fact]
    public void TryDetach_IgnoreLaCasseEtNormaliseLaLangue()
    {
        var detache = CulturePath.TryDetach(new PathString("/FR/about"), out var langue, out var reste);

        detache.ShouldBeTrue();
        langue.ShouldBe("fr");
        reste.Value.ShouldBe("/about");
    }

    [Theory]
    [InlineData("/repos/katas")]
    [InlineData("/about")]
    [InlineData("/health")]
    [InlineData("/")]
    public void TryDetach_QuandLeCheminNePorteAucuneLangue_LeLaisseIntact(string chemin)
    {
        var detache = CulturePath.TryDetach(new PathString(chemin), out _, out var reste);

        detache.ShouldBeFalse();
        reste.Value.ShouldBe(chemin);
    }

    [Theory]
    [InlineData("https://exemple.test/fr/", "https://exemple.test/")]
    [InlineData("https://exemple.test/en/", "https://exemple.test/")]
    [InlineData("https://exemple.test/", "https://exemple.test/")]
    public void SiteRootOf_OteLePrefixeDeLangueDeLaBase(string baseUri, string attendue)
    {
        // Les adresses des autres langues se construisent depuis la racine du site, pas
        // depuis la base de la page courante — qui porte déjà une langue.
        CulturePath.SiteRootOf(new Uri(baseUri)).ToString().ShouldBe(attendue);
    }

    [Theory]
    [InlineData("/french/toast")]
    [InlineData("/enigme")]
    [InlineData("/frontend")]
    public void TryDetach_NAcceptePasUnSegmentQuiCommenceSeulementParUneLangue(string chemin)
    {
        // « /french » n'est pas « /fr » suivi d'autre chose : la comparaison porte sur le
        // segment entier, faute de quoi une page nommée ainsi deviendrait inatteignable.
        var detache = CulturePath.TryDetach(new PathString(chemin), out _, out var reste);

        detache.ShouldBeFalse();
        reste.Value.ShouldBe(chemin);
    }
}
