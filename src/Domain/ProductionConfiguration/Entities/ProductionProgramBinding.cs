using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.Common;

namespace Domain.ProductionConfiguration.Entities;

public enum ProductionProgramBindingStatus
{
    Active = 0,
    Retired = 1
}

public enum ProductionProgramBindingCapabilityEvidenceStatus
{
    Declared = 0,
    Missing = 1
}

public enum ProductionProgramBindingAssurance
{
    OperatorDeclared = 0
}

// Immutable operator-confirmed binding. Capability codes are declarations, not proof of Lua behavior.
public sealed class ProductionProgramBinding : BusinessEntity
{
    public Guid OrganizationId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public Guid RecipeId { get; private set; }
    public int RecipeVersion { get; private set; }
    public Guid RobotProgramId { get; private set; }
    public string ProgramManifestChecksum { get; private set; } = null!;
    public string RequiredCapabilityCodesJson { get; private set; } = "[]";
    public ProductionProgramBindingCapabilityEvidenceStatus CapabilityEvidenceStatus { get; private set; }
    public ProductionProgramBindingAssurance Assurance { get; private set; }
    public string SupportedOptionCodesJson { get; private set; } = "[]";
    public string BindingChecksum { get; private set; } = null!;
    public ProductionProgramBindingStatus Status { get; private set; }
    public DateTimeOffset? RetiredAt { get; private set; }

    private ProductionProgramBinding() { }

    public static ProductionProgramBinding Create(
        Guid organizationId,
        Guid productVariantId,
        Guid recipeId,
        int recipeVersion,
        Guid robotProgramId,
        string programManifestChecksum,
        IReadOnlyCollection<string> requiredCapabilityCodes,
        ProductionProgramBindingCapabilityEvidenceStatus capabilityEvidenceStatus,
        ProductionProgramBindingAssurance assurance,
        IReadOnlyCollection<string> supportedOptionCodes,
        Guid actorId)
    {
        if (organizationId == Guid.Empty || productVariantId == Guid.Empty || recipeId == Guid.Empty || robotProgramId == Guid.Empty)
            throw new DomainRuleException("Production binding organization, variant, recipe, and robot program are required.");
        if (recipeVersion <= 0)
            throw new DomainRuleException("Production binding recipe version must be positive.");

        var normalizedOptions = NormalizeCodes(supportedOptionCodes);
        var normalizedManifestChecksum = RequireChecksum(programManifestChecksum, "Program manifest checksum");
        var normalizedCapabilityCodes = NormalizeOptionalCodes(requiredCapabilityCodes, "Required capability code");
        var binding = new ProductionProgramBinding
        {
            OrganizationId = organizationId,
            ProductVariantId = productVariantId,
            RecipeId = recipeId,
            RecipeVersion = recipeVersion,
            RobotProgramId = robotProgramId,
            ProgramManifestChecksum = normalizedManifestChecksum,
            RequiredCapabilityCodesJson = JsonSerializer.Serialize(normalizedCapabilityCodes),
            CapabilityEvidenceStatus = capabilityEvidenceStatus,
            Assurance = assurance,
            SupportedOptionCodesJson = JsonSerializer.Serialize(normalizedOptions),
            Status = ProductionProgramBindingStatus.Active,
            CreatedByAccountId = actorId
        };
        binding.BindingChecksum = CreateChecksum(binding);
        return binding;
    }

    public IReadOnlyCollection<string> GetSupportedOptionCodes() =>
        JsonSerializer.Deserialize<string[]>(SupportedOptionCodesJson) ?? [];

    public IReadOnlyCollection<string> GetRequiredCapabilityCodes() =>
        JsonSerializer.Deserialize<string[]>(RequiredCapabilityCodesJson) ?? [];

    public void Retire(DateTimeOffset now, Guid actorId)
    {
        if (Status == ProductionProgramBindingStatus.Retired) return;
        Status = ProductionProgramBindingStatus.Retired;
        RetiredAt = now;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    private static string[] NormalizeCodes(IReadOnlyCollection<string> codes)
    {
        var normalized = codes.Select(code => NormalizeCode(code, "Supported option code"))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (normalized.Length != codes.Count)
            throw new DomainRuleException("Production binding supported option codes must be non-empty and unique.");
        return normalized;
    }

    private static string[] NormalizeOptionalCodes(IReadOnlyCollection<string> codes, string name) =>
        codes.Select(code => NormalizeCode(code, name))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string CreateChecksum(ProductionProgramBinding binding)
    {
        var payload = string.Join("|", binding.OrganizationId, binding.ProductVariantId, binding.RecipeId,
            binding.RecipeVersion, binding.RobotProgramId, binding.ProgramManifestChecksum,
            binding.RequiredCapabilityCodesJson, binding.CapabilityEvidenceStatus, binding.Assurance,
            binding.SupportedOptionCodesJson);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static string RequireChecksum(string value, string name)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
            throw new DomainRuleException($"{name} must be a SHA-256 checksum.");
        return normalized;
    }

    private static string NormalizeCode(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainRuleException($"{name} is required.");
        return value.Trim().ToUpperInvariant();
    }
}
