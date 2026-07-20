using System.Text.Json;

namespace Application.EdgeIntegration.Dispatch.Services;

public static class ProductionDefinitionIngredientReader
{
    public static bool TryReadRequiredIngredientIds(
        string? productionDefinitionJson,
        out IReadOnlySet<Guid> ingredientIds)
    {
        var result = new HashSet<Guid>();
        ingredientIds = result;
        if (string.IsNullOrWhiteSpace(productionDefinitionJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(productionDefinitionJson);
            var root = document.RootElement;
            if (!root.TryGetProperty("SchemaVersion", out var schemaVersion) ||
                schemaVersion.GetInt32() != 1 ||
                !root.TryGetProperty("Recipe", out var recipe) ||
                !recipe.TryGetProperty("Items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("IsOptional", out var optional) && optional.GetBoolean())
                {
                    continue;
                }

                if (!item.TryGetProperty("IngredientId", out var ingredientIdElement) ||
                    !ingredientIdElement.TryGetGuid(out var ingredientId) ||
                    ingredientId == Guid.Empty)
                {
                    return false;
                }

                result.Add(ingredientId);
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
