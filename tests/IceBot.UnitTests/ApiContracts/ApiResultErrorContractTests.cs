using System.Text.Json;
using Application.Identity;
using Application.Orders.PlaceOrder;
using Application.Payments.PaymentSessions;
using Application.SalesCatalog.Admission;
using Application.Shared.Wrappers;

namespace IceBot.UnitTests.ApiContracts;

public sealed class ApiResultErrorContractTests
{
    [Fact]
    public void BusinessFailure_SerializesBusinessErrorAsStringAndSystemErrorAsNull()
    {
        var result = ApiResult<object>.BusinessFailure(
            OrderErrors.IdempotencyConflict);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));

        var root = document.RootElement;
        Assert.Equal(JsonValueKind.String, root.GetProperty("businessError").ValueKind);
        Assert.Equal(OrderErrors.IdempotencyConflict.Code, root.GetProperty("businessError").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("systemError").ValueKind);
    }

    [Fact]
    public void PublishedBusinessErrors_AreUniqueAndFollowTheDottedUppercaseConvention()
    {
        var definitions = IdentityErrors.All
            .Concat(OrderErrors.All)
            .Concat(PaymentErrors.All)
            .Concat(SalesAdmissionErrors.All)
            .ToArray();

        Assert.Equal(definitions.Length, definitions.Select(definition => definition.Code).Distinct(StringComparer.Ordinal).Count());
        Assert.All(definitions, definition => Assert.Matches("^[A-Z][A-Z0-9_]*(\\.[A-Z][A-Z0-9_]*)+$", definition.Code));
    }

    [Theory]
    [InlineData("invalid", 409, "Safe message")]
    [InlineData("ORDER.INVALID", 200, "Safe message")]
    [InlineData("ORDER.INVALID", 409, "unsafe\nmessage")]
    public void BusinessErrorDefinition_RejectsInvalidPublicContract(string code, int statusCode, string message)
    {
        Assert.ThrowsAny<ArgumentException>(() => new ApiBusinessErrorDefinition(code, statusCode, message));
    }
}
