namespace Application.ProductionConfiguration.Releases.ReadModels;

public sealed class ConfigurationReleaseAuthoringOptionsReadModel
{
    public IReadOnlyList<ConfigurationAuthoringProductVariantOption> ProductVariants { get; init; } = [];
    public IReadOnlyList<ConfigurationAuthoringRecipeOption> Recipes { get; init; } = [];
    public IReadOnlyList<ConfigurationAuthoringRobotProgramOption> RobotPrograms { get; init; } = [];
    public IReadOnlyList<ConfigurationAuthoringWorkcellCapabilityOption> WorkcellCapabilities { get; init; } = [];
}

public sealed class ConfigurationAuthoringProductVariantOption
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductCode { get; init; } = null!;
    public string ProductName { get; init; } = null!;
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string FulfillmentType { get; init; } = null!;
    public bool IsAvailable { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
}

public sealed class ConfigurationAuthoringRecipeOption
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductCode { get; init; } = null!;
    public string ProductName { get; init; } = null!;
    public Guid ProductVariantId { get; init; }
    public string ProductVariantCode { get; init; } = null!;
    public string ProductVariantName { get; init; } = null!;
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public int Version { get; init; }
    public string Status { get; init; } = null!;
    public bool IsDefault { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public IReadOnlyList<ConfigurationAuthoringProductionOption> ProductionOptionCandidates { get; init; } = [];
}

public sealed class ConfigurationAuthoringProductionOption
{
    public Guid Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string OptionGroupCode { get; init; } = null!;
    public string OptionGroupName { get; init; } = null!;
    public bool GroupIsRequired { get; init; }
    public bool IsAvailable { get; init; }
}

public sealed class ConfigurationAuthoringRobotProgramOption
{
    public Guid Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string ScopeType { get; init; } = null!;
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public Guid? DeviceId { get; init; }
    public string ProgramManifestChecksum { get; init; } = null!;
    public int ArtifactCount { get; init; }
    public IReadOnlyList<string> WorkcellCapabilityCodes { get; init; } = [];
}

public sealed class ConfigurationAuthoringWorkcellCapabilityOption
{
    public string Code { get; init; } = null!;
    public IReadOnlyList<Guid> RobotProgramIds { get; init; } = [];
}
