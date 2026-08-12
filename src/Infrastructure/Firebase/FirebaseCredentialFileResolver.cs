using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Firebase;

public static class FirebaseCredentialFileResolver
{
    private const string ServiceAccountPattern = "*firebase-adminsdk*.json";

    public static string? Resolve(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var configuredPath = configuration["Firebase:CredentialsPath"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return ResolvePath(configuredPath, environment.ContentRootPath);
        }

        var googleCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        if (!string.IsNullOrWhiteSpace(googleCredentials))
        {
            return ResolvePath(googleCredentials, environment.ContentRootPath);
        }

        var firebaseDirectory = Path.GetFullPath(Path.Combine(
            environment.ContentRootPath,
            "..",
            "Infrastructure",
            "Firebase"));

        if (!Directory.Exists(firebaseDirectory))
        {
            return null;
        }

        var candidates = Directory
            .EnumerateFiles(firebaseDirectory, ServiceAccountPattern, SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return candidates.Length switch
        {
            0 => null,
            1 => candidates[0],
            _ => throw new InvalidOperationException(
                $"Multiple Firebase service-account files were found in '{firebaseDirectory}'. " +
                "Set Firebase:CredentialsPath explicitly."),
        };
    }

    private static string ResolvePath(string path, string contentRootPath)
    {
        var expandedPath = Environment.ExpandEnvironmentVariables(path.Trim());
        return Path.GetFullPath(
            Path.IsPathRooted(expandedPath)
                ? expandedPath
                : Path.Combine(contentRootPath, expandedPath));
    }
}
