using System.ComponentModel.DataAnnotations;
using Application.ProductionPackages.Installation;
using Application.ProductionPackages.Workspace;
using Application.ProductionPackages.Upgrades;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.ProductionPackages;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/organizations/{organizationId:guid}/production-package-installations")]
public sealed class ManagementProductionPackageInstallationsController(
    ProductionPackageInstallationService service,
    ProductionPackageWorkspaceService workspaceService,
    ProductionPackageUpgradeService upgradeService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "package.read")]
    public async Task<IActionResult> List(Guid organizationId, [FromQuery] string? status,
        [FromQuery] Guid? storeId, [FromQuery] Guid? kioskId, [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await service.ListAsync(User.GetUserContext(), organizationId, status, storeId, kioskId,
            pageNumber, pageSize, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("preview")]
    [Authorize(Policy = "package.read")]
    public async Task<IActionResult> Preview(Guid organizationId,
        [FromBody] PreviewProductionPackageRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PreviewAsync(User.GetUserContext(), organizationId, request.PackageId,
            request.PackageVersionId, request.StoreId, request.KioskId, request.ProductSourceKeys, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Policy = "package.install")]
    public async Task<IActionResult> Install(Guid organizationId,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        [FromBody] InstallProductionPackageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.InstallAsync(new InstallProductionPackageCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            StoreId = request.StoreId,
            KioskId = request.KioskId,
            PackageId = request.PackageId,
            PackageVersionId = request.PackageVersionId,
            ProductSourceKeys = request.ProductSourceKeys,
            IdempotencyKey = idempotencyKey
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{installationId:guid}")]
    [Authorize(Policy = "package.read")]
    public async Task<IActionResult> Get(Guid organizationId, Guid installationId, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(User.GetUserContext(), organizationId, installationId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{installationId:guid}/workspace")]
    [Authorize(Policy = "package.read")]
    public async Task<IActionResult> Workspace(Guid organizationId, Guid installationId,
        CancellationToken cancellationToken)
    {
        var result = await workspaceService.GetAsync(User.GetUserContext(), organizationId, installationId,
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{installationId:guid}/fork")]
    [Authorize(Policy = "package.fork")]
    public async Task<IActionResult> Fork(Guid organizationId, Guid installationId, CancellationToken cancellationToken)
    {
        var result = await service.ForkAsync(User.GetUserContext(), organizationId, installationId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{installationId:guid}/retry")]
    [Authorize(Policy = "package.install")]
    public async Task<IActionResult> Retry(Guid organizationId, Guid installationId,
        CancellationToken cancellationToken)
    {
        var result = await service.RetryAsync(User.GetUserContext(), organizationId, installationId,
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{installationId:guid}/repair")]
    [Authorize(Policy = "package.install")]
    public async Task<IActionResult> Repair(Guid organizationId, Guid installationId,
        CancellationToken cancellationToken)
    {
        var result = await service.RepairAsync(User.GetUserContext(), organizationId, installationId,
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{installationId:guid}/upgrades/preview")]
    [Authorize(Policy = "package.read")]
    public async Task<IActionResult> PreviewUpgrade(Guid organizationId, Guid installationId,
        [FromBody] PreviewProductionPackageUpgradeRequest request, CancellationToken cancellationToken)
    {
        var result = await upgradeService.PreviewAsync(User.GetUserContext(), organizationId, installationId,
            request.TargetPackageVersionId, request.ProductSourceKeys, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{installationId:guid}/upgrades")]
    [Authorize(Policy = "package.install")]
    public async Task<IActionResult> Upgrade(Guid organizationId, Guid installationId,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        [FromBody] ExecuteProductionPackageUpgradeRequest request, CancellationToken cancellationToken)
    {
        var result = await upgradeService.ExecuteAsync(new ExecuteProductionPackageUpgradeCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            SourceInstallationId = installationId,
            TargetPackageVersionId = request.TargetPackageVersionId,
            PreviewChecksum = request.PreviewChecksum,
            IdempotencyKey = idempotencyKey,
            ProductSourceKeys = request.ProductSourceKeys
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{installationId:guid}/upgrades")]
    [Authorize(Policy = "package.read")]
    public async Task<IActionResult> ListUpgrades(Guid organizationId, Guid installationId,
        [FromQuery] string? status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await upgradeService.ListAsync(User.GetUserContext(), organizationId, installationId,
            status, pageNumber, pageSize, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{installationId:guid}/upgrades/{upgradeId:guid}")]
    [Authorize(Policy = "package.read")]
    public async Task<IActionResult> GetUpgrade(Guid organizationId, Guid installationId, Guid upgradeId,
        CancellationToken cancellationToken)
    {
        var result = await upgradeService.GetAsync(User.GetUserContext(), organizationId, installationId,
            upgradeId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{installationId:guid}/upgrades/{upgradeId:guid}/cutover")]
    [Authorize(Policy = "package.install")]
    public async Task<IActionResult> CutoverUpgrade(Guid organizationId, Guid installationId, Guid upgradeId,
        CancellationToken cancellationToken)
    {
        var result = await upgradeService.CutoverAsync(User.GetUserContext(), organizationId, installationId,
            upgradeId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{installationId:guid}/upgrades/{upgradeId:guid}/abandon")]
    [Authorize(Policy = "package.install")]
    public async Task<IActionResult> AbandonUpgrade(Guid organizationId, Guid installationId, Guid upgradeId,
        [FromBody] AbandonProductionPackageUpgradeRequest request, CancellationToken cancellationToken)
    {
        var result = await upgradeService.AbandonAsync(User.GetUserContext(), organizationId, installationId,
            upgradeId, request.Reason, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{installationId:guid}/upgrades/{upgradeId:guid}/rollback")]
    [Authorize(Policy = "release.rollback")]
    public async Task<IActionResult> RollbackUpgrade(Guid organizationId, Guid installationId, Guid upgradeId,
        [FromBody] RollbackProductionPackageUpgradeRequest request, CancellationToken cancellationToken)
    {
        var result = await upgradeService.RollbackAsync(User.GetUserContext(), organizationId, installationId,
            upgradeId, request.CommandExpiryAt, request.Reason, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class PreviewProductionPackageRequest
{
    public Guid PackageId { get; init; }
    public Guid PackageVersionId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    [MaxLength(100)] public IReadOnlyCollection<string> ProductSourceKeys { get; init; } = [];
}

public sealed class InstallProductionPackageRequest
{
    public Guid PackageId { get; init; }
    public Guid PackageVersionId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    [MaxLength(100)] public IReadOnlyCollection<string> ProductSourceKeys { get; init; } = [];
}

public sealed class PreviewProductionPackageUpgradeRequest
{
    public Guid TargetPackageVersionId { get; init; }
    [MaxLength(100)] public IReadOnlyCollection<string> ProductSourceKeys { get; init; } = [];
}

public sealed class ExecuteProductionPackageUpgradeRequest
{
    public Guid TargetPackageVersionId { get; init; }
    [Required, StringLength(64, MinimumLength = 64)] public string PreviewChecksum { get; init; } = null!;
    [MaxLength(100)] public IReadOnlyCollection<string> ProductSourceKeys { get; init; } = [];
}

public sealed class RollbackProductionPackageUpgradeRequest
{
    public DateTimeOffset? CommandExpiryAt { get; init; }
    [Required, StringLength(500)] public string Reason { get; init; } = null!;
}

public sealed class AbandonProductionPackageUpgradeRequest
{
    [Required, StringLength(500)] public string Reason { get; init; } = null!;
}
