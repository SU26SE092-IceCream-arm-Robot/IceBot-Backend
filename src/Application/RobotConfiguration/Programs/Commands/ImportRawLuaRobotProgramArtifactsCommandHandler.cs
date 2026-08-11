using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.Programs.Abstractions;
using Application.RobotConfiguration.Programs.Mapping;
using Application.RobotConfiguration.Programs.Results;
using Application.Shared.Concurrency;
using Application.Shared.Ownership;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.Programs;

namespace Application.RobotConfiguration.Programs.Commands;

public sealed class ImportRawLuaRobotProgramArtifactsCommandHandler
{
    private readonly IRobotProgramStore _programs;
    private readonly IRobotArtifactStore _artifacts;
    private readonly BulkUploadRobotArtifactsCommandHandler _uploads;
    private readonly ITechnicalResourceMutationPolicy _technicalOwnership;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public ImportRawLuaRobotProgramArtifactsCommandHandler(
        IRobotProgramStore programs,
        IRobotArtifactStore artifacts,
        BulkUploadRobotArtifactsCommandHandler uploads,
        ITechnicalResourceMutationPolicy technicalOwnership,
        ITechnicalResourceMutationCoordinator mutations)
    {
        _programs = programs;
        _artifacts = artifacts;
        _uploads = uploads;
        _technicalOwnership = technicalOwnership;
        _mutations = mutations;
    }

    public async Task<ApiResult<RawLuaRobotProgramArtifactImportResult>> HandleAsync(
        ImportRawLuaRobotProgramArtifactsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Artifacts.Count is < 1 or > 50)
            return ApiResult<RawLuaRobotProgramArtifactImportResult>.Fail("Import one to 50 raw Lua files at a time.", 400);

        if (command.Artifacts.Any(item => item.ContentLengthBytes <= 0 ||
                                          !item.FileName.EndsWith(".lua", StringComparison.OrdinalIgnoreCase) ||
                                          string.IsNullOrWhiteSpace(item.ArtifactCode) ||
                                          string.IsNullOrWhiteSpace(item.ArtifactName)))
        {
            return ApiResult<RawLuaRobotProgramArtifactImportResult>.Fail(
                "Every raw import item must have a Lua file, artifact code, artifact name, and content.", 400);
        }

        if (command.Artifacts.GroupBy(item => item.FileName, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1) ||
            command.Artifacts.GroupBy(item => item.ArtifactCode, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            return ApiResult<RawLuaRobotProgramArtifactImportResult>.Fail(
                "Raw Lua file names and generated artifact codes must be unique within one import.", 400);
        }

        var observedProgram = await _programs.GetProgramByIdAsync(command.ProgramId, cancellationToken);
        if (observedProgram is null || observedProgram.OrganizationId != command.OrganizationId)
            return ApiResult<RawLuaRobotProgramArtifactImportResult>.Fail("Robot program not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.ProgramManage, command.UserContext,
                observedProgram.OrganizationId, observedProgram.StoreId, observedProgram.KioskId))
        {
            return ApiResult<RawLuaRobotProgramArtifactImportResult>.Fail("Access denied.", 403);
        }
        if (observedProgram.Status != RobotProgramStatus.Draft)
            return ApiResult<RawLuaRobotProgramArtifactImportResult>.Fail("Only draft robot programs can receive raw Lua artifacts.", 409);

        var uploadResult = await _uploads.HandleAsync(new BulkUploadRobotArtifactsCommand
        {
            UserContext = command.UserContext,
            OrganizationId = command.OrganizationId,
            Items = command.Artifacts.Select(item => new BulkUploadRobotArtifactItem
            {
                FileName = item.FileName,
                ContentType = item.ContentType,
                ContentLengthBytes = item.ContentLengthBytes,
                Content = item.Content,
                ArtifactCode = item.ArtifactCode,
                ArtifactName = item.ArtifactName,
                RuntimeTargetCode = command.RuntimeTargetCode,
                MachineModelCode = command.MachineModelCode,
                Description = command.Description,
                MetadataJson = "{\"source\":\"raw-lua-import\",\"semanticMode\":\"Opaque\"}"
            }).ToArray()
        }, cancellationToken);

        var upload = uploadResult.Data;
        if (upload is null)
            return ApiResult<RawLuaRobotProgramArtifactImportResult>.Fail(
                uploadResult.Message ?? "Raw Lua artifact upload failed.", uploadResult.StatusCode);

        if (!uploadResult.Succeeded)
            return ApiResult<RawLuaRobotProgramArtifactImportResult>.Fail(
                uploadResult.Message ?? "All raw Lua artifact uploads failed.", uploadResult.StatusCode);

        if (upload.FailedCount > 0)
        {
            return ApiResult<RawLuaRobotProgramArtifactImportResult>.Success(
                new RawLuaRobotProgramArtifactImportResult { Upload = upload },
                "Raw Lua import completed with failures. Successful artifacts remain as unassigned drafts; fix the failed files and retry to append them.",
                207);
        }

        var artifactIds = upload.Items
            .Select(item => item.RobotArtifactId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
        if (artifactIds.Length != command.Artifacts.Count)
            return ApiResult<RawLuaRobotProgramArtifactImportResult>.Fail(
                "Raw Lua artifacts were uploaded but their identities could not be resolved for program assignment.", 500);

        var appendResult = await AppendToDraftProgramAsync(command, artifactIds, cancellationToken);
        if (!appendResult.Succeeded || appendResult.Data is null)
        {
            return ApiResult<RawLuaRobotProgramArtifactImportResult>.Fail(
                $"Raw Lua artifacts were uploaded as drafts but were not appended to the program: {appendResult.Message}",
                appendResult.StatusCode);
        }

        return ApiResult<RawLuaRobotProgramArtifactImportResult>.Success(
            new RawLuaRobotProgramArtifactImportResult
            {
                Upload = upload,
                Program = appendResult.Data.Program,
                AppendedArtifactIds = appendResult.Data.AppendedArtifactIds
            },
            appendResult.Data.AppendedArtifactIds.Count == 0
                ? "All raw Lua artifacts were already assigned to this draft program."
                : "Raw Lua artifacts were uploaded and appended to the draft program.",
            upload.UploadedCount == 0 ? 200 : 201);
    }

    private Task<ApiResult<AppendResult>> AppendToDraftProgramAsync(
        ImportRawLuaRobotProgramArtifactsCommand command,
        IReadOnlyCollection<Guid> artifactIds,
        CancellationToken cancellationToken)
    {
        var resources = artifactIds
            .Select(TechnicalResourceMutationIdentity.Artifact)
            .Append(TechnicalResourceMutationIdentity.Program(command.ProgramId))
            .ToArray();

        return _mutations.ExecuteAsync(resources, async ct =>
        {
            var program = await _programs.GetProgramForEditAsync(command.ProgramId, ct);
            if (program is null || program.OrganizationId != command.OrganizationId)
                return ApiResult<AppendResult>.Fail("Robot program not found.", 404);
            if (!ScopeAccessRules.CanAccessScopedRow(
                    ScopeRoleSets.ProgramManage, command.UserContext,
                    program.OrganizationId, program.StoreId, program.KioskId))
            {
                return ApiResult<AppendResult>.Fail("Access denied.", 403);
            }

            var ownershipError = await _technicalOwnership.ValidateDefinitionMutationAsync(
                TechnicalResourceKind.RobotProgram, program.Id, ct);
            if (ownershipError is not null)
                return ApiResult<AppendResult>.Fail(ownershipError, 409);

            if (!program.OrganizationId.HasValue)
                return ApiResult<AppendResult>.Fail("Robot program must belong to an organization.", 400);

            var artifacts = await _artifacts.ListArtifactsByIdsAsync(program.OrganizationId.Value, artifactIds, ct);
            if (artifacts.Count != artifactIds.Count)
                return ApiResult<AppendResult>.Fail("One or more uploaded robot artifacts were not found.", 400);
            if (artifacts.Any(artifact => artifact.Status == RobotArtifactStatus.Retired))
                return ApiResult<AppendResult>.Fail("Retired robot artifacts cannot be assigned to a robot program.", 409);

            try
            {
                var alreadyAssigned = program.RobotProgramArtifacts
                    .Select(item => item.RobotArtifactId)
                    .ToHashSet();
                var appendIds = artifactIds.Where(id => !alreadyAssigned.Contains(id)).ToArray();
                var nextRunOrder = program.RobotProgramArtifacts.Select(item => item.RunOrder).DefaultIfEmpty(0).Max();
                foreach (var artifactId in appendIds)
                    program.AddArtifact(artifactId, ++nextRunOrder);

                if (appendIds.Length > 0)
                {
                    program.UpdatedByAccountId = command.UserContext.AccountId;
                    await _programs.SaveChangesAsync(ct);
                }

                return ApiResult<AppendResult>.Success(new AppendResult(
                    await RobotProgramResultMapper.ToResultAsync(_artifacts, program, ct), appendIds));
            }
            catch (DomainRuleException ex)
            {
                return ApiResult<AppendResult>.Fail(ex.Message, 400);
            }
        }, cancellationToken);
    }

    private sealed record AppendResult(RobotProgramResult Program, IReadOnlyCollection<Guid> AppendedArtifactIds);
}
