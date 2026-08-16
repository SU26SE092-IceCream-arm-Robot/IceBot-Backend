using Application.Identity.Tokens.Claims;
using Application.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.ArtifactContracts;
using IceBot.UnitTests.TestSupport;
using NSubstitute;
using Application.Shared.Concurrency;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class RobotArtifactTechnicalContractLifecycleTests
{
    [Fact]
    public async Task SidecarReimportReplacesExistingDraftDefinition()
    {
        var organizationId = Guid.NewGuid();
        var contract = RobotArtifactTechnicalContract.CreateDraft("TOPPING", 1, "FAIRINO_LUA_V1", "FR5", organizationId);
        contract.ReplaceDefinition([Effect("OLD")], []);
        var store = Substitute.For<IRobotArtifactTechnicalContractStore>();
        store.GetByIdentityAsync(organizationId, "TOPPING", 1, false, Arg.Any<CancellationToken>())
            .Returns(contract);
        store.GetAsync(contract.Id, true, Arg.Any<CancellationToken>()).Returns(contract);
        var handlers = new RobotArtifactTechnicalContractHandlers(
            store, InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handlers.ImportSidecarAsync(new ImportRobotArtifactTechnicalContractSidecarCommand
        {
            UserContext = OrganizationAdmin(organizationId),
            OrganizationId = organizationId,
            ContractCode = "topping",
            ContractVersion = 1,
            RuntimeTargetCode = "FAIRINO_LUA_V1",
            MachineModelCode = "FR5",
            Effects = [EffectRequest("NEW")]
        }, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("NEW", Assert.Single(contract.Effects).EffectCode);
        await store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SidecarReimportRejectsContractPublishedAfterInitialObservation()
    {
        var organizationId = Guid.NewGuid();
        var observedDraft = RobotArtifactTechnicalContract.CreateDraft(
            "TOPPING", 1, "FAIRINO_LUA_V1", "FR5", organizationId);
        observedDraft.ReplaceDefinition([Effect("OLD")], []);
        var lockedPublished = RobotArtifactTechnicalContract.CreateDraft(
            "TOPPING", 1, "FAIRINO_LUA_V1", "FR5", organizationId);
        lockedPublished.Id = observedDraft.Id;
        lockedPublished.ReplaceDefinition([Effect("OLD")], []);
        lockedPublished.Publish(DateTimeOffset.UtcNow, Guid.NewGuid());

        var store = Substitute.For<IRobotArtifactTechnicalContractStore>();
        store.GetByIdentityAsync(organizationId, "TOPPING", 1, false, Arg.Any<CancellationToken>())
            .Returns(observedDraft);
        store.GetAsync(observedDraft.Id, true, Arg.Any<CancellationToken>()).Returns(lockedPublished);
        var handlers = new RobotArtifactTechnicalContractHandlers(
            store, InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handlers.ImportSidecarAsync(new ImportRobotArtifactTechnicalContractSidecarCommand
        {
            UserContext = OrganizationAdmin(organizationId),
            OrganizationId = organizationId,
            ContractCode = "TOPPING",
            ContractVersion = 1,
            RuntimeTargetCode = "FAIRINO_LUA_V1",
            MachineModelCode = "FR5",
            Effects = [EffectRequest("NEW")]
        }, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("OLD", Assert.Single(lockedPublished.Effects).EffectCode);
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SidecarImportCreatesSchemaVersionTwoContract()
    {
        var organizationId = Guid.NewGuid();
        var store = Substitute.For<IRobotArtifactTechnicalContractStore>();
        var handlers = new RobotArtifactTechnicalContractHandlers(
            store, InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handlers.ImportSidecarAsync(new ImportRobotArtifactTechnicalContractSidecarCommand
        {
            UserContext = OrganizationAdmin(organizationId),
            OrganizationId = organizationId,
            ContractCode = "ADD_OREO",
            ContractVersion = 1,
            SchemaVersion = 2,
            RuntimeTargetCode = "FAIRINO_LUA_V1",
            MachineModelCode = "FR5",
            Effects =
            [
                new RobotArtifactEffectRequest("ADD_OREO", RobotArtifactEffectKind.Option,
                    "OREO_CRUMB", "OREO", RobotArtifactQuantityMode.FixedInArtifact,
                    10, "g", "TOPPING_STATION")
            ]
        }, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Data!.SchemaVersion);
        await store.Received(1).AddAsync(
            Arg.Is<RobotArtifactTechnicalContract>(contract => contract.SchemaVersion == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SidecarImportRejectsProductionSemanticsDeclaredAsVersionOne()
    {
        var organizationId = Guid.NewGuid();
        var store = Substitute.For<IRobotArtifactTechnicalContractStore>();
        var handlers = new RobotArtifactTechnicalContractHandlers(
            store, InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handlers.ImportSidecarAsync(new ImportRobotArtifactTechnicalContractSidecarCommand
        {
            UserContext = OrganizationAdmin(organizationId),
            OrganizationId = organizationId,
            ContractCode = "DISPENSE",
            ContractVersion = 1,
            SchemaVersion = 1,
            RuntimeTargetCode = "FAIRINO_LUA_V1",
            MachineModelCode = "FR5",
            Effects =
            [
                new RobotArtifactEffectRequest("DISPENSE", RobotArtifactEffectKind.Ingredient,
                    "ICE_CREAM_BASE", null, RobotArtifactQuantityMode.FixedInArtifact,
                    100, "g", "DISPENSER")
            ]
        }, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        await store.DidNotReceive().AddAsync(
            Arg.Any<RobotArtifactTechnicalContract>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetireIsBlockedWhilePublishedTemplateReferencesContract()
    {
        var contract = RobotArtifactTechnicalContract.CreateDraft("PREPARE", 1, "FAIRINO_LUA_V1", "FR5");
        contract.ReplaceDefinition([Effect("PREPARE")], []);
        contract.Publish(DateTimeOffset.UtcNow, Guid.NewGuid());
        var store = Substitute.For<IRobotArtifactTechnicalContractStore>();
        store.GetAsync(contract.Id, true, Arg.Any<CancellationToken>()).Returns(contract);
        store.HasPublishedTemplateReferenceAsync(contract.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handlers = new RobotArtifactTechnicalContractHandlers(
            store, InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handlers.RetireAsync(
            new RetireRobotArtifactTechnicalContractCommand(TestData.SystemAdmin(), null, contract.Id),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(RobotArtifactContractStatus.Published, contract.Status);
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OrganizationAdminCanReadGlobalPublishedContractCatalog()
    {
        var organizationId = Guid.NewGuid();
        var store = Substitute.For<IRobotArtifactTechnicalContractStore>();
        store.CountAsync(null, null, null, true, Arg.Any<CancellationToken>()).Returns(0);
        store.ListAsync(null, null, null, true, 1, 20, Arg.Any<CancellationToken>()).Returns([]);
        var handlers = new RobotArtifactTechnicalContractHandlers(
            store, InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handlers.ListAsync(
            new ListRobotArtifactTechnicalContractsQuery(OrganizationAdmin(organizationId), null),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        await store.Received(1).ListAsync(
            null, null, null, true, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OrganizationAdminCannotReadGlobalDraftContract()
    {
        var organizationId = Guid.NewGuid();
        var contract = RobotArtifactTechnicalContract.CreateDraft(
            "DRAFT", 1, "FAIRINO_LUA_V1", "FR5");
        var store = Substitute.For<IRobotArtifactTechnicalContractStore>();
        store.GetAsync(contract.Id, false, Arg.Any<CancellationToken>()).Returns(contract);
        var handlers = new RobotArtifactTechnicalContractHandlers(
            store, InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handlers.GetAsync(
            new GetRobotArtifactTechnicalContractQuery(
                OrganizationAdmin(organizationId), null, contract.Id),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(404, result.StatusCode);
    }

    private static RobotArtifactEffectDefinition Effect(string code) => new(
        code, RobotArtifactEffectKind.System, null, null, RobotArtifactQuantityMode.None, null, null, null);

    private static RobotArtifactEffectRequest EffectRequest(string code) => new(
        code, RobotArtifactEffectKind.System, null, null, RobotArtifactQuantityMode.None, null, null, null);

    private static CurrentUserContext OrganizationAdmin(Guid organizationId) => new()
    {
        AccountId = Guid.NewGuid(),
        RoleScopes = [new UserRoleScope("OrgAdmin", organizationId, null, null)]
    };
}
