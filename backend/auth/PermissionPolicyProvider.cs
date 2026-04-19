using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace backend.auth;

public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith("perm:", StringComparison.OrdinalIgnoreCase))
        {
            var raw = policyName["perm:".Length..];

            if (long.TryParse(raw, out var bits))
            {
                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement((Permission)bits))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }
        }

        return base.GetPolicyAsync(policyName);
    }
}