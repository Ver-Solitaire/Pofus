using Pofus.Core.Navigation;

namespace Pofus.Core.Tests.Navigation;

public class KeyComboTests
{
    [Fact]
    public void TryParse_ParsesModifiersAndKey()
    {
        var combo = KeyCombo.TryParse("Ctrl+Shift+Tab");

        Assert.NotNull(combo);
        Assert.Equal(KeyModifiers.Control | KeyModifiers.Shift, combo!.Modifiers);
        Assert.Equal("Tab", combo.Key);
        Assert.Equal(0x09u, combo.VirtualKeyCode);
    }

    [Fact]
    public void TryParse_AcceptsFrenchShiftAlias()
    {
        var combo = KeyCombo.TryParse("Ctrl+Maj+Tab");

        Assert.NotNull(combo);
        Assert.Equal(KeyModifiers.Control | KeyModifiers.Shift, combo!.Modifiers);
    }

    [Fact]
    public void TryParse_SingleLetterKey_ProducesMatchingVirtualKeyCode()
    {
        var combo = KeyCombo.TryParse("Ctrl+L");

        Assert.NotNull(combo);
        Assert.Equal("L", combo!.Key);
        Assert.Equal((uint)'L', combo.VirtualKeyCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ctrl+")]
    [InlineData("Ctrl+UnknownKey")]
    [InlineData("Ctrl+Tab+L")]
    public void TryParse_ReturnsNull_ForInvalidInput(string text)
    {
        Assert.Null(KeyCombo.TryParse(text));
    }

    [Fact]
    public void ToDisplayString_OrdersModifiersConsistently()
    {
        var combo = new KeyCombo(KeyModifiers.Shift | KeyModifiers.Control, "Tab", 0x09);

        Assert.Equal("Ctrl+Maj+Tab", combo.ToDisplayString());
    }
}
