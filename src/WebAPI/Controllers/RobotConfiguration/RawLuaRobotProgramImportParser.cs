using System.IO.Compression;
using System.Text;
using Application.RobotConfiguration.Storage.Services;
using Microsoft.AspNetCore.Http;

namespace WebAPI.Controllers.RobotConfiguration;

internal static class RawLuaRobotProgramImportParser
{
    public const int MaximumItemCount = 50;
    public const long MaximumTotalExtractedBytes = 100L * 1024 * 1024;

    public static async Task<RawLuaRobotProgramImportParseResult> ParseAsync(
        IReadOnlyCollection<IFormFile> files,
        IFormFile? archive,
        CancellationToken cancellationToken)
    {
        if (archive is not null && files.Count > 0)
            return RawLuaRobotProgramImportParseResult.Failure("Upload either raw Lua files or one raw ZIP archive, not both.");
        if (archive is null && files.Count == 0)
            return RawLuaRobotProgramImportParseResult.Failure("Upload at least one raw Lua file or one raw ZIP archive.");

        if (archive is not null)
            return await ParseArchiveAsync(archive, cancellationToken);

        if (files.Count > MaximumItemCount)
            return RawLuaRobotProgramImportParseResult.Failure($"A maximum of {MaximumItemCount} raw Lua files is allowed.");
        if (files.Any(file => !IsLuaFile(file.FileName)))
            return RawLuaRobotProgramImportParseResult.Failure("Raw import accepts only .lua files.");
        if (files.Any(file => file.Length <= 0 || file.Length > ArtifactUploadContentService.MaximumFileSizeBytes))
            return RawLuaRobotProgramImportParseResult.Failure(
                $"Each Lua file must be between 1 byte and {ArtifactUploadContentService.MaximumFileSizeBytes} bytes.");

        var names = files.Select(file => NormalizeFileName(file.FileName)).ToArray();
        if (names.Any(string.IsNullOrWhiteSpace) || names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length)
            return RawLuaRobotProgramImportParseResult.Failure("Raw Lua file names must be present and unique.");

        var items = new List<RawLuaRobotProgramImportFile>(files.Count);
        try
        {
            foreach (var file in files)
            {
                var content = new MemoryStream((int)file.Length);
                await using var source = file.OpenReadStream();
                await source.CopyToAsync(content, cancellationToken);
                content.Position = 0;
                items.Add(new RawLuaRobotProgramImportFile(NormalizeFileName(file.FileName)!, content, file.ContentType));
            }

            return RawLuaRobotProgramImportParseResult.Success(items);
        }
        catch
        {
            await DisposeAsync(items);
            throw;
        }
    }

    private static async Task<RawLuaRobotProgramImportParseResult> ParseArchiveAsync(
        IFormFile archive,
        CancellationToken cancellationToken)
    {
        if (!archive.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return RawLuaRobotProgramImportParseResult.Failure("Raw archive import requires a .zip file.");
        if (archive.Length <= 0 || archive.Length > MaximumTotalExtractedBytes)
            return RawLuaRobotProgramImportParseResult.Failure("Raw ZIP archive size is invalid or exceeds 100 MB.");

        var items = new List<RawLuaRobotProgramImportFile>();
        try
        {
            await using var source = archive.OpenReadStream();
            using var zip = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: false);
            var entries = zip.Entries.Where(entry => !string.IsNullOrEmpty(entry.Name)).ToArray();
            if (entries.Length is < 1 or > MaximumItemCount)
                return RawLuaRobotProgramImportParseResult.Failure(
                    $"Raw ZIP archive must contain one to {MaximumItemCount} files.");
            if (entries.Any(entry => !IsLuaFile(entry.Name)))
                return RawLuaRobotProgramImportParseResult.Failure("Raw ZIP archive may contain only .lua files.");
            if (entries.Any(entry => entry.Length <= 0 || entry.Length > ArtifactUploadContentService.MaximumFileSizeBytes))
                return RawLuaRobotProgramImportParseResult.Failure(
                    $"Each Lua entry must be between 1 byte and {ArtifactUploadContentService.MaximumFileSizeBytes} bytes.");
            if (entries.Sum(entry => entry.Length) > MaximumTotalExtractedBytes)
                return RawLuaRobotProgramImportParseResult.Failure("Raw ZIP archive expands beyond the 100 MB safety limit.");

            var names = entries.Select(entry => NormalizeFileName(entry.Name)).ToArray();
            if (names.Any(string.IsNullOrWhiteSpace) || names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length)
                return RawLuaRobotProgramImportParseResult.Failure("Raw ZIP Lua file names must be unique.");

            foreach (var entry in entries)
            {
                var content = new MemoryStream((int)entry.Length);
                await using var entryStream = entry.Open();
                await entryStream.CopyToAsync(content, cancellationToken);
                if (content.Length != entry.Length)
                {
                    await content.DisposeAsync();
                    await DisposeAsync(items);
                    return RawLuaRobotProgramImportParseResult.Failure("Raw ZIP entry content length changed while reading.");
                }
                content.Position = 0;
                items.Add(new RawLuaRobotProgramImportFile(NormalizeFileName(entry.Name)!, content, "text/plain"));
            }

            return RawLuaRobotProgramImportParseResult.Success(items);
        }
        catch (InvalidDataException)
        {
            await DisposeAsync(items);
            return RawLuaRobotProgramImportParseResult.Failure("Raw ZIP archive is invalid.");
        }
        catch
        {
            await DisposeAsync(items);
            throw;
        }
    }

    public static string CreateArtifactCode(string fileName, int ordinal)
    {
        var stem = System.IO.Path.GetFileNameWithoutExtension(fileName);
        var builder = new StringBuilder(stem.Length);
        var previousSeparator = false;
        foreach (var character in stem)
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
                previousSeparator = false;
            }
            else if (!previousSeparator)
            {
                builder.Append('-');
                previousSeparator = true;
            }
        }

        var code = builder.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(code))
            code = "RAW-LUA";
        var suffix = $"-{ordinal}";
        return $"{code[..Math.Min(100 - suffix.Length, code.Length)]}{suffix}";
    }

    public static string CreateArtifactName(string fileName)
    {
        var name = System.IO.Path.GetFileNameWithoutExtension(fileName).Trim();
        return string.IsNullOrWhiteSpace(name) ? "Raw Lua artifact" : name[..Math.Min(200, name.Length)];
    }

    private static bool IsLuaFile(string? fileName) =>
        !string.IsNullOrWhiteSpace(fileName) && fileName.EndsWith(".lua", StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;
        var value = fileName.Replace('\\', '/');
        var separator = value.LastIndexOf('/');
        return (separator < 0 ? value : value[(separator + 1)..]).Trim();
    }

    public static async Task DisposeAsync(IEnumerable<RawLuaRobotProgramImportFile> items)
    {
        foreach (var item in items)
            await item.Content.DisposeAsync();
    }
}

internal sealed record RawLuaRobotProgramImportFile(string FileName, MemoryStream Content, string ContentType);

internal sealed record RawLuaRobotProgramImportParseResult(
    IReadOnlyCollection<RawLuaRobotProgramImportFile> Items,
    string? Error)
{
    public bool Succeeded => Error is null;

    public static RawLuaRobotProgramImportParseResult Success(IReadOnlyCollection<RawLuaRobotProgramImportFile> items) => new(items, null);
    public static RawLuaRobotProgramImportParseResult Failure(string error) => new([], error);
}
