using System.Text.Json;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Application.Identity.Tokens.Claims;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.AuthoringImports;
using Domain.RobotConfiguration.Programs;
using Domain.Tenants.Enums;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.Programs.Commands;
using Application.RobotConfiguration.ArtifactContracts;
using Application.Shared.Concurrency;

namespace Application.RobotConfiguration.AuthoringImports;

public interface IRobotAuthoringImportStore
{
    Task<bool> ScopeExistsAsync(Guid organizationId, Guid? storeId, Guid? kioskId, Guid? deviceId, CancellationToken cancellationToken);
    Task<RobotAuthoringImport?> GetByIdempotencyKeyAsync(Guid organizationId, string idempotencyKey, bool tracked, CancellationToken cancellationToken);
    Task<RobotAuthoringImport?> GetAsync(Guid organizationId, Guid importId, bool tracked, CancellationToken cancellationToken);
    Task<IReadOnlyList<RobotArtifactTechnicalContract>> GetContractsAsync(Guid organizationId, IReadOnlyCollection<string> codes, bool tracked, CancellationToken cancellationToken);
    Task<IReadOnlyList<RobotArtifact>> GetArtifactsAsync(Guid organizationId, IReadOnlyCollection<string> codes, bool tracked, CancellationToken cancellationToken);
    Task<RobotProgram?> GetProgramAsync(Guid organizationId, Guid? storeId, Guid? kioskId, Guid? deviceId, string code, bool tracked, CancellationToken cancellationToken);
    Task<RobotAuthoringImport?> BeginMutationAsync(Guid organizationId, Guid importId, CancellationToken cancellationToken);
    Task LockApplyResourceIdentitiesAsync(Guid organizationId, Guid? storeId, Guid? kioskId, Guid? deviceId,
        string programCode, IReadOnlyCollection<string> artifactCodes, CancellationToken cancellationToken);
    Task CommitMutationAsync(CancellationToken cancellationToken);
    Task RollbackMutationAsync(CancellationToken cancellationToken);
    Task<(bool Created, RobotAuthoringImport Import)> InsertOrGetExistingAsync(RobotAuthoringImport importSession, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task PrepareApplyAsync(IReadOnlyCollection<RobotArtifactTechnicalContract> contracts,
        IReadOnlyCollection<RobotArtifact> artifacts, RobotProgram? program, CancellationToken cancellationToken);
    Task CommitPreparedMutationAsync(CancellationToken cancellationToken);
}

public sealed class UploadRobotAuthoringImportCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public Guid? DeviceId { get; init; }
    public required string IdempotencyKey { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long ContentLengthBytes { get; init; }
    public required Stream Content { get; init; }
}

public sealed record GetRobotAuthoringImportQuery(CurrentUserContext UserContext, Guid OrganizationId, Guid ImportId);
public sealed record ValidateRobotAuthoringImportCommand(CurrentUserContext UserContext, Guid OrganizationId, Guid ImportId);
public sealed record ApplyRobotAuthoringImportCommand(CurrentUserContext UserContext, Guid OrganizationId, Guid ImportId);
public sealed record PublishRobotAuthoringImportCommand(CurrentUserContext UserContext, Guid OrganizationId, Guid ImportId);
public sealed record DiscardRobotAuthoringImportCommand(CurrentUserContext UserContext, Guid OrganizationId, Guid ImportId);

public sealed record RobotAuthoringImportValidationIssue(string Code, string Message, string? ArtifactCode = null);
public sealed record RobotAuthoringImportValidationReport(
    bool CanApply,
    IReadOnlyCollection<RobotAuthoringImportValidationIssue> Errors,
    IReadOnlyCollection<RobotAuthoringImportValidationIssue> Warnings,
    int ExistingArtifactCount,
    int NewArtifactCount,
    int ExistingContractCount,
    int NewContractCount);

public sealed record RobotAuthoringImportItemResult(Guid Id, string ArtifactCode, string FileName,
    string SidecarFileName, int RunOrder, string LuaChecksum, string SidecarChecksum, string Status,
    Guid? RobotArtifactId, Guid? TechnicalContractId, string? FailureCode, string? FailureMessage);

public sealed record RobotAuthoringImportResult(Guid Id, Guid OrganizationId, Guid? StoreId, Guid? KioskId,
    Guid? DeviceId, Guid ClientExportId, string ImportChecksum, int SchemaVersion, string Status,
    string ProposedProgramCode, string ProposedProgramName, string RuntimeTargetCode, string MachineModelCode,
    RobotAuthoringImportValidationReport? Validation, Guid? AppliedRobotProgramId,
    Guid? LinkedConfigurationReleaseId,
    Guid? ComposedRecipeId, IReadOnlyCollection<string> ComposedOptionCodes, string? CompositionPreviewChecksum,
    IReadOnlyCollection<RobotAuthoringImportItemResult> Items, IReadOnlyCollection<string> NextActions,
    DateTimeOffset CreatedAt, DateTimeOffset? ValidatedAt, DateTimeOffset? AppliedAt, DateTimeOffset? PublishedAt,
    string? FailureCode, string? FailureMessage)
{
    public static RobotAuthoringImportResult From(RobotAuthoringImport value)
    {
        RobotAuthoringImportValidationReport? validation = null;
        if (!string.IsNullOrWhiteSpace(value.ValidationReportJson))
            validation = JsonSerializer.Deserialize<RobotAuthoringImportValidationReport>(value.ValidationReportJson);
        var actions = value.Status switch
        {
            RobotAuthoringImportStatus.Uploaded => new[] { "ValidateImport", "DiscardImport" },
            RobotAuthoringImportStatus.Validated when validation?.CanApply == true => new[] { "ApplyImport", "DiscardImport" },
            RobotAuthoringImportStatus.Validated => new[] { "ResolveArtifactRevisionConflict", "DiscardImport" },
            RobotAuthoringImportStatus.Applied when value.LinkedConfigurationReleaseId.HasValue =>
                new[] { "ReviewConfigurationReleaseDraft", "PublishConfigurationRelease" },
            RobotAuthoringImportStatus.Applied when value.PublishedAt.HasValue =>
                new[] { "CreateConfigurationReleaseDraft" },
            RobotAuthoringImportStatus.Applied => new[] { "PreviewSemanticComposition", "ReviewTechnicalContracts", "PublishImportResources" },
            RobotAuthoringImportStatus.Failed => new[] { "ValidateImport", "DiscardImport" },
            _ => Array.Empty<string>()
        };
        return new RobotAuthoringImportResult(value.Id, value.OrganizationId, value.StoreId, value.KioskId,
            value.DeviceId, value.ClientExportId, value.ImportChecksum, value.SchemaVersion, value.Status.ToString(),
            value.ProposedProgramCode, value.ProposedProgramName, value.RuntimeTargetCode, value.MachineModelCode,
            validation, value.AppliedRobotProgramId, value.LinkedConfigurationReleaseId,
            value.ComposedRecipeId, value.GetComposedOptionCodes(), value.CompositionPreviewChecksum,
            value.Items.OrderBy(x => x.RunOrder).Select(x => new RobotAuthoringImportItemResult(x.Id,
                x.ArtifactCode, x.FileName, x.SidecarFileName, x.RunOrder, x.LuaChecksum, x.SidecarChecksum,
                x.Status.ToString(), x.RobotArtifactId, x.TechnicalContractId, x.FailureCode, x.FailureMessage)).ToArray(),
            actions, value.CreatedAt, value.ValidatedAt, value.AppliedAt, value.PublishedAt,
            value.FailureCode, value.FailureMessage);
    }
}

public sealed class RobotAuthoringImportHandlers(
    IRobotAuthoringImportStore store,
    IArtifactObjectStorage objectStorage,
    RobotArtifactTechnicalContractHandlers technicalContractHandlers,
    AssignRobotArtifactTechnicalContractHandler assignContractHandler,
    PublishRobotArtifactCommandHandler publishArtifactHandler,
    PublishRobotProgramCommandHandler publishProgramHandler,
    RobotAuthoringImportValidator validator,
    ITechnicalResourceMutationCoordinator mutationCoordinator)
{
    public async Task<ApiResult<RobotAuthoringImportResult>> UploadAsync(UploadRobotAuthoringImportCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = RobotAuthoringImportObservability.Start("icebot.robot_authoring.upload", command.OrganizationId);
        if (!CanUpload(command.UserContext, command.OrganizationId))
            return ApiResult<RobotAuthoringImportResult>.Fail("Access denied.", 403);
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Length > 200)
            return ApiResult<RobotAuthoringImportResult>.Fail("A valid Idempotency-Key header is required.", 400);
        if (!command.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
            command.ContentLengthBytes is <= 0 or > RobotAuthoringBundleCodec.MaximumArchiveBytes)
            return ApiResult<RobotAuthoringImportResult>.Fail("A bounded .zip authoring bundle is required.", 400);
        if (!await store.ScopeExistsAsync(command.OrganizationId, command.StoreId, command.KioskId, command.DeviceId, cancellationToken))
            return ApiResult<RobotAuthoringImportResult>.Fail("Robot program target scope was not found.", 404);

        byte[] bytes;
        try { bytes = await ReadBoundedAsync(command.Content, command.ContentLengthBytes, cancellationToken); }
        catch (RobotAuthoringBundleException ex) { return ApiResult<RobotAuthoringImportResult>.Fail(ex.Message, 400); }
        var checksum = RobotAuthoringBundleCodec.Sha256(bytes);
        RobotAuthoringBundle bundle;
        try { bundle = RobotAuthoringBundleCodec.Parse(bytes); }
        catch (RobotAuthoringBundleException ex) { return ApiResult<RobotAuthoringImportResult>.Fail(ex.Message, 400); }

        var existing = await store.GetByIdempotencyKeyAsync(command.OrganizationId, command.IdempotencyKey.Trim(), false, cancellationToken);
        if (existing is not null)
        {
            if (existing.ImportChecksum != checksum || existing.ClientExportId != bundle.Manifest.ExportId ||
                existing.StoreId != command.StoreId || existing.KioskId != command.KioskId || existing.DeviceId != command.DeviceId)
                return ApiResult<RobotAuthoringImportResult>.Fail("Idempotency key was already used with a different bundle or scope.", 409);
            return ApiResult<RobotAuthoringImportResult>.Success(RobotAuthoringImportResult.From(existing), "Existing import returned.");
        }

        var importId = Guid.NewGuid();
        var storageKey = $"robot-authoring-imports/{command.OrganizationId:D}/{importId:D}/{checksum}.zip";
        try
        {
            await objectStorage.WriteImmutableAsync(new ArtifactObjectWriteRequest(storageKey,
                "application/zip", bytes.LongLength, checksum), new MemoryStream(bytes, writable: false), cancellationToken);
        }
        catch (ArtifactObjectStorageUnavailableException)
        {
            return ApiResult<RobotAuthoringImportResult>.Fail("Artifact object storage is temporarily unavailable.", 503);
        }

        try
        {
            var session = RobotAuthoringImport.Create(command.OrganizationId, command.StoreId, command.KioskId,
                command.DeviceId, bundle.Manifest.ExportId, checksum, command.IdempotencyKey,
                bundle.Manifest.SchemaVersion, bundle.Manifest.Program.Code, bundle.Manifest.Program.Name,
                bundle.Manifest.Program.RuntimeTargetCode, bundle.Manifest.Program.MachineModelCode, storageKey,
                command.UserContext.AccountId);
            session.Id = importId;
            foreach (var item in bundle.Items)
                session.AddItem(item.ManifestItem.ArtifactCode, item.ManifestItem.FileName,
                    item.ManifestItem.SidecarFileName, item.ManifestItem.RunOrder, item.LuaChecksum, item.SidecarChecksum);
            var insert = await store.InsertOrGetExistingAsync(session, cancellationToken);
            if (!insert.Created)
            {
                RobotAuthoringImportObservability.Duplicate();
                await objectStorage.DeleteIfExistsAsync(storageKey, CancellationToken.None);
                if (insert.Import.ImportChecksum != checksum || insert.Import.ClientExportId != bundle.Manifest.ExportId ||
                    insert.Import.StoreId != command.StoreId || insert.Import.KioskId != command.KioskId ||
                    insert.Import.DeviceId != command.DeviceId)
                    return ApiResult<RobotAuthoringImportResult>.Fail("Idempotency key was concurrently used with a different bundle or scope.", 409);
                return ApiResult<RobotAuthoringImportResult>.Success(RobotAuthoringImportResult.From(insert.Import),
                    "Concurrent retry resolved to the existing import.");
            }
            RobotAuthoringImportObservability.Uploaded(bundle.Items.Count);
            return ApiResult<RobotAuthoringImportResult>.Success(RobotAuthoringImportResult.From(insert.Import),
                "Robot authoring bundle staged.", 201);
        }
        catch
        {
            try { await objectStorage.DeleteIfExistsAsync(storageKey, CancellationToken.None); } catch { }
            throw;
        }
    }

    public async Task<ApiResult<RobotAuthoringImportResult>> GetAsync(GetRobotAuthoringImportQuery query,
        CancellationToken cancellationToken)
    {
        if (!CanRead(query.UserContext, query.OrganizationId))
            return ApiResult<RobotAuthoringImportResult>.Fail("Access denied.", 403);
        var session = await store.GetAsync(query.OrganizationId, query.ImportId, false, cancellationToken);
        return session is null
            ? ApiResult<RobotAuthoringImportResult>.Fail("Robot authoring import not found.", 404)
            : ApiResult<RobotAuthoringImportResult>.Success(RobotAuthoringImportResult.From(session));
    }

    public async Task<ApiResult<RobotAuthoringImportResult>> ValidateAsync(ValidateRobotAuthoringImportCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = RobotAuthoringImportObservability.Start("icebot.robot_authoring.validate", command.OrganizationId, command.ImportId);
        if (!CanUpload(command.UserContext, command.OrganizationId))
            return ApiResult<RobotAuthoringImportResult>.Fail("Access denied.", 403);
        var transactionStarted = false;
        RobotAuthoringImport? session = null;
        try
        {
            session = await store.BeginMutationAsync(command.OrganizationId, command.ImportId, cancellationToken);
            transactionStarted = true;
            if (session is null)
                return await RollbackMutationAndReturnAsync(
                    ApiResult<RobotAuthoringImportResult>.Fail("Robot authoring import not found.", 404));
            if (session.Status is RobotAuthoringImportStatus.Applied or RobotAuthoringImportStatus.Discarded)
                return await RollbackMutationAndReturnAsync(
                    ApiResult<RobotAuthoringImportResult>.Fail(
                        "Applied or discarded imports cannot be validated.", 409));
            var bundle = await ReadBundleAsync(session, cancellationToken);
            var report = await validator.BuildReportAsync(session, bundle, cancellationToken);
            session.MarkValidated(JsonSerializer.Serialize(report), DateTimeOffset.UtcNow, command.UserContext.AccountId);
            RobotAuthoringImportObservability.Validated(report);
            await store.CommitMutationAsync(cancellationToken);
            transactionStarted = false;
            return ApiResult<RobotAuthoringImportResult>.Success(RobotAuthoringImportResult.From(session),
                report.CanApply ? "Import validated." : "Import validation found blocking conflicts.");
        }
        catch (RobotAuthoringBundleException ex)
        {
            if (transactionStarted && session is not null)
            {
                session.MarkFailed("BUNDLE_INVALID", ex.Message, DateTimeOffset.UtcNow,
                    command.UserContext.AccountId);
                await store.CommitMutationAsync(CancellationToken.None);
                transactionStarted = false;
            }
            return ApiResult<RobotAuthoringImportResult>.Fail(ex.Message, 400);
        }
        catch (ArtifactObjectNotFoundException)
        {
            if (transactionStarted && session is not null)
            {
                session.MarkFailed("STAGING_EXPIRED", "The staged authoring bundle is no longer available.",
                    DateTimeOffset.UtcNow, command.UserContext.AccountId);
                await store.CommitMutationAsync(CancellationToken.None);
                transactionStarted = false;
            }
            return ApiResult<RobotAuthoringImportResult>.Fail("The staged authoring bundle expired; upload it again.", 410);
        }
        catch (ArtifactObjectStorageUnavailableException)
        {
            if (transactionStarted) await store.RollbackMutationAsync(CancellationToken.None);
            return ApiResult<RobotAuthoringImportResult>.Fail("Artifact object storage is temporarily unavailable.", 503);
        }
        catch
        {
            if (transactionStarted) await store.RollbackMutationAsync(CancellationToken.None);
            throw;
        }

        async Task<ApiResult<RobotAuthoringImportResult>> RollbackMutationAndReturnAsync(
            ApiResult<RobotAuthoringImportResult> result)
        {
            await store.RollbackMutationAsync(CancellationToken.None);
            transactionStarted = false;
            return result;
        }
    }

    public async Task<ApiResult<RobotAuthoringImportResult>> ApplyAsync(ApplyRobotAuthoringImportCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = RobotAuthoringImportObservability.Start("icebot.robot_authoring.apply", command.OrganizationId, command.ImportId);
        var startedAt = Stopwatch.GetTimestamp();
        if (!CanUpload(command.UserContext, command.OrganizationId) || !CanManageProgram(command.UserContext, command.OrganizationId))
            return ApiResult<RobotAuthoringImportResult>.Fail("Both artifact.upload and program.manage access are required.", 403);
        var session = await store.GetAsync(command.OrganizationId, command.ImportId, false, cancellationToken);
        if (session is null) return ApiResult<RobotAuthoringImportResult>.Fail("Robot authoring import not found.", 404);
        if (session.Status == RobotAuthoringImportStatus.Applied)
            return ApiResult<RobotAuthoringImportResult>.Success(RobotAuthoringImportResult.From(session), "Import was already applied.");
        if (session.Status != RobotAuthoringImportStatus.Validated)
            return ApiResult<RobotAuthoringImportResult>.Fail("Import must be validated before apply.", 409);
        var currentReport = JsonSerializer.Deserialize<RobotAuthoringImportValidationReport>(session.ValidationReportJson!);
        if (currentReport?.CanApply != true)
            return ApiResult<RobotAuthoringImportResult>.Fail("Import has unresolved validation conflicts.", 409);

        var newContracts = new List<RobotArtifactTechnicalContract>();
        var newArtifacts = new List<RobotArtifact>();
        var writtenKeys = new List<string>();
        var usedKeys = new HashSet<string>(StringComparer.Ordinal);
        var stagedObjects = new Dictionary<string, StagedArtifactObject>(StringComparer.Ordinal);
        var transactionStarted = false;
        var commitAttempted = false;

        try
        {
            RobotAuthoringBundle bundle;
            try { bundle = await ReadBundleAsync(session, cancellationToken); }
            catch (ArtifactObjectNotFoundException)
            {
                return ApiResult<RobotAuthoringImportResult>.Fail(
                    "The staged authoring bundle expired; upload it again.", 410);
            }
            catch (ArtifactObjectStorageUnavailableException)
            {
                return ApiResult<RobotAuthoringImportResult>.Fail(
                    "Artifact object storage is temporarily unavailable.", 503);
            }

            var artifactCodes = bundle.Items.Select(item => Normalize(item.ManifestItem.ArtifactCode)).ToArray();
            var preliminaryArtifacts = await store.GetArtifactsAsync(session.OrganizationId,
                artifactCodes, false, cancellationToken);
            var preliminaryArtifactCodes = preliminaryArtifacts.Select(artifact => Normalize(artifact.ArtifactCode))
                .ToHashSet(StringComparer.Ordinal);
            foreach (var item in bundle.Items.Where(item =>
                         !preliminaryArtifactCodes.Contains(Normalize(item.ManifestItem.ArtifactCode))))
            {
                var code = Normalize(item.ManifestItem.ArtifactCode);
                var stagedObject = await StageArtifactObjectAsync(session, item, cancellationToken);
                stagedObjects.Add(code, stagedObject);
                writtenKeys.Add(stagedObject.Object.StorageKey);
            }

            session = await store.BeginMutationAsync(command.OrganizationId, command.ImportId, cancellationToken);
            transactionStarted = true;
            if (session is null)
                return await RollbackApplyAndReturnAsync(
                    ApiResult<RobotAuthoringImportResult>.Fail("Robot authoring import not found.", 404));
            if (session.Status == RobotAuthoringImportStatus.Applied)
                return await RollbackApplyAndReturnAsync(
                    ApiResult<RobotAuthoringImportResult>.Success(RobotAuthoringImportResult.From(session),
                        "Import was already applied."));
            if (session.Status != RobotAuthoringImportStatus.Validated)
                return await RollbackApplyAndReturnAsync(
                    ApiResult<RobotAuthoringImportResult>.Fail("Import state changed; validate again.", 409));
            await store.LockApplyResourceIdentitiesAsync(session.OrganizationId, session.StoreId, session.KioskId,
                session.DeviceId, session.ProposedProgramCode, artifactCodes, cancellationToken);

            var observedContracts = await store.GetContractsAsync(session.OrganizationId,
                artifactCodes, false, cancellationToken);
            var observedArtifacts = await store.GetArtifactsAsync(session.OrganizationId,
                artifactCodes, false, cancellationToken);
            var observedProgram = await store.GetProgramAsync(session.OrganizationId, session.StoreId,
                session.KioskId, session.DeviceId, session.ProposedProgramCode, false, cancellationToken);
            var existingResourceLocks = observedContracts
                .Select(contract => TechnicalResourceMutationIdentity.Contract(contract.Id))
                .Concat(observedArtifacts.Select(artifact => TechnicalResourceMutationIdentity.Artifact(artifact.Id)))
                .Concat(observedProgram is null
                    ? []
                    : new[] { TechnicalResourceMutationIdentity.Program(observedProgram.Id) })
                .ToArray();
            if (existingResourceLocks.Length > 0)
                await mutationCoordinator.ExecuteAsync(
                    existingResourceLocks, _ => Task.FromResult(true), cancellationToken);

            var freshReport = await validator.BuildReportAsync(session, bundle, cancellationToken);
            if (!freshReport.CanApply)
                return await RollbackApplyAndReturnAsync(
                    ApiResult<RobotAuthoringImportResult>.Fail(
                        "Authoring resources changed after validation; validate again.", 409));

            var existingContracts = await store.GetContractsAsync(session.OrganizationId,
                artifactCodes, true, cancellationToken);
            var existingArtifacts = await store.GetArtifactsAsync(session.OrganizationId,
                artifactCodes, true, cancellationToken);
            var program = await store.GetProgramAsync(session.OrganizationId, session.StoreId, session.KioskId,
                session.DeviceId, session.ProposedProgramCode, true, cancellationToken);

            foreach (var item in bundle.Items.OrderBy(x => x.ManifestItem.RunOrder))
            {
                var code = Normalize(item.ManifestItem.ArtifactCode);
                var contract = existingContracts.SingleOrDefault(x => x.ContractCode == code);
                if (contract is null)
                {
                    contract = CreateDraftContract(session, item, command.UserContext.AccountId);
                    newContracts.Add(contract);
                }

                var artifact = existingArtifacts.SingleOrDefault(x => x.ArtifactCode == code);
                var created = false;
                if (artifact is null)
                {
                    if (!stagedObjects.TryGetValue(code, out var stagedObject))
                        return await RollbackApplyAndReturnAsync(
                            ApiResult<RobotAuthoringImportResult>.Fail(
                                $"Artifact '{code}' changed while the import was being prepared; validate again.", 409));
                    artifact = CreateDraftArtifact(session, item, stagedObject,
                        contract.Status == RobotArtifactContractStatus.Published ? contract : null,
                        command.UserContext.AccountId);
                    usedKeys.Add(artifact.StorageKey);
                    newArtifacts.Add(artifact);
                    created = true;
                }
                session.Items.Single(x => x.ArtifactCode == code).MarkResolved(artifact.Id, contract.Id, created);
            }

            if (program is null)
            {
                program = RobotProgram.CreateDraft(session.ProposedProgramCode, session.ProposedProgramName,
                    ResolveScope(session), session.OrganizationId, session.StoreId, session.KioskId, session.DeviceId,
                    $"Created from robot authoring import {session.Id:D}.");
                program.Id = Guid.NewGuid();
                program.CreatedByAccountId = command.UserContext.AccountId;
                foreach (var item in session.Items.OrderBy(x => x.RunOrder))
                    program.AddArtifact(item.RobotArtifactId!.Value, item.RunOrder);
            }

            session.MarkApplied(program.Id, DateTimeOffset.UtcNow, command.UserContext.AccountId);
            await store.PrepareApplyAsync(newContracts, newArtifacts, program, cancellationToken);
            commitAttempted = true;
            // Once metadata is prepared, client cancellation must not interrupt the commit boundary.
            await store.CommitPreparedMutationAsync(CancellationToken.None);
            transactionStarted = false;
            await DeleteWrittenObjectsAsync(writtenKeys.Where(key => !usedKeys.Contains(key)));
            RobotAuthoringImportObservability.Applied(Stopwatch.GetElapsedTime(startedAt), session.Items.Count);
            return ApiResult<RobotAuthoringImportResult>.Success(RobotAuthoringImportResult.From(session),
                "Draft technical contracts, artifacts, and ordered program materialized.");
        }
        catch (DomainRuleException ex)
        {
            if (transactionStarted) await store.RollbackMutationAsync(CancellationToken.None);
            if (!commitAttempted) await DeleteWrittenObjectsAsync(writtenKeys);
            return ApiResult<RobotAuthoringImportResult>.Fail(ex.Message, 400);
        }
        catch (ArtifactObjectStorageUnavailableException)
        {
            if (transactionStarted) await store.RollbackMutationAsync(CancellationToken.None);
            if (!commitAttempted) await DeleteWrittenObjectsAsync(writtenKeys);
            return ApiResult<RobotAuthoringImportResult>.Fail(
                "Artifact object storage is temporarily unavailable.", 503);
        }
        catch
        {
            if (transactionStarted) await store.RollbackMutationAsync(CancellationToken.None);
            if (!commitAttempted) await DeleteWrittenObjectsAsync(writtenKeys);
            throw;
        }

        async Task<ApiResult<RobotAuthoringImportResult>> RollbackApplyAndReturnAsync(
            ApiResult<RobotAuthoringImportResult> result)
        {
            if (transactionStarted) await store.RollbackMutationAsync(CancellationToken.None);
            transactionStarted = false;
            if (!commitAttempted) await DeleteWrittenObjectsAsync(writtenKeys);
            return result;
        }

        async Task DeleteWrittenObjectsAsync(IEnumerable<string> keys)
        {
            foreach (var key in keys)
                try { await objectStorage.DeleteIfExistsAsync(key, CancellationToken.None); } catch { }
        }
    }

    public async Task<ApiResult<RobotAuthoringImportResult>> DiscardAsync(DiscardRobotAuthoringImportCommand command,
        CancellationToken cancellationToken)
    {
        if (!CanUpload(command.UserContext, command.OrganizationId))
            return ApiResult<RobotAuthoringImportResult>.Fail("Access denied.", 403);
        var transactionStarted = false;
        try
        {
            var session = await store.BeginMutationAsync(command.OrganizationId, command.ImportId, cancellationToken);
            transactionStarted = true;
            if (session is null)
            {
                await store.RollbackMutationAsync(CancellationToken.None);
                transactionStarted = false;
                return ApiResult<RobotAuthoringImportResult>.Fail("Robot authoring import not found.", 404);
            }

            session.Discard(DateTimeOffset.UtcNow, command.UserContext.AccountId);
            await store.CommitMutationAsync(cancellationToken);
            transactionStarted = false;
            try { await objectStorage.DeleteIfExistsAsync(session.StagingStorageKey, CancellationToken.None); } catch { }
            return ApiResult<RobotAuthoringImportResult>.Success(RobotAuthoringImportResult.From(session), "Import discarded.");
        }
        catch (DomainRuleException ex)
        {
            if (transactionStarted) await store.RollbackMutationAsync(CancellationToken.None);
            return ApiResult<RobotAuthoringImportResult>.Fail(ex.Message, 409);
        }
        catch
        {
            if (transactionStarted) await store.RollbackMutationAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<ApiResult<RobotAuthoringImportResult>> PublishAsync(PublishRobotAuthoringImportCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = RobotAuthoringImportObservability.Start("icebot.robot_authoring.publish",
            command.OrganizationId, command.ImportId);
        if (!CanUpload(command.UserContext, command.OrganizationId) || !CanManageProgram(command.UserContext, command.OrganizationId))
            return ApiResult<RobotAuthoringImportResult>.Fail("Both artifact.upload and program.manage access are required.", 403);
        var transactionStarted = false;
        RobotAuthoringImport? session = null;
        try
        {
            session = await store.BeginMutationAsync(command.OrganizationId, command.ImportId, cancellationToken);
            transactionStarted = true;
            if (session is null)
                return await RollbackPublicationFailureAsync("IMPORT_NOT_FOUND", "Robot authoring import not found.", 404);
            if (session.Status != RobotAuthoringImportStatus.Applied || !session.AppliedRobotProgramId.HasValue)
                return await RollbackPublicationFailureAsync("IMPORT_NOT_APPLIED", "Import must be applied before publication.");
            if (session.PublishedAt.HasValue)
            {
                await store.RollbackMutationAsync(CancellationToken.None);
                transactionStarted = false;
                return ApiResult<RobotAuthoringImportResult>.Success(RobotAuthoringImportResult.From(session),
                    "Import resources were already published.");
            }

            if (session.Items.Any(item => !item.TechnicalContractId.HasValue || !item.RobotArtifactId.HasValue))
                return await RollbackPublicationFailureAsync("IMPORT_ITEM_INCOMPLETE",
                    "Import item is missing materialized resource identities.");

            var publicationResources = session.Items
                .SelectMany(item => new[]
                {
                    TechnicalResourceMutationIdentity.Artifact(item.RobotArtifactId!.Value),
                    TechnicalResourceMutationIdentity.Contract(item.TechnicalContractId!.Value)
                })
                .Append(TechnicalResourceMutationIdentity.Program(session.AppliedRobotProgramId.Value))
                .ToArray();
            await mutationCoordinator.ExecuteAsync(publicationResources, _ => Task.FromResult(true), cancellationToken);

            foreach (var item in session.Items.OrderBy(x => x.RunOrder))
            {
                var publishContract = await technicalContractHandlers.PublishAsync(
                    new PublishRobotArtifactTechnicalContractCommand(command.UserContext, command.OrganizationId,
                        item.TechnicalContractId!.Value), cancellationToken);
                if (!publishContract.Succeeded && publishContract.StatusCode != 400)
                    return await RollbackPublicationFailureAsync("CONTRACT_PUBLICATION_FAILED",
                        publishContract.Message, artifactCode: item.ArtifactCode);
                if (!publishContract.Succeeded)
                {
                    var contracts = await store.GetContractsAsync(command.OrganizationId,
                        [item.ArtifactCode], false, cancellationToken);
                    var contract = contracts.SingleOrDefault(candidate =>
                        candidate.Id == item.TechnicalContractId.Value);
                    if (contract?.Status != RobotArtifactContractStatus.Published)
                        return await RollbackPublicationFailureAsync("CONTRACT_PUBLICATION_FAILED",
                            publishContract.Message, artifactCode: item.ArtifactCode);
                }

                var assignment = await assignContractHandler.AssignArtifactAsync(command.UserContext,
                    command.OrganizationId, item.RobotArtifactId!.Value, item.TechnicalContractId!.Value, cancellationToken);
                if (!assignment.Succeeded)
                {
                    var artifacts = await store.GetArtifactsAsync(command.OrganizationId,
                        [item.ArtifactCode], false, cancellationToken);
                    var artifact = artifacts.SingleOrDefault(candidate => candidate.Id == item.RobotArtifactId.Value);
                    if (artifact?.TechnicalContractId != item.TechnicalContractId.Value)
                        return await RollbackPublicationFailureAsync("CONTRACT_ASSIGNMENT_FAILED",
                            assignment.Message, artifactCode: item.ArtifactCode);
                }

                var publishArtifact = await publishArtifactHandler.HandleAsync(new PublishRobotArtifactCommand
                {
                    UserContext = command.UserContext,
                    OrganizationId = command.OrganizationId,
                    ArtifactId = item.RobotArtifactId.Value
                }, cancellationToken);
                if (!publishArtifact.Succeeded)
                {
                    var artifacts = await store.GetArtifactsAsync(command.OrganizationId,
                        [item.ArtifactCode], false, cancellationToken);
                    var artifact = artifacts.SingleOrDefault(candidate => candidate.Id == item.RobotArtifactId.Value);
                    if (artifact?.Status != RobotArtifactStatus.Published)
                        return await RollbackPublicationFailureAsync("ARTIFACT_PUBLICATION_FAILED",
                            publishArtifact.Message, artifactCode: item.ArtifactCode);
                }
            }

            var publishProgram = await publishProgramHandler.HandleAsync(new PublishRobotProgramCommand
            {
                UserContext = command.UserContext,
                OrganizationId = command.OrganizationId,
                ProgramId = session.AppliedRobotProgramId.Value
            }, cancellationToken);
            if (!publishProgram.Succeeded)
            {
                var program = await store.GetProgramAsync(session.OrganizationId, session.StoreId, session.KioskId,
                    session.DeviceId, session.ProposedProgramCode, false, cancellationToken);
                if (program?.Status != RobotProgramStatus.Published)
                    return await RollbackPublicationFailureAsync("PROGRAM_PUBLICATION_FAILED", publishProgram.Message);
            }

            session.MarkPublished(DateTimeOffset.UtcNow, command.UserContext.AccountId);
            await store.CommitMutationAsync(cancellationToken);
            transactionStarted = false;
            return ApiResult<RobotAuthoringImportResult>.Success(RobotAuthoringImportResult.From(session),
                "Technical contracts, artifacts, and robot program published.");
        }
        catch (DomainRuleException ex)
        {
            if (transactionStarted) await store.RollbackMutationAsync(CancellationToken.None);
            return ApiResult<RobotAuthoringImportResult>.Fail(ex.Message, 409);
        }
        catch
        {
            if (transactionStarted) await store.RollbackMutationAsync(CancellationToken.None);
            throw;
        }

        async Task<ApiResult<RobotAuthoringImportResult>> RollbackPublicationFailureAsync(
            string code, string? message, int statusCode = 409, string? artifactCode = null)
        {
            await store.RollbackMutationAsync(CancellationToken.None);
            transactionStarted = false;
            return session is null
                ? ApiResult<RobotAuthoringImportResult>.Fail(message ?? "Import resource publication failed.",
                    statusCode, businessError: code)
                : PublicationFailure(session, code, message, artifactCode, statusCode);
        }
    }

    private static ApiResult<RobotAuthoringImportResult> PublicationFailure(RobotAuthoringImport session,
        string code, string? message, string? artifactCode = null, int statusCode = 409) =>
        ApiResult<RobotAuthoringImportResult>.Fail(message ?? "Import resource publication failed.", statusCode,
            businessError: code).AddDetail("importId", session.Id)
            .AddDetail("artifactCode", artifactCode ?? string.Empty);

    private async Task<RobotAuthoringBundle> ReadBundleAsync(RobotAuthoringImport session, CancellationToken cancellationToken)
    {
        var bytes = await objectStorage.ReadBytesAsync(session.StagingStorageKey,
            RobotAuthoringBundleCodec.MaximumArchiveBytes, cancellationToken);
        if (RobotAuthoringBundleCodec.Sha256(bytes) != session.ImportChecksum)
            throw new RobotAuthoringBundleException("Staged bundle checksum does not match the import record.");
        return RobotAuthoringBundleCodec.Parse(bytes);
    }

    private async Task<StagedArtifactObject> StageArtifactObjectAsync(RobotAuthoringImport session,
        RobotAuthoringBundleItem item, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var storageKey = $"robot-artifacts/{session.OrganizationId:D}/{id:D}/{item.LuaChecksum}.lua";
        var written = await objectStorage.WriteImmutableAsync(new ArtifactObjectWriteRequest(storageKey, "text/x-lua",
            item.LuaBytes.LongLength, item.LuaChecksum), new MemoryStream(item.LuaBytes, writable: false), cancellationToken);
        return new StagedArtifactObject(id, written);
    }

    private static RobotArtifact CreateDraftArtifact(RobotAuthoringImport session,
        RobotAuthoringBundleItem item, StagedArtifactObject stagedObject,
        RobotArtifactTechnicalContract? publishedContract, Guid actorId)
    {
        var artifact = RobotArtifact.CreateDraft(session.OrganizationId, item.ManifestItem.ArtifactCode,
            item.ManifestItem.ArtifactCode, stagedObject.Object.StorageKey, item.ManifestItem.FileName,
            stagedObject.Object.Checksum, session.RuntimeTargetCode, session.MachineModelCode,
            stagedObject.Object.ContentLengthBytes,
            DateTimeOffset.UtcNow, $"Created from robot authoring import {session.Id:D}.",
            technicalContractId: publishedContract?.Id,
            technicalContractChecksum: publishedContract?.ContractChecksum);
        artifact.Id = stagedObject.ArtifactId;
        artifact.CreatedByAccountId = actorId;
        return artifact;
    }

    private static RobotArtifactTechnicalContract CreateDraftContract(RobotAuthoringImport session,
        RobotAuthoringBundleItem item, Guid actorId)
    {
        var contract = RobotArtifactTechnicalContract.CreateDraft(item.ManifestItem.ArtifactCode, 1,
            session.RuntimeTargetCode, session.MachineModelCode, session.OrganizationId,
            schemaVersion: item.Sidecar.SchemaVersion);
        contract.Id = Guid.NewGuid();
        contract.CreatedByAccountId = actorId;
        contract.ReplaceDefinition(
            item.Sidecar.Effects.Select(x => new RobotArtifactEffectDefinition(x.EffectCode, x.EffectKind,
                x.IngredientCode, x.OptionCode, x.QuantityMode, x.FixedQuantity, x.Unit,
                x.RequiredWorkcellCapabilityCode)).ToArray(),
            item.Sidecar.OrderingConstraints.Select(x => new RobotArtifactOrderingConstraintDefinition(
                x.ConstraintType, x.Value, x.SortHint)).ToArray());
        return contract;
    }

    private static TenantScopeType ResolveScope(RobotAuthoringImport session) => session.DeviceId.HasValue
        ? TenantScopeType.Device : session.KioskId.HasValue ? TenantScopeType.Kiosk
        : session.StoreId.HasValue ? TenantScopeType.Store : TenantScopeType.Organization;

    private static async Task<byte[]> ReadBoundedAsync(Stream source, long expectedLength, CancellationToken cancellationToken)
    {
        if (expectedLength is <= 0 or > RobotAuthoringBundleCodec.MaximumArchiveBytes)
            throw new RobotAuthoringBundleException("Bundle size is invalid.");
        using var output = new MemoryStream((int)expectedLength);
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += read;
            if (total > expectedLength || total > RobotAuthoringBundleCodec.MaximumArchiveBytes)
                throw new RobotAuthoringBundleException("Bundle exceeds its declared or maximum size.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        if (total != expectedLength) throw new RobotAuthoringBundleException("Bundle length does not match Content-Length.");
        return output.ToArray();
    }

    private static bool CanUpload(CurrentUserContext user, Guid organizationId) =>
        ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ArtifactUpload, user, organizationId, null, null);
    private static bool CanManageProgram(CurrentUserContext user, Guid organizationId) =>
        ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ProgramManage, user, organizationId, null, null);
    private static bool CanRead(CurrentUserContext user, Guid organizationId) =>
        ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ProgramRead, user, organizationId, null, null);
    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : Normalize(value);

    private sealed record StagedArtifactObject(Guid ArtifactId, ArtifactObjectWriteResult Object);
    private static bool EqualsCode(string left, string right) => string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}

public static class RobotAuthoringImportObservability
{
    public const string InstrumentationName = "IceBot.RobotAuthoring";
    private static readonly ActivitySource ActivitySource = new(InstrumentationName);
    private static readonly Meter Meter = new(InstrumentationName);
    private static readonly Counter<long> UploadedCounter = Meter.CreateCounter<long>("icebot.robot_authoring.import.uploaded");
    private static readonly Counter<long> DuplicateCounter = Meter.CreateCounter<long>("icebot.robot_authoring.import.duplicate");
    private static readonly Counter<long> ValidationFailureCounter = Meter.CreateCounter<long>("icebot.robot_authoring.import.validation_failed");
    private static readonly Counter<long> ItemCounter = Meter.CreateCounter<long>("icebot.robot_authoring.import.item");
    private static readonly Histogram<double> ApplyDuration = Meter.CreateHistogram<double>("icebot.robot_authoring.import.apply.duration", "ms");

    public static Activity? Start(string name, Guid organizationId, Guid? importId = null)
    {
        var activity = ActivitySource.StartActivity(name);
        activity?.SetTag("icebot.organization.id", organizationId);
        if (importId.HasValue) activity?.SetTag("icebot.robot_authoring.import.id", importId.Value);
        return activity;
    }

    public static void Uploaded(int itemCount)
    {
        UploadedCounter.Add(1);
        ItemCounter.Add(itemCount, new KeyValuePair<string, object?>("status", "staged"));
    }

    public static void Duplicate() => DuplicateCounter.Add(1);

    public static void Validated(RobotAuthoringImportValidationReport report)
    {
        foreach (var error in report.Errors)
            ValidationFailureCounter.Add(1, new KeyValuePair<string, object?>("code", error.Code));
    }

    public static void Applied(TimeSpan elapsed, int itemCount)
    {
        ApplyDuration.Record(elapsed.TotalMilliseconds);
        ItemCounter.Add(itemCount, new KeyValuePair<string, object?>("status", "applied"));
    }
}
