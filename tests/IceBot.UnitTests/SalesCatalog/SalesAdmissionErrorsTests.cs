using Application.SalesCatalog.Admission;

namespace IceBot.UnitTests.SalesCatalog;

public sealed class SalesAdmissionErrorsTests
{
    [Fact]
    public void EveryBlockerHasOneStablePublicCode()
    {
        var codes = Enum.GetValues<SalesAdmissionBlockerCode>()
            .Select(code => SalesAdmissionErrors.For(code).Code)
            .ToArray();

        Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(codes, code => Assert.Matches("^SALES\\.[A-Z][A-Z0-9_]*$", code));
    }

    [Fact]
    public void SelectPrimary_UsesDocumentedPriorityRatherThanInputOrder()
    {
        var blockers = new[]
        {
            new SalesAdmissionBlocker(SalesAdmissionBlockerCode.InventoryInsufficient, SalesAdmissionBlockerScope.Inventory),
            new SalesAdmissionBlocker(SalesAdmissionBlockerCode.KioskConnectivityUnavailable, SalesAdmissionBlockerScope.Kiosk),
            new SalesAdmissionBlocker(SalesAdmissionBlockerCode.OrganizationInactive, SalesAdmissionBlockerScope.Organization)
        };

        var primary = SalesAdmissionErrors.SelectPrimary(blockers);

        Assert.NotNull(primary);
        Assert.Equal(SalesAdmissionBlockerCode.OrganizationInactive, primary.Code);
    }
}
