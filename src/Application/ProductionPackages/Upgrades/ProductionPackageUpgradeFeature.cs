using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;
using Application.Identity.Tokens.Claims;
using Application.ProductionConfiguration.Deployments.Commands;
using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.ProductionConfiguration.Deployments.ReadModels;
using Application.ProductionPackages.Installation;
using Application.Shared.Concurrency;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Catalog.Entities;
using Domain.Common;
using Domain.Devices.ExecutionEndpoints;
using Domain.ProductionPackages;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;
using Domain.RobotConfiguration.Artifacts;

namespace Application.ProductionPackages.Upgrades;

public sealed class ProductionPackageUpgradeService(
    IProductionPackageUpgradeStore upgrades,
    ProductionPackageInstallationService installer,
    ProductionPackageUpgradePreviewService previewService,
    ITechnicalResourceMutationCoordinator mutationCoordinator,
    ProductionPackageUpgradeMutationPolicy mutationPolicy,
    IConfigurationDeploymentRollbackDispatcher rollbackDeploymentHandler,
    IConfigurationDeploymentObservationReader deploymentObservations)
{
    private const int MaxRollbackDeploymentAttempts = 3;

    public async Task<ApiResult<ProductionPackageUpgradePreviewResult>> PreviewAsync(
        CurrentUserContext user, Guid organizationId, Guid sourceInstallationId,
        Guid targetPackageVersionId, IReadOnlyCollection<string> requestedProductSourceKeys,
        CancellationToken cancellationToken)
    {
        var context = await previewService.BuildAsync(user, organizationId, sourceInstallationId,
            targetPackageVersionId, requestedProductSourceKeys, cancellationToken);
        ProductionPackageUpgradeMetrics.RecordPreview(
            context.Result.Succeeded ? "accepted" : "rejected",
            context.Result.Data?.Blockers.Count ?? 0);
        return context.Result;
    }

    public async Task<ApiResult<ProductionPackageUpgradeResult>> ExecuteAsync(
        ExecuteProductionPackageUpgradeCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            return ApiResult<ProductionPackageUpgradeResult>.Fail("Idempotency-Key is required.", 400);

        var sourceInstallation = await upgrades.GetSourceInstallationAsync(
            command.OrganizationId, command.SourceInstallationId, cancellationToken);
        if (sourceInstallation is null)
            return ApiResult<ProductionPackageUpgradeResult>.Fail("Source package installation not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageInstall, command.UserContext,
                command.OrganizationId, sourceInstallation.StoreId, sourceInstallation.KioskId))
            return ApiResult<ProductionPackageUpgradeResult>.Fail("Access denied.", 403);

        var existing = await upgrades.FindByIdempotencyKeyAsync(
            command.OrganizationId, command.IdempotencyKey, cancellationToken);
        if (existing is not null && existing.Status is ProductionPackageUpgradeStatus.ReadyForReview or
            ProductionPackageUpgradeStatus.Completed or ProductionPackageUpgradeStatus.RollbackPending or
            ProductionPackageUpgradeStatus.RolledBack)
        {
            if (!Matches(existing, command))
                return ApiResult<ProductionPackageUpgradeResult>.Fail(
                    "Idempotency key was already used with a different upgrade payload.", 409);
            return ApiResult<ProductionPackageUpgradeResult>.Success(
                ProductionPackageUpgradeResult.From(existing), "Existing package upgrade returned.");
        }

        var preview = await previewService.BuildAsync(command.UserContext, command.OrganizationId,
            command.SourceInstallationId, command.TargetPackageVersionId, command.ProductSourceKeys,
            cancellationToken);
        if (!preview.Result.Succeeded || preview.Result.Data is null)
            return ApiResult<ProductionPackageUpgradeResult>.Fail(
                preview.Result.Message ?? "Upgrade preview failed.", preview.Result.StatusCode);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(preview.Result.Data.PreviewChecksum),
                Encoding.UTF8.GetBytes(command.PreviewChecksum.Trim().ToLowerInvariant())))
            return ApiResult<ProductionPackageUpgradeResult>.Fail(
                "Upgrade preview is stale. Generate a new preview before execution.", 409);
        if (preview.Result.Data.Blockers.Count > 0)
            return ApiResult<ProductionPackageUpgradeResult>.Fail(
                "Upgrade preview contains blockers.", 409).AddDetail("blockers", preview.Result.Data.Blockers);
        var sourceState = preview.SourceState!;
        var targetVersion = preview.TargetVersion!;

        ProductionPackageUpgrade upgrade;
        if (existing is not null)
        {
            if (!Matches(existing, preview.Result.Data))
                return ApiResult<ProductionPackageUpgradeResult>.Fail(
                    "Idempotency key was already used with a different upgrade payload.", 409);
            if (existing.Status == ProductionPackageUpgradeStatus.Failed)
            {
                var activeBeforeResume = await upgrades.FindActiveBySourceInstallationAsync(
                    command.OrganizationId, command.SourceInstallationId, cancellationToken);
                if (activeBeforeResume is not null && activeBeforeResume.Id != existing.Id)
                    return ApiResult<ProductionPackageUpgradeResult>.Fail(
                        "Another active upgrade already owns this source installation.", 409)
                        .AddDetail("upgradeId", activeBeforeResume.Id);
                ProductionPackageUpgrade? resumed;
                try
                {
                    resumed = await upgrades.ResumeFailedAsync(
                        command.OrganizationId, existing.Id, cancellationToken);
                }
                catch (DomainRuleException ex)
                {
                    return ApiResult<ProductionPackageUpgradeResult>.Fail(ex.Message, 409);
                }
                if (resumed is null)
                    return ApiResult<ProductionPackageUpgradeResult>.Fail(
                        "Failed package upgrade could not be resumed.", 409);
                upgrade = resumed;
            }
            else
            {
                upgrade = existing;
            }
        }
        else
        {
            var active = await upgrades.FindActiveBySourceInstallationAsync(
                command.OrganizationId, command.SourceInstallationId, cancellationToken);
            if (active is not null)
            {
                if (!string.Equals(active.IdempotencyKey, command.IdempotencyKey.Trim(), StringComparison.Ordinal) ||
                    !Matches(active, preview.Result.Data))
                    return ApiResult<ProductionPackageUpgradeResult>.Fail(
                        "Another active upgrade already owns this source installation.", 409)
                        .AddDetail("upgradeId", active.Id);
                upgrade = active;
            }
            else
            {
                upgrade = ProductionPackageUpgrade.Approve(command.OrganizationId, command.SourceInstallationId,
                    command.TargetPackageVersionId, preview.Result.Data.PreviewChecksum,
                    sourceState.SourceInstallation.PackageManifestChecksum,
                    targetVersion.ManifestChecksum!, preview.Result.Data.SelectedProductSourceKeys,
                    command.IdempotencyKey, command.UserContext.AccountId, DateTimeOffset.UtcNow);
                ProductionPackageUpgradeInsertResult inserted;
                try
                {
                    inserted = await upgrades.InsertOrGetAsync(upgrade, cancellationToken);
                }
                catch (DomainRuleException ex)
                {
                    return ApiResult<ProductionPackageUpgradeResult>.Fail(ex.Message, 409);
                }
                if (!Matches(inserted.Upgrade, preview.Result.Data))
                    return ApiResult<ProductionPackageUpgradeResult>.Fail(
                        "Concurrent upgrade used the idempotency key with a different payload.", 409);
                upgrade = inserted.Upgrade;
            }
        }

        var competing = await upgrades.FindActiveBySourceInstallationAsync(
            command.OrganizationId, command.SourceInstallationId, cancellationToken);
        if (competing is not null && competing.Id != upgrade.Id)
            return ApiResult<ProductionPackageUpgradeResult>.Fail(
                "Another active upgrade already owns this source installation.", 409)
                .AddDetail("upgradeId", competing.Id);

        var suffix = $"UPG_{upgrade.Id:N}";
        try
        {
            await EnsureApprovedScopeIsCurrentAsync(command, upgrade, cancellationToken);
            var installResult = await installer.InstallAsync(new InstallProductionPackageCommand
            {
                UserContext = command.UserContext,
                OrganizationId = command.OrganizationId,
                StoreId = sourceState.SourceInstallation.StoreId,
                KioskId = sourceState.SourceInstallation.KioskId,
                PackageId = targetVersion.ProductionPackageId,
                PackageVersionId = targetVersion.Id,
                ProductSourceKeys = preview.Result.Data.SelectedProductSourceKeys,
                IdempotencyKey = $"production-package-upgrade:{upgrade.Id:N}",
                MaterializationIdentitySuffix = suffix
            }, cancellationToken);
            if (!installResult.Succeeded || installResult.Data is null)
            {
                ProductionPackageUpgradeMetrics.RecordMaterialization("failed");
                await upgrades.MarkFailedAsync(command.OrganizationId, upgrade.Id,
                    "SuccessorInstallationFailed", installResult.Message ?? "Successor installation failed.",
                    cancellationToken);
                return ApiResult<ProductionPackageUpgradeResult>.Fail(
                    installResult.Message ?? "Successor installation failed.", installResult.StatusCode);
            }

            upgrade = await upgrades.AttachTargetInstallationAsync(command.OrganizationId, upgrade.Id,
                installResult.Data.Id, cancellationToken)
                ?? throw new DomainRuleException("Upgrade successor installation identity could not be persisted.");

            var resources = mutationPolicy.PreparationMutationIdentities(upgrade, sourceState);
            return await mutationCoordinator.ExecuteAsync(resources, async ct =>
            {
                await EnsureApprovedScopeIsCurrentAsync(command, upgrade, ct);
                var state = await upgrades.GetPreparationStateAsync(command.OrganizationId, upgrade.Id,
                    installResult.Data.Id, ct);
                if (state is null)
                {
                    var winner = await upgrades.GetAsync(command.OrganizationId,
                        command.SourceInstallationId, upgrade.Id, false, ct);
                    if (winner is not null && winner.TargetInstallationId == installResult.Data.Id &&
                        winner.Status is ProductionPackageUpgradeStatus.ReadyForReview or
                            ProductionPackageUpgradeStatus.Completed or
                            ProductionPackageUpgradeStatus.RollbackPending or
                            ProductionPackageUpgradeStatus.RolledBack)
                        return ApiResult<ProductionPackageUpgradeResult>.Success(
                            ProductionPackageUpgradeResult.From(winner),
                            "Concurrent package upgrade returned.");
                    throw new DomainRuleException("Upgrade successor materialization could not be reloaded.");
                }
                ProductionPackageUpgradePreparationPolicy.PrepareSuccessor(
                    state, command.UserContext.AccountId, DateTimeOffset.UtcNow);
                await upgrades.SaveChangesAsync(ct);
                ProductionPackageUpgradeMetrics.RecordMaterialization("ready_for_review");
                return ApiResult<ProductionPackageUpgradeResult>.Success(
                    ProductionPackageUpgradeResult.From(state.Upgrade),
                    "Package upgrade successor is ready for review.", 201);
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is DomainRuleException or InvalidOperationException)
        {
            ProductionPackageUpgradeMetrics.RecordMaterialization("failed");
            await upgrades.MarkFailedAsync(command.OrganizationId, upgrade.Id,
                "UpgradeMaterializationFailed", ex.Message, cancellationToken);
            return ApiResult<ProductionPackageUpgradeResult>.Fail(ex.Message, 409);
        }
    }

    public async Task<ApiResult<ProductionPackageUpgradeDetailResult>> GetAsync(CurrentUserContext user,
        Guid organizationId, Guid sourceInstallationId, Guid upgradeId, CancellationToken cancellationToken)
    {
        var upgrade = await upgrades.GetAsync(organizationId, sourceInstallationId, upgradeId, false,
            cancellationToken);
        if (upgrade is null) return ApiResult<ProductionPackageUpgradeDetailResult>.Fail("Package upgrade not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageRead, user, organizationId,
                upgrade.SourceInstallation.StoreId, upgrade.SourceInstallation.KioskId))
            return ApiResult<ProductionPackageUpgradeDetailResult>.Fail("Access denied.", 403);
        return ApiResult<ProductionPackageUpgradeDetailResult>.Success(
            await BuildDetailAsync(upgrade, cancellationToken));
    }

    public async Task<PagedResult<ProductionPackageUpgradeResult>> ListAsync(CurrentUserContext user,
        Guid organizationId, Guid sourceInstallationId, string? status, int pageNumber, int pageSize,
        CancellationToken cancellationToken)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var source = await upgrades.GetSourceInstallationAsync(
            organizationId, sourceInstallationId, cancellationToken);
        if (source is null)
            return PagedResult<ProductionPackageUpgradeResult>.Fail(
                "Package installation not found.", 404, pageNumber, pageSize);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageRead, user, organizationId,
                source.StoreId, source.KioskId))
            return PagedResult<ProductionPackageUpgradeResult>.Forbidden(
                "Access denied.", pageNumber, pageSize);
        ProductionPackageUpgradeStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ProductionPackageUpgradeStatus>(status, true, out var value))
                return PagedResult<ProductionPackageUpgradeResult>.Fail(
                    "Invalid production package upgrade status.", 400, pageNumber, pageSize);
            parsedStatus = value;
        }
        var total = await upgrades.CountAsync(
            organizationId, sourceInstallationId, parsedStatus, cancellationToken);
        var rows = await upgrades.ListAsync(
            organizationId, sourceInstallationId, parsedStatus, pageNumber, pageSize, cancellationToken);
        return PagedResult<ProductionPackageUpgradeResult>.Success(
            rows.Select(ProductionPackageUpgradeResult.From), total, pageNumber, pageSize);
    }

    public async Task<ApiResult<ProductionPackageUpgradeResult>> CutoverAsync(CurrentUserContext user,
        Guid organizationId, Guid sourceInstallationId, Guid upgradeId, CancellationToken cancellationToken)
    {
        var observed = await upgrades.GetAsync(organizationId, sourceInstallationId, upgradeId, false,
            cancellationToken);
        if (observed is null) return ApiResult<ProductionPackageUpgradeResult>.Fail("Package upgrade not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageInstall, user, organizationId,
                observed.SourceInstallation.StoreId, observed.SourceInstallation.KioskId))
            return ApiResult<ProductionPackageUpgradeResult>.Fail("Access denied.", 403);
        if (observed.Status == ProductionPackageUpgradeStatus.Completed)
            return ApiResult<ProductionPackageUpgradeResult>.Success(ProductionPackageUpgradeResult.From(observed),
                "Package upgrade is already cut over.");
        if (observed.Status != ProductionPackageUpgradeStatus.ReadyForReview)
            return ApiResult<ProductionPackageUpgradeResult>.Fail(
                "Only a ReadyForReview package upgrade can cut over.", 409);

        try
        {
            return await mutationCoordinator.ExecuteAsync(mutationPolicy.MutationIdentities(observed), async ct =>
            {
                var state = await upgrades.GetMutationStateAsync(organizationId, upgradeId, ct)
                    ?? throw new DomainRuleException("Package upgrade mutation state is incomplete.");
                mutationPolicy.ValidateCutover(state);

                foreach (var identity in state.Upgrade.CatalogIdentityChanges.Where(item => item.SourceProductId.HasValue))
                    state.SourceResources.Products[identity.ProductSourceKey].Code = identity.SourceCodeAfter;
                await upgrades.SaveChangesAsync(ct);
                foreach (var identity in state.Upgrade.CatalogIdentityChanges)
                    state.TargetResources.Products[identity.ProductSourceKey].Code = identity.TargetCodeAfter;
                mutationPolicy.ApplyAvailability(state, after: true);
                mutationPolicy.ApplyMenuBindings(state, after: true);
                foreach (var endpointTarget in state.Upgrade.EndpointTargets)
                {
                    var endpoint = state.Endpoints.Single(item => item.Id == endpointTarget.KioskExecutionEndpointId);
                    endpointTarget.RecordTargetDeployment(endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
                        ? endpoint.ActiveConfigurationDeploymentId!.Value
                        : endpoint.ActiveArtifactSetDeploymentId!.Value);
                }
                state.SourceInstallation.Supersede();
                state.Upgrade.Complete(user.AccountId, DateTimeOffset.UtcNow);
                await upgrades.SaveChangesAsync(ct);
                ProductionPackageUpgradeMetrics.RecordCutover("completed");
                return ApiResult<ProductionPackageUpgradeResult>.Success(
                    ProductionPackageUpgradeResult.From(state.Upgrade), "Package upgrade cutover completed.");
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is DomainRuleException or InvalidOperationException)
        {
            ProductionPackageUpgradeMetrics.RecordCutover("blocked");
            return ApiResult<ProductionPackageUpgradeResult>.Fail(ex.Message, 409);
        }
    }

    public async Task<ApiResult<ProductionPackageUpgradeResult>> RollbackAsync(CurrentUserContext user,
        Guid organizationId, Guid sourceInstallationId, Guid upgradeId, DateTimeOffset? commandExpiryAt,
        string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return ApiResult<ProductionPackageUpgradeResult>.Fail("Rollback reason is required.", 400);
        var observed = await upgrades.GetAsync(organizationId, sourceInstallationId, upgradeId, true,
            cancellationToken);
        if (observed is null) return ApiResult<ProductionPackageUpgradeResult>.Fail("Package upgrade not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ReleaseRollback, user, organizationId,
                observed.SourceInstallation.StoreId, observed.SourceInstallation.KioskId))
            return ApiResult<ProductionPackageUpgradeResult>.Fail("Access denied.", 403);
        if (observed.Status == ProductionPackageUpgradeStatus.RolledBack)
            return ApiResult<ProductionPackageUpgradeResult>.Success(ProductionPackageUpgradeResult.From(observed),
                "Package upgrade is already rolled back.");
        if (observed.Status is not (ProductionPackageUpgradeStatus.Completed or
            ProductionPackageUpgradeStatus.RollbackPending))
            return ApiResult<ProductionPackageUpgradeResult>.Fail(
                "Only a Completed package upgrade can roll back.", 409);
        foreach (var endpoint in observed.EndpointTargets)
        {
            ConfigurationDeploymentReadModel? currentRollback = null;
            if (endpoint.RollbackDeploymentId.HasValue)
            {
                currentRollback = await deploymentObservations.GetConfigurationDeploymentAsync(
                    endpoint.RollbackDeploymentId.Value, cancellationToken);
                if (currentRollback is null || currentRollback.Status != ConfigurationDeploymentReadStatus.Failed)
                    continue;
                if (endpoint.RollbackAttempts.Count >= MaxRollbackDeploymentAttempts)
                {
                    ProductionPackageUpgradeMetrics.RecordRollback("blocked");
                    return ApiResult<ProductionPackageUpgradeResult>.Fail(
                        "Rollback deployment retry limit was reached.", 409)
                        .AddDetail("kioskExecutionEndpointId", endpoint.KioskExecutionEndpointId)
                        .AddDetail("deploymentId", endpoint.RollbackDeploymentId.Value);
                }
            }
            var attemptNo = endpoint.RollbackAttempts.Count + 1;
            var result = await rollbackDeploymentHandler.HandleAsync(new RollbackConfigurationDeploymentCommand
            {
                UserContext = user,
                KioskId = endpoint.KioskId,
                TargetDeploymentId = endpoint.SourceDeploymentId,
                IdempotencyKey = $"package-upgrade-rollback:{upgradeId:N}:{endpoint.KioskExecutionEndpointId:N}:{attemptNo}",
                Reason = $"Production package upgrade rollback: {reason.Trim()}",
                ExpectedActiveDeploymentId = endpoint.TargetDeploymentId,
                CommandExpiryAt = commandExpiryAt
            }, cancellationToken);
            if (!result.Succeeded || result.Data is null)
            {
                ProductionPackageUpgradeMetrics.RecordRollback("blocked");
                return ApiResult<ProductionPackageUpgradeResult>.Fail(
                    result.Message ?? "Package upgrade rollback deployment failed.", result.StatusCode)
                    .AddDetail("kioskExecutionEndpointId", endpoint.KioskExecutionEndpointId);
            }
            ProductionPackageUpgradeRollbackAttemptRecordResult recorded;
            try
            {
                recorded = await upgrades.RecordRollbackAttemptAsync(
                    organizationId, sourceInstallationId, upgradeId, endpoint.KioskExecutionEndpointId,
                    result.Data.NewDeploymentId, user.AccountId, reason, DateTimeOffset.UtcNow,
                    MaxRollbackDeploymentAttempts, cancellationToken);
            }
            catch (DomainRuleException ex)
            {
                ProductionPackageUpgradeMetrics.RecordRollback("blocked");
                return ApiResult<ProductionPackageUpgradeResult>.Fail(ex.Message, 409)
                    .AddDetail("kioskExecutionEndpointId", endpoint.KioskExecutionEndpointId);
            }
            if (recorded.Recorded)
                ProductionPackageUpgradeMetrics.RecordRollbackAttempt(result.Data.Profile, recorded.AttemptNo);
        }

        try
        {
            var state = await upgrades.GetMutationStateAsync(organizationId, upgradeId, cancellationToken)
                ?? throw new DomainRuleException("Package upgrade rollback state is incomplete.");
            if (!mutationPolicy.RollbackDeploymentsAreActive(state))
            {
                ProductionPackageUpgradeMetrics.RecordRollback("pending");
                if (state.Upgrade.RollbackRequestedAt.HasValue)
                    ProductionPackageUpgradeMetrics.RecordPendingAge(
                        state.Upgrade.RollbackRequestedAt.Value, DateTimeOffset.UtcNow, "rollback");
                return ApiResult<ProductionPackageUpgradeResult>.Success(
                    ProductionPackageUpgradeResult.From(state.Upgrade),
                    "Rollback deployments were requested and are awaiting Active reports.", 202);
            }

            return await mutationCoordinator.ExecuteAsync(mutationPolicy.MutationIdentities(state.Upgrade), async ct =>
            {
                var locked = await upgrades.GetMutationStateAsync(organizationId, upgradeId, ct)
                    ?? throw new DomainRuleException("Package upgrade rollback state is incomplete.");
                if (!mutationPolicy.RollbackDeploymentsAreActive(locked))
                    return ApiResult<ProductionPackageUpgradeResult>.Fail(
                        "Rollback deployments are not Active on every required endpoint.", 409);
                mutationPolicy.ValidateRollback(locked);

                foreach (var identity in locked.Upgrade.CatalogIdentityChanges)
                    locked.TargetResources.Products[identity.ProductSourceKey].Code = identity.TargetCodeBefore;
                await upgrades.SaveChangesAsync(ct);
                foreach (var identity in locked.Upgrade.CatalogIdentityChanges.Where(item => item.SourceProductId.HasValue))
                    locked.SourceResources.Products[identity.ProductSourceKey].Code = identity.SourceCodeBefore;
                mutationPolicy.ApplyAvailability(locked, after: false);
                mutationPolicy.ApplyMenuBindings(locked, after: false);
                locked.SourceInstallation.RestoreFromSuperseded();
                locked.TargetInstallation.Supersede();
                locked.Upgrade.CompleteRollback(user.AccountId, DateTimeOffset.UtcNow);
                await upgrades.SaveChangesAsync(ct);
                ProductionPackageUpgradeMetrics.RecordRollback("completed");
                return ApiResult<ProductionPackageUpgradeResult>.Success(
                    ProductionPackageUpgradeResult.From(locked.Upgrade), "Package upgrade rollback completed.");
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is DomainRuleException or InvalidOperationException)
        {
            ProductionPackageUpgradeMetrics.RecordRollback("blocked");
            return ApiResult<ProductionPackageUpgradeResult>.Fail(ex.Message, 409);
        }
    }

    public async Task<ApiResult<ProductionPackageUpgradeResult>> AbandonAsync(
        CurrentUserContext user, Guid organizationId, Guid sourceInstallationId, Guid upgradeId,
        string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return ApiResult<ProductionPackageUpgradeResult>.Fail("Abandon reason is required.", 400);
        var observed = await upgrades.GetAsync(
            organizationId, sourceInstallationId, upgradeId, false, cancellationToken);
        if (observed is null)
            return ApiResult<ProductionPackageUpgradeResult>.Fail("Package upgrade not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageInstall, user, organizationId,
                observed.SourceInstallation.StoreId, observed.SourceInstallation.KioskId))
            return ApiResult<ProductionPackageUpgradeResult>.Fail("Access denied.", 403);
        if (observed.Status == ProductionPackageUpgradeStatus.Abandoned)
            return ApiResult<ProductionPackageUpgradeResult>.Success(
                ProductionPackageUpgradeResult.From(observed), "Package upgrade is already abandoned.");
        if (observed.Status is not (ProductionPackageUpgradeStatus.ReadyForReview or
            ProductionPackageUpgradeStatus.Failed))
            return ApiResult<ProductionPackageUpgradeResult>.Fail(
                "Only a ReadyForReview or Failed package upgrade can be abandoned.", 409);

        try
        {
            return await mutationCoordinator.ExecuteAsync(mutationPolicy.MutationIdentities(observed), async ct =>
            {
                var upgrade = await upgrades.GetAsync(
                    organizationId, sourceInstallationId, upgradeId, true, ct)
                    ?? throw new DomainRuleException("Package upgrade disappeared while abandoning.");
                if (upgrade.Status == ProductionPackageUpgradeStatus.Abandoned)
                    return ApiResult<ProductionPackageUpgradeResult>.Success(
                        ProductionPackageUpgradeResult.From(upgrade), "Package upgrade is already abandoned.");
                if (upgrade.Status is not (ProductionPackageUpgradeStatus.ReadyForReview or
                    ProductionPackageUpgradeStatus.Failed))
                    throw new DomainRuleException(
                        "Package upgrade state changed and can no longer be abandoned.");

                var now = DateTimeOffset.UtcNow;
                if (upgrade.TargetInstallation is not null)
                {
                    if (await upgrades.HasAbandonOperationalReferencesAsync(
                            organizationId, upgrade.TargetInstallation.Id, ct))
                        throw new DomainRuleException(
                            "Successor resources are referenced by a MenuItem or active deployment. " +
                            "Remove the binding or roll back deployment before abandoning the upgrade.");
                    await upgrades.SoftDeleteAbandonedTargetRootsAsync(
                        organizationId, upgrade.TargetInstallation.Id, user.AccountId, now, ct);
                    upgrade.TargetInstallation.Abandon();
                }
                upgrade.Abandon(user.AccountId, reason, now);
                await upgrades.SaveChangesAsync(ct);
                ProductionPackageUpgradeMetrics.RecordAbandon("completed");
                return ApiResult<ProductionPackageUpgradeResult>.Success(
                    ProductionPackageUpgradeResult.From(upgrade), "Package upgrade abandoned.");
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is DomainRuleException or InvalidOperationException)
        {
            ProductionPackageUpgradeMetrics.RecordAbandon("blocked");
            return ApiResult<ProductionPackageUpgradeResult>.Fail(ex.Message, 409);
        }
    }

    private async Task EnsureApprovedScopeIsCurrentAsync(
        ExecuteProductionPackageUpgradeCommand command,
        ProductionPackageUpgrade upgrade,
        CancellationToken cancellationToken)
    {
        var current = await previewService.BuildAsync(
            command.UserContext,
            command.OrganizationId,
            command.SourceInstallationId,
            command.TargetPackageVersionId,
            command.ProductSourceKeys,
            cancellationToken);

        if (!current.Result.Succeeded || current.Result.Data is null ||
            current.Result.Data.Blockers.Count > 0 ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(current.Result.Data.PreviewChecksum),
                Encoding.UTF8.GetBytes(upgrade.PreviewChecksum)))
        {
            throw new DomainRuleException(
                "Upgrade approval scope changed during successor materialization. Generate a new preview before retrying.");
        }
    }

    private async Task<ProductionPackageUpgradeDetailResult> BuildDetailAsync(
        ProductionPackageUpgrade upgrade, CancellationToken cancellationToken)
    {
        var endpoints = new List<ProductionPackageUpgradeEndpointDetail>();
        foreach (var endpoint in upgrade.EndpointTargets.OrderBy(item => item.KioskExecutionEndpointId))
        {
            var attempts = new List<ProductionPackageUpgradeRollbackAttemptResult>();
            foreach (var attempt in endpoint.RollbackAttempts.OrderBy(item => item.AttemptNo))
            {
                var deployment = await deploymentObservations.GetConfigurationDeploymentAsync(
                    attempt.DeploymentId, cancellationToken);
                attempts.Add(new ProductionPackageUpgradeRollbackAttemptResult(
                    attempt.AttemptNo, attempt.DeploymentId, attempt.ReplacedDeploymentId,
                    deployment?.Status.ToString() ?? "Unknown", deployment?.FailureCode,
                    deployment?.FailureReason, attempt.RequestedByAccountId, attempt.Reason,
                    attempt.RequestedAt));
            }
            var current = endpoint.RollbackDeploymentId.HasValue
                ? await deploymentObservations.GetConfigurationDeploymentAsync(
                    endpoint.RollbackDeploymentId.Value, cancellationToken)
                : null;
            endpoints.Add(new ProductionPackageUpgradeEndpointDetail(
                endpoint.KioskExecutionEndpointId, endpoint.KioskId,
                endpoint.SourceConfigurationReleaseId, endpoint.SourceDeploymentId,
                endpoint.TargetDeploymentId, endpoint.RollbackDeploymentId,
                current?.Status.ToString(), current?.FailureCode, current?.FailureReason, attempts));
        }
        var menuChanges = upgrade.MenuChanges.OrderBy(item => item.MenuItemId).Select(item =>
            new ProductionPackageUpgradeMenuChangeResult(
                item.ChangeKind.ToString(), item.MenuId, item.MenuItemId,
                item.BeforeProductId, item.AfterProductId,
                item.BeforeProductVariantId, item.AfterProductVariantId,
                item.BeforeRecipeId, item.AfterRecipeId,
                item.BeforeMenuItemStatus.ToString(), item.AfterMenuItemStatus.ToString(),
                item.OptionChanges.Select(option => option.OptionSourceKey).Order(StringComparer.Ordinal).ToArray()))
            .ToArray();
        return new ProductionPackageUpgradeDetailResult(
            ProductionPackageUpgradeResult.From(upgrade), upgrade.ApprovedByAccountId, upgrade.ApprovedAt,
            upgrade.CompletedByAccountId, upgrade.CompletedAt,
            upgrade.RollbackRequestedByAccountId, upgrade.RollbackRequestedAt,
            upgrade.RolledBackByAccountId, upgrade.RolledBackAt,
            upgrade.AbandonedByAccountId, upgrade.AbandonedAt, upgrade.AbandonReason,
            menuChanges, endpoints);
    }

    internal static string MenuBindingChecksum(Guid? productId, Guid? variantId, Guid? recipeId,
        MenuItemStatus status, IEnumerable<Guid?> optionIds) => Hash(new
        {
            ProductId = productId,
            ProductVariantId = variantId,
            RecipeId = recipeId,
            Status = status,
            OptionIds = optionIds.Where(id => id.HasValue).Select(id => id!.Value).Order()
        });

    internal static string Hash(object value)
    {
        var json = JsonSerializer.Serialize(value);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    internal static string HistoricalCode(string canonicalCode, Guid upgradeId) =>
        ProductionPackageMaterializationCode.WithSuffix(canonicalCode, $"OLD_{upgradeId:N}");

    private static bool Matches(ProductionPackageUpgrade upgrade, ProductionPackageUpgradePreviewResult preview) =>
        upgrade.SourceInstallationId == preview.SourceInstallationId &&
        upgrade.TargetPackageVersionId == preview.TargetPackageVersionId &&
        string.Equals(upgrade.PreviewChecksum, preview.PreviewChecksum, StringComparison.Ordinal) &&
        upgrade.GetSelectedProductSourceKeys().SequenceEqual(preview.SelectedProductSourceKeys);

    private static bool Matches(ProductionPackageUpgrade upgrade, ExecuteProductionPackageUpgradeCommand command)
    {
        var requested = command.ProductSourceKeys.Select(key => key.Trim().ToUpperInvariant())
            .Where(key => key.Length > 0).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (requested.Length == 0)
        {
            var targetKeys = upgrade.TargetPackageVersion.Products.Select(item => item.SourceKey)
                .ToHashSet(StringComparer.Ordinal);
            requested = upgrade.SourceInstallation.GetSelectedProductSourceKeys()
                .Where(targetKeys.Contains).Order(StringComparer.Ordinal).ToArray();
        }
        return upgrade.SourceInstallationId == command.SourceInstallationId &&
               upgrade.TargetPackageVersionId == command.TargetPackageVersionId &&
               string.Equals(upgrade.PreviewChecksum, command.PreviewChecksum.Trim().ToLowerInvariant(),
                   StringComparison.Ordinal) &&
               upgrade.GetSelectedProductSourceKeys().SequenceEqual(requested);
    }

}
