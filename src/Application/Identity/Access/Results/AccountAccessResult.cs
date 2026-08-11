namespace Application.Identity.Access.Results;

public sealed class AccountAccessResult
{
    public Guid AccountId { get; set; }
    public bool IsSystemAdmin { get; set; }
    public List<string> Roles { get; set; } = [];
    public List<string> PermissionCodes { get; set; } = [];
    public List<AccountRoleScopeAccessResult> RoleScopes { get; set; } = [];
    public EffectiveScopeResult EffectiveScope { get; set; } = new();
}

public sealed class AccountRoleScopeAccessResult
{
    public string RoleCode { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public Guid? StoreId { get; set; }
    public Guid? KioskId { get; set; }
}

public sealed class EffectiveScopeResult
{
    public List<Guid> OrganizationIds { get; set; } = [];
    public List<Guid> StoreIds { get; set; } = [];
    public List<Guid> KioskIds { get; set; } = [];
}
