using System.Diagnostics.Metrics;
using System.Net;
using Infrastructure.Payments;
using Infrastructure.Payments.Observability;
using Infrastructure.Payments.Options;
using Infrastructure.Payments.Providers.PayOS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Domain.Orders.Entities;
using Domain.Payments.Entities;
using IceBot.IntegrationTests.Infrastructure;

namespace IceBot.IntegrationTests.Payments;

public sealed class PayOsResilienceTests
{
    [Fact]
    public async Task FakeHttpServerConfirmsPaymentPostIsNotRetriedAfterTransientResponse()
    {
        await using var server = new FakeHttpServer((_, context) =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            return Task.CompletedTask;
        });
        var services = new ServiceCollection();
        services.AddHttpClient("payos-network-test", client => client.BaseAddress = server.BaseAddress)
            .AddPayOsResilience(new PayOsResilienceOptions());

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("payos-network-test");

        using var response = await client.PostAsync("v2/payment-requests", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, server.RequestCount);
    }

    [Fact]
    public async Task CallerCancellationStopsPayOsPipelineWithoutRetry()
    {
        await using var server = new FakeHttpServer(async (_, context) =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(300));
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        });
        var services = new ServiceCollection();
        services.AddHttpClient("payos-cancellation-test", client => client.BaseAddress = server.BaseAddress)
            .AddPayOsResilience(new PayOsResilienceOptions());
        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("payos-cancellation-test");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.PostAsync("v2/payment-requests", new StringContent("{}"), cancellation.Token));

        Assert.Equal(1, server.RequestCount);
    }
    [Fact]
    public async Task PostPaymentRequestIsNotRetried()
    {
        var handler = new CountingHandler(HttpStatusCode.ServiceUnavailable);
        var services = new ServiceCollection();
        services.AddHttpClient("payos-test", client => client.BaseAddress = new Uri("https://payos.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddPayOsResilience(new PayOsResilienceOptions());

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("payos-test");

        using var response = await client.PostAsync("v2/payment-requests", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task GatewayRecordsTransientFailureForFinalServerError()
    {
        using var listener = new MeterListener();
        var failureKinds = new List<string>();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == PayOsResilienceMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            var tag = tags.ToArray().Single(item => item.Key == "failure.kind");
            failureKinds.Add((string)tag.Value!);
        });
        listener.Start();

        var gateway = CreateGateway(new CountingHandler(HttpStatusCode.ServiceUnavailable));

        await Assert.ThrowsAsync<InvalidOperationException>(() => gateway.CreatePaymentSessionAsync(
            new PaymentTransaction { Id = Guid.NewGuid(), Amount = 10_000 },
            new Order { OrderNumber = "ORDER-1" }));

        Assert.Contains("transient", failureKinds);
    }

    private static PayOsPaymentGateway CreateGateway(HttpMessageHandler handler)
    {
        var options = Options.Create(new PayOsOptions
        {
            BaseUrl = "https://payos.test",
            ClientId = "client",
            ApiKey = "api-key",
            ChecksumKey = "checksum",
            ReturnUrl = "https://app.test/return",
            CancelUrl = "https://app.test/cancel"
        });
        return new PayOsPaymentGateway(
            options,
            new HttpClient(handler) { BaseAddress = new Uri("https://payos.test/") },
            NullLogger<PayOsPaymentGateway>.Instance);
    }

    private sealed class CountingHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("{\"code\":\"error\"}")
            });
        }
    }
}
