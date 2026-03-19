using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Security;
using UniAcademic.Infrastructure.Authorization;
using Xunit;

namespace UniAcademic.Tests.Unit.Authorization;

public sealed class CourseOfferingPermissionAuthorizationTests
{
    [Fact]
    public async Task HandleRequirementAsync_ShouldNotSucceed_WhenCourseOfferingDeletePermissionIsMissing()
    {
        var currentUser = new FakeCurrentUser
        {
            PermissionsValue = [PermissionConstants.CourseOfferings.View]
        };
        var handler = new PermissionAuthorizationHandler(currentUser);
        var requirement = new PermissionRequirement(PermissionConstants.CourseOfferings.Delete);
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
