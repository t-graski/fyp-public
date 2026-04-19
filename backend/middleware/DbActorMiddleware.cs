using System.Security.Claims;
using backend.data;

namespace backend.middleware;

public class DbActorMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, AppDbContext db)
    {
        db.RequestPath = context.Request.Path.ToString();
        db.CorrelationId = context.TraceIdentifier;
        
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var idStr =
                context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                context.User.FindFirstValue("sub");

            if (Guid.TryParse(idStr, out var userId))
            {
                db.ActorUserId = userId;
            }
        }

        await next(context);
    }
}