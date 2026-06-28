using System.Text.Json;
using System.Text.Json.Nodes;
using Application.RobotConfiguration.Abstractions;
using Domain.Sync.Entities;
using Domain.Sync.Enums;

namespace Application.EdgeIntegration.Services;

public sealed class ArtifactCommandPayloadEnricher
{
    private readonly IArtifactObjectStorage _artifactObjectStorage;

    public ArtifactCommandPayloadEnricher(IArtifactObjectStorage artifactObjectStorage)
    {
        _artifactObjectStorage = artifactObjectStorage;
    }

    public async Task<string> EnrichAsync(
        EdgeCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.CommandType != EdgeCommandType.DeployConfiguration)
        {
            return command.PayloadJson;
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(command.PayloadJson);
        }
        catch (JsonException)
        {
            return command.PayloadJson;
        }

        if (root is null)
        {
            return command.PayloadJson;
        }

        var artifactNodes = new List<(JsonObject Node, string StorageKey)>();
        CollectArtifactNodes(root, artifactNodes);

        foreach (var group in artifactNodes.GroupBy(item => item.StorageKey, StringComparer.Ordinal))
        {
            var readUrl = await _artifactObjectStorage.CreateReadUrlAsync(group.Key, cancellationToken);
            foreach (var (node, _) in group)
            {
                node["DownloadUrl"] = readUrl.Url;
                node["DownloadUrlExpiresAt"] = readUrl.ExpiresAt;
            }
        }

        return root.ToJsonString();
    }

    private static void CollectArtifactNodes(
        JsonNode node,
        ICollection<(JsonObject Node, string StorageKey)> artifactNodes)
    {
        if (node is JsonObject jsonObject)
        {
            if (jsonObject["StorageKey"] is JsonValue storageKeyValue &&
                storageKeyValue.TryGetValue<string>(out var storageKey) &&
                !string.IsNullOrWhiteSpace(storageKey))
            {
                artifactNodes.Add((jsonObject, storageKey));
            }

            foreach (var child in jsonObject.Select(property => property.Value).Where(value => value is not null))
            {
                CollectArtifactNodes(child!, artifactNodes);
            }

            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var child in jsonArray.Where(value => value is not null))
            {
                CollectArtifactNodes(child!, artifactNodes);
            }
        }
    }
}
