using Application.Identity.Tokens.Claims;
using Application.Operations.OperationLogs.Abstractions;
using Application.ProductionConfiguration.Deployments.Services;
using Application.Tenants;
using Domain.Operations.Entities;
using NSubstitute;

namespace IceBot.UnitTests.ProductionConfiguration;

public sealed class DeploymentOperationAuditWriterTests
{
    [Fact]
    public async Task WriteRequestedAsync_PersistsReasonAndMatchingAuthorizationScope()
    {
        var organizationId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var kioskId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var operationLogs = Substitute.For<IOperationLogStore>();
        var writer = new DeploymentOperationAuditWriter(operationLogs);
        var user = new CurrentUserContext
        {
            AccountId = accountId,
            RoleScopes =
            [
                new UserRoleScope("Manager", organizationId, storeId, null),
                new UserRoleScope("Manager", Guid.NewGuid(), null, null)
            ]
        };

        await writer.WriteRequestedAsync(
            user,
            ScopeRoleSets.ReleaseDeploy,
            "ConfigurationDeploymentRequested",
            "Deploy the approved release after kiosk maintenance.",
            organizationId,
            storeId,
            kioskId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new string('a', 64),
            Guid.NewGuid(),
            null,
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        await operationLogs.Received(1).AddAsync(
            Arg.Is<OperationLog>(log =>
                log.AccountId == accountId &&
                log.KioskId == kioskId &&
                log.Action == "ConfigurationDeploymentRequested" &&
                log.Category == "ProductionConfiguration" &&
                (log.PayloadJson ?? string.Empty).Contains("Deploy the approved release after kiosk maintenance.") &&
                (log.PayloadJson ?? string.Empty).Contains("Manager") &&
                (log.PayloadJson ?? string.Empty).Contains(storeId.ToString())),
            Arg.Any<CancellationToken>());
    }
}
