using Domain.Common;
using Domain.Identity.Enums;
using Domain.Identity.ValueObjects;
using Domain.Tenants.Entities;

namespace Domain.Identity.Entities;

public partial class Account : BusinessEntity
{
    public string UserName { get; set; } = null!;

    public string? FullName { get; set; }

    public string Email { get; set; } = null!;

    public bool EmailConfirmed { get; set; }

    public DateTimeOffset? EmailConfirmedAt { get; set; }

    public HashedPassword? Password { get; set; }

    public string? ImageUrl { get; set; }

    public string? PhoneNumber { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public DateTimeOffset? PhoneNumberConfirmedAt { get; set; }

    public string? Address { get; set; }

    public string Gender { get; set; } = "Other";

    public AccountStatus Status { get; set; } = AccountStatus.Active;

    public bool LocalLoginEnabled { get; set; }

    public bool GoogleLoginEnabled { get; set; }

    public string? GoogleSubjectId { get; set; }

    public string? GoogleEmail { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    public int FailedLoginCount { get; set; }

    public virtual ICollection<AccountDevice> AccountDevices { get; set; } = new List<AccountDevice>();

    public virtual ICollection<AccountRole> AccountRoles { get; set; } = new List<AccountRole>();

    public virtual ICollection<PasswordResetRequest> PasswordResetRequests { get; set; } = new List<PasswordResetRequest>();

    public virtual ICollection<AccountInvitation> AccountInvitations { get; set; } = new List<AccountInvitation>();

    public virtual ICollection<Store> Stores { get; set; } = new List<Store>();
}
