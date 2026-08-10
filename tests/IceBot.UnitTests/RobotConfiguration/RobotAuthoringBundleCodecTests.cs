using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Application.RobotConfiguration.AuthoringImports;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class RobotAuthoringBundleCodecTests
{
    [Fact]
    public void Parse_ValidBundle_PreservesExplicitRunOrderAndChecksums()
    {
        var bytes = CreateBundle();

        var bundle = RobotAuthoringBundleCodec.Parse(bytes);

        Assert.Equal("MAKE_ICE_CREAM", bundle.Manifest.Program.Code);
        Assert.Collection(bundle.Items,
            first => { Assert.Equal(1, first.ManifestItem.RunOrder); Assert.Equal("PREPARE", first.Sidecar.ArtifactCode); },
            second => { Assert.Equal(2, second.ManifestItem.RunOrder); Assert.Equal("DISPENSE", second.Sidecar.ArtifactCode); });
        Assert.All(bundle.Items, item => Assert.Equal(64, item.LuaChecksum.Length));
    }

    [Fact]
    public void Parse_NonContiguousRunOrder_RejectsBundle()
    {
        var bytes = CreateBundle(secondRunOrder: 3);

        var exception = Assert.Throws<RobotAuthoringBundleException>(() => RobotAuthoringBundleCodec.Parse(bytes));

        Assert.Contains("contiguous", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_PathTraversalEntry_RejectsBundle()
    {
        var bytes = CreateBundle(extraEntry: "../escape.lua");

        var exception = Assert.Throws<RobotAuthoringBundleException>(() => RobotAuthoringBundleCodec.Parse(bytes));

        Assert.Contains("Unsafe archive entry", exception.Message);
    }

    [Fact]
    public void Parse_V2IngredientEffect_PreservesExplicitProductionSemantics()
    {
        var bytes = CreateSingleV2Bundle(fixedQuantity: 100, unit: "gram");

        var bundle = RobotAuthoringBundleCodec.Parse(bytes);

        var sidecar = Assert.Single(bundle.Items).Sidecar;
        Assert.Equal(2, sidecar.SchemaVersion);
        var effect = Assert.Single(sidecar.Effects);
        Assert.Equal("ICE_CREAM_BASE", effect.IngredientCode);
        Assert.Equal(100, effect.FixedQuantity);
        Assert.Equal("DISPENSER", effect.RequiredWorkcellCapabilityCode);
    }

    [Fact]
    public void Parse_V2FixedQuantityWithoutUnit_RejectsBundle()
    {
        var bytes = CreateSingleV2Bundle(fixedQuantity: 100, unit: null);

        var exception = Assert.Throws<RobotAuthoringBundleException>(() => RobotAuthoringBundleCodec.Parse(bytes));

        Assert.Contains("positive value and unit", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_V2SystemEffectWithOptionCode_RejectsBundle()
    {
        var bytes = CreateSingleSemanticBundle(2, null, null, "System", null, "OREO", "None");

        var exception = Assert.Throws<RobotAuthoringBundleException>(() => RobotAuthoringBundleCodec.Parse(bytes));

        Assert.Contains("cannot declare ingredientCode or optionCode", exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_NumericEffectKind_RejectsBundleEvenWhenNumberMapsToKnownEnum()
    {
        var bytes = CreateSingleSemanticBundle(2, 10, "gram", 1);

        var exception = Assert.Throws<RobotAuthoringBundleException>(() => RobotAuthoringBundleCodec.Parse(bytes));

        Assert.Contains("is invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_UndefinedNumericConstraintType_RejectsBundle()
    {
        var bytes = CreateSingleSemanticBundle(2, 10, "gram", constraintType: 999);

        var exception = Assert.Throws<RobotAuthoringBundleException>(() => RobotAuthoringBundleCodec.Parse(bytes));

        Assert.Contains("is invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_CompositeEffect_RejectsCurrentAuthoringSchema()
    {
        var bytes = CreateSingleSemanticBundle(2, null, null, "Composite", null, null, "None");

        var exception = Assert.Throws<RobotAuthoringBundleException>(() => RobotAuthoringBundleCodec.Parse(bytes));

        Assert.Contains("not supported by authoring schema V2", exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_ParameterizedQuantity_IsAnOperatorDeclarationNotRuntimeProof()
    {
        var bytes = CreateSingleSemanticBundle(2, null, "gram", quantityMode: "Parameterized");

        var bundle = RobotAuthoringBundleCodec.Parse(bytes);

        Assert.Equal("Parameterized", Assert.Single(Assert.Single(bundle.Items).Sidecar.Effects).QuantityMode.ToString());
    }

    [Fact]
    public void Parse_EmptyEffects_AllowsBlackBoxArtifact()
    {
        var bytes = CreateSingleSemanticBundle(1, null, null, emptyEffects: true);

        var bundle = RobotAuthoringBundleCodec.Parse(bytes);

        Assert.Empty(Assert.Single(bundle.Items).Sidecar.Effects);
    }

    [Fact]
    public void Parse_V2OptionEffectMayIdentifyItsConsumedIngredient()
    {
        var bytes = CreateSingleSemanticBundle(2, 10, "gram", "Option", "OREO_CRUMB", "OREO");

        var effect = Assert.Single(Assert.Single(RobotAuthoringBundleCodec.Parse(bytes).Items).Sidecar.Effects);

        Assert.Equal("OREO", effect.OptionCode);
        Assert.Equal("OREO_CRUMB", effect.IngredientCode);
    }

    [Fact]
    public void Parse_NullManifestArtifacts_RejectsBundleWithoutNullReference()
    {
        var bytes = CreateManifestWithNullArtifacts();

        var exception = Assert.Throws<RobotAuthoringBundleException>(() => RobotAuthoringBundleCodec.Parse(bytes));

        Assert.Contains("at least one artifact", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Parse_NullSidecarCollections_RejectsBundleWithoutNullReference(
        bool nullEffects, bool nullOrderingConstraints)
    {
        var bytes = CreateSingleSemanticBundle(2, 100, "gram",
            nullEffects: nullEffects, nullOrderingConstraints: nullOrderingConstraints);

        Assert.Throws<RobotAuthoringBundleException>(() => RobotAuthoringBundleCodec.Parse(bytes));
    }

    [Fact]
    public void Parse_V1IngredientEffect_RejectsProductionSemantics()
    {
        var bytes = CreateSingleSemanticBundle(schemaVersion: 1, fixedQuantity: 100, unit: "gram");

        var exception = Assert.Throws<RobotAuthoringBundleException>(() => RobotAuthoringBundleCodec.Parse(bytes));

        Assert.Contains("schema version 1 is opaque", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_NonLuaArtifactFile_RejectsBundleBeforeStaging()
    {
        var bytes = CreateBundle(firstLuaFileName: "01_prepare.txt");

        var exception = Assert.Throws<RobotAuthoringBundleException>(() => RobotAuthoringBundleCodec.Parse(bytes));

        Assert.Contains("must reference a .lua file", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_OversizedEffectCode_RejectsBundleBeforeStaging()
    {
        var bytes = CreateBundle(firstEffectCode: new string('E', 101));

        var exception = Assert.Throws<RobotAuthoringBundleException>(() => RobotAuthoringBundleCodec.Parse(bytes));

        Assert.Contains("Effect code", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("at most 100", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] CreateBundle(
        int secondRunOrder = 2,
        string? extraEntry = null,
        string firstLuaFileName = "01_prepare.lua",
        string? firstEffectCode = null)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(archive, "export-manifest.json", JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                exportId = Guid.NewGuid(),
                exportedAt = DateTimeOffset.UtcNow,
                program = new
                {
                    code = "MAKE_ICE_CREAM",
                    name = "Make ice cream",
                    runtimeTargetCode = "FAIRINO_LUA_V1",
                    machineModelCode = "FR5",
                    artifacts = new[]
                    {
                        new { artifactCode = "PREPARE", fileName = firstLuaFileName, sidecarFileName = "01_prepare.icebot.json", runOrder = 1 },
                        new { artifactCode = "DISPENSE", fileName = "02_dispense.lua", sidecarFileName = "02_dispense.icebot.json", runOrder = secondRunOrder }
                    }
                }
            }));
            WriteArtifact(archive, "PREPARE", firstLuaFileName, "01_prepare.icebot.json", "PREPARE", 1,
                firstEffectCode);
            WriteArtifact(archive, "DISPENSE", "02_dispense.lua", "02_dispense.icebot.json", "BASE", 2);
            if (extraEntry is not null) Write(archive, extraEntry, "return 0");
        }
        return output.ToArray();
    }

    private static void WriteArtifact(ZipArchive archive, string code, string luaName, string sidecarName,
        string phase, int sortHint, string? effectCode = null)
    {
        Write(archive, $"artifacts/{luaName}", "-- generated\nreturn 0");
        Write(archive, $"contracts/{sidecarName}", JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            artifactCode = code,
            artifactFileName = luaName,
            runtimeTargetCode = "FAIRINO_LUA_V1",
            machineModelCode = "FR5",
            effects = new[] { new { effectCode = effectCode ?? $"{code}_EXECUTE", effectKind = "Motion", quantityMode = "None" } },
            orderingConstraints = new[] { new { constraintType = "Phase", value = phase, sortHint } }
        }));
    }

    private static byte[] CreateSingleV2Bundle(decimal fixedQuantity, string? unit)
        => CreateSingleSemanticBundle(2, fixedQuantity, unit);

    private static byte[] CreateSingleSemanticBundle(int schemaVersion, decimal? fixedQuantity, string? unit,
        object? effectKind = null, string? ingredientCode = "ICE_CREAM_BASE", string? optionCode = null,
        object? quantityMode = null, object? constraintType = null,
        bool nullEffects = false, bool nullOrderingConstraints = false, bool emptyEffects = false)
    {
        effectKind ??= "Ingredient";
        quantityMode ??= "FixedInArtifact";
        constraintType ??= "Phase";
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(archive, "export-manifest.json", JsonSerializer.Serialize(new
            {
                schemaVersion = 1, exportId = Guid.NewGuid(), exportedAt = DateTimeOffset.UtcNow,
                program = new
                {
                    code = "MAKE_ICE_CREAM", name = "Make ice cream", runtimeTargetCode = "FAIRINO_LUA_V1",
                    machineModelCode = "FR5",
                    artifacts = new[] { new { artifactCode = "DISPENSE", fileName = "dispense.lua", sidecarFileName = "dispense.icebot.json", runOrder = 1 } }
                }
            }));
            Write(archive, "artifacts/dispense.lua", "-- generated\nreturn 0");
            object? effects = nullEffects ? null : emptyEffects ? Array.Empty<object>() : new object[]
            {
                new { effectCode = "DISPENSE_BASE", effectKind, ingredientCode, optionCode,
                    quantityMode, fixedQuantity, unit, requiredWorkcellCapabilityCode = "DISPENSER" }
            };
            object? orderingConstraints = nullOrderingConstraints ? null : new object[]
            {
                new { constraintType, value = "BASE", sortHint = 1 }
            };
            Write(archive, "contracts/dispense.icebot.json", JsonSerializer.Serialize(new
            {
                schemaVersion, artifactCode = "DISPENSE", artifactFileName = "dispense.lua",
                runtimeTargetCode = "FAIRINO_LUA_V1", machineModelCode = "FR5",
                effects,
                orderingConstraints
            }));
        }
        return output.ToArray();
    }

    private static byte[] CreateManifestWithNullArtifacts()
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(archive, "export-manifest.json", JsonSerializer.Serialize(new
            {
                schemaVersion = 1, exportId = Guid.NewGuid(), exportedAt = DateTimeOffset.UtcNow,
                program = new
                {
                    code = "MAKE_ICE_CREAM", name = "Make ice cream", runtimeTargetCode = "FAIRINO_LUA_V1",
                    machineModelCode = "FR5", artifacts = (object?)null
                }
            }));
        }
        return output.ToArray();
    }

    private static void Write(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8, leaveOpen: false);
        writer.Write(content);
    }
}
