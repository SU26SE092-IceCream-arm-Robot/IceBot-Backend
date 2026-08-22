using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.Artifacts;

namespace Application.RobotConfiguration.AuthoringImports;

public sealed class RobotAuthoringBundleException(string message) : Exception(message);

public sealed record RobotAuthoringBundle(
    RobotAuthoringExportManifest Manifest,
    IReadOnlyList<RobotAuthoringBundleItem> Items,
    RobotRuntimeProfileSource RuntimeProfileSource);

public sealed record RobotAuthoringBundleItem(
    RobotAuthoringManifestArtifact ManifestItem,
    RobotAuthoringSidecar Sidecar,
    byte[] LuaBytes,
    string LuaChecksum,
    string SidecarChecksum);

public sealed class RobotAuthoringExportManifest
{
    public int SchemaVersion { get; init; }
    public Guid ExportId { get; init; }
    public DateTimeOffset ExportedAt { get; init; }
    public required RobotAuthoringManifestProgram Program { get; init; }
}

public sealed class RobotAuthoringManifestProgram
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string RuntimeTargetCode { get; init; }
    public required string MachineModelCode { get; init; }
    public required IReadOnlyList<RobotAuthoringManifestArtifact> Artifacts { get; init; }
}

public sealed class RobotAuthoringManifestArtifact
{
    public required string ArtifactCode { get; init; }
    public required string FileName { get; init; }
    public string? SidecarFileName { get; init; }
    public int RunOrder { get; init; }
}

public sealed class RobotAuthoringSidecar
{
    public int SchemaVersion { get; init; }
    public required string ArtifactCode { get; init; }
    public required string ArtifactFileName { get; init; }
    public required string RuntimeTargetCode { get; init; }
    public required string MachineModelCode { get; init; }
    public IReadOnlyList<RobotAuthoringSidecarEffect> Effects { get; init; } = [];
    public IReadOnlyList<RobotAuthoringSidecarConstraint> OrderingConstraints { get; init; } = [];
}

public sealed class RobotAuthoringSidecarEffect
{
    public required string EffectCode { get; init; }
    public RobotArtifactEffectKind EffectKind { get; init; }
    public string? IngredientCode { get; init; }
    public string? OptionCode { get; init; }
    public RobotArtifactQuantityMode QuantityMode { get; init; }
    public decimal? FixedQuantity { get; init; }
    public string? Unit { get; init; }
    public string? RequiredWorkcellCapabilityCode { get; init; }
}

public sealed class RobotAuthoringSidecarConstraint
{
    public RobotArtifactOrderingConstraintType ConstraintType { get; init; }
    public required string Value { get; init; }
    public int SortHint { get; init; }
}

public static class RobotAuthoringBundleCodec
{
    public const string DefaultRuntimeTargetCode = "FAIRINO_LUA_V1";
    public const string DefaultMachineModelCode = "FR5";
    public const long MaximumArchiveBytes = 50 * 1024 * 1024;
    public const long MaximumExpandedBytes = 100 * 1024 * 1024;
    public const int MaximumEntries = 200;
    private const long MaximumManifestBytes = 256 * 1024;
    private const long MaximumSidecarBytes = 1024 * 1024;
    private const long MaximumLuaBytes = 10 * 1024 * 1024;
    private const int MaximumCodeLength = 100;
    private const int MaximumNameLength = 200;
    private const int MaximumFileNameLength = 255;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters =
        {
            new JsonStringEnumConverter<RobotArtifactEffectKind>(namingPolicy: null, allowIntegerValues: false),
            new JsonStringEnumConverter<RobotArtifactQuantityMode>(namingPolicy: null, allowIntegerValues: false),
            new JsonStringEnumConverter<RobotArtifactOrderingConstraintType>(namingPolicy: null, allowIntegerValues: false)
        }
    };

    public static RobotAuthoringBundle Parse(byte[] archiveBytes, string? sourceFileName = null)
    {
        try
        {
            return ParseCore(archiveBytes, sourceFileName);
        }
        catch (RobotAuthoringBundleException)
        {
            throw;
        }
        catch (InvalidDataException ex)
        {
            throw new RobotAuthoringBundleException($"ZIP archive is invalid, unsupported, or encrypted: {ex.Message}");
        }
        catch (NotSupportedException ex)
        {
            throw new RobotAuthoringBundleException($"ZIP archive uses an unsupported feature: {ex.Message}");
        }
    }

    private static RobotAuthoringBundle ParseCore(byte[] archiveBytes, string? sourceFileName)
    {
        if (archiveBytes.LongLength is <= 0 or > MaximumArchiveBytes)
            throw new RobotAuthoringBundleException($"Bundle must be between 1 and {MaximumArchiveBytes} bytes.");

        using var stream = new MemoryStream(archiveBytes, writable: false);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        if (archive.Entries.Count is 0 or > MaximumEntries)
            throw new RobotAuthoringBundleException($"Bundle must contain between 1 and {MaximumEntries} entries.");

        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        var orderedEntries = new List<KeyValuePair<string, ZipArchiveEntry>>();
        long expandedBytes = 0;
        foreach (var entry in archive.Entries)
        {
            var normalized = NormalizeEntryName(entry.FullName);
            if (normalized.EndsWith('/')) continue;
            if (!entries.TryAdd(normalized, entry))
                throw new RobotAuthoringBundleException($"Duplicate archive entry '{normalized}'.");
            orderedEntries.Add(new KeyValuePair<string, ZipArchiveEntry>(normalized, entry));
            if (IsSymbolicLink(entry))
                throw new RobotAuthoringBundleException($"Symbolic-link archive entry '{normalized}' is not allowed.");
            expandedBytes = checked(expandedBytes + entry.Length);
            if (expandedBytes > MaximumExpandedBytes)
                throw new RobotAuthoringBundleException("Bundle expanded size exceeds the configured limit.");
            if (entry.CompressedLength > 0 && entry.Length / (double)entry.CompressedLength > 100)
                throw new RobotAuthoringBundleException($"Archive entry '{normalized}' has an unsafe compression ratio.");
        }

        if (!entries.TryGetValue("export-manifest.json", out var manifestEntry))
            return ParseRawLuaArchive(orderedEntries, archiveBytes, sourceFileName);

        var manifestBytes = ReadEntry(manifestEntry, "export-manifest.json", MaximumManifestBytes);
        RobotAuthoringExportManifest manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<RobotAuthoringExportManifest>(TrimUtf8Bom(manifestBytes), JsonOptions)
                ?? throw new RobotAuthoringBundleException("Export manifest is empty.");
        }
        catch (JsonException ex)
        {
            throw new RobotAuthoringBundleException($"Export manifest is invalid: {ex.Message}");
        }

        ValidateManifest(manifest);
        var items = new List<RobotAuthoringBundleItem>(manifest.Program.Artifacts.Count);
        foreach (var manifestItem in manifest.Program.Artifacts.OrderBy(x => x.RunOrder))
        {
            var luaPath = $"artifacts/{NormalizeLeafName(manifestItem.FileName)}";
            var luaBytes = ReadEntry(entries, luaPath, MaximumLuaBytes);
            RobotAuthoringSidecar sidecar;
            byte[] sidecarBytes;
            if (string.IsNullOrWhiteSpace(manifestItem.SidecarFileName))
            {
                sidecarBytes = [];
                sidecar = CreateMinimalSidecar(manifest.Program, manifestItem);
            }
            else
            {
                var sidecarPath = $"contracts/{NormalizeLeafName(manifestItem.SidecarFileName)}";
                sidecarBytes = ReadEntry(entries, sidecarPath, MaximumSidecarBytes);
                try
                {
                    sidecar = JsonSerializer.Deserialize<RobotAuthoringSidecar>(TrimUtf8Bom(sidecarBytes), JsonOptions)
                        ?? throw new RobotAuthoringBundleException($"Sidecar '{sidecarPath}' is empty.");
                }
                catch (JsonException ex)
                {
                    throw new RobotAuthoringBundleException($"Sidecar '{sidecarPath}' is invalid: {ex.Message}");
                }
                ValidateSidecar(manifest.Program, manifestItem, sidecar);
            }
            items.Add(new RobotAuthoringBundleItem(manifestItem, sidecar, luaBytes,
                Sha256(luaBytes), Sha256(sidecarBytes)));
        }

        return new RobotAuthoringBundle(manifest, items, RobotRuntimeProfileSource.BundleDeclared);
    }

    private static RobotAuthoringBundle ParseRawLuaArchive(
        IReadOnlyList<KeyValuePair<string, ZipArchiveEntry>> orderedEntries,
        byte[] archiveBytes,
        string? sourceFileName)
    {
        var luaEntries = orderedEntries
            .Where(entry => entry.Key.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (luaEntries.Length is < 1 or > 50 || luaEntries.Length != orderedEntries.Count)
        {
            throw new RobotAuthoringBundleException(
                "A ZIP without export-manifest.json must contain one to 50 non-empty .lua files only.");
        }

        var fileNames = luaEntries.Select(entry => NormalizeLeafName(entry.Key)).ToArray();
        if (fileNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != fileNames.Length)
            throw new RobotAuthoringBundleException("Raw Lua ZIP file names must be unique.");

        var exportId = CreateDeterministicExportId(archiveBytes);
        var artifacts = new List<RobotAuthoringManifestArtifact>(luaEntries.Length);
        var items = new List<RobotAuthoringBundleItem>(luaEntries.Length);
        for (var index = 0; index < luaEntries.Length; index++)
        {
            var fileName = fileNames[index];
            var artifactCode = CreateRawCode(fileName, index + 1);
            var luaBytes = ReadEntry(luaEntries[index].Value, luaEntries[index].Key, MaximumLuaBytes);
            var manifestItem = new RobotAuthoringManifestArtifact
            {
                ArtifactCode = artifactCode,
                FileName = fileName,
                SidecarFileName = string.Empty,
                RunOrder = index + 1
            };
            artifacts.Add(manifestItem);
            items.Add(new RobotAuthoringBundleItem(
                manifestItem,
                new RobotAuthoringSidecar
                {
                    SchemaVersion = 1,
                    ArtifactCode = artifactCode,
                    ArtifactFileName = fileName,
                    RuntimeTargetCode = DefaultRuntimeTargetCode,
                    MachineModelCode = DefaultMachineModelCode,
                    Effects = [],
                    OrderingConstraints = []
                },
                luaBytes,
                Sha256(luaBytes),
                Sha256(Array.Empty<byte>())));
        }

        return new RobotAuthoringBundle(
            new RobotAuthoringExportManifest
            {
                SchemaVersion = 1,
                ExportId = exportId,
                ExportedAt = DateTimeOffset.UnixEpoch,
                Program = new RobotAuthoringManifestProgram
                {
                    Code = CreateRawProgramCode(sourceFileName, exportId),
                    Name = CreateRawProgramName(sourceFileName, exportId),
                    RuntimeTargetCode = DefaultRuntimeTargetCode,
                    MachineModelCode = DefaultMachineModelCode,
                    Artifacts = artifacts
                }
            },
            items,
            RobotRuntimeProfileSource.SystemDefault);
    }

    private static void ValidateManifest(RobotAuthoringExportManifest manifest)
    {
        if (manifest.SchemaVersion != 1 || manifest.ExportId == Guid.Empty)
            throw new RobotAuthoringBundleException("Manifest schemaVersion 1 and exportId are required.");
        if (manifest.Program is null)
            throw new RobotAuthoringBundleException("Manifest program is required.");
        if (string.IsNullOrWhiteSpace(manifest.Program.Code) || string.IsNullOrWhiteSpace(manifest.Program.Name) ||
            string.IsNullOrWhiteSpace(manifest.Program.RuntimeTargetCode) ||
            string.IsNullOrWhiteSpace(manifest.Program.MachineModelCode))
            throw new RobotAuthoringBundleException("Program code, name, runtime target, and machine model are required.");
        RequireLength(manifest.Program.Code, MaximumCodeLength, "Program code");
        RequireLength(manifest.Program.Name, MaximumNameLength, "Program name");
        RequireLength(manifest.Program.RuntimeTargetCode, MaximumCodeLength, "Runtime target code");
        RequireLength(manifest.Program.MachineModelCode, MaximumCodeLength, "Machine model code");
        if (manifest.Program.Artifacts is null || manifest.Program.Artifacts.Count == 0)
            throw new RobotAuthoringBundleException("A bundle must contain at least one artifact.");
        if (manifest.Program.Artifacts.Any(artifact => artifact is null))
            throw new RobotAuthoringBundleException("Manifest artifacts cannot contain null items.");

        foreach (var artifact in manifest.Program.Artifacts)
        {
            if (string.IsNullOrWhiteSpace(artifact.ArtifactCode) || string.IsNullOrWhiteSpace(artifact.FileName))
                throw new RobotAuthoringBundleException("Artifact code and Lua file name are required.");
            RequireLength(artifact.ArtifactCode, MaximumCodeLength, "Artifact code");
            RequireLength(artifact.FileName, MaximumFileNameLength, "Lua file name");
            if (!artifact.FileName.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
                throw new RobotAuthoringBundleException($"Artifact '{artifact.ArtifactCode}' must reference a .lua file.");
            NormalizeLeafName(artifact.FileName);
            if (!string.IsNullOrWhiteSpace(artifact.SidecarFileName))
            {
                RequireLength(artifact.SidecarFileName, MaximumFileNameLength, "Sidecar file name");
                if (!artifact.SidecarFileName.EndsWith(".icebot.json", StringComparison.OrdinalIgnoreCase))
                    throw new RobotAuthoringBundleException(
                        $"Artifact '{artifact.ArtifactCode}' sidecar must use the .icebot.json extension.");
                NormalizeLeafName(artifact.SidecarFileName);
            }
        }

        var ordered = manifest.Program.Artifacts.Select(x => x.RunOrder).Order().ToArray();
        if (!ordered.SequenceEqual(Enumerable.Range(1, ordered.Length)))
            throw new RobotAuthoringBundleException("Artifact runOrder values must be unique and contiguous from 1.");
        if (manifest.Program.Artifacts.GroupBy(x => x.ArtifactCode.Trim(), StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            throw new RobotAuthoringBundleException("Artifact codes must be unique within one export.");
    }

    private static void ValidateSidecar(RobotAuthoringManifestProgram program, RobotAuthoringManifestArtifact item,
        RobotAuthoringSidecar sidecar)
    {
        if (sidecar.SchemaVersion is not 1 and not 2)
            throw new RobotAuthoringBundleException($"Sidecar '{item.SidecarFileName}' must use schema version 1 or 2.");
        if (!EqualsCode(sidecar.ArtifactCode, item.ArtifactCode) ||
            !string.Equals(sidecar.ArtifactFileName, item.FileName, StringComparison.OrdinalIgnoreCase))
            throw new RobotAuthoringBundleException($"Sidecar '{item.SidecarFileName}' does not match its manifest artifact.");
        if (!EqualsCode(sidecar.RuntimeTargetCode, program.RuntimeTargetCode) ||
            !EqualsCode(sidecar.MachineModelCode, program.MachineModelCode))
            throw new RobotAuthoringBundleException(
                $"Sidecar '{item.SidecarFileName}' runtime target and machine model must match the program manifest.");
    }

    private static RobotAuthoringSidecar CreateMinimalSidecar(
        RobotAuthoringManifestProgram program,
        RobotAuthoringManifestArtifact item) => new()
        {
            SchemaVersion = 1,
            ArtifactCode = item.ArtifactCode,
            ArtifactFileName = item.FileName,
            RuntimeTargetCode = program.RuntimeTargetCode,
            MachineModelCode = program.MachineModelCode
        };

    private static byte[] ReadEntry(IReadOnlyDictionary<string, ZipArchiveEntry> entries, string path, long maximumBytes)
    {
        if (!entries.TryGetValue(path, out var entry))
            throw new RobotAuthoringBundleException($"Required archive entry '{path}' was not found.");
        return ReadEntry(entry, path, maximumBytes);
    }

    private static byte[] ReadEntry(ZipArchiveEntry entry, string path, long maximumBytes)
    {
        if (entry.Length <= 0 || entry.Length > maximumBytes)
            throw new RobotAuthoringBundleException($"Archive entry '{path}' has an invalid size.");
        using var source = entry.Open();
        using var destination = new MemoryStream((int)entry.Length);
        source.CopyTo(destination);
        if (destination.Length != entry.Length)
            throw new RobotAuthoringBundleException($"Archive entry '{path}' length changed while reading.");
        return destination.ToArray();
    }

    private static string NormalizeEntryName(string value)
    {
        var normalized = value.Replace('\\', '/');
        if (normalized.StartsWith('/') || normalized.Contains(':') ||
            normalized.Split('/').Any(segment => segment is "" or "." or ".."))
            throw new RobotAuthoringBundleException($"Unsafe archive entry path '{value}'.");
        return normalized;
    }

    private static string NormalizeLeafName(string value)
    {
        var normalized = NormalizeEntryName(value);
        if (normalized.Contains('/'))
            throw new RobotAuthoringBundleException($"Manifest file name '{value}' must not contain a directory.");
        return normalized;
    }

    private static bool IsSymbolicLink(ZipArchiveEntry entry) =>
        ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000;

    private static bool EqualsCode(string left, string right) =>
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static void RequireLength(string value, int maximumLength, string fieldName)
    {
        if (value.Trim().Length > maximumLength)
            throw new RobotAuthoringBundleException($"{fieldName} must be at most {maximumLength} characters.");
    }

    public static string Sha256(byte[] value) =>
        Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();

    private static Guid CreateDeterministicExportId(byte[] archiveBytes) =>
        Guid.ParseExact(Sha256(archiveBytes)[..32], "N");

    private static string CreateRawProgramCode(string? sourceFileName, Guid exportId)
    {
        var sourceCode = CreateNormalizedCode(Path.GetFileNameWithoutExtension(sourceFileName ?? string.Empty));
        return string.IsNullOrWhiteSpace(sourceCode)
            ? $"RAW-LUA-{exportId:N}"[..20]
            : sourceCode[..Math.Min(sourceCode.Length, MaximumCodeLength)];
    }

    private static string CreateRawProgramName(string? sourceFileName, Guid exportId)
    {
        var name = Path.GetFileNameWithoutExtension(sourceFileName ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(name)
            ? $"Raw Lua {exportId:N}"[..20]
            : name[..Math.Min(name.Length, MaximumNameLength)];
    }

    private static string CreateRawCode(string fileName, int ordinal)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var code = CreateNormalizedCode(stem);
        if (string.IsNullOrWhiteSpace(code)) code = "RAW-LUA";
        var suffix = $"-{ordinal}";
        return $"{code[..Math.Min(code.Length, MaximumCodeLength - suffix.Length)]}{suffix}";
    }

    private static string CreateNormalizedCode(string value)
    {
        var characters = new List<char>(value.Length);
        var previousSeparator = false;
        foreach (var character in value)
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                characters.Add(char.ToUpperInvariant(character));
                previousSeparator = false;
            }
            else if (!previousSeparator)
            {
                characters.Add('-');
                previousSeparator = true;
            }
        }

        return new string(characters.ToArray()).Trim('-');
    }

    private static ReadOnlySpan<byte> TrimUtf8Bom(byte[] value) =>
        value.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }) ? value.AsSpan(3) : value;
}
