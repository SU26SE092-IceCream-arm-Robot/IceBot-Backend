using Domain.Common;
using Domain.Devices.Entities;
using Domain.Devices.Enums;
using Domain.Tenants.Entities;

namespace Domain.Devices.ExecutionEndpoints;

public class KioskExecutionEndpoint : BusinessEntity
{
    private readonly List<ExecutionEndpointSupportedRobotTarget> _supportedRobotTargets = [];

    public Guid KioskId { get; private set; }
    public string EndpointCode { get; private set; } = null!;
    public KioskExecutionProfile ExecutionProfile { get; private set; }
    public ExecutionEndpointAuthenticationMode AuthenticationMode { get; private set; }
    public Guid? CredentialBindingId { get; private set; }
    public KioskExecutionEndpointStatus Status { get; private set; } = KioskExecutionEndpointStatus.Provisioning;
    public Guid? FullEdgeRuntimeId { get; private set; }
    public Guid? ControllerId { get; private set; }
    public DateTimeOffset? ProvisionedAt { get; private set; }

    // Full Edge observed active-release snapshot. Deployment history remains in Production Configuration.
    public Guid? ActiveConfigurationDeploymentId { get; private set; }
    public Guid? ActiveConfigurationReleaseId { get; private set; }
    public string? ActiveConfigurationReleaseChecksum { get; private set; }
    public Guid? LastEdgeActivationEventId { get; private set; }
    public DateTimeOffset? ActiveConfigurationEdgeReportedAt { get; private set; }
    public DateTimeOffset? ActiveConfigurationCloudReceivedAt { get; private set; }

    // Low-cost controller observed active-artifact-set snapshot. Controller deployment history remains separate.
    public Guid? ActiveArtifactSetDeploymentId { get; private set; }
    public Guid? ActiveArtifactSetReleaseId { get; private set; }
    public string? ActiveArtifactSetReleaseChecksum { get; private set; }
    public long? ActiveArtifactSetVersion { get; private set; }
    public string? ActiveArtifactSetChecksum { get; private set; }
    public Guid? LastControllerActivationReportId { get; private set; }
    public DateTimeOffset? ActiveArtifactSetControllerReportedAt { get; private set; }
    public DateTimeOffset? ActiveArtifactSetCloudReceivedAt { get; private set; }
    public virtual Kiosk Kiosk { get; private set; } = null!;

    public virtual ExecutionEndpointCredentialBinding? CredentialBinding { get; private set; }
    public virtual ExecutionEndpointMqttCredential? MqttCredential { get; private set; }

    public IReadOnlyCollection<ExecutionEndpointSupportedRobotTarget> SupportedRobotTargets => _supportedRobotTargets;

    private KioskExecutionEndpoint()
    {
    }

    public static KioskExecutionEndpoint CreateProvisioning(
        Guid kioskId,
        string endpointCode,
        KioskExecutionProfile profile,
        ExecutionEndpointAuthenticationMode authenticationMode)
    {
        if (kioskId == Guid.Empty || string.IsNullOrWhiteSpace(endpointCode))
        {
            throw new DomainRuleException("Kiosk and endpoint code are required.");
        }

        if ((profile == KioskExecutionProfile.FullEdge && authenticationMode != ExecutionEndpointAuthenticationMode.MutualTls) ||
            (profile == KioskExecutionProfile.LowCostController && authenticationMode != ExecutionEndpointAuthenticationMode.SignedCommandTls))
        {
            throw new DomainRuleException("Full Edge requires mutual TLS; low-cost controller requires signed-command TLS.");
        }

        return new KioskExecutionEndpoint
        {
            KioskId = kioskId,
            EndpointCode = endpointCode.Trim(),
            ExecutionProfile = profile,
            AuthenticationMode = authenticationMode
        };
    }

    private void AttachCredentialBinding(ExecutionEndpointCredentialBinding credentialBinding)
    {
        if (Status is KioskExecutionEndpointStatus.Active or KioskExecutionEndpointStatus.Retired ||
            credentialBinding is null ||
            credentialBinding.Id == Guid.Empty ||
            credentialBinding.KioskExecutionEndpointId != Id ||
            credentialBinding.AuthenticationMode != AuthenticationMode ||
            credentialBinding.Status == ExecutionEndpointCredentialBindingStatus.Revoked)
        {
            throw new DomainRuleException("Only non-active endpoints can attach a matching non-revoked credential binding.");
        }

        CredentialBinding = credentialBinding;
        CredentialBindingId = credentialBinding.Id;
    }

    private void ActivateCredentialBinding(DateTimeOffset activatedAt)
    {
        if (Status is KioskExecutionEndpointStatus.Active or KioskExecutionEndpointStatus.Retired ||
            CredentialBinding is null ||
            CredentialBinding.Status != ExecutionEndpointCredentialBindingStatus.Provisioned)
        {
            throw new DomainRuleException("Only a provisioned endpoint with a provisioned credential binding can activate that binding.");
        }

        CredentialBinding.Activate(activatedAt);
    }

    private void RevokeCredential(DateTimeOffset revokedAt)
    {
        if (CredentialBinding is null || CredentialBinding.Status == ExecutionEndpointCredentialBindingStatus.Revoked)
        {
            throw new DomainRuleException("The endpoint does not have an active credential binding to revoke.");
        }

        CredentialBinding.Revoke(revokedAt);
        CredentialBinding = null;
        CredentialBindingId = null;

        if (Status == KioskExecutionEndpointStatus.Active)
        {
            Status = KioskExecutionEndpointStatus.Disabled;
        }
    }

    private void AddSupportedRobotTarget(
        string runtimeTargetCode,
        string machineModelCode,
        Device? device = null)
    {
        if (Status is KioskExecutionEndpointStatus.Active or KioskExecutionEndpointStatus.Retired)
        {
            throw new DomainRuleException("Only non-active endpoints can change supported robot targets.");
        }

        var normalizedRuntimeTargetCode = runtimeTargetCode?.Trim();
        var normalizedMachineModelCode = machineModelCode?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedRuntimeTargetCode) ||
            string.IsNullOrWhiteSpace(normalizedMachineModelCode))
        {
            throw new DomainRuleException("Runtime target code and machine model code are required.");
        }

        if (device is not null && device.KioskId != KioskId)
        {
            throw new DomainRuleException("A supported robot target device must belong to the endpoint kiosk.");
        }

        if (_supportedRobotTargets.Any(target =>
                string.Equals(target.RuntimeTargetCode, normalizedRuntimeTargetCode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(target.MachineModelCode, normalizedMachineModelCode, StringComparison.OrdinalIgnoreCase) &&
                target.DeviceId == device?.Id))
        {
            throw new DomainRuleException("The endpoint already supports this robot runtime target and machine model.");
        }

        _supportedRobotTargets.Add(ExecutionEndpointSupportedRobotTarget.Create(
            Id,
            KioskId,
            normalizedRuntimeTargetCode,
            normalizedMachineModelCode,
            device));
    }

    private void ClearSupportedRobotTargets()
    {
        if (Status is KioskExecutionEndpointStatus.Active or KioskExecutionEndpointStatus.Retired)
        {
            throw new DomainRuleException("Only non-active endpoints can change supported robot targets.");
        }

        _supportedRobotTargets.Clear();
    }

    public IReadOnlyCollection<ExecutionEndpointSupportedRobotTarget> ReplaceSupportedRobotTargets(
        IEnumerable<(string RuntimeTargetCode, string MachineModelCode, Device? Device)> replacements)
    {
        if (replacements is null)
        {
            throw new DomainRuleException("Supported robot targets are required.");
        }

        var removed = _supportedRobotTargets.ToArray();
        ClearSupportedRobotTargets();
        foreach (var replacement in replacements)
        {
            AddSupportedRobotTarget(
                replacement.RuntimeTargetCode,
                replacement.MachineModelCode,
                replacement.Device);
        }

        return removed;
    }

    public ExecutionEndpointCredentialBinding ProvisionCredential(
        string credentialReference,
        DateTimeOffset provisionedAt,
        string? publicKeyPem = null)
    {
        if (Status != KioskExecutionEndpointStatus.Provisioning || CredentialBinding is not null)
        {
            throw new DomainRuleException("Only an unbound provisioning endpoint can provision credentials.");
        }

        var credential = ExecutionEndpointCredentialBinding.CreateProvisioned(
            Id,
            AuthenticationMode,
            credentialReference,
            provisionedAt,
            publicKeyPem);
        AttachCredentialBinding(credential);
        ActivateCredentialBinding(provisionedAt);
        return credential;
    }

    public ExecutionEndpointCredentialBinding RotateCredential(
        string credentialReference,
        DateTimeOffset rotatedAt,
        string? publicKeyPem = null)
    {
        if (Status is not (KioskExecutionEndpointStatus.Active or KioskExecutionEndpointStatus.Disabled))
        {
            throw new DomainRuleException("Only active or disabled execution endpoints can rotate credentials.");
        }

        var wasActive = Status == KioskExecutionEndpointStatus.Active;
        if (CredentialBinding is not null)
        {
            RevokeCredential(rotatedAt);
        }

        var credential = ExecutionEndpointCredentialBinding.CreateProvisioned(
            Id,
            AuthenticationMode,
            credentialReference,
            rotatedAt,
            publicKeyPem);
        AttachCredentialBinding(credential);
        ActivateCredentialBinding(rotatedAt);
        if (wasActive)
        {
            ReactivateWithCurrentCredential(rotatedAt);
        }

        return credential;
    }

    public bool SupportsRobotTarget(string runtimeTargetCode, string machineModelCode, Guid? deviceId = null)
    {
        return _supportedRobotTargets.Any(target =>
            string.Equals(target.RuntimeTargetCode, runtimeTargetCode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(target.MachineModelCode, machineModelCode, StringComparison.OrdinalIgnoreCase) &&
            (!deviceId.HasValue || target.DeviceId == deviceId));
    }

    public void Activate(Guid profileIdentity, DateTimeOffset provisionedAt)
    {
        if (Status != KioskExecutionEndpointStatus.Provisioning ||
            profileIdentity == Guid.Empty ||
            CredentialBindingId is null ||
            CredentialBinding?.Status != ExecutionEndpointCredentialBindingStatus.Active)
        {
            throw new DomainRuleException("Only a provisioned endpoint with a profile identity and active credential binding can be activated.");
        }

        if (ExecutionProfile == KioskExecutionProfile.FullEdge)
        {
            FullEdgeRuntimeId = profileIdentity;
            ControllerId = null;
        }
        else
        {
            ControllerId = profileIdentity;
            FullEdgeRuntimeId = null;
        }

        ProvisionedAt = provisionedAt;
        Status = KioskExecutionEndpointStatus.Active;
    }

    public void ReactivateWithCurrentCredential(DateTimeOffset reactivatedAt)
    {
        if (Status != KioskExecutionEndpointStatus.Disabled ||
            CredentialBindingId is null ||
            CredentialBinding?.Status != ExecutionEndpointCredentialBindingStatus.Active ||
            (ExecutionProfile == KioskExecutionProfile.FullEdge && FullEdgeRuntimeId is null) ||
            (ExecutionProfile == KioskExecutionProfile.LowCostController && ControllerId is null))
        {
            throw new DomainRuleException("Only a disabled endpoint with an active credential and existing profile identity can be reactivated.");
        }

        ProvisionedAt = reactivatedAt;
        Status = KioskExecutionEndpointStatus.Active;
    }

    public void Disable()
    {
        if (Status != KioskExecutionEndpointStatus.Active)
        {
            throw new DomainRuleException("Only an active endpoint can be disabled.");
        }

        Status = KioskExecutionEndpointStatus.Disabled;
    }

    public bool ApplyFullEdgeObservedActivation(
        Guid deploymentId,
        Guid configurationReleaseId,
        string releaseChecksum,
        Guid edgeActivationEventId,
        DateTimeOffset edgeReportedAt,
        DateTimeOffset cloudReceivedAt)
    {
        if (ExecutionProfile != KioskExecutionProfile.FullEdge || Status != KioskExecutionEndpointStatus.Active ||
            FullEdgeRuntimeId is null || deploymentId == Guid.Empty || configurationReleaseId == Guid.Empty ||
            edgeActivationEventId == Guid.Empty || string.IsNullOrWhiteSpace(releaseChecksum))
        {
            throw new DomainRuleException("An active Full Edge endpoint and complete activation provenance are required.");
        }

        if (LastEdgeActivationEventId == edgeActivationEventId)
        {
            return false;
        }

        ActiveConfigurationDeploymentId = deploymentId;
        ActiveConfigurationReleaseId = configurationReleaseId;
        ActiveConfigurationReleaseChecksum = releaseChecksum.Trim();
        LastEdgeActivationEventId = edgeActivationEventId;
        ActiveConfigurationEdgeReportedAt = edgeReportedAt;
        ActiveConfigurationCloudReceivedAt = cloudReceivedAt;
        return true;
    }

    public bool ApplyLowCostObservedActivation(
        Guid deploymentId,
        Guid configurationReleaseId,
        string releaseChecksum,
        long activeSetVersion,
        string activeSetChecksum,
        Guid controllerReportId,
        DateTimeOffset controllerReportedAt,
        DateTimeOffset cloudReceivedAt)
    {
        if (ExecutionProfile != KioskExecutionProfile.LowCostController || Status != KioskExecutionEndpointStatus.Active ||
            ControllerId is null || deploymentId == Guid.Empty || configurationReleaseId == Guid.Empty ||
            activeSetVersion <= 0 || controllerReportId == Guid.Empty || string.IsNullOrWhiteSpace(releaseChecksum) ||
            string.IsNullOrWhiteSpace(activeSetChecksum))
        {
            throw new DomainRuleException("An active low-cost endpoint and complete artifact-set activation provenance are required.");
        }

        if (LastControllerActivationReportId == controllerReportId)
        {
            return false;
        }

        ActiveArtifactSetDeploymentId = deploymentId;
        ActiveArtifactSetReleaseId = configurationReleaseId;
        ActiveArtifactSetReleaseChecksum = releaseChecksum.Trim();
        ActiveArtifactSetVersion = activeSetVersion;
        ActiveArtifactSetChecksum = activeSetChecksum.Trim();
        LastControllerActivationReportId = controllerReportId;
        ActiveArtifactSetControllerReportedAt = controllerReportedAt;
        ActiveArtifactSetCloudReceivedAt = cloudReceivedAt;
        return true;
    }

    public void Retire()
    {
        if (Status == KioskExecutionEndpointStatus.Active)
        {
            throw new DomainRuleException("Disable an endpoint before retiring it.");
        }

        Status = KioskExecutionEndpointStatus.Retired;
    }
}
