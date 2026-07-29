namespace Application.Operations.Alerts.Automation;

public static class InventoryAlertScanWindow
{
    public static int CalculateOffset(int candidateCount, int batchSize, long scanSlot)
    {
        if (candidateCount <= 0 || batchSize <= 0)
        {
            return 0;
        }

        var normalizedSlot = scanSlot % candidateCount;
        if (normalizedSlot < 0)
        {
            normalizedSlot += candidateCount;
        }

        return (int)((normalizedSlot * batchSize) % candidateCount);
    }
}
