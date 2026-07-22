using Domain.RobotConfiguration.AuthoringImports;
using System.Linq.Expressions;

namespace Application.RobotConfiguration.AuthoringImports.Rules;

public static class RobotAuthoringImportStagingRetentionPolicy
{
    public static Expression<Func<RobotAuthoringImport, bool>> BuildPredicate(
        DateTimeOffset appliedRetentionThreshold) => importSession =>
        importSession.Status == RobotAuthoringImportStatus.Uploaded ||
        importSession.Status == RobotAuthoringImportStatus.Validated ||
        importSession.Status == RobotAuthoringImportStatus.Failed ||
        importSession.Status == RobotAuthoringImportStatus.Applied &&
        importSession.AppliedAt.HasValue &&
        importSession.AppliedAt.Value >= appliedRetentionThreshold;

    public static bool ShouldRetain(
        RobotAuthoringImportStatus status,
        DateTimeOffset? appliedAt,
        DateTimeOffset appliedRetentionThreshold) =>
        status is RobotAuthoringImportStatus.Uploaded or
            RobotAuthoringImportStatus.Validated or
            RobotAuthoringImportStatus.Failed ||
        status == RobotAuthoringImportStatus.Applied &&
        appliedAt.HasValue && appliedAt.Value >= appliedRetentionThreshold;
}
