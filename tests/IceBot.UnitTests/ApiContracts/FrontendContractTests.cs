using Application.RobotConfiguration.Programs.Results;
using Application.RobotConfiguration.ArtifactTemplates.Results;
using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.ArtifactTemplates.Commands;
using Application.RobotConfiguration.Programs.Commands;
using System.Text.Json;
using Application.Identity.InternalAccounts.Results;
using Application.Orders.PlaceOrder.Results;
using Application.Payments.PaymentSessions.Results;
using Application.ProductionConfiguration.Releases.Results;
using Application.ProductionConfiguration.Deployments.Results;
using Application.SalesCatalog.Menus.Requests;
using Application.Catalog.Products.Requests;
using Application.Devices.Catalog.Requests;
using Application.Devices.ExecutionEndpoints.Requests;
using Application.Devices.Catalog.Results;
using Application.Devices.ExecutionEndpoints.Results;
using Application.Devices.Telemetry.Results;
using Application.Devices.Connectivity.Results;
using Application.Devices.Credentials.Results;
using Application.Identity.Authentication.Results;
using Application.Identity.InternalAccounts.Requests;
using Application.ProductionConfiguration.Releases.Commands;
using Application.ProductionConfiguration.Deployments.Commands;
using Application.ProductionConfiguration.Routes.Commands;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.Tenants.Kiosks.Requests;
using Application.Tenants.Stores.Requests;
using Domain.Orders.Enums;
using Domain.Payments.Enums;
using Application.Orders.PlaceOrder.Requests;

namespace IceBot.UnitTests.ApiContracts;

public sealed class FrontendContractTests
{
    [Fact]
    public void CustomerTracking_DoesNotSerializeInternalStateMachines()
    {
        var orderJson = JsonSerializer.Serialize(new OrderResult
        {
            Status = OrderStatus.Preparing,
            PaymentStatus = PaymentStatus.Paid,
            CustomerStatus = "Preparing",
            CustomerStatusMessage = "Preparing your order."
        });
        var paymentJson = JsonSerializer.Serialize(new PaymentStatusResult
        {
            PaymentTransactionStatus = PaymentTransactionStatus.Paid,
            OrderPaymentStatus = PaymentStatus.Paid,
            OrderStatus = OrderStatus.Preparing,
            CustomerStatus = "Preparing",
            CustomerStatusMessage = "Preparing your order."
        });

        Assert.DoesNotContain("\"Status\"", orderJson);
        Assert.DoesNotContain("\"PaymentStatus\"", orderJson);
        Assert.DoesNotContain("PaymentTransactionStatus", paymentJson);
        Assert.DoesNotContain("OrderPaymentStatus", paymentJson);
        Assert.DoesNotContain("OrderStatus", paymentJson);
        Assert.Contains("CustomerStatus", orderJson);
        Assert.Contains("CustomerStatus", paymentJson);
    }

    [Fact]
    public void DeploymentResponse_DoesNotSerializeEdgeCommandId()
    {
        var json = JsonSerializer.Serialize(new KioskConfigurationDeploymentResult
        {
            Id = Guid.NewGuid(),
            EdgeCommandId = Guid.NewGuid()
        });

        Assert.DoesNotContain("EdgeCommandId", json);
    }

    [Fact]
    public void ManagementAccountResponse_ContainsConfiguredGoogleEmail()
    {
        var json = JsonSerializer.Serialize(new InternalAccountResult
        {
            GoogleLoginEnabled = true,
            GoogleEmail = "allowed@example.com"
        });

        Assert.Contains("allowed@example.com", json);
    }

    [Fact]
    public void AuthoringRequests_DoNotExposeDerivedLifecycleOrCurrencyFields()
    {
        Assert.Null(typeof(CreateMenuRequest).GetProperty("Status"));
        Assert.Null(typeof(CreateMenuItemRequest).GetProperty("Status"));
        Assert.Null(typeof(CreateMenuItemRequest).GetProperty("Currency"));
        Assert.Null(typeof(UpsertProductVariantRequest).GetProperty("Currency"));
        Assert.Null(typeof(CreateProductRequest).GetProperty("ScopeType"));
        Assert.Null(typeof(CreateProductRequest).GetProperty("IsAvailable"));
        Assert.Null(typeof(UpsertProductVariantRequest).GetProperty("IsAvailable"));
        Assert.Null(typeof(CreateMenuRequest).GetProperty("ScopeType"));
        Assert.Null(typeof(CreateRobotProgramCommand).GetProperty("ScopeType"));
        Assert.Null(typeof(CreateExecutionEndpointRequest).GetProperty("AuthenticationMode"));
        Assert.Null(typeof(CreateDeviceRequest).GetProperty("MetadataJson"));
        Assert.Null(typeof(CreateKioskRequest).GetProperty("SettingsJson"));
        Assert.Null(typeof(SetInternalAccountPasswordRequest).GetProperty("EnableLocalLogin"));
        Assert.Null(typeof(ConfigurationReleaseRouteInput).GetProperty("ProductVariantId"));
        Assert.Null(typeof(CreateProductOptionRequest).GetProperty("Currency"));
        Assert.Null(typeof(CreateProductOptionRequest).GetProperty("IsAvailable"));
        Assert.Null(typeof(UpdateProductOptionRequest).GetProperty("Currency"));
        Assert.Null(typeof(UpdateProductOptionRequest).GetProperty("IsAvailable"));
        Assert.Null(typeof(PlaceOrderItemRequest).GetProperty("OptionsJson"));
        Assert.NotNull(typeof(PlaceOrderItemRequest).GetProperty("SelectedOptions"));
        Assert.NotNull(typeof(UpdateMenuItemRequest).GetProperty("ClearRecipe"));
        Assert.Null(typeof(ConfigurationReleaseRouteInput).GetProperty("RequiredCapabilitiesJson"));
        Assert.NotNull(typeof(ConfigurationReleaseRouteInput).GetProperty("RequiredCapabilities"));
        Assert.NotNull(typeof(ReplaceConfigurationReleaseRoutesCommand).GetProperty("ExpectedRevision"));
        Assert.Null(typeof(ExecutionRouteResult).GetProperty("RequiredCapabilitiesJson"));
        Assert.NotNull(typeof(ExecutionRouteResult).GetProperty("RequiredCapabilities"));
        Assert.NotNull(typeof(ConfigurationReleaseResult).GetProperty("Revision"));
    }

    [Fact]
    public void NormalResponses_DoNotExposeProfileOrTelemetryInternals()
    {
        Assert.Null(typeof(AuthenticatedAccountResult).GetProperty("Status"));
        Assert.Null(typeof(AuthenticatedAccountResult).GetProperty("Address"));
        Assert.Null(typeof(AuthenticatedAccountResult).GetProperty("Gender"));
        Assert.Null(typeof(KioskHeartbeatResult).GetProperty("NodeId"));
        Assert.Null(typeof(KioskHeartbeatResult).GetProperty("OriginNodeId"));
        Assert.Null(typeof(KioskHeartbeatResult).GetProperty("HeartbeatSequence"));
        Assert.Null(typeof(DeviceEventResult).GetProperty("OriginNodeId"));
        Assert.Null(typeof(DeviceEventResult).GetProperty("CorrelationId"));
        Assert.Null(typeof(DeviceEventResult).GetProperty("CausationId"));
    }

    [Fact]
    public void StoreOpeningHours_UsesTypedContract()
    {
        Assert.Null(typeof(CreateStoreRequest).GetProperty("OpeningHoursJson"));
        Assert.NotNull(typeof(CreateStoreRequest).GetProperty("OpeningHours"));
    }
}
