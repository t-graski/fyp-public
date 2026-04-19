using System.Text.Json;
using backend.errors;
using backend.responses;
using Npgsql;

namespace backend.middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (PostgresException ex) when (ex.SqlState == "23503")
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/json";

            var payload = ApiResponse<object>.Fail(
                409, "FK_CONSTRAINT", "Cannot delete because other records depend on it.");

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (AppException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Fail(
                ex.StatusCode,
                ex.ErrorCode,
                ex.Message
            );

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Fail(
                StatusCodes.Status500InternalServerError,
                "INTERNAL_ERROR",
                "An unexpected error occured"
            );

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}