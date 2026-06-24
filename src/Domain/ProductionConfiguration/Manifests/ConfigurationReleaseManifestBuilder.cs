using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.RobotConfiguration.Enums;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Domain.ProductionConfiguration.Manifests;

public static class ConfigurationReleaseManifestBuilder
{
    public static ConfigurationReleaseManifest Create(ConfigurationRelease release)
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
                RequiredCapabilities = CanonicalizeOptionalJson(route.RequiredCapabilitiesJson, "execution route required capabilities"),
                RobotBindings = route.RobotBindings
                    .OrderBy(binding => binding.BindingOrder)
                    .ThenBy(binding => binding.Id)
                    .Select(binding => CreateBinding(release.OrganizationId, binding))
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

    private static object CreateBinding(Guid organizationId, ExecutionRouteRobotBinding binding)
    {
        if (binding.RobotProgram is null)
        {
            throw new DomainRuleException("Robot program bindings must be loaded before publishing a configuration release.");
        }

        if (binding.RobotProgram.Status != RobotProgramStatus.Published)
        {
            throw new DomainRuleException("Configuration release bindings require published robot programs.");
        }

        if (binding.RobotProgram.OrganizationId.HasValue && binding.RobotProgram.OrganizationId.Value != organizationId)
        {
            throw new DomainRuleException("Organization-scoped robot programs must belong to the configuration release organization.");
        }

        var artifacts = binding.RobotProgram.RobotProgramArtifacts
            .OrderBy(artifact => artifact.RunOrder)
            .ThenBy(artifact => artifact.Id)
            .Select(programArtifact =>
            {
                if (programArtifact.RobotArtifact is null)
                {
                    throw new DomainRuleException("Robot program artifacts must be loaded before publishing a configuration release.");
                }

                if (programArtifact.RobotArtifact.OrganizationId != organizationId)
                {
                    throw new DomainRuleException("Robot artifacts must belong to the configuration release organization.");
                }

                if (programArtifact.RobotArtifact.Status != RobotArtifactStatus.Published)
                {
                    throw new DomainRuleException("Configuration release bindings require published robot artifacts.");
                }

                return new
                {
                    programArtifact.Id,
                    programArtifact.RunOrder,
                    Parameters = CanonicalizeOptionalJson(programArtifact.ParametersJson, "robot program artifact parameters"),
                    programArtifact.ParametersSchemaVersion,
                    RobotArtifact = new
                    {
                        programArtifact.RobotArtifact.Id,
                        programArtifact.RobotArtifact.Checksum,
                        programArtifact.RobotArtifact.StorageKey,
                        programArtifact.RobotArtifact.RuntimeTargetCode,
                        programArtifact.RobotArtifact.MachineModelCode
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
                binding.RobotProgram.Id,
                binding.RobotProgram.Code,
                binding.RobotProgram.ProgramManifestSchemaVersion,
                binding.RobotProgram.ProgramManifestChecksum,
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
