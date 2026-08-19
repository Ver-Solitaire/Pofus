using Pofus.Core.Craft;

namespace Pofus.Core.Tests.Craft;

public class WorkshopParserTests
{
    /// <summary>Mirrors the real payload shape returned by DofusBook's workshop API.</summary>
    private const string TwoCraftsSharingAnIngredient = """
    {
      "v": 2,
      "source": "dofusbook",
      "crafts": [
        {
          "itemId": 1450, "name": "Cape Cérémoniale", "quantity": 1,
          "ingredients": [
            { "id": 8485, "name": "Crinière de Rat Noir", "count": 2 },
            { "id": 8490, "name": "Fémur de Sphincter Cell", "count": 3 }
          ]
        },
        {
          "itemId": 1451, "name": "Anneau Cérémonial", "quantity": 1,
          "ingredients": [
            { "id": 8490, "name": "Fémur de Sphincter Cell", "count": 3 },
            { "id": 6458, "name": "Kobalite", "count": 1 }
          ]
        }
      ]
    }
    """;

    [Fact]
    public void TryParse_ReadsCraftsWithTheirIngredients()
    {
        var crafts = WorkshopParser.TryParse(TwoCraftsSharingAnIngredient, out var error);

        Assert.Null(error);
        Assert.Equal(2, crafts!.Count);
        Assert.Equal("Cape Cérémoniale", crafts[0].Name);
        Assert.Equal(2, crafts[0].Ingredients.Count);
        Assert.Equal("Crinière de Rat Noir", crafts[0].Ingredients[0].Name);
        Assert.Equal(2, crafts[0].Ingredients[0].Count);
    }

    [Fact]
    public void BuildShoppingList_SumsAnIngredientSharedByTwoCrafts()
    {
        var crafts = WorkshopParser.TryParse(TwoCraftsSharingAnIngredient, out _)!;

        var list = WorkshopParser.BuildShoppingList(crafts);

        // 3 + 3 for the shared one, and it must appear on a single line.
        var shared = Assert.Single(list, r => r.ItemId == 8490);
        Assert.Equal(6, shared.TotalQuantity);
        Assert.Equal(3, list.Count);
        Assert.Equal(2, list.Single(r => r.ItemId == 8485).TotalQuantity);
        Assert.Equal(1, list.Single(r => r.ItemId == 6458).TotalQuantity);
    }

    [Fact]
    public void BuildShoppingList_ScalesByHowManyCopiesAreWanted()
    {
        var json = """
        { "v":2, "crafts":[ { "itemId":1, "name":"Épée", "quantity":3,
          "ingredients":[ { "id":100, "name":"Fer", "count":10 },
                          { "id":200, "name":"Bois", "count":5 } ] } ] }
        """;
        var crafts = WorkshopParser.TryParse(json, out _)!;

        var list = WorkshopParser.BuildShoppingList(crafts);

        Assert.Equal(30, list.Single(r => r.ItemId == 100).TotalQuantity);
        Assert.Equal(15, list.Single(r => r.ItemId == 200).TotalQuantity);
    }

    [Fact]
    public void BuildShoppingList_MergesOnItemId_NotOnDisplayName()
    {
        // Same id, labels that differ: still one resource.
        var json = """
        { "v":2, "crafts":[
          { "itemId":1, "name":"A", "quantity":1, "ingredients":[ { "id":50, "name":"Kobalite", "count":1 } ] },
          { "itemId":2, "name":"B", "quantity":1, "ingredients":[ { "id":50, "name":"kobalite", "count":2 } ] } ] }
        """;
        var crafts = WorkshopParser.TryParse(json, out _)!;

        var only = Assert.Single(WorkshopParser.BuildShoppingList(crafts));

        Assert.Equal(3, only.TotalQuantity);
    }

    [Fact]
    public void TryParse_DefaultsQuantityToOne_WhenAbsentOrInvalid()
    {
        var json = """
        { "v":2, "crafts":[ { "itemId":1, "name":"X",
          "ingredients":[ { "id":10, "name":"Fer", "count":4 } ] } ] }
        """;

        var crafts = WorkshopParser.TryParse(json, out _)!;

        Assert.Equal(1, Assert.Single(crafts).Quantity);
    }

    [Fact]
    public void TryParse_SkipsIngredientsMissingIdNameOrCount()
    {
        var json = """
        { "v":2, "crafts":[ { "itemId":1, "name":"X", "quantity":1, "ingredients":[
          { "name":"sans id", "count":2 },
          { "id":11, "count":2 },
          { "id":12, "name":"sans count" },
          { "id":13, "name":"valide", "count":7 } ] } ] }
        """;

        var crafts = WorkshopParser.TryParse(json, out _)!;

        var ingredient = Assert.Single(Assert.Single(crafts).Ingredients);
        Assert.Equal(13, ingredient.ItemId);
        Assert.Equal(7, ingredient.Count);
    }

    [Fact]
    public void TryParse_IgnoresACraftWhoseIngredientsAreAllUnusable()
    {
        var json = """
        { "v":2, "crafts":[
          { "itemId":1, "name":"Sans recette exploitable", "quantity":1, "ingredients":[ { "id":0 } ] },
          { "itemId":2, "name":"Bon", "quantity":1, "ingredients":[ { "id":9, "name":"Fer", "count":1 } ] } ] }
        """;

        var crafts = WorkshopParser.TryParse(json, out _)!;

        Assert.Equal(2, Assert.Single(crafts).ItemId);
    }

    [Fact]
    public void TryParse_RejectsAZeroOrNegativeCount()
    {
        var json = """
        { "v":2, "crafts":[ { "itemId":1, "name":"X", "quantity":1, "ingredients":[
          { "id":10, "name":"Fer", "count":0 }, { "id":11, "name":"Bois", "count":-3 } ] } ] }
        """;

        Assert.Null(WorkshopParser.TryParse(json, out var error));
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryParse_ExplainsMissingData(string? text)
    {
        Assert.Null(WorkshopParser.TryParse(text, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void TryParse_ExplainsNonJsonContent_RatherThanThrowing()
    {
        Assert.Null(WorkshopParser.TryParse("https://d-bk.net/fr/dw/XXXX", out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void TryParse_ExplainsAnEmptyWorkshop()
    {
        Assert.Null(WorkshopParser.TryParse("""{"v":2,"crafts":[]}""", out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void TryParse_RefusesAPayloadFromANewerExtractor()
    {
        var json = """{"v":99,"crafts":[{"itemId":1,"name":"X","quantity":1,"ingredients":[{"id":1,"name":"F","count":1}]}]}""";

        Assert.Null(WorkshopParser.TryParse(json, out var error));
        Assert.Contains("99", error!);
    }

    [Fact]
    public void BuildShoppingList_SortsByNameSoTheListIsScannable()
    {
        var crafts = WorkshopParser.TryParse(TwoCraftsSharingAnIngredient, out _)!;

        var names = WorkshopParser.BuildShoppingList(crafts).Select(r => r.Name).ToList();

        Assert.Equal(names.OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase), names);
    }
}
