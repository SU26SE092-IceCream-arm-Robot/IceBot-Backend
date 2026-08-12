using Infrastructure.Firebase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace IceBot.IntegrationTests.Firebase;

public sealed class FirebaseCredentialFileResolverTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(
        Path.GetTempPath(),
        $"icebot-firebase-resolver-{Guid.NewGuid():N}");

    [Fact]
    public void Resolve_UsesConfiguredPathRelativeToContentRoot()
    {
        var environment = CreateEnvironment();
        var expectedPath = Path.Combine(environment.ContentRootPath, "secrets", "firebase.json");
        Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);
        File.WriteAllText(expectedPath, "{}");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Firebase:CredentialsPath"] = Path.Combine("secrets", "firebase.json"),
            })
            .Build();

        var resolvedPath = FirebaseCredentialFileResolver.Resolve(configuration, environment);

        Assert.Equal(Path.GetFullPath(expectedPath), resolvedPath);
    }

    [Fact]
    public void Resolve_DiscoversSingleServiceAccountFileWithoutDependingOnHash()
    {
        var environment = CreateEnvironment();
        var firebaseDirectory = Path.Combine(_rootPath, "Infrastructure", "Firebase");
        var expectedPath = Path.Combine(
            firebaseDirectory,
            "icecream-arm-robot-firebase-adminsdk-fbsvc-newhash.json");
        Directory.CreateDirectory(firebaseDirectory);
        File.WriteAllText(expectedPath, "{}");

        var resolvedPath = FirebaseCredentialFileResolver.Resolve(
            new ConfigurationBuilder().Build(),
            environment);

        Assert.Equal(Path.GetFullPath(expectedPath), resolvedPath);
    }

    [Fact]
    public void Resolve_RejectsAmbiguousServiceAccountFiles()
    {
        var environment = CreateEnvironment();
        var firebaseDirectory = Path.Combine(_rootPath, "Infrastructure", "Firebase");
        Directory.CreateDirectory(firebaseDirectory);
        File.WriteAllText(Path.Combine(firebaseDirectory, "first-firebase-adminsdk.json"), "{}");
        File.WriteAllText(Path.Combine(firebaseDirectory, "second-firebase-adminsdk.json"), "{}");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            FirebaseCredentialFileResolver.Resolve(
                new ConfigurationBuilder().Build(),
                environment));

        Assert.Contains("Set Firebase:CredentialsPath explicitly", exception.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private TestHostEnvironment CreateEnvironment()
    {
        var contentRootPath = Path.Combine(_rootPath, "WebAPI");
        Directory.CreateDirectory(contentRootPath);

        return new TestHostEnvironment
        {
            ContentRootPath = contentRootPath,
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath),
        };
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "IceBot.IntegrationTests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
