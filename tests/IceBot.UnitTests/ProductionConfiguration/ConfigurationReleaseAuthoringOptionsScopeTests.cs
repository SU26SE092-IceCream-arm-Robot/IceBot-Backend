using Application.Identity.Tokens.Claims;
using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Releases.Queries;
using Application.ProductionConfiguration.Releases.ReadModels;
using NSubstitute;

namespace IceBot.UnitTests.ProductionConfiguration;

public sealed class ConfigurationReleaseAuthoringOptionsScopeTests
{
    [Fact]
    public async Task OrganizationAdmin_CannotIncludeGlobalTemplatesInReleaseAuthoringOptions()
    {
        var organizationId = Guid.NewGuid();
        var store = Substitute.For<IConfigurationReleaseStore>();
        store.OrganizationExistsAsync(organizationId, Arg.Any<CancellationToken>()).Returns(true);
        store.GetAuthoringOptionsAsync(organizationId, null, null, false, 50, Arg.Any<CancellationToken>())
            .Returns(new ConfigurationReleaseAuthoringOptionsReadModel());
        var handler = new GetConfigurationReleaseAuthoringOptionsQueryHandler(store);

        var result = await handler.HandleAsync(new GetConfigurationReleaseAuthoringOptionsQuery
        {
            OrganizationId = organizationId,
            IncludeGlobalTemplates = true,
            UserContext = new CurrentUserContext
            {
                AccountId = Guid.NewGuid(),
                AllowedOrganizationIds = new HashSet<Guid> { organizationId },
                RoleScopes = [new UserRoleScope("OrgAdmin", organizationId, null, null)]
            }
        });

        Assert.True(result.Succeeded);
        await store.Received(1).GetAuthoringOptionsAsync(
            organizationId, null, null, false, 50, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SystemAdmin_CanExplicitlyIncludeGlobalTemplatesInReleaseAuthoringOptions()
    {
        var organizationId = Guid.NewGuid();
        var store = Substitute.For<IConfigurationReleaseStore>();
        store.OrganizationExistsAsync(organizationId, Arg.Any<CancellationToken>()).Returns(true);
        store.GetAuthoringOptionsAsync(organizationId, null, null, true, 50, Arg.Any<CancellationToken>())
            .Returns(new ConfigurationReleaseAuthoringOptionsReadModel());
        var handler = new GetConfigurationReleaseAuthoringOptionsQueryHandler(store);

        var result = await handler.HandleAsync(new GetConfigurationReleaseAuthoringOptionsQuery
        {
            OrganizationId = organizationId,
            IncludeGlobalTemplates = true,
            UserContext = new CurrentUserContext { AccountId = Guid.NewGuid(), IsSystemAdmin = true }
        });

        Assert.True(result.Succeeded);
        await store.Received(1).GetAuthoringOptionsAsync(
            organizationId, null, null, true, 50, Arg.Any<CancellationToken>());
    }
}
