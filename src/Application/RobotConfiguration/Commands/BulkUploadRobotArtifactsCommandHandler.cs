using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;

namespace Application.RobotConfiguration.Commands;

public sealed class BulkUploadRobotArtifactsCommandHandler
{
    private const int MaximumItemCount = 50;
    private readonly UploadRobotArtifactCommandHandler _singleUploadHandler;

    public BulkUploadRobotArtifactsCommandHandler(UploadRobotArtifactCommandHandler singleUploadHandler)
    {
        _singleUploadHandler = singleUploadHandler;
    }

    public async Task<ApiResult<BulkRobotArtifactUploadResult>> HandleAsync(
        BulkUploadRobotArtifactsCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRequest(command.Items);
        if (validationError is not null)
        {
            return ApiResult<BulkRobotArtifactUploadResult>.Fail(validationError, 400);
        }

        var results = new List<BulkRobotArtifactUploadItemResult>(command.Items.Count);
        foreach (var item in command.Items)
        {
            var result = await _singleUploadHandler.HandleAsync(
                new UploadRobotArtifactCommand
                {
                    UserContext = command.UserContext,
                    OrganizationId = command.OrganizationId,
                    ArtifactCode = item.ArtifactCode,
                    ArtifactName = item.ArtifactName,
                    FileName = item.FileName,
                    RuntimeTargetCode = item.RuntimeTargetCode,
                    MachineModelCode = item.MachineModelCode,
                    ContentType = item.ContentType,
                    ContentLengthBytes = item.ContentLengthBytes,
                    Content = item.Content,
                    ExportedAt = item.ExportedAt,
                    Description = item.Description,
                    MetadataJson = item.MetadataJson
                },
                cancellationToken);

            results.Add(new BulkRobotArtifactUploadItemResult
            {
                FileName = item.FileName,
                Succeeded = result.Succeeded,
                StatusCode = result.StatusCode,
                Message = result.Message,
                RobotArtifactId = result.Data?.Id,
                Artifact = result.Data
            });
        }

        var succeededCount = results.Count(item => item.Succeeded);
        var response = new BulkRobotArtifactUploadResult
        {
            TotalCount = results.Count,
            SucceededCount = succeededCount,
            FailedCount = results.Count - succeededCount,
            Items = results
        };

        if (succeededCount == results.Count)
        {
            return ApiResult<BulkRobotArtifactUploadResult>.Success(
                response, "All robot artifacts uploaded successfully.", 201);
        }

        if (succeededCount == 0)
        {
            return new ApiResult<BulkRobotArtifactUploadResult>
            {
                Succeeded = false,
                StatusCode = 400,
                Message = "All robot artifact uploads failed.",
                Data = response
            };
        }

        return ApiResult<BulkRobotArtifactUploadResult>.Success(
            response, "Robot artifact bulk upload completed with partial failures.", 207);
    }

    private static string? ValidateRequest(IReadOnlyCollection<BulkUploadRobotArtifactItem> items)
    {
        if (items.Count == 0)
        {
            return "At least one robot artifact file is required.";
        }

        if (items.Count > MaximumItemCount)
        {
            return $"A maximum of {MaximumItemCount} robot artifact files is allowed per request.";
        }

        if (items.GroupBy(item => item.FileName, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            return "Robot artifact file names must be unique within the bulk request.";
        }

        return null;
    }
}
