using System.ComponentModel.DataAnnotations;
using Domain.Inventory.Enums;

namespace Application.Inventory.Requests;

public sealed class CreateDispenserStateRequest
{
    public Guid DeviceId { get; set; }
    public Guid IngredientId { get; set; }

    [Required, StringLength(50, MinimumLength = 1)]
    public string ContainerCode { get; set; } = null!;

    [Range(typeof(decimal), "0.000001", "999999999")]
    public decimal? CapacityQuantity { get; set; }

    [Required, StringLength(30)]
    public string Unit { get; set; } = "gram";

    [MaxLength(10)]
    public IReadOnlyList<DispenserLevelQuantityPointRequest> LevelToQuantityProfile { get; set; } = [];
}

public sealed class UpdateDispenserStateRequest
{
    [Range(typeof(decimal), "0.000001", "999999999")]
    public decimal? CapacityQuantity { get; set; }

    [Required, StringLength(30)]
    public string Unit { get; set; } = "gram";

    [MaxLength(10)]
    public IReadOnlyList<DispenserLevelQuantityPointRequest> LevelToQuantityProfile { get; set; } = [];
}

public sealed class DispenserLevelQuantityPointRequest
{
    [EnumDataType(typeof(IngredientLevelStatus))]
    public IngredientLevelStatus Level { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal EstimatedQuantity { get; set; }
}

public sealed class SetDispenserStateStatusRequest
{
    public bool IsActive { get; set; }
}
