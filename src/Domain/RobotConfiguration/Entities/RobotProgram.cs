using Domain.Common;
using Domain.Devices.Entities;
using Domain.RobotConfiguration.Manifests;
using Domain.RobotConfiguration.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;

namespace Domain.RobotConfiguration.Entities;

public partial class RobotProgram : RobotConfigurationEntity, IKioskScoped
{
    private readonly List<RobotProgramArtifact> _robotProgramArtifacts = [];

    public Guid? OrganizationId { get; private set; }

    public Guid? StoreId { get; private set; }

    public Guid? KioskId { get; private set; }

    public Guid? DeviceId { get; private set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public TenantScopeType ScopeType { get; private set; } = TenantScopeType.Global;

    public RobotProgramStatus Status { get; private set; } = RobotProgramStatus.Draft;

    public int ProgramManifestSchemaVersion { get; private set; } = 1;

    public string? ProgramManifestJson { get; private set; }

    public string? ProgramManifestChecksum { get; private set; }

    public string? Description { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset? RetiredAt { get; private set; }

    public virtual Organization? Organization { get; private set; }

    public virtual Store? Store { get; private set; }

    public virtual Kiosk? Kiosk { get; private set; }

    public virtual Device? Device { get; private set; }

    public IReadOnlyCollection<RobotProgramArtifact> RobotProgramArtifacts => _robotProgramArtifacts;

    Guid? IOrganizationScoped.OrganizationId
    {
        get => OrganizationId;
        set => throw new InvalidOperationException("Robot program scope is immutable after draft creation.");
    }

    Guid? IStoreScoped.StoreId
    {
        get => StoreId;
        set => throw new InvalidOperationException("Robot program scope is immutable after draft creation.");
    }

    Guid? IKioskScoped.KioskId
    {
        get => KioskId;
        set => throw new InvalidOperationException("Robot program scope is immutable after draft creation.");
    }

    private RobotProgram()
    {
    }

    public static RobotProgram CreateDraft(
        string code,
        string name,
        TenantScopeType scopeType,
        Guid? organizationId = null,
        Guid? storeId = null,
        Guid? kioskId = null,
        Guid? deviceId = null,
        string? description = null)
    {
        ValidateScope(scopeType, organizationId, storeId, kioskId, deviceId);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Robot program code and name are required.");
        }

        return new RobotProgram
        {
            Code = code.Trim(),
            Name = name.Trim(),
            ScopeType = scopeType,
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            DeviceId = deviceId,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };
    }

    public RobotProgramArtifact AddArtifact(
        Guid robotArtifactId,
        int runOrder,
        string? parametersJson = null,
        int parametersSchemaVersion = 1)
    {
        EnsureDraft();

        if (_robotProgramArtifacts.Any(artifact => artifact.RunOrder == runOrder))
        {
            throw new DomainRuleException("A robot program artifact with the same run order already exists.");
        }

        var artifact = RobotProgramArtifact.Create(robotArtifactId, runOrder, parametersJson, parametersSchemaVersion);
        _robotProgramArtifacts.Add(artifact);
        return artifact;
    }

    public void ClearArtifacts()
    {
        EnsureDraft();
        _robotProgramArtifacts.Clear();
    }

    public void UpdateDraftDetails(string code, string name, string? description)
    {
        EnsureDraft();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Robot program code and name are required.");
        }

        Code = code.Trim();
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void RemoveArtifact(Guid robotProgramArtifactId)
    {
        EnsureDraft();

        var artifact = _robotProgramArtifacts.SingleOrDefault(item => item.Id == robotProgramArtifactId);
        if (artifact == null)
        {
            throw new DomainRuleException("Robot program artifact was not found.");
        }

        _robotProgramArtifacts.Remove(artifact);
    }

    public void Publish(DateTimeOffset publishedAt, int programManifestSchemaVersion = 1)
    {
        EnsureDraft();

        if (!_robotProgramArtifacts.Any())
        {
            throw new DomainRuleException("Cannot publish a robot program without artifacts.");
        }

        if (programManifestSchemaVersion <= 0)
        {
            throw new DomainRuleException("Robot program manifest schema version must be greater than zero.");
        }

        var manifest = RobotProgramManifestBuilder.Create(this, programManifestSchemaVersion);
        ProgramManifestSchemaVersion = programManifestSchemaVersion;
        ProgramManifestJson = manifest.Json;
        ProgramManifestChecksum = manifest.Checksum;

        Status = RobotProgramStatus.Published;
        PublishedAt = publishedAt;
    }

    public void Retire(DateTimeOffset retiredAt)
    {
        if (Status == RobotProgramStatus.Retired)
        {
            return;
        }

        if (Status == RobotProgramStatus.Draft)
        {
            throw new DomainRuleException("Draft robot programs should be deleted or disabled, not retired.");
        }

        Status = RobotProgramStatus.Retired;
        RetiredAt = retiredAt;
    }

    private void EnsureDraft()
    {
        if (Status != RobotProgramStatus.Draft)
        {
            throw new DomainRuleException("Only draft robot programs can be modified.");
        }
    }

    private static void ValidateScope(
        TenantScopeType scopeType,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? deviceId)
    {
        var valid = scopeType switch
        {
            TenantScopeType.Global => organizationId is null && storeId is null && kioskId is null && deviceId is null,
            TenantScopeType.Organization => organizationId.HasValue && storeId is null && kioskId is null && deviceId is null,
            TenantScopeType.Store => organizationId.HasValue && storeId.HasValue && kioskId is null && deviceId is null,
            TenantScopeType.Kiosk => organizationId.HasValue && storeId.HasValue && kioskId.HasValue && deviceId is null,
            TenantScopeType.Device => organizationId.HasValue && storeId.HasValue && kioskId.HasValue && deviceId.HasValue,
            _ => false
        };

        if (!valid)
        {
            throw new DomainRuleException("Robot program scope ids do not match the selected scope type.");
        }
    }
}
