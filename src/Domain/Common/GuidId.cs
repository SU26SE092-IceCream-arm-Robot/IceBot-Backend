namespace Domain.Common;

public static class GuidId
{
    public static Guid New() => Guid.CreateVersion7();
}
