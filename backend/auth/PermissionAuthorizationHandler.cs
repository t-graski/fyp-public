using Microsoft.AspNetCore.Authorization;

namespace backend.auth;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permClaim = context.User.FindFirst("perm")?.Value;
        if (!long.TryParse(permClaim, out var userPerms))
            return Task.CompletedTask;

        var required = (long)requirement.Permission;

        if (((Permission)userPerms).HasFlag(Permission.SuperAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if ((userPerms & required) == required)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public record PermissionRequirement(Permission Permission) : IAuthorizationRequirement;