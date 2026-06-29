namespace IceBot.IntegrationTests.Infrastructure;

[AttributeUsage(AttributeTargets.Method)]
public sealed class IntegrationFactAttribute : FactAttribute
{
    public IntegrationFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("ICEBOT_RUN_INTEGRATION_TESTS"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set ICEBOT_RUN_INTEGRATION_TESTS=true to run Docker-backed integration tests.";
        }
    }
}
