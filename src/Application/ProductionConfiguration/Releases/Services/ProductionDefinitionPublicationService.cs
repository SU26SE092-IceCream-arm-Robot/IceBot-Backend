using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.Common;
using Domain.Catalog.Enums;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.ValueObjects;

namespace Application.ProductionConfiguration.Releases.Services;

public sealed class ProductionDefinitionPublicationService
{
    public void Build(ConfigurationRelease release,
        IReadOnlyDictionary<Guid, PublishedRobotProgramSnapshot> programSnapshots)
    {
        foreach (var route in release.ExecutionRoutes)
        {
            if (route.ProductVariant?.Product is null || route.Recipe is null)
                throw new DomainRuleException("Release publication requires Product, Variant, and Recipe graph.");
            if (route.Recipe.Status is not RecipeStatus.Published and not RecipeStatus.Active)
                throw new DomainRuleException("Release publication requires a Published or Active Recipe.");
            if (route.Recipe.RecipeItems.Count == 0)
                throw new DomainRuleException("Production definition requires Recipe items.");

            var programs = route.RobotBindings.OrderBy(x => x.BindingOrder).Select(binding =>
            {
                if (!programSnapshots.TryGetValue(binding.RobotProgramId, out var program))
                    throw new DomainRuleException("Production definition requires published RobotProgram snapshots.");
                if (program.Artifacts.Any(x => !x.TechnicalContractId.HasValue || string.IsNullOrWhiteSpace(x.TechnicalContractChecksum)))
                    throw new DomainRuleException("Production definition requires artifact technical-contract provenance.");
                return new
                {
                    binding.BindingOrder,
                    binding.RequiredWorkcellCapabilityCode,
                    RobotProgramId = program.Id,
                    program.ManifestSchemaVersion,
                    program.ManifestChecksum,
                    Artifacts = program.Artifacts.OrderBy(x => x.RunOrder).Select(x => new
                    {
                        x.ProgramArtifactId, x.RobotArtifactId, x.RunOrder, x.ParametersSchemaVersion,
                        x.ParametersJson, x.Checksum, x.RuntimeTargetCode, x.MachineModelCode,
                        x.TechnicalContractId, x.TechnicalContractChecksum, x.RequiredOptionCode
                    })
                };
            }).ToArray();

            var product = route.ProductVariant.Product;
            var supportedOptionCodes = route.GetSupportedOptionCodes()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var conditionalArtifactCodes = programs.SelectMany(program => program.Artifacts)
                .Where(artifact => !string.IsNullOrWhiteSpace(artifact.RequiredOptionCode))
                .Select(artifact => artifact.RequiredOptionCode!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!supportedOptionCodes.SetEquals(conditionalArtifactCodes))
                throw new DomainRuleException(
                    "Execution route supported option policy must exactly match its conditional robot artifacts.");
            var document = new
            {
                SchemaVersion = 1,
                route.Id,
                route.RouteCode,
                route.ProductVariantId,
                ProductCode = product.Code,
                ProductVariantCode = route.ProductVariant.Code,
                route.RecipeId,
                Recipe = new
                {
                    route.Recipe.Code, route.Recipe.Version, route.Recipe.YieldQuantity, route.Recipe.Unit,
                    Items = route.Recipe.RecipeItems.Where(x => x.DeletedAt == null).OrderBy(x => x.StepOrder)
                        .ThenBy(x => x.IngredientId).Select(x => new
                        { x.IngredientId, IngredientCode = x.Ingredient.Code, x.Quantity, x.Unit, x.StepOrder, x.IsOptional })
                },
                SupportedOptions = product.OptionGroups
                    .Where(group => group.ProductOptions.Any(option => option.DeletedAt == null &&
                        supportedOptionCodes.Contains(option.Code)))
                    .OrderBy(x => x.DisplayOrder).Select(group => new
                {
                    group.Code, group.SelectionType, group.MinSelections, group.MaxSelections, group.IsRequired,
                    Options = group.ProductOptions.Where(x => x.DeletedAt == null &&
                            supportedOptionCodes.Contains(x.Code)).OrderBy(x => x.DisplayOrder)
                        .Select(option => new
                        {
                            option.Id, option.Code,
                            IngredientRequirements = option.IngredientRequirements.Where(x => x.DeletedAt == null)
                                .OrderBy(x => x.IngredientId).Select(x => new
                                { x.IngredientId, IngredientCode = x.Ingredient.Code, x.Quantity, x.Unit, x.RequiredWorkcellCapabilityCode })
                        })
                }),
                route.RequiredCapabilitiesJson,
                RobotPrograms = programs
            };
            var json = JsonSerializer.Serialize(document);
            var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
            route.SetPublishedProductionDefinition(1, json, checksum);
        }
    }
}
