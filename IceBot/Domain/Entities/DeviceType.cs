using Domain.Common;

namespace Domain.Entities;

public partial class DeviceType : CatalogEntity
{
    public string Category { get; set; } = "Peripheral";

    public bool RequiresKioskAssignment { get; set; } = true;

    public virtual ICollection<DeviceModel> DeviceModels { get; set; } = new List<DeviceModel>();

    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
}
