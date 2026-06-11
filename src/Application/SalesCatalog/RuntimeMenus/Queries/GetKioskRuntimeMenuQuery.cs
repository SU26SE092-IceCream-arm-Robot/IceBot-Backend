namespace Application.SalesCatalog.RuntimeMenus.Queries;

public sealed class GetKioskRuntimeMenuQuery
{
    public Guid KioskId { get; init; }

    public GetKioskRuntimeMenuQuery(Guid kioskId)
    {
        KioskId = kioskId;
    }
}
