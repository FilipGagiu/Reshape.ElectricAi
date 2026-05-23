using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Presentation.Middleware;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Middleware;

public sealed class ExceptionHandlerMiddlewareMappingTests
{
    [Fact]
    public async Task LlmException_Returns502()
    {
        var ctx = BuildContext();
        var middleware = new ExceptionHandlerMiddleware(
            _ => throw new LlmException("llm-unavailable", "boom"),
            NullLogger<ExceptionHandlerMiddleware>.Instance);

        await middleware.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be((int)HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task TooManyRequestsException_Returns429()
    {
        var ctx = BuildContext();
        var middleware = new ExceptionHandlerMiddleware(
            _ => throw new TooManyRequestsException(retryAfterSeconds: 42),
            NullLogger<ExceptionHandlerMiddleware>.Instance);

        await middleware.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests);
    }

    private static DefaultHttpContext BuildContext() =>
        new() { Response = { Body = new MemoryStream() } };
}
