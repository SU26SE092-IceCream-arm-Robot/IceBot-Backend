using Application.Identity.Tokens.Claims;

namespace Application.ProductionConfiguration.Releases.Queries;

public sealed class GetConfigurationReleaseAuthoringOptionsQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid? ProductVariantId { get; init; }
    public string? Search { get; init; }
    public bool IncludeGlobalTemplates { get; init; }
    public int Limit { get; init; } = 50;
}
