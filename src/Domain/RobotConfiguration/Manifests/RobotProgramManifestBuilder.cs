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
    public static RobotProgramManifest Create(
        RobotProgram program,
        IReadOnlyCollection<RobotArtifactManifestSnapshot> artifactSnapshots,
        int schemaVersion)
    {
        var snapshotsById = artifactSnapshots.ToDictionary(item => item.RobotArtifactId);
        var artifacts = program.RobotProgramArtifacts
            .OrderBy(item => item.RunOrder)
            .ThenBy(item => item.Id)
            .Select(item => CreateItem(item, snapshotsById))
            .ToArray();

        if (artifacts.Length == 0)
        {
            throw new DomainRuleException("Robot program manifest requires at least one artifact.");
        }

        if (snapshotsById.Count != artifacts.Select(item => item.RobotArtifact.Id).Distinct().Count())
        {
            throw new DomainRuleException("Robot program publication artifact snapshots do not match program membership.");
        }

        var document = new RobotProgramManifestDocument(
            program.Id,
            program.Code,
            schemaVersion,
            artifacts);
        var json = JsonSerializer.Serialize(document);
        var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
        return new RobotProgramManifest(document, json, checksum);
    }

    public static RobotProgramManifestDocument Parse(string manifestJson)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
        {
            throw new DomainRuleException("Published robot program manifest is required.");
        }

        try
        {
            var document = JsonSerializer.Deserialize<RobotProgramManifestDocument>(manifestJson)
                ?? throw new DomainRuleException("Published robot program manifest is empty.");
            if (document.Id == Guid.Empty || string.IsNullOrWhiteSpace(document.Code) ||
                document.SchemaVersion <= 0 || document.Artifacts.Count == 0)
            {
                throw new DomainRuleException("Published robot program manifest is invalid.");
            }

            return document;
        }
        catch (JsonException exception)
        {
            throw new DomainRuleException($"Published robot program manifest is invalid: {exception.Message}");
        }
    }

    private static RobotProgramManifestItem CreateItem(
        RobotProgramArtifact membership,
        IReadOnlyDictionary<Guid, RobotArtifactManifestSnapshot> snapshotsById)
    {
        if (!snapshotsById.TryGetValue(membership.RobotArtifactId, out var artifact) ||
            artifact.Status != RobotArtifactStatus.Published)
        {
            throw new DomainRuleException("Robot program publication requires published robot artifact snapshots.");
        }

        return new RobotProgramManifestItem(
            membership.Id,
            membership.RunOrder,
            membership.ParametersSchemaVersion,
            CanonicalizeOptionalJson(membership.ParametersJson),
            new RobotProgramManifestArtifact(
                artifact.RobotArtifactId,
                artifact.Checksum,
                artifact.StorageKey,
                artifact.RuntimeTargetCode,
                artifact.MachineModelCode,
                artifact.ContentLengthBytes));
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

public sealed record RobotArtifactManifestSnapshot(
    Guid RobotArtifactId,
    string ArtifactCode,
    string ArtifactName,
    string FileName,
    RobotArtifactStatus Status,
    string Checksum,
    string StorageKey,
    string RuntimeTargetCode,
    string MachineModelCode,
    long ContentLengthBytes);

public sealed record RobotProgramManifestDocument(
    Guid Id,
    string Code,
    int SchemaVersion,
    IReadOnlyList<RobotProgramManifestItem> Artifacts);

public sealed record RobotProgramManifestItem(
    Guid Id,
    int RunOrder,
    int ParametersSchemaVersion,
    JsonNode? Parameters,
    RobotProgramManifestArtifact RobotArtifact);

public sealed record RobotProgramManifestArtifact(
    Guid Id,
    string Checksum,
    string StorageKey,
    string RuntimeTargetCode,
    string MachineModelCode,
    long ContentLengthBytes);

public sealed record RobotProgramManifest(
    RobotProgramManifestDocument Document,
    string Json,
    string Checksum);
