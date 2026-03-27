using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Chat;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.Persistence;
using UniAcademic.Application.Common;
using UniAcademic.Application.Models.Chat;
using UniAcademic.Domain.Entities.Identity;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;

namespace UniAcademic.Application.Features.Chat;

public sealed class CourseChatService : ICourseChatService
{
    private const int MaxMessageLength = 2000;
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CourseChatService(IAppDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<CourseChatRoomItemModel>> GetMyRoomsAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        var rooms = BuildAccessibleCourseOfferingsQuery(user);

        return await rooms
            .OrderBy(x => x.Code)
            .Select(x => new CourseChatRoomItemModel
            {
                CourseOfferingId = x.Id,
                CourseOfferingCode = x.Code,
                CourseName = x.Course != null ? x.Course.Name : string.Empty,
                SemesterName = x.Semester != null ? x.Semester.Name : string.Empty,
                DisplayName = x.DisplayName,
                MessageCount = _dbContext.CourseChatMessages.Count(m => m.CourseOfferingId == x.Id),
                LatestMessagePreview = _dbContext.CourseChatMessages
                    .Where(m => m.CourseOfferingId == x.Id)
                    .OrderByDescending(m => m.CreatedAtUtc)
                    .Select(m => m.MessageText)
                    .FirstOrDefault(),
                LatestSenderDisplayName = _dbContext.CourseChatMessages
                    .Where(m => m.CourseOfferingId == x.Id)
                    .OrderByDescending(m => m.CreatedAtUtc)
                    .Select(m => m.SenderDisplayName)
                    .FirstOrDefault(),
                LatestMessageAtUtc = _dbContext.CourseChatMessages
                    .Where(m => m.CourseOfferingId == x.Id)
                    .OrderByDescending(m => m.CreatedAtUtc)
                    .Select(m => (DateTime?)m.CreatedAtUtc)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CourseChatConversationModel> GetConversationAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        await EnsureAccessibleCourseOfferingAsync(user, courseOfferingId, cancellationToken);

        var offering = await _dbContext.CourseOfferings
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Semester)
            .Where(x => x.Id == courseOfferingId)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.DisplayName,
                CourseName = x.Course != null ? x.Course.Name : string.Empty,
                SemesterName = x.Semester != null ? x.Semester.Name : string.Empty
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AuthException("Course offering was not found.");

        var userId = _currentUser.UserId ?? throw new AuthException("Current user was not found.");
        var messages = await _dbContext.CourseChatMessages
            .AsNoTracking()
            .Where(x => x.CourseOfferingId == courseOfferingId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .Select(x => new CourseChatMessageModel
            {
                Id = x.Id,
                CourseOfferingId = x.CourseOfferingId,
                SenderUserId = x.SenderUserId,
                SenderDisplayName = x.SenderDisplayName,
                SenderRole = x.SenderRole,
                MessageText = x.MessageText,
                SentAtUtc = x.CreatedAtUtc,
                IsMine = x.SenderUserId == userId
            })
            .ToListAsync(cancellationToken);

        messages.Reverse();

        return new CourseChatConversationModel
        {
            CourseOfferingId = offering.Id,
            CourseOfferingCode = offering.Code,
            CourseName = offering.CourseName,
            SemesterName = offering.SemesterName,
            DisplayName = offering.DisplayName,
            CurrentUserId = userId,
            CurrentUserDisplayName = ResolveDisplayName(user),
            CurrentUserRole = await ResolveRoleForCourseOfferingAsync(user, courseOfferingId, cancellationToken),
            Messages = messages
        };
    }

    public async Task<CourseChatMessageModel> SendMessageAsync(SendCourseChatMessageCommand command, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new AuthException("Current user was not found.");
        return await SendMessageAsync(userId, _currentUser.Username, command, cancellationToken);
    }

    public async Task<CourseChatMessageModel> SendMessageAsync(Guid? userId, string? username, SendCourseChatMessageCommand command, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, username, cancellationToken);
        await EnsureAccessibleCourseOfferingAsync(user, command.CourseOfferingId, cancellationToken);

        var trimmedMessage = (command.MessageText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedMessage))
        {
            throw new AuthException("Message text is required.");
        }

        if (trimmedMessage.Length > MaxMessageLength)
        {
            throw new AuthException($"Message text cannot exceed {MaxMessageLength} characters.");
        }

        var senderDisplayName = ResolveDisplayName(user);
        var senderRole = await ResolveRoleForCourseOfferingAsync(user, command.CourseOfferingId, cancellationToken);
        var createdBy = user.Username;

        var message = new CourseChatMessage
        {
            CourseOfferingId = command.CourseOfferingId,
            SenderUserId = user.Id,
            SenderUsername = user.Username,
            SenderDisplayName = senderDisplayName,
            SenderRole = senderRole,
            MessageText = trimmedMessage,
            CreatedBy = createdBy
        };

        await _dbContext.AddAsync(message, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CourseChatMessageModel
        {
            Id = message.Id,
            CourseOfferingId = message.CourseOfferingId,
            SenderUserId = message.SenderUserId,
            SenderDisplayName = message.SenderDisplayName,
            SenderRole = message.SenderRole,
            MessageText = message.MessageText,
            SentAtUtc = message.CreatedAtUtc,
            IsMine = true
        };
    }

    public async Task<bool> CanAccessCourseOfferingAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return await IsAccessibleCourseOfferingAsync(user, courseOfferingId, cancellationToken);
    }

    public async Task<bool> CanAccessCourseOfferingAsync(Guid? userId, string? username, Guid courseOfferingId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, username, cancellationToken);
        return await IsAccessibleCourseOfferingAsync(user, courseOfferingId, cancellationToken);
    }

    private IQueryable<CourseOffering> BuildAccessibleCourseOfferingsQuery(User user)
    {
        var studentOfferingIds = user.StudentProfileId.HasValue
            ? _dbContext.Enrollments
                .Where(x => x.StudentProfileId == user.StudentProfileId.Value && x.Status == EnrollmentStatus.Enrolled)
                .Select(x => x.CourseOfferingId)
            : _dbContext.Enrollments
                .Where(static _ => false)
                .Select(x => x.CourseOfferingId);

        var lecturerOfferingIds = user.LecturerProfileId.HasValue
            ? _dbContext.LecturerAssignments
                .Where(x => x.LecturerProfileId == user.LecturerProfileId.Value)
                .Select(x => x.CourseOfferingId)
            : _dbContext.LecturerAssignments
                .Where(static _ => false)
                .Select(x => x.CourseOfferingId);

        return _dbContext.CourseOfferings
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Semester)
            .Where(x => studentOfferingIds.Contains(x.Id) || lecturerOfferingIds.Contains(x.Id));
    }

    private async Task<User> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new AuthException("Current user was not found.");

        return await GetUserAsync(userId, _currentUser.Username, cancellationToken);
    }

    private async Task<User> GetUserAsync(Guid? userId, string? username, CancellationToken cancellationToken)
    {
        if (userId.HasValue)
        {
            var byId = await _dbContext.Users
                .AsNoTracking()
                .Include(x => x.StudentProfile)
                .Include(x => x.LecturerProfile)
                .FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);

            if (byId is not null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(username))
        {
            var byUsername = await _dbContext.Users
                .AsNoTracking()
                .Include(x => x.StudentProfile)
                .Include(x => x.LecturerProfile)
                .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);

            if (byUsername is not null)
            {
                return byUsername;
            }
        }

        throw new AuthException("Current user was not found.");
    }

    private async Task<User> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.StudentProfile)
            .Include(x => x.LecturerProfile)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new AuthException("Current user was not found.");
    }

    private async Task EnsureAccessibleCourseOfferingAsync(User user, Guid courseOfferingId, CancellationToken cancellationToken)
    {
        if (!await IsAccessibleCourseOfferingAsync(user, courseOfferingId, cancellationToken))
        {
            throw new AuthException("Current user does not have access to this class conversation.");
        }
    }

    private async Task<bool> IsAccessibleCourseOfferingAsync(User user, Guid courseOfferingId, CancellationToken cancellationToken)
    {
        if (user.StudentProfileId.HasValue)
        {
            var hasEnrollment = await _dbContext.Enrollments
                .AnyAsync(x =>
                    x.StudentProfileId == user.StudentProfileId.Value
                    && x.CourseOfferingId == courseOfferingId
                    && x.Status == EnrollmentStatus.Enrolled,
                    cancellationToken);

            if (hasEnrollment)
            {
                return true;
            }
        }

        if (user.LecturerProfileId.HasValue)
        {
            var hasAssignment = await _dbContext.LecturerAssignments
                .AnyAsync(x =>
                    x.LecturerProfileId == user.LecturerProfileId.Value
                    && x.CourseOfferingId == courseOfferingId,
                    cancellationToken);

            if (hasAssignment)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<string> ResolveRoleForCourseOfferingAsync(User user, Guid courseOfferingId, CancellationToken cancellationToken)
    {
        if (user.LecturerProfileId.HasValue)
        {
            var isAssigned = await _dbContext.LecturerAssignments
                .AnyAsync(x =>
                    x.LecturerProfileId == user.LecturerProfileId.Value
                    && x.CourseOfferingId == courseOfferingId,
                    cancellationToken);

            if (isAssigned)
            {
                return "Lecturer";
            }
        }

        if (user.StudentProfileId.HasValue)
        {
            var isEnrolled = await _dbContext.Enrollments
                .AnyAsync(x =>
                    x.StudentProfileId == user.StudentProfileId.Value
                    && x.CourseOfferingId == courseOfferingId
                    && x.Status == EnrollmentStatus.Enrolled,
                    cancellationToken);

            if (isEnrolled)
            {
                return "Student";
            }
        }

        return "User";
    }

    private static string ResolveDisplayName(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.StudentProfile?.FullName))
        {
            return user.StudentProfile.FullName;
        }

        if (!string.IsNullOrWhiteSpace(user.LecturerProfile?.FullName))
        {
            return user.LecturerProfile.FullName;
        }

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            return user.DisplayName;
        }

        return user.Username;
    }
}
