using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Application.RobotConfiguration.Services;
using Microsoft.AspNetCore.Http;

namespace WebAPI.Controllers.RobotConfiguration;

internal static class RobotArtifactMultipartManifestParser
{
    public const int MaximumItemCount = 50;

    public static RobotArtifactMultipartManifestParseResult<TItem> Parse<TItem>(
        IReadOnlyCollection<IFormFile> files,
        string manifestJson,
        Func<TItem, string?> fileNameSelector)
    {
        TItem[]? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<TItem[]>(
                manifestJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return RobotArtifactMultipartManifestParseResult<TItem>.Failure(
                "ManifestJson must be a valid JSON array.");
        }

        if (manifest is null || manifest.Length is < 1 or > MaximumItemCount || files.Count != manifest.Length)
        {
            return RobotArtifactMultipartManifestParseResult<TItem>.Failure(
                $"Files and manifest must contain the same 1 to {MaximumItemCount} items.");
        }

        if (files.Any(file => file.Length <= 0 || file.Length > ArtifactUploadContentService.MaximumFileSizeBytes))
        {
            return RobotArtifactMultipartManifestParseResult<TItem>.Failure(
                $"Each Lua file must be between 1 byte and {ArtifactUploadContentService.MaximumFileSizeBytes} bytes.");
        }

        foreach (var item in manifest)
        {
            if (item is null || !Validator.TryValidateObject(
                    item,
                    new ValidationContext(item),
                    new List<ValidationResult>(),
                    validateAllProperties: true))
            {
                return RobotArtifactMultipartManifestParseResult<TItem>.Failure(
                    "Every manifest item must satisfy its required metadata contract.");
            }

            var itemFileName = NormalizeFileName(fileNameSelector(item));
            if (string.IsNullOrWhiteSpace(itemFileName) ||
                !itemFileName.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
            {
                return RobotArtifactMultipartManifestParseResult<TItem>.Failure(
                    "Every manifest item must reference a .lua file.");
            }
        }

        var filesByName = new Dictionary<string, IFormFile>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var fileName = NormalizeFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(fileName) ||
                !fileName.EndsWith(".lua", StringComparison.OrdinalIgnoreCase) ||
                !filesByName.TryAdd(fileName, file))
            {
                return RobotArtifactMultipartManifestParseResult<TItem>.Failure(
                    "Uploaded .lua file names must be present and unique.");
            }
        }

        var manifestFileNames = manifest
            .Select(item => NormalizeFileName(fileNameSelector(item))!)
            .ToArray();
        if (manifestFileNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != manifest.Length ||
            !filesByName.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(manifestFileNames))
        {
            return RobotArtifactMultipartManifestParseResult<TItem>.Failure(
                "Manifest file names must uniquely match all uploaded files.");
        }

        return RobotArtifactMultipartManifestParseResult<TItem>.Success(manifest, filesByName);
    }

    internal static string? NormalizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var normalizedPath = fileName.Replace('\\', '/');
        var separatorIndex = normalizedPath.LastIndexOf('/');
        return (separatorIndex >= 0 ? normalizedPath[(separatorIndex + 1)..] : normalizedPath).Trim();
    }
}

internal sealed record RobotArtifactMultipartManifestParseResult<TItem>(
    IReadOnlyList<TItem> Items,
    IReadOnlyDictionary<string, IFormFile> FilesByName,
    string? Error)
{
    public bool Succeeded => Error is null;

    public static RobotArtifactMultipartManifestParseResult<TItem> Success(
        IReadOnlyList<TItem> items,
        IReadOnlyDictionary<string, IFormFile> filesByName) =>
        new(items, filesByName, null);

    public static RobotArtifactMultipartManifestParseResult<TItem> Failure(string error) =>
        new([], new Dictionary<string, IFormFile>(), error);
}
