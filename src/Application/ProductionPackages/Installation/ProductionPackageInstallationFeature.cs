using System.Text.Json;
using Application.Identity.Tokens.Claims;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Storage.Services;
using Application.Shared.Wrappers;
using Application.Tenants;
using Application.ProductionConfiguration.Routes.Support;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionPackages;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.ArtifactTemplates;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.Programs;
using Application.Shared.Concurrency;

namespace Application.ProductionPackages.Installation;

public sealed class ProductionPackageInstallationService(
    IProductionPackageStore packages,
    IProductionPackageInstallationStore installations,
    IArtifactObjectStorage objectStorage,
    ArtifactUploadContentService contentService,
    ArtifactPublicationValidator publicationValidator,
    ITechnicalResourceMutationCoordinator mutationCoordinator)
{
    public async Task<ApiResult<ProductionPackageInstallationPreview>> PreviewAsync(
        CurrentUserContext user, Guid organizationId, Guid packageId, Guid versionId,
        Guid? storeId, Guid? kioskId, IReadOnlyCollection<string> selectedProductSourceKeys,
        CancellationToken cancellationToken)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageRead, user, organizationId, storeId, kioskId))
            return ApiResult<ProductionPackageInstallationPreview>.Fail("Access denied.", 403);
        if (!await installations.ScopeExistsAsync(organizationId, storeId, kioskId, cancellationToken))
            return ApiResult<ProductionPackageInstallationPreview>.Fail("Package installation scope was not found.", 404);
        var version = await packages.GetVersionAsync(packageId, versionId, false, cancellationToken);
        if (version is null || version.Status != ProductionPackageVersionStatus.Published || string.IsNullOrWhiteSpace(version.ManifestChecksum))
            return ApiResult<ProductionPackageInstallationPreview>.Fail("Published package version not found.", 404);
        IReadOnlySet<string> selected;
        try
        {
            selected = ProductionPackageInstallationRequestRules.ResolveSelectedProductKeys(
                version, selectedProductSourceKeys);
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ProductionPackageInstallationPreview>.Fail(ex.Message, 400);
        }
        try
        {
            var contracts = await packages.LoadTechnicalContractsAsync(
                version.Artifacts.Select(x => x.TechnicalContractId).Distinct().ToArray(), cancellationToken);
            ProductionPackageDefinitionValidator.Validate(version, contracts);
            var selection = ProductionPackageInstallationSelectionRules.Resolve(version, selected);
            return ApiResult<ProductionPackageInstallationPreview>.Success(new ProductionPackageInstallationPreview(
                version.Id, version.ManifestChecksum, selected.Order().ToArray(),
                selection.Programs.Select(x => x.BlueprintCode).Order().ToArray(),
                selection.Routes.Select(x => x.RouteCode).Order().ToArray(),
                ["Package installation creates Draft technical configuration and requires review before publication."]));
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ProductionPackageInstallationPreview>.Fail(ex.Message, 409);
        }
    }

    public async Task<ApiResult<ProductionPackageInstallationResult>> InstallAsync(
        InstallProductionPackageCommand command, CancellationToken cancellationToken)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageInstall, command.UserContext,
                command.OrganizationId, command.StoreId, command.KioskId))
            return ApiResult<ProductionPackageInstallationResult>.Fail("Access denied.", 403);
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            return ApiResult<ProductionPackageInstallationResult>.Fail("Idempotency-Key is required.", 400);
        if (!await installations.ScopeExistsAsync(command.OrganizationId, command.StoreId, command.KioskId, cancellationToken))
            return ApiResult<ProductionPackageInstallationResult>.Fail("Package installation scope was not found.", 404);

        var version = await packages.GetVersionAsync(command.PackageId, command.PackageVersionId, false, cancellationToken);
        if (version is null || version.Status != ProductionPackageVersionStatus.Published || string.IsNullOrWhiteSpace(version.ManifestChecksum))
            return ApiResult<ProductionPackageInstallationResult>.Fail("Published production package version not found.", 404);

        IReadOnlySet<string> selectedKeys;
        try
        {
            selectedKeys = ProductionPackageInstallationRequestRules.ResolveSelectedProductKeys(
                version, command.ProductSourceKeys);
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ProductionPackageInstallationResult>.Fail(ex.Message, 400);
        }
        var requestChecksum = ProductionPackageInstallationRequestRules.ComputeRequestChecksum(
            command.OrganizationId, command.StoreId, command.KioskId, version, selectedKeys,
            command.MaterializationIdentitySuffix);
        var validationContracts = await packages.LoadTechnicalContractsAsync(
            version.Artifacts.Select(x => x.TechnicalContractId).Distinct().ToArray(), cancellationToken);
        try { ProductionPackageDefinitionValidator.Validate(version, validationContracts); }
        catch (DomainRuleException ex) { return ApiResult<ProductionPackageInstallationResult>.Fail(ex.Message, 409); }

        var existing = await installations.FindByIdempotencyKeyAsync(command.OrganizationId, command.IdempotencyKey.Trim(), cancellationToken);
        ProductionPackageInstallation installation;
        if (existing is not null)
        {
            if (existing.PackageVersionId != version.Id || existing.PackageManifestChecksum != version.ManifestChecksum ||
                existing.RequestChecksum != requestChecksum)
                return ApiResult<ProductionPackageInstallationResult>.Fail("Idempotency key was already used with a different installation payload.", 409);
            if (existing.Status != ProductionPackageInstallationStatus.Failed)
                return ApiResult<ProductionPackageInstallationResult>.Success(ProductionPackageInstallationResult.From(existing),
                    "Existing package installation returned.");
            var restarted = await installations.TryRestartFailedAsync(
                command.OrganizationId, existing.Id, DateTimeOffset.UtcNow, cancellationToken);
            var restartedInstallation = await installations.GetForEditAsync(
                command.OrganizationId, existing.Id, cancellationToken);
            if (restartedInstallation is null)
                return ApiResult<ProductionPackageInstallationResult>.Fail(
                    "Package installation disappeared while retrying.", 409);
            installation = restartedInstallation;
            if (!restarted)
                return ApiResult<ProductionPackageInstallationResult>.Success(
                    ProductionPackageInstallationResult.From(installation),
                    "Concurrent package installation retry returned.");
        }
        else
        {
            installation = ProductionPackageInstallation.Start(command.OrganizationId, command.StoreId, command.KioskId,
                version.Id, version.ManifestChecksum, requestChecksum, command.IdempotencyKey,
                selectedKeys.ToArray(), DateTimeOffset.UtcNow, command.MaterializationIdentitySuffix);
            installation.CreatedByAccountId = command.UserContext.AccountId;
            var inserted = await installations.InsertOrGetAsync(installation, cancellationToken);
            if (!inserted.Created)
            {
                var winner = inserted.Installation;
                if (winner.PackageVersionId != version.Id || winner.PackageManifestChecksum != version.ManifestChecksum ||
                    winner.RequestChecksum != requestChecksum)
                    return ApiResult<ProductionPackageInstallationResult>.Fail(
                        "Idempotency key was concurrently used with a different installation payload.", 409);
                return ApiResult<ProductionPackageInstallationResult>.Success(
                    ProductionPackageInstallationResult.From(winner), "Concurrent package installation returned.");
            }
        }
        installation.MarkMaterializing();
        await installations.SaveChangesAsync(cancellationToken);

        await using var preparedObjects = new UncommittedArtifactObjectSet(contentService);
        try
        {
            var selection = ProductionPackageInstallationSelectionRules.Resolve(version, selectedKeys);
            var selectedRoutes = selection.Routes;
            var selectedArtifacts = selection.Artifacts;
            var artifactCodes = selectedArtifacts.Select(artifact => artifact.SourceKey).Distinct().ToArray();
            var observedArtifacts = await installations.ListArtifactsByCodesAsync(
                command.OrganizationId, artifactCodes, cancellationToken);
            var observedPackageManagedIds = await installations.ListPackageManagedArtifactIdsAsync(
                observedArtifacts.Select(artifact => artifact.Id).ToArray(), cancellationToken);
            var observedTemplates = await packages.LoadArtifactTemplatesAsync(
                selectedArtifacts.Select(x => x.RobotArtifactTemplateId).ToArray(), cancellationToken);
            var observedContracts = await packages.LoadTechnicalContractsAsync(
                version.Artifacts.Select(x => x.TechnicalContractId).ToArray(), cancellationToken);
            ProductionPackageDefinitionValidator.Validate(version, observedContracts);
            var preparedArtifacts = await ProductionPackageArtifactPreparation.PrepareAsync(
                command.OrganizationId, command.UserContext.AccountId, selectedArtifacts, observedTemplates,
                observedContracts, observedArtifacts, observedPackageManagedIds, objectStorage,
                publicationValidator, preparedObjects, cancellationToken);
            var mutationResources = selectedArtifacts
                .SelectMany(artifact => new[]
                {
                    TechnicalResourceMutationIdentity.ArtifactDefinition(command.OrganizationId, artifact.SourceKey),
                    TechnicalResourceMutationIdentity.Template(artifact.RobotArtifactTemplateId),
                    TechnicalResourceMutationIdentity.Contract(artifact.TechnicalContractId)
                })
                .Concat(selectedRoutes.Select(route => TechnicalResourceMutationIdentity.ProgramDefinition(
                    command.OrganizationId, command.StoreId, command.KioskId, null,
                    ProductionPackageInstallationMaterializer.PackageProgramCode(
                        version.Version, route.RouteCode, command.MaterializationIdentitySuffix))))
                .Concat(observedArtifacts.Select(artifact =>
                    TechnicalResourceMutationIdentity.Artifact(artifact.Id)))
                .Append(TechnicalResourceMutationIdentity.PackageInstallation(installation.Id))
                .ToArray();

            async Task<ApiResult<ProductionPackageInstallationResult>> MaterializeLockedAsync(CancellationToken ct)
            {
                var currentStatus = await installations.GetCurrentStatusAsync(
                    command.OrganizationId, installation.Id, ct);
                if (currentStatus is ProductionPackageInstallationStatus.Installed or
                    ProductionPackageInstallationStatus.Superseded)
                {
                    await preparedObjects.CompensateAsync();
                    var completed = await installations.GetAsync(command.OrganizationId, installation.Id, ct)
                        ?? throw new DomainRuleException("Completed package installation could not be reloaded.");
                    return ApiResult<ProductionPackageInstallationResult>.Success(
                        ProductionPackageInstallationResult.From(completed),
                        "Existing package installation returned.");
                }
                if (currentStatus.HasValue && currentStatus != ProductionPackageInstallationStatus.Materializing)
                    throw new DomainRuleException(
                        $"Package installation is {currentStatus.Value} and cannot materialize.");

                var productDefinitions = version.Products.Where(x => selectedKeys.Contains(x.SourceKey)).ToArray();
                if (productDefinitions.Length != selectedKeys.Count)
                    throw new DomainRuleException("One or more selected package products do not exist.");

                var templates = await packages.LoadArtifactTemplatesAsync(
                    selectedArtifacts.Select(x => x.RobotArtifactTemplateId).ToArray(), ct);
                var contracts = await packages.LoadTechnicalContractsAsync(
                    version.Artifacts.Select(x => x.TechnicalContractId).ToArray(), ct);
                ProductionPackageDefinitionValidator.Validate(version, contracts);

                var existingArtifacts = await installations.ListArtifactsByCodesAsync(
                    command.OrganizationId, artifactCodes, ct);
                if (!observedArtifacts.Select(artifact => artifact.Id).ToHashSet()
                        .SetEquals(existingArtifacts.Select(artifact => artifact.Id)))
                {
                    throw new DomainRuleException(
                        "Organization artifact identities changed while package installation was waiting; retry installation.");
                }
                var packageManagedArtifactIds = await installations.ListPackageManagedArtifactIdsAsync(
                    existingArtifacts.Select(artifact => artifact.Id).ToArray(), ct);

                var optionImpacts = ProductionPackageDefinitionValidator.ResolveOptionExecutionImpacts(
                    version, contracts);
                var products = ProductionPackageInstallationMaterializer.MaterializeProducts(
                    command, installation, productDefinitions, optionImpacts);
                var artifacts = ProductionPackageInstallationMaterializer.MaterializeArtifacts(
                    installation, selectedArtifacts, templates, contracts, existingArtifacts,
                    packageManagedArtifactIds, preparedArtifacts);
                var programsAndCompositions = ProductionPackageInstallationMaterializer.ComposePrograms(
                    command, installation, version, products, artifacts.All, contracts, optionImpacts);

                await installations.PersistMaterializedGraphAsync(
                    installation, products.Select(x => x.Product).ToArray(), artifacts.Created,
                    programsAndCompositions.Programs.Values.ToArray(),
                    programsAndCompositions.Bindings.Values.ToArray(), programsAndCompositions.Compositions,
                    releaseNumber => ProductionPackageInstallationMaterializer.CreateRelease(
                        command, installation, version, releaseNumber, products,
                        programsAndCompositions.Programs, programsAndCompositions.Bindings, optionImpacts), ct);
                preparedObjects.Commit();
                return ApiResult<ProductionPackageInstallationResult>.Success(
                    ProductionPackageInstallationResult.From(installation),
                    "Production package installed as Draft configuration.", 201);
            }

            return mutationResources.Length == 0
                ? await MaterializeLockedAsync(cancellationToken)
                : await mutationCoordinator.ExecuteAsync(
                    mutationResources, MaterializeLockedAsync, cancellationToken);
        }
        catch (Exception ex) when (ex is DomainRuleException or ArtifactObjectNotFoundException or
                                   ArtifactObjectIntegrityException or ArtifactObjectStorageUnavailableException or
                                   Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            await installations.MarkFailedAsync(command.OrganizationId, installation.Id,
                "PackageMaterializationFailed", ex.Message, CancellationToken.None);
            return ApiResult<ProductionPackageInstallationResult>.Fail(ex.Message, ex is ArtifactObjectStorageUnavailableException ? 503 : 409);
        }
    }

    public async Task<ApiResult<ProductionPackageInstallationResult>> GetAsync(CurrentUserContext user,
        Guid organizationId, Guid installationId, CancellationToken cancellationToken)
    {
        var installation = await installations.GetAsync(organizationId, installationId, cancellationToken);
        if (installation is null)
            return ApiResult<ProductionPackageInstallationResult>.Fail("Package installation not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageRead, user, organizationId,
                installation.StoreId, installation.KioskId))
            return ApiResult<ProductionPackageInstallationResult>.Fail("Access denied.", 403);
        return ApiResult<ProductionPackageInstallationResult>.Success(ProductionPackageInstallationResult.From(installation));
    }

    public async Task<PagedResult<ProductionPackageInstallationResult>> ListAsync(CurrentUserContext user,
        Guid organizationId, string? status, Guid? storeId, Guid? kioskId, int pageNumber, int pageSize,
        CancellationToken cancellationToken)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageRead, user, organizationId, storeId, kioskId))
            return PagedResult<ProductionPackageInstallationResult>.Forbidden("Access denied.", pageNumber, pageSize);
        ProductionPackageInstallationStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ProductionPackageInstallationStatus>(status, true, out var value))
                return PagedResult<ProductionPackageInstallationResult>.Fail(
                    "Invalid production package installation status.", 400, pageNumber, pageSize);
            parsedStatus = value;
        }
        var total = await installations.CountAsync(organizationId, parsedStatus, storeId, kioskId, cancellationToken);
        var rows = await installations.ListAsync(organizationId, parsedStatus, storeId, kioskId,
            pageNumber, pageSize, cancellationToken);
        return PagedResult<ProductionPackageInstallationResult>.Success(
            rows.Select(ProductionPackageInstallationResult.From), total, pageNumber, pageSize);
    }

    public async Task<ApiResult<ProductionPackageInstallationResult>> RetryAsync(CurrentUserContext user,
        Guid organizationId, Guid installationId, CancellationToken cancellationToken)
    {
        var installation = await installations.GetForEditAsync(organizationId, installationId, cancellationToken);
        if (installation is null)
            return ApiResult<ProductionPackageInstallationResult>.Fail("Package installation not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageInstall, user, organizationId,
                installation.StoreId, installation.KioskId))
            return ApiResult<ProductionPackageInstallationResult>.Fail("Access denied.", 403);
        if (installation.Status != ProductionPackageInstallationStatus.Failed)
            return ApiResult<ProductionPackageInstallationResult>.Fail("Only a Failed installation can be retried.", 409);

        return await InstallAsync(new InstallProductionPackageCommand
        {
            UserContext = user,
            OrganizationId = organizationId,
            StoreId = installation.StoreId,
            KioskId = installation.KioskId,
            PackageId = installation.PackageVersion.ProductionPackageId,
            PackageVersionId = installation.PackageVersionId,
            IdempotencyKey = installation.IdempotencyKey,
            ProductSourceKeys = installation.GetSelectedProductSourceKeys(),
            MaterializationIdentitySuffix = installation.MaterializationIdentitySuffix
        }, cancellationToken);
    }

    public async Task<ApiResult<ProductionPackageInstallationResult>> ForkAsync(CurrentUserContext user,
        Guid organizationId, Guid installationId, CancellationToken cancellationToken)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageFork, user, organizationId, null, null))
            return ApiResult<ProductionPackageInstallationResult>.Fail("Access denied.", 403);
        var observed = await installations.GetForkGraphAsync(
            organizationId, installationId, tracked: false, cancellationToken);
        if (observed is null)
            return ApiResult<ProductionPackageInstallationResult>.Fail("Package installation not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageFork, user, organizationId,
                observed.Installation.StoreId, observed.Installation.KioskId))
            return ApiResult<ProductionPackageInstallationResult>.Fail("Access denied.", 403);
        if (observed.Installation.Status != ProductionPackageInstallationStatus.Installed ||
            observed.Installation.OwnershipMode != ProductionPackageOwnershipMode.PackageManaged)
            return ApiResult<ProductionPackageInstallationResult>.Fail(
                "Only an Installed package-managed configuration can be forked.", 409);
        if (await installations.HasActiveUpgradeAsync(organizationId, installationId, cancellationToken))
            return ApiResult<ProductionPackageInstallationResult>.Fail(
                "A package installation participating in an active upgrade cannot be forked.", 409);

        var stagedBySourceArtifactId = new Dictionary<Guid, RobotArtifact>();
        await using var preparedObjects = new UncommittedArtifactObjectSet(contentService);
        try
        {
            var copyableSharedArtifactIds = observed.SharedPackageManagedArtifactIds;
            foreach (var source in observed.Artifacts
                         .Where(artifact => copyableSharedArtifactIds.Contains(artifact.Id)))
            {
                await publicationValidator.ValidateAsync(source, cancellationToken);
                var cloneId = Guid.NewGuid();
                var destination = $"robot-artifacts/{organizationId:D}/{cloneId:D}/{source.Checksum}.lua";
                preparedObjects.Track(destination);
                await objectStorage.CopyImmutableAsync(source.StorageKey,
                    new ArtifactObjectWriteRequest(destination, "application/octet-stream",
                        source.ContentLengthBytes, source.Checksum), cancellationToken);
                var clone = RobotArtifact.CreateDraft(
                    organizationId,
                    ProductionPackageInstallationMaterializer.ForkArtifactCode(source.ArtifactCode, installationId),
                    source.ArtifactName,
                    destination,
                    source.FileName,
                    source.Checksum,
                    source.RuntimeTargetCode,
                    source.MachineModelCode,
                    source.ContentLengthBytes,
                    source.ExportedAt,
                    source.Description,
                    source.MetadataJson,
                    source.SourceRobotArtifactTemplateId,
                    source.TechnicalContractId,
                    source.TechnicalContractChecksum);
                clone.Id = cloneId;
                clone.CreatedByAccountId = user.AccountId;
                stagedBySourceArtifactId.Add(source.Id, clone);
            }

            var resources = observed.Artifacts.Select(artifact => TechnicalResourceMutationIdentity.Artifact(artifact.Id))
                .Concat(observed.Programs.Select(program => TechnicalResourceMutationIdentity.Program(program.Id)))
                .Concat(stagedBySourceArtifactId.Values.Select(artifact =>
                    TechnicalResourceMutationIdentity.ArtifactDefinition(organizationId, artifact.ArtifactCode)))
                .Append(new TechnicalResourceMutationIdentity("ProductionPackageInstallation", installationId.ToString("D")))
                .ToArray();

            return await mutationCoordinator.ExecuteAsync(resources, async ct =>
            {
                var current = await installations.GetForkGraphAsync(
                    organizationId, installationId, tracked: true, ct)
                    ?? throw new DomainRuleException("Package installation disappeared while forking.");
                if (await installations.HasActiveUpgradeAsync(organizationId, installationId, ct))
                    throw new DomainRuleException(
                        "A package installation participating in an active upgrade cannot be forked.");
                if (!observed.Artifacts.Select(x => x.Id).ToHashSet().SetEquals(current.Artifacts.Select(x => x.Id)) ||
                    !observed.Programs.Select(x => x.Id).ToHashSet().SetEquals(current.Programs.Select(x => x.Id)) ||
                    !observed.SharedPackageManagedArtifactIds.SetEquals(current.SharedPackageManagedArtifactIds))
                    throw new DomainRuleException(
                        "Package technical ownership changed while the fork was being prepared; retry the fork.");

                var createdPrograms = new List<RobotProgram>();
                var removedProgramArtifacts = new List<RobotProgramArtifact>();
                foreach (var program in current.Programs)
                {
                    var replacements = program.RobotProgramArtifacts
                        .OrderBy(item => item.RunOrder)
                        .Select(item => (
                            ArtifactId: stagedBySourceArtifactId.TryGetValue(item.RobotArtifactId, out var clone)
                                ? clone.Id
                                : item.RobotArtifactId,
                            RunOrder: item.RunOrder,
                            ParametersJson: item.ParametersJson,
                            ParametersSchemaVersion: item.ParametersSchemaVersion,
                            RequiredOptionCode: item.RequiredOptionCode))
                        .ToArray();
                    if (!replacements.Any(replacement =>
                            program.RobotProgramArtifacts.Any(item =>
                                item.RunOrder == replacement.RunOrder &&
                                item.RobotArtifactId != replacement.ArtifactId)))
                    {
                        continue;
                    }

                    if (program.Status == RobotProgramStatus.Draft)
                    {
                        removedProgramArtifacts.AddRange(program.ReplaceArtifacts(replacements));
                        program.UpdatedByAccountId = user.AccountId;
                        continue;
                    }

                    var clone = RobotProgram.CreateDraft(
                        ProductionPackageInstallationMaterializer.ForkProgramCode(program.Code, installationId),
                        program.Name,
                        program.ScopeType,
                        program.OrganizationId,
                        program.StoreId,
                        program.KioskId,
                        program.DeviceId,
                        program.Description);
                    clone.CreatedByAccountId = user.AccountId;
                    foreach (var replacement in replacements)
                    {
                        clone.AddArtifact(replacement.ArtifactId, replacement.RunOrder, replacement.ParametersJson,
                            replacement.ParametersSchemaVersion, replacement.RequiredOptionCode);
                    }
                    createdPrograms.Add(clone);

                    foreach (var materialization in current.Installation.Materializations.Where(item =>
                                 item.ResourceKind == ProductionPackageResourceKind.RobotProgram &&
                                 item.TargetKey == program.Id.ToString("D")))
                    {
                        materialization.Retarget(clone.Id.ToString("D"));
                    }
                }

                foreach (var materialization in current.Installation.Materializations
                             .Where(item => item.ResourceKind == ProductionPackageResourceKind.RobotArtifact &&
                                 Guid.TryParse(item.TargetKey, out var sourceId) &&
                                 stagedBySourceArtifactId.ContainsKey(sourceId)))
                {
                    var sourceId = Guid.Parse(materialization.TargetKey);
                    var clone = stagedBySourceArtifactId[sourceId];
                    materialization.Retarget(clone.Id.ToString("D"), clone.Checksum);
                }

                current.Installation.Fork();
                current.Installation.UpdatedByAccountId = user.AccountId;
                await installations.PersistForkAsync(
                    current.Installation,
                    stagedBySourceArtifactId.Values.ToArray(),
                    createdPrograms,
                    removedProgramArtifacts,
                    ct);
                preparedObjects.Commit();
                return ApiResult<ProductionPackageInstallationResult>.Success(
                    ProductionPackageInstallationResult.From(current.Installation),
                    "Package-managed configuration converted to an organization fork.");
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is DomainRuleException or ArtifactObjectNotFoundException or
                                   ArtifactObjectIntegrityException or ArtifactObjectStorageUnavailableException or
                                   Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return ApiResult<ProductionPackageInstallationResult>.Fail(
                ex.Message, ex is ArtifactObjectStorageUnavailableException ? 503 : 409);
        }
    }

    public async Task<ApiResult<ProductionPackageRepairResult>> RepairAsync(
        CurrentUserContext user, Guid organizationId, Guid installationId,
        CancellationToken cancellationToken)
    {
        var installation = await installations.GetForEditAsync(organizationId, installationId, cancellationToken);
        if (installation is null)
            return ApiResult<ProductionPackageRepairResult>.Fail("Package installation not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageInstall, user, organizationId,
                installation.StoreId, installation.KioskId))
            return ApiResult<ProductionPackageRepairResult>.Fail("Access denied.", 403);
        if (installation.Status != ProductionPackageInstallationStatus.Installed ||
            installation.OwnershipMode != ProductionPackageOwnershipMode.PackageManaged)
            return ApiResult<ProductionPackageRepairResult>.Fail(
                "Only an Installed package-managed configuration can be repaired.", 409);

        var repair = await installations.RestoreSoftDeletedMaterializationsAsync(
            organizationId, installationId, user.AccountId, cancellationToken);
        if (repair.Issues.Count > 0)
        {
            var issueCodes = string.Join(", ", repair.Issues.Select(x => x.Code).Distinct(StringComparer.Ordinal));
            return ApiResult<ProductionPackageRepairResult>.Fail(
                    $"Package materializations cannot be repaired automatically: {issueCodes}.", 409)
                .AddDetail("issues", repair.Issues);
        }
        return ApiResult<ProductionPackageRepairResult>.Success(
            new ProductionPackageRepairResult(installationId, repair.Restored),
            repair.Restored.Count == 0
                ? "Package materialization targets are already active."
                : "Soft-deleted package materialization targets were restored in place.");
    }

}
