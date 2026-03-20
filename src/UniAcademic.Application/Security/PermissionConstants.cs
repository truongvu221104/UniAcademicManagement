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

    public static class StudentClasses
    {
        public const string View = "academic.studentclasses.view";
        public const string Create = "academic.studentclasses.create";
        public const string Edit = "academic.studentclasses.edit";
        public const string Delete = "academic.studentclasses.delete";
    }

    public static class Courses
    {
        public const string View = "academic.courses.view";
        public const string Create = "academic.courses.create";
        public const string Edit = "academic.courses.edit";
        public const string Delete = "academic.courses.delete";
    }

    public static class Semesters
    {
        public const string View = "academic.semesters.view";
        public const string Create = "academic.semesters.create";
        public const string Edit = "academic.semesters.edit";
        public const string Delete = "academic.semesters.delete";
    }

    public static class CourseOfferings
    {
        public const string View = "academic.courseofferings.view";
        public const string Create = "academic.courseofferings.create";
        public const string Edit = "academic.courseofferings.edit";
        public const string Delete = "academic.courseofferings.delete";
    }

    public static class StudentProfiles
    {
        public const string View = "academic.studentprofiles.view";
        public const string Create = "academic.studentprofiles.create";
        public const string Edit = "academic.studentprofiles.edit";
        public const string Delete = "academic.studentprofiles.delete";
    }

    public static class Enrollments
    {
        public const string View = "academic.enrollments.view";
        public const string Create = "academic.enrollments.create";
        public const string Delete = "academic.enrollments.delete";
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
        Faculties.Delete,
        StudentClasses.View,
        StudentClasses.Create,
        StudentClasses.Edit,
        StudentClasses.Delete,
        Courses.View,
        Courses.Create,
        Courses.Edit,
        Courses.Delete,
        Semesters.View,
        Semesters.Create,
        Semesters.Edit,
        Semesters.Delete,
        CourseOfferings.View,
        CourseOfferings.Create,
        CourseOfferings.Edit,
        CourseOfferings.Delete,
        StudentProfiles.View,
        StudentProfiles.Create,
        StudentProfiles.Edit,
        StudentProfiles.Delete,
        Enrollments.View,
        Enrollments.Create,
        Enrollments.Delete
    };

    public static string BuildPolicy(string permission) => $"{PolicyPrefix}{permission}";
}
