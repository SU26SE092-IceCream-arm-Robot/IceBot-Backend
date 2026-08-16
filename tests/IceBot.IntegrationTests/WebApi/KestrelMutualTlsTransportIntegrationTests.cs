using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebAPI.Configuration.Security;

namespace IceBot.IntegrationTests.WebApi;

public sealed class KestrelMutualTlsTransportIntegrationTests
{
    [Fact]
    public async Task DirectHttpsListener_ExposesTheEdgeClientCertificateToWebApi()
    {
        using var serverCertificate = CreateCertificate("icebot-edge-test", isServerCertificate: true);
        using var clientCertificate = CreateCertificate("icebot-edge-client", isServerCertificate: false);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseIceBotExecutionEndpointMutualTls();
        builder.WebHost.UseKestrel(options =>
            options.Listen(IPAddress.Loopback, 0, listen => listen.UseHttps(serverCertificate)));

        var app = builder.Build();
        app.MapGet("/edge-client-certificate", async context =>
        {
            var certificate = await context.Connection.GetClientCertificateAsync();
            await context.Response.WriteAsync(certificate?.Thumbprint ?? "missing");
        });

        await app.StartAsync();
        try
        {
            var address = app.Urls.Single(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
            using var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(clientCertificate);
            handler.ServerCertificateCustomValidationCallback =
                static (_, _, _, _) => true;
            using var client = new HttpClient(handler);

            var actualThumbprint = await client.GetStringAsync($"{address}/edge-client-certificate");

            Assert.Equal(clientCertificate.Thumbprint, actualThumbprint);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private static X509Certificate2 CreateCertificate(string commonName, bool isServerCertificate)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={commonName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            critical: true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection
            {
                new(isServerCertificate
                    ? "1.3.6.1.5.5.7.3.1"
                    : "1.3.6.1.5.5.7.3.2")
            }, critical: true));

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddDays(1));
        return X509CertificateLoader.LoadPkcs12(
            certificate.Export(X509ContentType.Pfx),
            password: null,
            X509KeyStorageFlags.Exportable);
    }
}
