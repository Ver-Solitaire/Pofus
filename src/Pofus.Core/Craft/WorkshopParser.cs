using System.Text.Json;

namespace Pofus.Core.Craft;

/// <summary>One equipment entry of a workshop, with the recipe DofusBook already supplies.</summary>
public sealed record WorkshopCraft(
    int ItemId, string Name, int Quantity, IReadOnlyList<WorkshopIngredient> Ingredients, int Picture = 0);

/// <param name="Count">Needed for ONE unit of the equipment.</param>
public sealed record WorkshopIngredient(int ItemId, string Name, int Count, int Picture = 0);

/// <summary>
/// Reads the workshop envelope produced inside the page.
///
/// DofusBook's workshop already carries each recipe's ingredients, their labels
/// and their per-unit counts, so nothing has to be looked up elsewhere: the
/// shopping list is a pure function of this payload, which is why the arithmetic
/// lives here (testable) rather than in the page script.
/// </summary>
public static class WorkshopParser
{
    public const int SupportedVersion = 2;

    public static IReadOnlyList<WorkshopCraft>? TryParse(string? text, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Aucune donnée d'atelier reçue.";
            return null;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            error = "Données d'atelier illisibles.";
            return null;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("crafts", out var craftsElement)
                || craftsElement.ValueKind != JsonValueKind.Array)
            {
                error = "Données d'atelier non reconnues.";
                return null;
            }

            if (root.TryGetProperty("v", out var version)
                && version.TryGetInt32(out var versionValue)
                && versionValue > SupportedVersion)
            {
                error = $"Format d'atelier plus récent que Pofus (version {versionValue}).";
                return null;
            }

            var crafts = new List<WorkshopCraft>();
            foreach (var element in craftsElement.EnumerateArray())
            {
                var craft = TryReadCraft(element);
                if (craft is not null)
                {
                    crafts.Add(craft);
                }
            }

            if (crafts.Count == 0)
            {
                error = "Cet atelier ne contient aucun équipement avec une recette.";
                return null;
            }

            return crafts;
        }
    }

    /// <summary>
    /// The shopping list: every ingredient summed across the whole workshop,
    /// each craft's needs scaled by how many copies are wanted.
    /// Merging is keyed on the ingredient's item id — two entries are the same
    /// resource only when their ids match, never merely their labels.
    /// </summary>
    public static IReadOnlyList<RequiredResource> BuildShoppingList(IReadOnlyList<WorkshopCraft> crafts)
    {
        var totals = new Dictionary<int, int>();
        var names = new Dictionary<int, string>();
        var pictures = new Dictionary<int, int>();

        foreach (var craft in crafts)
        {
            foreach (var ingredient in craft.Ingredients)
            {
                totals[ingredient.ItemId] = totals.GetValueOrDefault(ingredient.ItemId)
                    + (ingredient.Count * craft.Quantity);
                names.TryAdd(ingredient.ItemId, ingredient.Name);
                if (ingredient.Picture > 0)
                {
                    pictures.TryAdd(ingredient.ItemId, ingredient.Picture);
                }
            }
        }

        return totals
            .Select(pair => new RequiredResource(
                pair.Key, names[pair.Key], pair.Value, pictures.GetValueOrDefault(pair.Key)))
            .OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static WorkshopCraft? TryReadCraft(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty("ingredients", out var ingredientsElement)
            || ingredientsElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var ingredients = new List<WorkshopIngredient>();
        foreach (var entry in ingredientsElement.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty("id", out var idElement) && idElement.TryGetInt32(out var id)
                && entry.TryGetProperty("count", out var countElement) && countElement.TryGetInt32(out var count)
                && count > 0
                && entry.TryGetProperty("name", out var nameElement)
                && nameElement.ValueKind == JsonValueKind.String
                && nameElement.GetString() is { Length: > 0 } name)
            {
                var picture = entry.TryGetProperty("picture", out var pictureElement)
                    && pictureElement.TryGetInt32(out var parsedPicture)
                        ? parsedPicture
                        : 0;
                ingredients.Add(new WorkshopIngredient(id, name, count, picture));
            }
        }

        if (ingredients.Count == 0)
        {
            return null;
        }

        var itemId = element.TryGetProperty("itemId", out var itemIdElement) && itemIdElement.TryGetInt32(out var parsedId)
            ? parsedId
            : 0;
        var craftName = element.TryGetProperty("name", out var craftNameElement)
            && craftNameElement.ValueKind == JsonValueKind.String
                ? craftNameElement.GetString() ?? "Équipement"
                : "Équipement";
        var quantity = element.TryGetProperty("quantity", out var qtyElement) && qtyElement.TryGetInt32(out var parsedQty) && parsedQty > 0
            ? parsedQty
            : 1;
        var craftPicture = element.TryGetProperty("picture", out var craftPictureElement)
            && craftPictureElement.TryGetInt32(out var parsedCraftPicture)
                ? parsedCraftPicture
                : 0;

        return new WorkshopCraft(itemId, craftName, quantity, ingredients, craftPicture);
    }
}
