using Domain.Common.Enums;
using Domain.RobotConfiguration.AuthoringImports;
using Domain.Tenants.Entities;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.RobotConfiguration.AuthoringImports.Persistence;
using Application.Shared.Concurrency;
using Infrastructure.Concurrency;
using Domain.RobotConfiguration.Programs;
using Domain.Tenants.Enums;
using Infrastructure.RobotConfiguration.Programs.Persistence;
using Application.RobotConfiguration.Programs.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace IceBot.IntegrationTests.RobotConfiguration;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class RobotAuthoringImportPersistenceIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task ConcurrentSameIdempotencyKey_ConvergesToOneImport()
    {
        var organizationId = await SeedOrganizationAsync();
        var actorId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString("N");
        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();
        var firstStore = new RobotAuthoringImportStore(firstContext);
        var secondStore = new RobotAuthoringImportStore(secondContext);

        var results = await Task.WhenAll(
            firstStore.InsertOrGetExistingAsync(CreateImport(organizationId, actorId, idempotencyKey), default),
            secondStore.InsertOrGetExistingAsync(CreateImport(organizationId, actorId, idempotencyKey), default));

        Assert.Single(results.Select(result => result.Import.Id).Distinct());
        Assert.Single(results, result => result.Created);
        await using var assertionContext = fixture.CreateDbContext();
        Assert.Equal(1, await assertionContext.RobotAuthoringImports.CountAsync(item =>
            item.OrganizationId == organizationId && item.IdempotencyKey == idempotencyKey));
    }

    [IntegrationFact]
    public async Task ApplyTransactionRollback_DoesNotPersistImportMutation()
    {
        var organizationId = await SeedOrganizationAsync();
        var actorId = Guid.NewGuid();
        await using var db = fixture.CreateDbContext();
        var store = new RobotAuthoringImportStore(db);
        var inserted = await store.InsertOrGetExistingAsync(
            CreateImport(organizationId, actorId, Guid.NewGuid().ToString("N")), default);

        var tracked = await store.BeginMutationAsync(organizationId, inserted.Import.Id, default);
        Assert.NotNull(tracked);
        await store.LockMaterializationResourceIdentitiesAsync(
            organizationId, null, null, null, tracked!.ProposedProgramCode,
            tracked.Items.Select(item => item.ArtifactCode).ToArray(), default);
        tracked.MarkValidated("{\"canApply\":true,\"errors\":[],\"warnings\":[],\"existingArtifactCount\":0,\"newArtifactCount\":1,\"existingContractCount\":0,\"newContractCount\":1}",
            DateTimeOffset.UtcNow, actorId);
        await store.SaveChangesAsync(default);
        await store.RollbackMutationAsync(default);

        await using var assertionContext = fixture.CreateDbContext();
        var persisted = await assertionContext.RobotAuthoringImports.AsNoTracking()
            .SingleAsync(item => item.Id == inserted.Import.Id);
        Assert.Equal(RobotAuthoringImportStatus.Uploaded, persisted.Status);
        Assert.Null(persisted.ValidationReportJson);
    }

    [IntegrationFact]
    public async Task ConcurrentImportMutations_AreSerializedByImportIdentity()
    {
        var organizationId = await SeedOrganizationAsync();
        var actorId = Guid.NewGuid();
        Guid importId;
        await using (var seedContext = fixture.CreateDbContext())
        {
            var seedStore = new RobotAuthoringImportStore(seedContext);
            var inserted = await seedStore.InsertOrGetExistingAsync(
                CreateImport(organizationId, actorId, Guid.NewGuid().ToString("N")), default);
            importId = inserted.Import.Id;
        }

        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();
        var firstStore = new RobotAuthoringImportStore(firstContext);
        var secondStore = new RobotAuthoringImportStore(secondContext);

        Assert.NotNull(await firstStore.BeginMutationAsync(organizationId, importId, default));
        var secondMutation = secondStore.BeginMutationAsync(organizationId, importId, default);
        await Task.Delay(100);
        Assert.False(secondMutation.IsCompleted);

        await firstStore.RollbackMutationAsync(default);
        Assert.NotNull(await secondMutation.WaitAsync(TimeSpan.FromSeconds(5)));
        await secondStore.RollbackMutationAsync(default);
    }

    [IntegrationFact]
    public async Task ConcurrentTechnicalResourceMutations_AreSerializedByResourceIdentity()
    {
        var artifactId = Guid.NewGuid();
        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();
        var firstCoordinator = new PostgresTechnicalResourceMutationCoordinator(firstContext);
        var secondCoordinator = new PostgresTechnicalResourceMutationCoordinator(secondContext);
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var contractId = Guid.NewGuid();
        var artifactIdentity = TechnicalResourceMutationIdentity.Artifact(artifactId);
        var contractIdentity = TechnicalResourceMutationIdentity.Contract(contractId);

        var firstMutation = firstCoordinator.ExecuteAsync([contractIdentity, artifactIdentity], async cancellationToken =>
        {
            firstEntered.SetResult();
            await releaseFirst.Task.WaitAsync(cancellationToken);
            return true;
        });
        await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var secondMutation = secondCoordinator.ExecuteAsync(
            [artifactIdentity, contractIdentity], _ => Task.FromResult(true));
        await Task.Delay(100);
        Assert.False(secondMutation.IsCompleted);

        releaseFirst.SetResult();
        Assert.True(await firstMutation.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(await secondMutation.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [IntegrationFact]
    public async Task DiscardDraftProgram_ReusesCoordinatorTransaction()
    {
        var organizationId = await SeedOrganizationAsync();
        var programId = Guid.NewGuid();
        await using (var seedContext = fixture.CreateDbContext())
        {
            var program = RobotProgram.CreateDraft(
                $"DISCARD-{Guid.NewGuid():N}", "Discard test", TenantScopeType.Organization, organizationId);
            program.Id = programId;
            seedContext.RobotPrograms.Add(program);
            await seedContext.SaveChangesAsync();
        }

        await using var db = fixture.CreateDbContext();
        var store = new RobotProgramStore(db);
        var coordinator = new PostgresTechnicalResourceMutationCoordinator(db);
        var outcome = await coordinator.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Program(programId)],
            async cancellationToken =>
            {
                var program = await store.GetProgramForEditAsync(programId, cancellationToken);
                Assert.NotNull(program);
                return await store.DiscardDraftProgramAsync(program!, cancellationToken);
            });

        Assert.Equal(RobotProgramDiscardOutcome.Deleted, outcome);
        await using var assertionContext = fixture.CreateDbContext();
        Assert.False(await assertionContext.RobotPrograms.AnyAsync(item => item.Id == programId));
    }

    private async Task<Guid> SeedOrganizationAsync()
    {
        await using var db = fixture.CreateDbContext();
        var organization = new Organization
        {
            Code = $"AUTHORING-{Guid.NewGuid():N}",
            Name = "Robot authoring integration",
            Status = EntityStatus.Active
        };
        db.Organizations.Add(organization);
        await db.SaveChangesAsync();
        return organization.Id;
    }

    private static RobotAuthoringImport CreateImport(Guid organizationId, Guid actorId, string idempotencyKey)
    {
        var import = RobotAuthoringImport.Create(
            organizationId, null, null, null, Guid.NewGuid(), new string('a', 64), idempotencyKey, 1,
            "MAKE_TEST", "Make test", "FAIRINO_LUA_V1", "FR5",
            $"robot-authoring-imports/{organizationId:D}/{Guid.NewGuid():N}.zip", actorId);
        import.AddItem("PREPARE", "01_prepare.lua", "01_prepare.icebot.json", 1,
            new string('b', 64), new string('c', 64));
        return import;
    }
}
