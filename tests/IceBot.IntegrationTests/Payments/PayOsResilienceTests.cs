using System.Diagnostics.Metrics;
using System.Net;
using System.Text;
using System.Text.Json;
using Application.Payments.Providers;
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
using Domain.Catalog.Enums;

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

        var exception = await Assert.ThrowsAsync<ProviderPaymentSessionCreationException>(() => gateway.CreatePaymentSessionAsync(
            Payment(gateway),
            new Order { OrderNumber = "ORDER-1" }));

        Assert.True(exception.OutcomeUnknown);
        Assert.Contains("transient", failureKinds);
    }

    [Fact]
    public async Task GatewayRejectsSuccessfulResponseWithoutPaymentInstructions()
    {
        const string responseJson = """
            {
              "code": "00",
              "data": {
                "orderCode": 1234567890123,
                "paymentLinkId": "link-1",
                "status": "PENDING"
              }
            }
            """;
        var gateway = CreateGateway(new JsonResponseHandler(responseJson));

        var exception = await Assert.ThrowsAsync<ProviderPaymentSessionCreationException>(() => gateway.CreatePaymentSessionAsync(
            Payment(gateway),
            new Order { OrderNumber = "ORDER-1" }));

        Assert.Contains("invalid or incomplete payment session", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GatewayRejectsSuccessfulResponseWithDifferentProviderOrderCode()
    {
        const string responseJson = """
            {
              "code": "00",
              "data": {
                "orderCode": 9999999999999,
                "paymentLinkId": "link-1",
                "status": "PENDING",
                "checkoutUrl": "https://pay.test/link-1"
              }
            }
            """;
        var gateway = CreateGateway(new JsonResponseHandler(responseJson));

        var exception = await Assert.ThrowsAsync<ProviderPaymentSessionCreationException>(() => gateway.CreatePaymentSessionAsync(
            Payment(gateway),
            new Order { OrderNumber = "ORDER-1" }));

        Assert.Contains("invalid or incomplete payment session", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GatewayReadsExistingPaymentSessionByPersistedProviderOrderCode()
    {
        const string responseJson = """
            {
              "code": "00",
              "data": {
                "id": "link-1",
                "orderCode": 1234567890123,
                "amount": 10000,
                "amountPaid": 0,
                "status": "PENDING",
                "checkoutUrl": "https://pay.test/link-1"
              }
            }
            """;
        var gateway = CreateGateway(new JsonResponseHandler(responseJson));

        var session = await gateway.GetPaymentSessionAsync("1234567890123");

        Assert.NotNull(session);
        Assert.Equal("link-1", session.ProviderPaymentLinkId);
        Assert.Equal(10_000, session.Amount);
        Assert.Equal("https://pay.test/link-1", session.CheckoutUrl);
    }

    [Fact]
    public async Task GatewayCapsProviderExpiryAtOrderPaymentDeadline()
    {
        const string responseJson = """
            {
              "code": "00",
              "data": {
                "orderCode": 1234567890123,
                "paymentLinkId": "link-1",
                "status": "PENDING",
                "checkoutUrl": "https://pay.test/link-1"
              }
            }
            """;
        var handler = new CapturingJsonResponseHandler(responseJson);
        var gateway = CreateGateway(handler);
        var order = new Order { OrderNumber = "ORDER-DEADLINE" };
        order.AddItem(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            "ITEM", "Item", "PRODUCT", "Product", "VARIANT", "Variant", null,
            FulfillmentType.Packaged, 1, 10_000);
        var placedAt = DateTimeOffset.UtcNow;
        var deadline = placedAt.AddMinutes(5);
        order.Place(placedAt, deadline);

        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            Amount = 10_000,
            ProviderOrderCode = "1234567890123"
        };
        var session = await gateway.CreatePaymentSessionAsync(payment, order);

        using var request = JsonDocument.Parse(handler.RequestJson!);
        Assert.Equal(deadline.ToUnixTimeSeconds(), request.RootElement.GetProperty("expiredAt").GetInt64());
        Assert.Equal(deadline, session.ExpiresAt);
    }

    private static PaymentTransaction Payment(PayOsPaymentGateway gateway)
    {
        var payment = new PaymentTransaction { Id = Guid.NewGuid(), Amount = 10_000 };
        payment.ProviderOrderCode = gateway.CreateProviderOrderCode(payment.Id);
        return payment;
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

    private sealed class JsonResponseHandler(string responseJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });
    }

    private sealed class CapturingJsonResponseHandler(string responseJson) : HttpMessageHandler
    {
        public string? RequestJson { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestJson = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        }
    }
}
