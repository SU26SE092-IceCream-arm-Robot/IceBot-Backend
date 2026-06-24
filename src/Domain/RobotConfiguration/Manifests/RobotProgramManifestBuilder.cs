using Domain.Common;
using Domain.RobotConfiguration.Entities;
using Domain.RobotConfiguration.Enums;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Domain.RobotConfiguration.Manifests;

public static class RobotProgramManifestBuilder
{
    public static RobotProgramManifest Create(RobotProgram program, int schemaVersion)
    {
        var artifacts = program.RobotProgramArtifacts
            .OrderBy(item => item.RunOrder)
            .ThenBy(item => item.Id)
            .Select(item =>
            {
                if (item.RobotArtifact is null || item.RobotArtifact.Status != RobotArtifactStatus.Published)
                {
                    throw new DomainRuleException("Robot program publication requires loaded published robot artifacts.");
                }

                return new
                {
                    item.Id,
                    item.RunOrder,
                    item.ParametersSchemaVersion,
                    Parameters = CanonicalizeOptionalJson(item.ParametersJson),
                    RobotArtifact = new
                    {
                        item.RobotArtifact.Id,
                        item.RobotArtifact.Checksum,
                        item.RobotArtifact.StorageKey,
                        item.RobotArtifact.RuntimeTargetCode,
                        item.RobotArtifact.MachineModelCode,
                        item.RobotArtifact.ContentLengthBytes
                    }
                };
            })
            .ToArray();

        if (artifacts.Length == 0)
        {
            throw new DomainRuleException("Robot program manifest requires at least one artifact.");
        }

        var document = new
        {
            program.Id,
            program.Code,
            SchemaVersion = schemaVersion,
            Artifacts = artifacts
        };
        var json = JsonSerializer.Serialize(document);
        var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
        return new RobotProgramManifest(json, checksum);
    }

    private static JsonNode? CanonicalizeOptionalJson(string? json)
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
            throw new DomainRuleException($"Robot program artifact parameters must be valid JSON: {exception.Message}");
        }
    }

    private static JsonNode? Sort(JsonNode? node)
    {
        return node switch
        {
            JsonObject jsonObject => new JsonObject(jsonObject.OrderBy(property => property.Key, StringComparer.Ordinal)
                .Select(property => KeyValuePair.Create(property.Key, Sort(property.Value)))),
            JsonArray jsonArray => new JsonArray(jsonArray.Select(Sort).ToArray()),
            _ => node?.DeepClone()
        };
    }
}

public sealed record RobotProgramManifest(string Json, string Checksum);
