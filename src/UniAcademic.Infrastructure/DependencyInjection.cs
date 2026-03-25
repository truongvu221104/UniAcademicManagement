using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.LecturerPortal;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Abstractions.Storage;
using UniAcademic.Application.Abstractions.StudentPortal;
using UniAcademic.Infrastructure.Options;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Security;
using UniAcademic.Infrastructure.Seed;
using UniAcademic.Infrastructure.SeedData;
using UniAcademic.Infrastructure.SeedData.Services;
using UniAcademic.Infrastructure.Services.Auth;
using UniAcademic.Infrastructure.Services.Common;
using UniAcademic.Infrastructure.Storage;

namespace UniAcademic.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<BootstrapAdminOptions>(configuration.GetSection(BootstrapAdminOptions.SectionName));
        services.Configure<SeedDataOptions>(configuration.GetSection(SeedDataOptions.SectionName));
        services.Configure<LocalFileStorageOptions>(configuration.GetSection(LocalFileStorageOptions.SectionName));

        services.AddHttpContextAccessor();

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DB")));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILocalFileStorage, LocalFileStorage>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<ICurrentUser, CurrentUserAccessor>();
        services.AddScoped<ICurrentStudentContext, CurrentStudentContext>();
        services.AddScoped<ICurrentLecturerContext, CurrentLecturerContext>();
        services.AddScoped<IClientContextAccessor, ClientContextAccessor>();
        services.AddScoped<AuthSeedData>();
        services.AddScoped<JsonSeedDataFileReader>();
        services.AddSingleton<DatasetHashService>();
        services.AddScoped<FacultyDatasetSynchronizer>();
        services.AddScoped<SeedDataBootstrapService>();

        return services;
    }

    public static JwtOptions GetJwtOptions(this IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<JwtOptions>>().Value;
    }
}
