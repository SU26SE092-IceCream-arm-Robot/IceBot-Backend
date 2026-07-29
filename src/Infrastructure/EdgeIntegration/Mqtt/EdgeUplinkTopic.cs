namespace Infrastructure.EdgeIntegration.Mqtt;

public static class EdgeUplinkTopic
{
    public static IReadOnlyList<string> Subscriptions(string topicPrefix, string consumerGroup)
    {
        var prefix = Normalize(topicPrefix);
        return Application.EdgeIntegration.Uplink.EdgeUplinkMessageTypes.All
            .Select(messageType =>
                $"$share/{consumerGroup.Trim()}/{prefix}/execution-endpoints/+/uplink/{messageType}")
            .OrderBy(topic => topic, StringComparer.Ordinal)
            .ToArray();
    }

    public static string Result(string topicPrefix, Guid endpointId) =>
        $"{Normalize(topicPrefix)}/execution-endpoints/{endpointId:D}/uplink/results";

    public static bool TryParse(
        string topicPrefix,
        string topic,
        out Guid endpointId,
        out string messageType)
    {
        endpointId = Guid.Empty;
        messageType = string.Empty;
        var expectedPrefix = $"{Normalize(topicPrefix)}/execution-endpoints/";
        if (!topic.StartsWith(expectedPrefix, StringComparison.Ordinal))
            return false;

        var suffix = topic[expectedPrefix.Length..].Split('/', StringSplitOptions.None);
        if (suffix.Length != 3 ||
            !Guid.TryParseExact(suffix[0], "D", out endpointId) ||
            !string.Equals(suffix[1], "uplink", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(suffix[2]) ||
            string.Equals(suffix[2], "results", StringComparison.Ordinal))
        {
            endpointId = Guid.Empty;
            return false;
        }

        messageType = suffix[2];
        return true;
    }

    private static string Normalize(string topicPrefix) => topicPrefix.Trim().Trim('/');
}
