using System.Data.Common;
using System.Text;
using System.Text.Json;
using Application.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebAPI.Middlewares;

namespace IceBot.IntegrationTests.WebApi;

public sealed class GlobalExceptionMiddlewareContractTests
{
    public static IEnumerable<object[]> HandledExceptions()
    {
        yield return [new AppException("internal-marker-app", 409), StatusCodes.Status409Conflict];
        yield return [new DbUpdateException("internal-marker-db-update"), StatusCodes.Status500InternalServerError];
        yield return [new TestDbException("internal-marker-db"), StatusCodes.Status500InternalServerError];
        yield return [new UnauthorizedAccessException("internal-marker-unauthorized"), StatusCodes.Status401Unauthorized];
        yield return [new KeyNotFoundException("internal-marker-not-found"), StatusCodes.Status404NotFound];
        yield return [new OperationCanceledException("internal-marker-cancelled"), StatusCodes.Status408RequestTimeout];
        yield return [new InvalidOperationException("internal-marker-unhandled"), StatusCodes.Status500InternalServerError];
    }

    [Theory]
    [MemberData(nameof(HandledExceptions))]
    public async Task HandledException_ExposesOnlySafeEnvelope(Exception exception, int expectedStatusCode)
    {
        var context = new DefaultHttpContext();
        context.Items["X-Correlation-ID"] = "correlation-test";
        context.Response.Body = new MemoryStream();
        var middleware = new GlobalExceptionMiddleware(
            _ => throw exception,
            NullLogger<GlobalExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
        Assert.DoesNotContain("internal-marker", body, StringComparison.Ordinal);
        Assert.DoesNotContain(exception.GetType().Name, body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", body, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("systemError").ValueKind);
        Assert.False(document.RootElement.TryGetProperty("details", out var details) &&
                     details.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined);
    }

    private sealed class TestDbException(string message) : DbException(message);
}
