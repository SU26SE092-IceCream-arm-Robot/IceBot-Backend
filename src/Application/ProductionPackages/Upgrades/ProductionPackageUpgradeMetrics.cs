using System.Diagnostics.Metrics;

namespace Application.ProductionPackages.Upgrades;

public static class ProductionPackageUpgradeMetrics
{
    public const string MeterName = "IceBot.ProductionPackages";
    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> PreviewOutcomes = Meter.CreateCounter<long>(
        "icebot.production_package.upgrade.preview");
    private static readonly Counter<long> MaterializationOutcomes = Meter.CreateCounter<long>(
        "icebot.production_package.upgrade.materialization");
    private static readonly Counter<long> CutoverOutcomes = Meter.CreateCounter<long>(
        "icebot.production_package.upgrade.cutover");
    private static readonly Counter<long> RollbackAttempts = Meter.CreateCounter<long>(
        "icebot.production_package.upgrade.rollback_attempt");
    private static readonly Counter<long> RollbackOutcomes = Meter.CreateCounter<long>(
        "icebot.production_package.upgrade.rollback");
    private static readonly Counter<long> AbandonOutcomes = Meter.CreateCounter<long>(
        "icebot.production_package.upgrade.abandon");
    private static readonly Counter<long> ReconciliationOutcomes = Meter.CreateCounter<long>(
        "icebot.production_package.upgrade.reconciliation");
    private static readonly Histogram<double> PendingAge = Meter.CreateHistogram<double>(
        "icebot.production_package.upgrade.rollback_pending_age", "s");

    public static void RecordPreview(string outcome, int blockerCount) => PreviewOutcomes.Add(1,
        new KeyValuePair<string, object?>("outcome", outcome),
        new KeyValuePair<string, object?>("has_blockers", blockerCount > 0));

    public static void RecordMaterialization(string outcome) => MaterializationOutcomes.Add(1,
        new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordCutover(string outcome) => CutoverOutcomes.Add(1,
        new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordRollbackAttempt(string profile, int attemptNo) => RollbackAttempts.Add(1,
        new KeyValuePair<string, object?>("profile", profile),
        new KeyValuePair<string, object?>("attempt_no", attemptNo));

    public static void RecordRollback(string outcome) => RollbackOutcomes.Add(1,
        new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordAbandon(string outcome) => AbandonOutcomes.Add(1,
        new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordReconciliation(int failedCount) => ReconciliationOutcomes.Add(1,
        new KeyValuePair<string, object?>("outcome", failedCount > 0 ? "failed_stale" : "no_change"));

    public static void RecordPendingAge(DateTimeOffset startedAt, DateTimeOffset observedAt, string stage) =>
        PendingAge.Record(Math.Max(0, (observedAt - startedAt).TotalSeconds),
            new KeyValuePair<string, object?>("stage", stage));
}
