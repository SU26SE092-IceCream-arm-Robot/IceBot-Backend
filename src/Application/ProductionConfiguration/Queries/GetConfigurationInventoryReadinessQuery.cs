using Application.Identity.Tokens.Claims;

namespace Application.ProductionConfiguration.Queries;

public sealed record GetConfigurationInventoryReadinessQuery(
    Guid KioskId,
    Guid ConfigurationReleaseId,
    CurrentUserContext UserContext);
