namespace UniAcademic.Application.Security;

public static class RoleConstants
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Staff = "Staff";
    public const string Student = "Student";
    public const string Lecturer = "Lecturer";

    public const string AcademicManagement = SuperAdmin + "," + Staff;
}
