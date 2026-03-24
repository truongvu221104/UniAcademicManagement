using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Security;
using UniAcademic.Infrastructure.Authorization;
using Xunit;

namespace UniAcademic.Tests.Unit.Authorization;

public sealed class CourseMaterialPermissionAuthorizationTests
{
    [Fact]
    public async Task HandleRequirementAsync_ShouldNotSucceed_WhenCourseMaterialCreatePermissionIsMissing()
    {
        var currentUser = new FakeCurrentUser
        {
            PermissionsValue = [PermissionConstants.CourseMaterials.View]
        };
        var handler = new PermissionAuthorizationHandler(currentUser);
        var requirement = new PermissionRequirement(PermissionConstants.CourseMaterials.Create);
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_ShouldNotSucceed_WhenCourseMaterialEditPermissionIsMissing()
    {
        var currentUser = new FakeCurrentUser
        {
            PermissionsValue = [PermissionConstants.CourseMaterials.View, PermissionConstants.CourseMaterials.Create]
        };
        var handler = new PermissionAuthorizationHandler(currentUser);
        var requirement = new PermissionRequirement(PermissionConstants.CourseMaterials.Edit);
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_ShouldNotSucceed_WhenCourseMaterialDownloadPermissionIsMissing()
    {
        var currentUser = new FakeCurrentUser
        {
            PermissionsValue = [PermissionConstants.CourseMaterials.View, PermissionConstants.CourseMaterials.Create, PermissionConstants.CourseMaterials.Edit]
        };
        var handler = new PermissionAuthorizationHandler(currentUser);
        var requirement = new PermissionRequirement(PermissionConstants.CourseMaterials.Download);
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
