namespace Application.ClientDevices;

public sealed class ClientDeviceRuntimeOptions
{
    public const string SectionName = "ClientDevices:Runtime";

    public int MaxOrderLines { get; init; } = 20;
    public int MaxQuantityPerLine { get; init; } = 10;
    public int MaxTotalUnits { get; init; } = 50;
    public int MaxSelectedOptionsPerLine { get; init; } = 20;
    public int MaxClientOrderIdLength { get; init; } = 100;
    public int MaxCustomerNameLength { get; init; } = 120;
    public int MaxCustomerPhoneNumberLength { get; init; } = 40;
    public int MaxNotesLength { get; init; } = 2_000;
    public int MaxClientLineIdLength { get; init; } = 100;
}
