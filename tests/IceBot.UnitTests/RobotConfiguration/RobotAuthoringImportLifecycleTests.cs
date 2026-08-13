using System.Text.Json;
using Application.RobotConfiguration.AuthoringImports;
using Domain.RobotConfiguration.AuthoringImports;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class RobotAuthoringImportLifecycleTests
{
    [Fact]
    public void ValidatedImport_AdvertisesResumeAction()
    {
        var importSession = CreateImport();
        var validation = new RobotAuthoringImportValidationReport(true, [], [], 0, 1, 0, 1);
        importSession.MarkValidated(
            JsonSerializer.Serialize(validation),
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        var result = RobotAuthoringImportResult.From(importSession);

        Assert.Equal("Validated", result.Status);
        Assert.True(result.Validation?.CanMaterialize);
        Assert.Contains("ResumeImport", result.NextActions);
        Assert.DoesNotContain("ApplyImport", result.NextActions);
    }

    [Fact]
    public void MaterializedAndPublishedImport_UsePublicLifecycleNames()
    {
        var importSession = CreateImport();
        var now = DateTimeOffset.UtcNow;
        var programId = Guid.NewGuid();
        importSession.MarkValidated(
            JsonSerializer.Serialize(new RobotAuthoringImportValidationReport(true, [], [], 0, 1, 0, 1)),
            now,
            Guid.NewGuid());
        importSession.MarkApplied(programId, now, Guid.NewGuid());

        var materialized = RobotAuthoringImportResult.From(importSession);

        Assert.Equal("Materialized", materialized.Status);
        Assert.Equal(programId, materialized.MaterializedRobotProgramId);
        Assert.Equal(now, materialized.MaterializedAt);
        Assert.Contains("PublishImportResources", materialized.NextActions);

        importSession.MarkPublished(now.AddMinutes(1), Guid.NewGuid());
        var published = RobotAuthoringImportResult.From(importSession);

        Assert.Equal("ResourcesPublished", published.Status);
    }

    [Fact]
    public void PublishingTechnicalResources_DoesNotCreateOrRequireRecipeComposition()
    {
        var importSession = CreateImport();
        var now = DateTimeOffset.UtcNow;
        importSession.MarkValidated("{}", now, Guid.NewGuid());
        importSession.MarkApplied(Guid.NewGuid(), now, Guid.NewGuid());

        importSession.MarkPublished(now.AddMinutes(1), Guid.NewGuid());

        Assert.Null(importSession.ComposedRecipeId);
        Assert.NotNull(importSession.PublishedAt);
        Assert.Contains("CreateProductionBinding", RobotAuthoringImportResult.From(importSession).NextActions);
    }

    private static RobotAuthoringImport CreateImport() => RobotAuthoringImport.Create(
        Guid.NewGuid(),
        null,
        null,
        null,
        Guid.NewGuid(),
        new string('a', 64),
        Guid.NewGuid().ToString("N"),
        1,
        "MAKE_ICE_CREAM",
        "Make ice cream",
        "FAIRINO_LUA_V1",
        "FR5",
        "robot-authoring-imports/staged.zip",
        Guid.NewGuid());
}
