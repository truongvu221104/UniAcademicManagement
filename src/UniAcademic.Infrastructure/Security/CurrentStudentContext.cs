using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Common;

namespace UniAcademic.Infrastructure.Security;

public sealed class CurrentStudentContext : ICurrentStudentContext
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CurrentStudentContext(IAppDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Guid?> GetStudentProfileIdAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return null;
        }

        var mappedProfile = await _dbContext.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(x => x.StudentProfile)
            .Where(x => x.Id == _currentUser.UserId.Value)
            .Select(x => new
            {
                x.StudentProfileId,
                StudentProfileDeleted = x.StudentProfile != null && x.StudentProfile.IsDeleted
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (mappedProfile is null || !mappedProfile.StudentProfileId.HasValue || mappedProfile.StudentProfileDeleted)
        {
            return null;
        }

        return mappedProfile.StudentProfileId.Value;
    }

    public async Task<Guid> GetRequiredStudentProfileIdAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AuthException("Current user is not authenticated.");
        }

        var mappedProfile = await _dbContext.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(x => x.StudentProfile)
            .Where(x => x.Id == _currentUser.UserId.Value)
            .Select(x => new
            {
                x.StudentProfileId,
                StudentProfileDeleted = x.StudentProfile != null && x.StudentProfile.IsDeleted
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (mappedProfile is null || !mappedProfile.StudentProfileId.HasValue)
        {
            throw new AuthException("Current user is not mapped to a student profile.");
        }

        if (mappedProfile.StudentProfileDeleted)
        {
            throw new AuthException("Current student profile was not found.");
        }

        return mappedProfile.StudentProfileId.Value;
    }
}
