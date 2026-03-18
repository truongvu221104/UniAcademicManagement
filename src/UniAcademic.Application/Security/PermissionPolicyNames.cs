namespace UniAcademic.Application.Security;

public static class PermissionPolicyNames
{
    public static string FromPermission(string permission) => PermissionConstants.BuildPolicy(permission);
}
