using Application.Identity.Tokens.Claims;
using System.ComponentModel.DataAnnotations;

namespace Application.Identity.Workforce.Staff;

public sealed class StaffWorkforceScopeRequest
{
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
}

public sealed class CreateStaffWorkforceRequest
{
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public bool LocalLoginEnabled { get; init; } = true;
    public bool GoogleLoginEnabled { get; init; }
    public string? GoogleEmail { get; init; }
    public bool SendInvitationEmail { get; init; } = true;
    [Required]
    public IReadOnlyList<StaffWorkforceScopeRequest> StaffScopes { get; init; } = [];
}

public sealed class UpdateStaffWorkforceRequest
{
    public string? FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public long ExpectedRevision { get; init; }
}

public sealed class UpdateStaffWorkforceScopesRequest
{
    [Required]
    public IReadOnlyList<StaffWorkforceScopeRequest> StaffScopes { get; init; } = [];
    public long ExpectedRevision { get; init; }
}

public sealed class StaffLifecycleRequest
{
    [Required]
    [StringLength(128)]
    public string IdempotencyKey { get; init; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Reason { get; init; } = string.Empty;

    [Range(0, long.MaxValue)]
    public long ExpectedRevision { get; init; }
}

public sealed class StaffWorkforceResult
{
    public Guid AccountId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool LocalLoginEnabled { get; init; }
    public bool GoogleLoginEnabled { get; init; }
    public IReadOnlyList<StaffWorkforceScopeResult> StaffScopes { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public long Revision { get; init; }
    public StaffWorkforceInvitationResult? Invitation { get; init; }
}

public sealed class StaffWorkforceScopeResult
{
    public Guid? StoreId { get; init; }
    public string? StoreCode { get; init; }
    public Guid? KioskId { get; init; }
    public string? KioskCode { get; init; }
}

public sealed class StaffWorkforceInvitationResult
{
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? EmailSentAt { get; init; }
}

public sealed class ListStaffWorkforceQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class GetStaffWorkforceQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid AccountId { get; init; }
}

public sealed class CreateStaffWorkforceCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid? ActorAccountId { get; init; }
    public string? IdempotencyKey { get; init; }
    public required CreateStaffWorkforceRequest Request { get; init; }
}

public sealed class UpdateStaffWorkforceCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid AccountId { get; init; }
    public Guid? ActorAccountId { get; init; }
    public required UpdateStaffWorkforceRequest Request { get; init; }
}

public sealed class UpdateStaffWorkforceScopesCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid AccountId { get; init; }
    public Guid? ActorAccountId { get; init; }
    public required UpdateStaffWorkforceScopesRequest Request { get; init; }
}

public sealed class ChangeStaffWorkforceLifecycleCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid AccountId { get; init; }
    public Guid? ActorAccountId { get; init; }
    public required StaffLifecycleRequest Request { get; init; }
    public bool Reactivate { get; init; }
}

public sealed class SendStaffWorkforceInvitationCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid AccountId { get; init; }
    public Guid? ActorAccountId { get; init; }
    public bool SendEmail { get; init; } = true;
}

public interface IStaffSessionRevoker
{
    Task<int> RevokeAllAsync(Guid accountId, string reason, CancellationToken cancellationToken = default);
}
