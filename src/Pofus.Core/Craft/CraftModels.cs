namespace Pofus.Core.Craft;

/// <summary>One equipment entry imported from a DofusBook workshop.</summary>
/// <param name="ItemId">DofusBook/Dofus item id — the identity used for
/// de-duplication, never the display name.</param>
/// <param name="Quantity">How many copies of this equipment the user wants.</param>
public sealed record CraftItem(int ItemId, string Name, int Quantity, int Picture = 0);

/// <summary>A crafting recipe: which ingredients, and how many of each, for ONE unit.</summary>
public sealed record CraftRecipe(int ResultItemId, IReadOnlyList<RecipeIngredient> Ingredients);

public sealed record RecipeIngredient(int ItemId, int Quantity);

/// <summary>One line of the shopping list, after aggregation.</summary>
public sealed record RequiredResource(int ItemId, string Name, int TotalQuantity, int Picture = 0);

/// <summary>What an import produced, including what could not be resolved.</summary>
public sealed record WorkshopImportResult(
    IReadOnlyList<CraftItem> Equipment,
    IReadOnlyList<RequiredResource> Resources,
    IReadOnlyList<string> Warnings);
