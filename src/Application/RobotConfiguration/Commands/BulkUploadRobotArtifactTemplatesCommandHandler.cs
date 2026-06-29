using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;

namespace Application.RobotConfiguration.Commands;

public sealed class BulkUploadRobotArtifactTemplatesCommandHandler
{
    private readonly UploadRobotArtifactTemplateCommandHandler _itemHandler;

    public BulkUploadRobotArtifactTemplatesCommandHandler(UploadRobotArtifactTemplateCommandHandler itemHandler)
    {
        _itemHandler = itemHandler;
    }

    public async Task<ApiResult<BulkRobotArtifactTemplateUploadResult>> HandleAsync(
        BulkUploadRobotArtifactTemplatesCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Items.Count is < 1 or > 50)
        {
            return ApiResult<BulkRobotArtifactTemplateUploadResult>.Fail(
                "Bulk template upload requires 1 to 50 files.",
                400);
        }

        var items = new List<BulkRobotArtifactTemplateUploadItemResult>(command.Items.Count);
        foreach (var item in command.Items)
        {
            var result = await _itemHandler.HandleAsync(item, cancellationToken);
            items.Add(new BulkRobotArtifactTemplateUploadItemResult
            {
                FileName = item.FileName,
                Succeeded = result.Succeeded,
                WasExisting = result.Succeeded && result.StatusCode == 200,
                StatusCode = result.StatusCode,
                Message = result.Message ?? "Template upload failed.",
                Template = result.Data
            });
        }

        var response = new BulkRobotArtifactTemplateUploadResult
        {
            UploadedCount = items.Count(item => item.Succeeded && !item.WasExisting),
            ExistingCount = items.Count(item => item.WasExisting),
            FailedCount = items.Count(item => !item.Succeeded),
            Items = items
        };

        if (response.FailedCount == 0)
        {
            return ApiResult<BulkRobotArtifactTemplateUploadResult>.Success(
                response,
                "Template upload completed.",
                response.ExistingCount == 0 ? 201 : 200);
        }

        if (response.FailedCount == items.Count)
        {
            return new ApiResult<BulkRobotArtifactTemplateUploadResult>
            {
                Succeeded = false,
                StatusCode = 400,
                Message = "All template uploads failed.",
                Data = response
            };
        }

        return ApiResult<BulkRobotArtifactTemplateUploadResult>.Success(
            response,
            "Template upload completed with partial failures.",
            207);
    }
}
