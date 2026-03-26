using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Entities.Identity;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Persistence.SeedData;
using UniAcademic.Infrastructure.SeedData.Models;

namespace UniAcademic.Infrastructure.SeedData.Services;

public sealed class DemoFoundationDatasetSynchronizer
{
    public const string DatasetName = "academic.demo-foundation";

    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public DemoFoundationDatasetSynchronizer(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<bool> SynchronizeAsync(
        string filePath,
        string fileHash,
        DemoFoundationSeedData data,
        CancellationToken cancellationToken = default)
    {
        var datasetState = await _dbContext.SeedDatasetStates
            .FirstOrDefaultAsync(x => x.DatasetName == DatasetName, cancellationToken);

        if (datasetState is not null && string.Equals(datasetState.FileHash, fileHash, StringComparison.Ordinal))
        {
            return false;
        }

        ValidateDuplicates(data);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var facultiesByCode = await LoadFacultiesAsync(data, cancellationToken);
        var studentClassesByCode = await SynchronizeStudentClassesAsync(data.StudentClasses, facultiesByCode, cancellationToken);
        var coursesByCode = await SynchronizeCoursesAsync(data.Courses, facultiesByCode, cancellationToken);
        var semestersByCode = await SynchronizeSemestersAsync(data.Semesters, cancellationToken);
        var studentProfilesByCode = await SynchronizeStudentProfilesAsync(data.StudentProfiles, studentClassesByCode, cancellationToken);
        var lecturerProfilesByCode = await SynchronizeLecturerProfilesAsync(data.LecturerProfiles, facultiesByCode, cancellationToken);
        var courseOfferingsByCode = await SynchronizeCourseOfferingsAsync(data.CourseOfferings, coursesByCode, semestersByCode, cancellationToken);
        await SynchronizeLecturerAssignmentsAsync(data.LecturerAssignments, courseOfferingsByCode, lecturerProfilesByCode, cancellationToken);
        await SynchronizeUsersAsync(data.Users, studentProfilesByCode, lecturerProfilesByCode, cancellationToken);

        if (datasetState is null)
        {
            datasetState = new SeedDatasetState
            {
                DatasetName = DatasetName
            };

            await _dbContext.SeedDatasetStates.AddAsync(datasetState, cancellationToken);
        }

        datasetState.FilePath = filePath;
        datasetState.FileHash = fileHash;
        datasetState.AppliedAtUtc = DateTime.UtcNow;
        datasetState.Status = "Applied";

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<Dictionary<string, Faculty>> LoadFacultiesAsync(
        DemoFoundationSeedData data,
        CancellationToken cancellationToken)
    {
        var facultyCodes = data.StudentClasses.Select(x => NormalizeRequired(x.FacultyCode, "StudentClass.FacultyCode"))
            .Concat(data.Courses.Where(x => !string.IsNullOrWhiteSpace(x.FacultyCode)).Select(x => NormalizeRequired(x.FacultyCode!, "Course.FacultyCode")))
            .Concat(data.LecturerProfiles.Select(x => NormalizeRequired(x.FacultyCode, "LecturerProfile.FacultyCode")))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var facultiesByCode = await _dbContext.FacultiesSet
            .IgnoreQueryFilters()
            .Where(x => facultyCodes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var facultyCode in facultyCodes)
        {
            if (!facultiesByCode.ContainsKey(facultyCode))
            {
                throw new InvalidOperationException($"Demo seed requires faculty '{facultyCode}' to exist.");
            }
        }

        return facultiesByCode;
    }

    private async Task<Dictionary<string, StudentClass>> SynchronizeStudentClassesAsync(
        IReadOnlyCollection<StudentClassSeedItem> items,
        IReadOnlyDictionary<string, Faculty> facultiesByCode,
        CancellationToken cancellationToken)
    {
        var codes = items.Select(x => NormalizeRequired(x.Code, "StudentClass.Code")).ToList();
        var entitiesByCode = await _dbContext.StudentClassesSet
            .IgnoreQueryFilters()
            .Where(x => codes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var item in items)
        {
            var code = NormalizeRequired(item.Code, "StudentClass.Code");
            var isNew = !entitiesByCode.TryGetValue(code, out var entity);
            entity ??= new StudentClass
            {
                Code = code,
                CreatedBy = "seed-data"
            };

            if (isNew)
            {
                await _dbContext.StudentClassesSet.AddAsync(entity, cancellationToken);
                entitiesByCode[code] = entity;
            }

            entity.Name = NormalizeRequired(item.Name, "StudentClass.Name");
            entity.FacultyId = facultiesByCode[NormalizeRequired(item.FacultyCode, "StudentClass.FacultyCode")].Id;
            entity.IntakeYear = item.IntakeYear;
            entity.Status = ParseEnum<StudentClassStatus>(item.Status, "StudentClass.Status");
            entity.Description = NormalizeOptional(item.Description);
            entity.IsDeleted = false;
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
            entity.ModifiedBy = "seed-data";
        }

        return entitiesByCode;
    }

    private async Task<Dictionary<string, Course>> SynchronizeCoursesAsync(
        IReadOnlyCollection<CourseSeedItem> items,
        IReadOnlyDictionary<string, Faculty> facultiesByCode,
        CancellationToken cancellationToken)
    {
        var codes = items.Select(x => NormalizeRequired(x.Code, "Course.Code")).ToList();
        var entitiesByCode = await _dbContext.CoursesSet
            .IgnoreQueryFilters()
            .Where(x => codes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var item in items)
        {
            var code = NormalizeRequired(item.Code, "Course.Code");
            var isNew = !entitiesByCode.TryGetValue(code, out var entity);
            entity ??= new Course
            {
                Code = code,
                CreatedBy = "seed-data"
            };

            if (isNew)
            {
                await _dbContext.CoursesSet.AddAsync(entity, cancellationToken);
                entitiesByCode[code] = entity;
            }

            entity.Name = NormalizeRequired(item.Name, "Course.Name");
            entity.Credits = item.Credits;
            entity.FacultyId = string.IsNullOrWhiteSpace(item.FacultyCode)
                ? null
                : facultiesByCode[NormalizeRequired(item.FacultyCode!, "Course.FacultyCode")].Id;
            entity.Status = ParseEnum<CourseStatus>(item.Status, "Course.Status");
            entity.Description = NormalizeOptional(item.Description);
            entity.IsDeleted = false;
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
            entity.ModifiedBy = "seed-data";
        }

        return entitiesByCode;
    }

    private async Task<Dictionary<string, Semester>> SynchronizeSemestersAsync(
        IReadOnlyCollection<SemesterSeedItem> items,
        CancellationToken cancellationToken)
    {
        var codes = items.Select(x => NormalizeRequired(x.Code, "Semester.Code")).ToList();
        var entitiesByCode = await _dbContext.SemestersSet
            .IgnoreQueryFilters()
            .Where(x => codes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var item in items)
        {
            var code = NormalizeRequired(item.Code, "Semester.Code");
            var isNew = !entitiesByCode.TryGetValue(code, out var entity);
            entity ??= new Semester
            {
                Code = code,
                CreatedBy = "seed-data"
            };

            if (isNew)
            {
                await _dbContext.SemestersSet.AddAsync(entity, cancellationToken);
                entitiesByCode[code] = entity;
            }

            entity.Name = NormalizeRequired(item.Name, "Semester.Name");
            entity.AcademicYear = NormalizeRequired(item.AcademicYear, "Semester.AcademicYear");
            entity.TermNo = item.TermNo;
            entity.StartDate = item.StartDate;
            entity.EndDate = item.EndDate;
            entity.Status = ParseEnum<SemesterStatus>(item.Status, "Semester.Status");
            entity.Description = NormalizeOptional(item.Description);
            entity.IsDeleted = false;
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
            entity.ModifiedBy = "seed-data";
        }

        return entitiesByCode;
    }

    private async Task<Dictionary<string, StudentProfile>> SynchronizeStudentProfilesAsync(
        IReadOnlyCollection<StudentProfileSeedItem> items,
        IReadOnlyDictionary<string, StudentClass> studentClassesByCode,
        CancellationToken cancellationToken)
    {
        var codes = items.Select(x => NormalizeRequired(x.StudentCode, "StudentProfile.StudentCode")).ToList();
        var entitiesByCode = await _dbContext.StudentProfilesSet
            .IgnoreQueryFilters()
            .Where(x => codes.Contains(x.StudentCode))
            .ToDictionaryAsync(x => x.StudentCode, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var item in items)
        {
            var code = NormalizeRequired(item.StudentCode, "StudentProfile.StudentCode");
            var isNew = !entitiesByCode.TryGetValue(code, out var entity);
            entity ??= new StudentProfile
            {
                StudentCode = code,
                CreatedBy = "seed-data"
            };

            if (isNew)
            {
                await _dbContext.StudentProfilesSet.AddAsync(entity, cancellationToken);
                entitiesByCode[code] = entity;
            }

            entity.FullName = NormalizeRequired(item.FullName, "StudentProfile.FullName");
            entity.StudentClassId = studentClassesByCode[NormalizeRequired(item.StudentClassCode, "StudentProfile.StudentClassCode")].Id;
            entity.Email = NormalizeOptional(item.Email);
            entity.Phone = NormalizeOptional(item.Phone);
            entity.Gender = ParseEnum<StudentGender>(item.Gender, "StudentProfile.Gender");
            entity.Status = ParseEnum<StudentProfileStatus>(item.Status, "StudentProfile.Status");
            entity.Note = NormalizeOptional(item.Note);
            entity.IsDeleted = false;
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
            entity.ModifiedBy = "seed-data";
        }

        return entitiesByCode;
    }

    private async Task<Dictionary<string, LecturerProfile>> SynchronizeLecturerProfilesAsync(
        IReadOnlyCollection<LecturerProfileSeedItem> items,
        IReadOnlyDictionary<string, Faculty> facultiesByCode,
        CancellationToken cancellationToken)
    {
        var codes = items.Select(x => NormalizeRequired(x.Code, "LecturerProfile.Code")).ToList();
        var entitiesByCode = await _dbContext.LecturerProfilesSet
            .IgnoreQueryFilters()
            .Where(x => codes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var item in items)
        {
            var code = NormalizeRequired(item.Code, "LecturerProfile.Code");
            var isNew = !entitiesByCode.TryGetValue(code, out var entity);
            entity ??= new LecturerProfile
            {
                Code = code,
                CreatedBy = "seed-data"
            };

            if (isNew)
            {
                await _dbContext.LecturerProfilesSet.AddAsync(entity, cancellationToken);
                entitiesByCode[code] = entity;
            }

            entity.FullName = NormalizeRequired(item.FullName, "LecturerProfile.FullName");
            entity.Email = NormalizeOptional(item.Email);
            entity.PhoneNumber = NormalizeOptional(item.PhoneNumber);
            entity.FacultyId = facultiesByCode[NormalizeRequired(item.FacultyCode, "LecturerProfile.FacultyCode")].Id;
            entity.IsActive = item.IsActive;
            entity.Note = NormalizeOptional(item.Note);
            entity.IsDeleted = false;
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
            entity.ModifiedBy = "seed-data";
        }

        return entitiesByCode;
    }

    private async Task<Dictionary<string, CourseOffering>> SynchronizeCourseOfferingsAsync(
        IReadOnlyCollection<CourseOfferingSeedItem> items,
        IReadOnlyDictionary<string, Course> coursesByCode,
        IReadOnlyDictionary<string, Semester> semestersByCode,
        CancellationToken cancellationToken)
    {
        var codes = items.Select(x => NormalizeRequired(x.Code, "CourseOffering.Code")).ToList();
        var entitiesByCode = await _dbContext.CourseOfferingsSet
            .IgnoreQueryFilters()
            .Where(x => codes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var item in items)
        {
            var code = NormalizeRequired(item.Code, "CourseOffering.Code");
            var isNew = !entitiesByCode.TryGetValue(code, out var entity);
            entity ??= new CourseOffering
            {
                Code = code,
                CreatedBy = "seed-data"
            };

            if (isNew)
            {
                await _dbContext.CourseOfferingsSet.AddAsync(entity, cancellationToken);
                entitiesByCode[code] = entity;
            }

            entity.CourseId = coursesByCode[NormalizeRequired(item.CourseCode, "CourseOffering.CourseCode")].Id;
            entity.SemesterId = semestersByCode[NormalizeRequired(item.SemesterCode, "CourseOffering.SemesterCode")].Id;
            entity.DisplayName = NormalizeRequired(item.DisplayName, "CourseOffering.DisplayName");
            entity.Capacity = item.Capacity;
            entity.Status = ParseEnum<CourseOfferingStatus>(item.Status, "CourseOffering.Status");
            entity.Description = NormalizeOptional(item.Description);
            entity.IsDeleted = false;
            entity.DeletedAtUtc = null;
            entity.DeletedBy = null;
            entity.ModifiedBy = "seed-data";
        }

        return entitiesByCode;
    }

    private async Task SynchronizeLecturerAssignmentsAsync(
        IReadOnlyCollection<LecturerAssignmentSeedItem> items,
        IReadOnlyDictionary<string, CourseOffering> courseOfferingsByCode,
        IReadOnlyDictionary<string, LecturerProfile> lecturerProfilesByCode,
        CancellationToken cancellationToken)
    {
        var offeringIds = courseOfferingsByCode.Values.Select(x => x.Id).ToList();
        var lecturerIds = lecturerProfilesByCode.Values.Select(x => x.Id).ToList();

        var entities = await _dbContext.LecturerAssignmentsSet
            .Where(x => offeringIds.Contains(x.CourseOfferingId) && lecturerIds.Contains(x.LecturerProfileId))
            .ToListAsync(cancellationToken);

        var entitiesByKey = entities.ToDictionary(
            x => BuildAssignmentKey(x.CourseOfferingId, x.LecturerProfileId),
            StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var offering = courseOfferingsByCode[NormalizeRequired(item.CourseOfferingCode, "LecturerAssignment.CourseOfferingCode")];
            var lecturer = lecturerProfilesByCode[NormalizeRequired(item.LecturerCode, "LecturerAssignment.LecturerCode")];
            var key = BuildAssignmentKey(offering.Id, lecturer.Id);

            if (!entitiesByKey.TryGetValue(key, out var entity))
            {
                entity = new LecturerAssignment
                {
                    CourseOfferingId = offering.Id,
                    LecturerProfileId = lecturer.Id,
                    CreatedBy = "seed-data"
                };

                await _dbContext.LecturerAssignmentsSet.AddAsync(entity, cancellationToken);
                entitiesByKey[key] = entity;
            }

            entity.IsPrimary = item.IsPrimary;
            entity.AssignedAtUtc = item.AssignedAtUtc;
            entity.ModifiedBy = "seed-data";
        }
    }

    private async Task SynchronizeUsersAsync(
        IReadOnlyCollection<DemoUserSeedItem> items,
        IReadOnlyDictionary<string, StudentProfile> studentProfilesByCode,
        IReadOnlyDictionary<string, LecturerProfile> lecturerProfilesByCode,
        CancellationToken cancellationToken)
    {
        var usernames = items.Select(x => NormalizeRequired(x.Username, "User.Username").ToUpperInvariant()).ToList();
        var usersByUsername = await _dbContext.Users
            .Where(x => usernames.Contains(x.NormalizedUsername))
            .ToDictionaryAsync(x => x.NormalizedUsername, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var superAdminRole = await _dbContext.Roles
            .FirstOrDefaultAsync(x => x.NormalizedName == "SUPERADMIN", cancellationToken)
            ?? throw new InvalidOperationException("Demo seed requires the SUPERADMIN role to exist.");

        foreach (var item in items)
        {
            var username = NormalizeRequired(item.Username, "User.Username");
            var normalizedUsername = username.ToUpperInvariant();

            if (!usersByUsername.TryGetValue(normalizedUsername, out var entity))
            {
                entity = new User
                {
                    Username = username,
                    NormalizedUsername = normalizedUsername,
                    CreatedBy = "seed-data"
                };

                await _dbContext.Users.AddAsync(entity, cancellationToken);
                usersByUsername[normalizedUsername] = entity;
            }

            entity.Email = NormalizeRequired(item.Email, "User.Email");
            entity.NormalizedEmail = entity.Email.ToUpperInvariant();
            entity.DisplayName = NormalizeRequired(item.DisplayName, "User.DisplayName");
            entity.PasswordHash = _passwordHasher.HashPassword(NormalizeRequired(item.Password, "User.Password"));
            entity.StudentProfileId = string.IsNullOrWhiteSpace(item.StudentCode)
                ? null
                : studentProfilesByCode[NormalizeRequired(item.StudentCode!, "User.StudentCode")].Id;
            entity.LecturerProfileId = string.IsNullOrWhiteSpace(item.LecturerCode)
                ? null
                : lecturerProfilesByCode[NormalizeRequired(item.LecturerCode!, "User.LecturerCode")].Id;
            entity.IsActive = item.IsActive;
            entity.IsLocked = false;
            entity.FailedLoginCount = 0;
            entity.LockoutEndUtc = null;
            entity.ModifiedBy = "seed-data";

            var hasRole = await _dbContext.UserRoles.AnyAsync(
                x => x.UserId == entity.Id && x.RoleId == superAdminRole.Id,
                cancellationToken);

            if (!hasRole)
            {
                await _dbContext.UserRoles.AddAsync(new UserRole
                {
                    UserId = entity.Id,
                    RoleId = superAdminRole.Id
                }, cancellationToken);
            }
        }
    }

    private static void ValidateDuplicates(DemoFoundationSeedData data)
    {
        EnsureNoDuplicates(data.Users.Select(x => x.Username), "User.Username");
        EnsureNoDuplicates(data.StudentClasses.Select(x => x.Code), "StudentClass.Code");
        EnsureNoDuplicates(data.Courses.Select(x => x.Code), "Course.Code");
        EnsureNoDuplicates(data.Semesters.Select(x => x.Code), "Semester.Code");
        EnsureNoDuplicates(data.StudentProfiles.Select(x => x.StudentCode), "StudentProfile.StudentCode");
        EnsureNoDuplicates(data.LecturerProfiles.Select(x => x.Code), "LecturerProfile.Code");
        EnsureNoDuplicates(data.CourseOfferings.Select(x => x.Code), "CourseOffering.Code");
        EnsureNoDuplicates(
            data.LecturerAssignments.Select(x => $"{x.CourseOfferingCode}::{x.LecturerCode}"),
            "LecturerAssignment.CourseOfferingCode::LecturerCode");
    }

    private static void EnsureNoDuplicates(IEnumerable<string> values, string fieldName)
    {
        var duplicates = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException($"Demo seed contains duplicate values for {fieldName}: {string.Join(", ", duplicates)}");
        }
    }

    private static string BuildAssignmentKey(Guid courseOfferingId, Guid lecturerProfileId)
        => $"{courseOfferingId:N}:{lecturerProfileId:N}";

    private static string NormalizeRequired(string value, string fieldName)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException($"{fieldName} is required.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static TEnum ParseEnum<TEnum>(string value, string fieldName)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(NormalizeRequired(value, fieldName), true, out var parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"{fieldName} value '{value}' is invalid.");
    }
}
