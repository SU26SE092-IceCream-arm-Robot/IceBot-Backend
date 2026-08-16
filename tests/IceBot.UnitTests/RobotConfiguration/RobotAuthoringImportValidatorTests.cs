using Application.RobotConfiguration.AuthoringImports;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.AuthoringImports;
using NSubstitute;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class RobotAuthoringImportValidatorTests
{
    [Fact]
    public async Task ExistingArtifactWithDifferentRuntimeProfile_IsAConflict()
    {
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var session = RobotAuthoringImport.Create(
            organizationId, null, null, null, Guid.NewGuid(), new string('a', 64), "idem", 1,
            "PROGRAM", "Program", "FAIRINO_LUA_V1", "FR5", "staging/import.zip", actorId,
            RobotRuntimeProfileSource.BundleDeclared);
        var artifact = RobotArtifact.CreateDraft(
            organizationId, "STEP", "Step", "objects/step.lua", "step.lua", new string('b', 64),
            "FAIRINO_LUA_V1", "FR3", 10, DateTimeOffset.UtcNow);
        var bundle = new RobotAuthoringBundle(
            new RobotAuthoringExportManifest
            {
                SchemaVersion = 1,
                ExportId = session.ClientExportId,
                ExportedAt = DateTimeOffset.UtcNow,
                Program = new RobotAuthoringManifestProgram
                {
                    Code = "PROGRAM",
                    Name = "Program",
                    RuntimeTargetCode = "FAIRINO_LUA_V1",
                    MachineModelCode = "FR5",
                    Artifacts =
                    [
                        new RobotAuthoringManifestArtifact
                        {
                            ArtifactCode = "STEP",
                            FileName = "step.lua",
                            RunOrder = 1
                        }
                    ]
                }
            },
            [
                new RobotAuthoringBundleItem(
                    new RobotAuthoringManifestArtifact
                    {
                        ArtifactCode = "STEP",
                        FileName = "step.lua",
                        RunOrder = 1
                    },
                    new RobotAuthoringSidecar
                    {
                        SchemaVersion = 1,
                        ArtifactCode = "STEP",
                        ArtifactFileName = "step.lua",
                        RuntimeTargetCode = "FAIRINO_LUA_V1",
                        MachineModelCode = "FR5"
                    },
                    [], artifact.Checksum, new string('c', 64))
            ],
            RobotRuntimeProfileSource.BundleDeclared);
        var store = Substitute.For<IRobotAuthoringImportStore>();
        store.GetArtifactsAsync(organizationId, Arg.Any<IReadOnlyCollection<string>>(), false,
                Arg.Any<CancellationToken>())
            .Returns([artifact]);
        store.GetProgramAsync(organizationId, null, null, null, "PROGRAM", false,
                Arg.Any<CancellationToken>())
            .Returns((Domain.RobotConfiguration.Programs.RobotProgram?)null);

        var report = await new RobotAuthoringImportValidator(store)
            .BuildReportAsync(session, bundle, CancellationToken.None);

        Assert.False(report.CanMaterialize);
        Assert.Contains(report.Errors, issue => issue.Code == "ARTIFACT_RUNTIME_PROFILE_CONFLICT");
    }
}
