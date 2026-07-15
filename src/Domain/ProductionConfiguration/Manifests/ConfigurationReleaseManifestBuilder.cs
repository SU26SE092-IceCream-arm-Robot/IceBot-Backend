using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.ValueObjects;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Domain.ProductionConfiguration.Manifests;

public static class ConfigurationReleaseManifestBuilder
{
    public static ConfigurationReleaseManifest CreateContent(
        ConfigurationRelease release,
        IReadOnlyDictionary<Guid, PublishedRobotProgramSnapshot> programSnapshots)
    {
        var routes = release.ExecutionRoutes
            .OrderBy(route => route.Priority)
            .ThenBy(route => route.RouteCode, StringComparer.Ordinal)
            .ThenBy(route => route.Id)
            .Select(route => new
            {
                route.Id,
                route.RouteCode,
                route.Priority,
                route.ProductVariantId,
                route.RecipeId,
                route.ProductionDefinitionSchemaVersion,
                route.ProductionDefinitionChecksum,
                ProductionDefinition = CanonicalizeOptionalJson(route.ProductionDefinitionJson, "production definition"),
                SupportedOptionCodes = route.GetSupportedOptionCodes(),
                RequiredCapabilities = CanonicalizeOptionalJson(route.RequiredCapabilitiesJson, "execution route required capabilities"),
                RobotBindings = route.RobotBindings
                    .OrderBy(binding => binding.BindingOrder)
                    .ThenBy(binding => binding.Id)
                    .Select(binding => CreateBinding(release.OrganizationId, binding, programSnapshots))
                    .ToArray()
            })
            .ToArray();

        var document = new
        {
            release.Id,
            release.OrganizationId,
            release.ReleaseNumber,
            release.ReleaseManifestSchemaVersion,
            ExecutionRoutes = routes
        };

        var json = JsonSerializer.Serialize(document);
        var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
        return new ConfigurationReleaseManifest(json, checksum);
    }

    private static object CreateBinding(
        Guid organizationId,
        ExecutionRouteRobotBinding binding,
        IReadOnlyDictionary<Guid, PublishedRobotProgramSnapshot> programSnapshots)
    {
        if (!programSnapshots.TryGetValue(binding.RobotProgramId, out var program))
            throw new DomainRuleException("Configuration release bindings require published robot program snapshots.");
        if (program.OrganizationId != organizationId)
        {
            throw new DomainRuleException("Organization-scoped robot programs must belong to the configuration release organization.");
        }

        var artifacts = program.Artifacts
            .OrderBy(artifact => artifact.RunOrder)
            .ThenBy(artifact => artifact.ProgramArtifactId)
            .Select(artifact =>
            {
                return new
                {
                    Id = artifact.ProgramArtifactId,
                    artifact.RunOrder,
                    Parameters = CanonicalizeOptionalJson(artifact.ParametersJson, "robot program artifact parameters"),
                    artifact.ParametersSchemaVersion,
                    artifact.RequiredOptionCode,
                    RobotArtifact = new
                    {
                        Id = artifact.RobotArtifactId,
                        artifact.Checksum,
                        artifact.StorageKey,
                        artifact.RuntimeTargetCode,
                        artifact.MachineModelCode,
                        artifact.ContentLengthBytes,
                        artifact.TechnicalContractId,
                        artifact.TechnicalContractChecksum,
                        BundleEntryName = $"artifacts/{artifact.RobotArtifactId:D}.lua"
                    }
                };
            })
            .ToArray();

        if (artifacts.Length == 0)
        {
            throw new DomainRuleException("Configuration release bindings require robot programs with artifacts.");
        }

        return new
        {
            binding.Id,
            binding.BindingOrder,
            binding.RequiredWorkcellCapabilityCode,
            RobotProgram = new
            {
                program.Id,
                program.Code,
                ProgramManifestSchemaVersion = program.ManifestSchemaVersion,
                ProgramManifestChecksum = program.ManifestChecksum,
                Artifacts = artifacts
            }
        };
    }

    private static JsonNode? CanonicalizeOptionalJson(string? json, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return Sort(JsonNode.Parse(json));
        }
        catch (JsonException exception)
        {
            throw new DomainRuleException($"{fieldName} must be valid JSON: {exception.Message}");
        }
    }

    private static JsonNode? Sort(JsonNode? node)
    {
        return node switch
        {
            JsonObject jsonObject => new JsonObject(
                jsonObject
                    .OrderBy(property => property.Key, StringComparer.Ordinal)
                    .Select(property => KeyValuePair.Create(property.Key, Sort(property.Value)))),
            JsonArray jsonArray => new JsonArray(jsonArray.Select(Sort).ToArray()),
            _ => node?.DeepClone()
        };
    }
}

public sealed record ConfigurationReleaseManifest(string Json, string Checksum);
