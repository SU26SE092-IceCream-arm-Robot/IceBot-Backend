using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Application.EdgeIntegration.Dispatch.Contracts;
using Application.EdgeIntegration.Reports.Contracts;
using Domain.Common;
using Domain.Sync.Entities;

namespace Application.EdgeIntegration.Dispatch.Services;

internal static class ExecuteOrderReleaseValidator
{
    public static void Validate(IngestExecutionReportCommand command, EdgeCommand edgeCommand)
    {
        if (!command.SourceConfigurationReleaseId.HasValue || command.SourceConfigurationReleaseId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.ReleaseChecksum))
            throw new DomainRuleException("Production execution reports require source configuration release and checksum.");

        var provenance = ExecuteOrderCommandPayloadCodec.ReadProvenance(edgeCommand.PayloadJson);
        if (command.SourceConfigurationReleaseId.Value != provenance.ConfigurationReleaseId ||
            !string.Equals(command.ReleaseChecksum.Trim(), provenance.ReleaseChecksum, StringComparison.Ordinal))
            throw new DomainRuleException("Production execution report release does not match the dispatched command.");
    }
}
