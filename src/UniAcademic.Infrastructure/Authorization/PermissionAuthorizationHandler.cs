using Microsoft.AspNetCore.Authorization;
using UniAcademic.Application.Abstractions.Common;

namespace UniAcademic.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ICurrentUser _currentUser;

    public PermissionAuthorizationHandler(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (_currentUser.Permissions.Contains(requirement.Permission, StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
