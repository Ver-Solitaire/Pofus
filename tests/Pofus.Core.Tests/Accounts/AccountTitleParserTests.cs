using Pofus.Core.Accounts;

namespace Pofus.Core.Tests.Accounts;

public class AccountTitleParserTests
{
    [Fact]
    public void Parse_ExtractsPseudoAndClassName_FromRealClientTitleFormat()
    {
        var result = AccountTitleParser.Parse("Mon-Perso - Ouginak - 3.6.10.10 - Release");

        Assert.NotNull(result);
        Assert.Equal("Mon-Perso", result!.Pseudo);
        Assert.Equal("Ouginak", result.ClassName);
    }

    [Fact]
    public void Parse_ReturnsUnknownClassName_WhenClassPartIsMissing()
    {
        var result = AccountTitleParser.Parse("MonPersonnage");

        Assert.NotNull(result);
        Assert.Equal("MonPersonnage", result!.Pseudo);
        Assert.Equal(AccountTitleParser.UnknownClassName, result.ClassName);
    }

    [Theory]
    [InlineData("Dofus")]
    [InlineData("dofus")]
    [InlineData("Dofus Launcher")]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_ReturnsNull_ForLauncherOrEmptyTitles(string title)
    {
        Assert.Null(AccountTitleParser.Parse(title));
    }

    [Fact]
    public void Parse_TrimsWhitespace_AroundPseudoAndClassName()
    {
        var result = AccountTitleParser.Parse("  MonPersonnage   -   Iop  ");

        Assert.NotNull(result);
        Assert.Equal("MonPersonnage", result!.Pseudo);
        Assert.Equal("Iop", result.ClassName);
    }
}
