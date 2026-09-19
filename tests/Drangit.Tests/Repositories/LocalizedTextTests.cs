using System.Globalization;
using Drangit.Web.Repositories;
using Shouldly;

namespace Drangit.Tests.Repositories;

public sealed class LocalizedTextTests
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void For_QuandLaLangueDemandeeEstRenseignee_LaRetourne()
    {
        var text = new LocalizedText(English: "A space shooter", French: "Un shoot spatial");

        text.For(French).ShouldBe("Un shoot spatial");
        text.For(English).ShouldBe("A space shooter");
    }

    [Fact]
    public void For_QuandLaTraductionFrancaiseManque_RetombeSurLAnglais()
    {
        var text = new LocalizedText(English: "A space shooter", French: null);

        text.For(French).ShouldBe("A space shooter");
    }

    [Fact]
    public void For_QuandLAnglaisManque_RetombeSurLeFrancais()
    {
        var text = new LocalizedText(English: null, French: "Un shoot spatial");

        text.For(English).ShouldBe("Un shoot spatial");
    }

    [Fact]
    public void For_QuandUneTraductionEstVide_LaTraiteCommeAbsente()
    {
        var text = new LocalizedText(English: "A space shooter", French: "   ");

        text.For(French).ShouldBe("A space shooter");
    }

    [Fact]
    public void For_QuandAucuneTraductionNExiste_RetourneNull()
    {
        var text = new LocalizedText(English: null, French: null);

        text.For(French).ShouldBeNull();
        text.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void For_SuitLaLangueEtNonLePays()
    {
        var text = new LocalizedText(English: "Hello", French: "Bonjour");

        text.For(CultureInfo.GetCultureInfo("fr-CA")).ShouldBe("Bonjour");
        text.For(CultureInfo.GetCultureInfo("en-GB")).ShouldBe("Hello");
    }
}
