using Pofus.Core.Accounts;

namespace Pofus.Core.Tests.Accounts;

public class ClassColorPaletteTests
{
    [Fact]
    public void ForClassName_IsDeterministic_ForTheSameClassName()
    {
        var first = ClassColorPalette.ForClassName("Ouginak");
        var second = ClassColorPalette.ForClassName("Ouginak");

        Assert.Equal(first, second);
    }

    [Fact]
    public void ForClassName_ReturnsDifferentColors_ForDifferentClasses()
    {
        var ouginak = ClassColorPalette.ForClassName("Ouginak");
        var eliotrope = ClassColorPalette.ForClassName("Eliotrope");

        Assert.NotEqual(ouginak, eliotrope);
    }

    [Fact]
    public void ForClassName_HandlesUnknownClassName_WithoutThrowing()
    {
        var exception = Record.Exception(() => ClassColorPalette.ForClassName(AccountTitleParser.UnknownClassName));

        Assert.Null(exception);
    }
}
