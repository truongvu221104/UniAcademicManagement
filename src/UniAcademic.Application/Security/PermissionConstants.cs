namespace UniAcademic.Application.Security;

public static class PermissionConstants
{
    public const string PolicyPrefix = "Permission:";

    public static class Auth
    {
        public const string Login = "system.auth.login";
        public const string ManageSessions = "system.auth.manage_sessions";
        public const string ChangePassword = "system.auth.change_password";
    }

    public static class Users
    {
        public const string View = "system.users.view";
        public const string Manage = "system.users.manage";
        public const string AssignRoles = "system.users.assign_roles";
    }

    public static class Roles
    {
        public const string View = "system.roles.view";
        public const string Manage = "system.roles.manage";
    }

    public static class Faculties
    {
        public const string View = "academic.faculties.view";
        public const string Create = "academic.faculties.create";
        public const string Edit = "academic.faculties.edit";
        public const string Delete = "academic.faculties.delete";
    }

    public static IReadOnlyCollection<string> All => new[]
    {
        Auth.Login,
        Auth.ManageSessions,
        Auth.ChangePassword,
        Users.View,
        Users.Manage,
        Users.AssignRoles,
        Roles.View,
        Roles.Manage,
        Faculties.View,
        Faculties.Create,
        Faculties.Edit,
        Faculties.Delete
    };

    public static string BuildPolicy(string permission) => $"{PolicyPrefix}{permission}";
}
