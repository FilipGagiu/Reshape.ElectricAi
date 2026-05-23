using System.Net;
using System.Text.Json;
using Reshape.ElectricAi.Core.Domain.Exceptions;

namespace Reshape.ElectricAi.Presentation.Middleware;

public sealed partial class ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            var status = MapStatus(ex);
            if (status >= 500)
            {
                LogDomainServerError(_logger, ex.Code, ex);
            }
            else
            {
                LogDomainClientError(_logger, ex.Code, ex.Message);
            }
            await WriteEnvelopeAsync(context, status, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            LogUnhandled(_logger, ex);
            await WriteEnvelopeAsync(context, (int)HttpStatusCode.InternalServerError, "internal-error", "An unexpected error occurred.");
        }
    }

    private static int MapStatus(DomainException exception) => exception switch
    {
        UnauthorizedException => (int)HttpStatusCode.Unauthorized,
        ForbiddenException => (int)HttpStatusCode.Forbidden,
        NotFoundException => (int)HttpStatusCode.NotFound,
        ConflictException => (int)HttpStatusCode.Conflict,
        PreconditionFailedException => 422,
        _ => (int)HttpStatusCode.BadRequest
    };

    private async Task WriteEnvelopeAsync(HttpContext context, int status, string code, string message)
    {
        if (context.Response.HasStarted)
        {
            LogResponseAlreadyStarted(_logger, code);
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";

        await JsonSerializer.SerializeAsync(context.Response.Body, ErrorEnvelope.Simple(code, message), JsonOptions);
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Error, Message = "Domain exception with 5xx mapping: {Code}")]
    private static partial void LogDomainServerError(ILogger logger, string code, Exception exception);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Domain exception {Code}: {DomainMessage}")]
    private static partial void LogDomainClientError(ILogger logger, string code, string domainMessage);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Unhandled exception.")]
    private static partial void LogUnhandled(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Warning, Message = "Response already started; dropped envelope for code {Code}.")]
    private static partial void LogResponseAlreadyStarted(ILogger logger, string code);
}
