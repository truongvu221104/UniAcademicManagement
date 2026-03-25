using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using UniAcademic.AdminApp.Services.Auth;
using UniAcademic.AdminApp.Services.Attendance;
using UniAcademic.AdminApp.Services.CourseOfferings;
using UniAcademic.AdminApp.Services.Courses;
using UniAcademic.AdminApp.Services.Enrollments;
using UniAcademic.AdminApp.Services.Faculties;
using UniAcademic.AdminApp.Services.Grades;
using UniAcademic.AdminApp.Services.GradeResults;
using UniAcademic.AdminApp.Services.LecturerAssignments;
using UniAcademic.AdminApp.Services.LecturerProfiles;
using UniAcademic.AdminApp.Services.Materials;
using UniAcademic.AdminApp.Services.Rosters;
using UniAcademic.AdminApp.Services.Semesters;
using UniAcademic.AdminApp.Services.StudentClasses;
using UniAcademic.AdminApp.Services.StudentProfiles;
using UniAcademic.AdminApp.Dialogs;
using UniAcademic.AdminApp.Navigation;
using UniAcademic.AdminApp.ViewModels;

namespace UniAcademic.AdminApp;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var shellWindow = _serviceProvider.GetRequiredService<ShellWindow>();
        MainWindow = shellWindow;
        shellWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ShellWindow>();
        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<AdminModuleCatalog>();
        services.AddSingleton<IMessageDialogService, MessageDialogService>();
        services.AddSingleton<IFormDialogService, FormDialogService>();
        services.AddSingleton<ITextEditorDialogService, TextEditorDialogService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();

        services.AddSingleton<ITokenStore, ProtectedTokenStore>();
        services.AddSingleton<IAuthSessionService, AuthSessionService>();
        services.AddTransient<BearerTokenHandler>();

        services.AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7271/");
        });

        services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri("https://localhost:7271/");
        }).AddHttpMessageHandler<BearerTokenHandler>();

        services.AddHttpClient<IFacultyApiClient, FacultyApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7271/");
        }).AddHttpMessageHandler<BearerTokenHandler>();

        services.AddHttpClient<IStudentClassApiClient, StudentClassApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7271/");
        }).AddHttpMessageHandler<BearerTokenHandler>();

        services.AddHttpClient<ICourseApiClient, CourseApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7271/");
        }).AddHttpMessageHandler<BearerTokenHandler>();

        services.AddHttpClient<ISemesterApiClient, SemesterApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7271/");
        }).AddHttpMessageHandler<BearerTokenHandler>();

        services.AddHttpClient<ICourseOfferingApiClient, CourseOfferingApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7271/");
        }).AddHttpMessageHandler<BearerTokenHandler>();

        services.AddHttpClient<IStudentProfileApiClient, StudentProfileApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7271/");
        }).AddHttpMessageHandler<BearerTokenHandler>();

        services.AddHttpClient<IEnrollmentApiClient, EnrollmentApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7271/");
        }).AddHttpMessageHandler<BearerTokenHandler>();

        services.AddHttpClient<ICourseOfferingRosterApiClient, CourseOfferingRosterApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7271/");
        }).AddHttpMessageHandler<BearerTokenHandler>();

        services.AddHttpClient<IAttendanceApiClient, AttendanceApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7271/");
        }).AddHttpMessageHandler<BearerTokenHandler>();

        services.AddHttpClient<IGradeApiClient, GradeApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7271/");
        }).AddHttpMessageHandler<BearerTokenHandler>();

        services.AddHttpClient<IGradeResultApiClient, GradeResultApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7271/");
        }).AddHttpMessageHandler<BearerTokenHandler>();

        services.AddHttpClient<ILecturerProfileApiClient, LecturerProfileApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7271/");
        }).AddHttpMessageHandler<BearerTokenHandler>();

        services.AddHttpClient<ILecturerAssignmentApiClient, LecturerAssignmentApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7271/");
        }).AddHttpMessageHandler<BearerTokenHandler>();

        services.AddHttpClient<ICourseMaterialApiClient, CourseMaterialApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7271/");
        }).AddHttpMessageHandler<BearerTokenHandler>();
    }
}
