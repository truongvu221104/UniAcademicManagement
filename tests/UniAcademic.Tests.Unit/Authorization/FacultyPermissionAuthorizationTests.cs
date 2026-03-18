using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Security;
using UniAcademic.Infrastructure.Authorization;
using Xunit;

namespace UniAcademic.Tests.Unit.Authorization;

public sealed class FacultyPermissionAuthorizationTests
{
    [Fact]
    public async Task HandleRequirementAsync_ShouldNotSucceed_WhenFacultyDeletePermissionIsMissing()
    {
        var currentUser = new FakeCurrentUser
        {
            PermissionsValue = [PermissionConstants.Faculties.View]
        };
        var handler = new PermissionAuthorizationHandler(currentUser);
        var requirement = new PermissionRequirement(PermissionConstants.Faculties.Delete);
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
        public string? Username => null;
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions => PermissionsValue;
        public IReadOnlyCollection<string> PermissionsValue { get; init; } = [];
    }
}
