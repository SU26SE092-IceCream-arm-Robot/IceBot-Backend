using Application.EdgeIntegration.Dispatch.Services;

namespace IceBot.UnitTests.EdgeIntegration;

public sealed class ProductionDefinitionIngredientReaderTests
{
    [Fact]
    public void ReadsOnlyRequiredRecipeIngredientsFromPublishedDefinition()
    {
        var requiredIngredientId = Guid.NewGuid();
        var optionalIngredientId = Guid.NewGuid();
        var json = $$"""
            {
              "SchemaVersion": 1,
              "Recipe": {
                "Items": [
                  { "IngredientId": "{{requiredIngredientId}}", "IsOptional": false },
                  { "IngredientId": "{{optionalIngredientId}}", "IsOptional": true }
                ]
              }
            }
            """;

        var succeeded = ProductionDefinitionIngredientReader.TryReadRequiredIngredientIds(json, out var ingredientIds);

        Assert.True(succeeded);
        Assert.Equal([requiredIngredientId], ingredientIds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("{\"SchemaVersion\":2,\"Recipe\":{\"Items\":[]}}")]
    [InlineData("{\"SchemaVersion\":999999999999,\"Recipe\":{\"Items\":[]}}")]
    [InlineData("{not-json}")]
    public void RejectsMissingOrUnsupportedProductionDefinition(string? json)
    {
        Assert.False(ProductionDefinitionIngredientReader.TryReadRequiredIngredientIds(json, out _));
    }
}
