using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Common;

namespace UniAcademic.Infrastructure.Security;

public sealed class CurrentLecturerContext : ICurrentLecturerContext
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CurrentLecturerContext(IAppDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Guid?> GetLecturerProfileIdAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return null;
        }

        var mappedProfile = await _dbContext.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(x => x.LecturerProfile)
            .Where(x => x.Id == _currentUser.UserId.Value)
            .Select(x => new
            {
                x.LecturerProfileId,
                LecturerProfileDeleted = x.LecturerProfile != null && x.LecturerProfile.IsDeleted,
                LecturerProfileActive = x.LecturerProfile != null && x.LecturerProfile.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (mappedProfile is null || !mappedProfile.LecturerProfileId.HasValue || mappedProfile.LecturerProfileDeleted || !mappedProfile.LecturerProfileActive)
        {
            return null;
        }

        return mappedProfile.LecturerProfileId.Value;
    }

    public async Task<Guid> GetRequiredLecturerProfileIdAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AuthException("Current user is not authenticated.");
        }

        var mappedProfile = await _dbContext.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(x => x.LecturerProfile)
            .Where(x => x.Id == _currentUser.UserId.Value)
            .Select(x => new
            {
                x.LecturerProfileId,
                LecturerProfileDeleted = x.LecturerProfile != null && x.LecturerProfile.IsDeleted,
                LecturerProfileActive = x.LecturerProfile != null && x.LecturerProfile.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (mappedProfile is null || !mappedProfile.LecturerProfileId.HasValue)
        {
            throw new AuthException("Current user is not mapped to a lecturer profile.");
        }

        if (mappedProfile.LecturerProfileDeleted)
        {
            throw new AuthException("Current lecturer profile was not found.");
        }

        if (!mappedProfile.LecturerProfileActive)
        {
            throw new AuthException("Current lecturer profile is inactive.");
        }

        return mappedProfile.LecturerProfileId.Value;
    }
}
