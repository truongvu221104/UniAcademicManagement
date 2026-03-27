using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UniAcademic.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer("server=localhost;database=UniAcademicManagementDb;uid=sa;pwd=123;Encrypt=False;TrustServerCertificate=True");
        return new AppDbContext(optionsBuilder.Options);
    }
}
