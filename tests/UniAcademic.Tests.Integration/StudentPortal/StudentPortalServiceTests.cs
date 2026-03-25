using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.Storage;
using UniAcademic.Application.Common;
using UniAcademic.Application.Features.Materials;
using UniAcademic.Application.Features.StudentPortal;
using UniAcademic.Application.Models.StudentPortal;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.StudentPortal;

public sealed class StudentPortalServiceTests
{
    [Fact]
    public async Task GetMyCourseOfferingsAsync_ShouldReturnOnlyCurrentStudentsOfferings()
    {
        await using var dbContext = CreateDbContext();
        var studentA = await SeedStudentProfileAsync(dbContext, "SV001");
        var studentB = await SeedStudentProfileAsync(dbContext, "SV002");
        var offeringA = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var offeringB = await SeedCourseOfferingAsync(dbContext, "CO-02");

        await SeedEnrollmentAsync(dbContext, studentA.Id, offeringA.Id);
        await SeedEnrollmentAsync(dbContext, studentB.Id, offeringB.Id);

        var service = CreateStudentPortalService(dbContext, studentA.Id);
        var result = await service.GetMyCourseOfferingsAsync(new GetMyCourseOfferingsQuery());

        Assert.Single(result);
        Assert.Equal(offeringA.Id, result.Single().Id);
    }

    [Fact]
    public async Task GetMyAttendanceAsync_ShouldNotReturnAnotherStudentsAttendance()
    {
        await using var dbContext = CreateDbContext();
        var offering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, offering, ["SV001", "SV002"]);
        await SeedAttendanceSessionAsync(dbContext, offering, snapshot);

        var currentStudentId = snapshot.Items.Single(x => x.StudentCode == "SV001").StudentProfileId;
        var service = CreateStudentPortalService(dbContext, currentStudentId);
        var result = await service.GetMyAttendanceAsync(new GetMyAttendanceQuery());

        Assert.Single(result);
        Assert.Equal("CO-01", result.Single().CourseOfferingCode);
    }

    [Fact]
    public async Task GetMyGradesAndGradeResultsAsync_ShouldNotReturnAnotherStudentsData()
    {
        await using var dbContext = CreateDbContext();
        var offering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, offering, ["SV001", "SV002"]);
        var category = await SeedGradeCategoryAsync(dbContext, offering, snapshot);
        var rosterItemA = snapshot.Items.Single(x => x.StudentCode == "SV001");
        var rosterItemB = snapshot.Items.Single(x => x.StudentCode == "SV002");

        await SetGradeScoreAsync(dbContext, category.Id, rosterItemA.Id, 9m);
        await SetGradeScoreAsync(dbContext, category.Id, rosterItemB.Id, 6m);
        await SeedGradeResultAsync(dbContext, offering, snapshot, rosterItemA, 90m);
        await SeedGradeResultAsync(dbContext, offering, snapshot, rosterItemB, 60m);

        var service = CreateStudentPortalService(dbContext, rosterItemA.StudentProfileId);
        var grades = await service.GetMyGradesAsync(new GetMyGradesQuery());
        var results = await service.GetMyGradeResultsAsync(new GetMyGradeResultsQuery());

        Assert.Single(grades);
        Assert.Equal(9m, grades.Single().Score);
        Assert.Single(results);
        Assert.Equal(rosterItemA.Id, results.Single().RosterItemId);
    }

    [Fact]
    public async Task GetMyMaterialsAsync_ShouldReturnOnlyPublishedMaterials()
    {
        await using var dbContext = CreateDbContext();
        var student = await SeedStudentProfileAsync(dbContext, "SV001");
        var offering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        await SeedEnrollmentAsync(dbContext, student.Id, offering.Id);
        await SeedCourseMaterialAsync(dbContext, offering.Id, "Published", true);
        await SeedCourseMaterialAsync(dbContext, offering.Id, "Hidden", false);

        var service = CreateStudentPortalService(dbContext, student.Id);
        var result = await service.GetMyMaterialsAsync(new GetMyMaterialsQuery());

        Assert.Single(result);
        Assert.Equal("Published", result.Single().Title);
        Assert.True(result.Single().IsPublished);
    }

    [Fact]
    public async Task GetMyCourseOfferingsAsync_ShouldFail_WhenCurrentUserIsNotMappedToStudent()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateStudentPortalService(dbContext, null);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.GetMyCourseOfferingsAsync(new GetMyCourseOfferingsQuery()));

        Assert.Equal("Current user is not mapped to a student profile.", exception.Message);
    }

    private static StudentPortalService CreateStudentPortalService(AppDbContext dbContext, Guid? studentProfileId)
    {
        var materialService = new CourseMaterialService(
            dbContext,
            new AuditService(dbContext, new FakeClientContextAccessor()),
            new FakeCurrentUser(),
            new FakeDateTimeProvider(),
            new FakeLocalFileStorage());

        return new StudentPortalService(dbContext, new FakeCurrentStudentContext(studentProfileId), materialService);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<Faculty> SeedFacultyAsync(AppDbContext dbContext)
    {
        var faculty = new Faculty
        {
            Code = $"CNTT{dbContext.FacultiesSet.Count() + 1}",
            Name = "Cong nghe thong tin",
            Status = FacultyStatus.Active,
            CreatedBy = "seed"
        };

        dbContext.FacultiesSet.Add(faculty);
        await dbContext.SaveChangesAsync();
        return faculty;
    }

    private static async Task<StudentClass> SeedStudentClassAsync(AppDbContext dbContext)
    {
        var faculty = await SeedFacultyAsync(dbContext);
        var studentClass = new StudentClass
        {
            Code = $"DHKTPM{dbContext.StudentClassesSet.Count() + 1}",
            Name = "KTPM",
            FacultyId = faculty.Id,
            IntakeYear = 2024,
            Status = StudentClassStatus.Active,
            CreatedBy = "seed"
        };

        dbContext.StudentClassesSet.Add(studentClass);
        await dbContext.SaveChangesAsync();
        return studentClass;
    }

    private static async Task<StudentProfile> SeedStudentProfileAsync(AppDbContext dbContext, string studentCode)
    {
        var studentClass = await SeedStudentClassAsync(dbContext);
        var studentProfile = new StudentProfile
        {
            StudentCode = studentCode,
            FullName = $"Sinh vien {studentCode}",
            StudentClassId = studentClass.Id,
            Status = StudentProfileStatus.Active,
            CreatedBy = "seed",
            StudentClass = studentClass
        };

        dbContext.StudentProfilesSet.Add(studentProfile);
        await dbContext.SaveChangesAsync();
        return studentProfile;
    }

    private static async Task<CourseOffering> SeedCourseOfferingAsync(AppDbContext dbContext, string code)
    {
        var course = new Course
        {
            Code = $"CS{100 + dbContext.CoursesSet.Count()}",
            Name = "Nhap mon lap trinh",
            Credits = 3,
            Status = CourseStatus.Active,
            CreatedBy = "seed"
        };
        var semester = new Semester
        {
            Code = $"HK1-2526-{dbContext.SemestersSet.Count() + 1}",
            Name = "Hoc ky 1",
            AcademicYear = "2025-2026",
            TermNo = 1,
            StartDate = new DateTime(2025, 9, 1),
            EndDate = new DateTime(2026, 1, 15),
            Status = SemesterStatus.Active,
            CreatedBy = "seed"
        };

        dbContext.CoursesSet.Add(course);
        dbContext.SemestersSet.Add(semester);
        await dbContext.SaveChangesAsync();

        var offering = new CourseOffering
        {
            Code = code,
            CourseId = course.Id,
            SemesterId = semester.Id,
            DisplayName = $"Offering {code}",
            Capacity = 50,
            Status = CourseOfferingStatus.Active,
            CreatedBy = "seed",
            Course = course,
            Semester = semester
        };

        dbContext.CourseOfferingsSet.Add(offering);
        await dbContext.SaveChangesAsync();
        return offering;
    }

    private static async Task SeedEnrollmentAsync(AppDbContext dbContext, Guid studentProfileId, Guid offeringId)
    {
        dbContext.EnrollmentsSet.Add(new Enrollment
        {
            StudentProfileId = studentProfileId,
            CourseOfferingId = offeringId,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task<CourseOfferingRosterSnapshot> SeedFinalizedRosterAsync(AppDbContext dbContext, CourseOffering offering, IReadOnlyCollection<string> studentCodes)
    {
        var snapshot = new CourseOfferingRosterSnapshot
        {
            CourseOfferingId = offering.Id,
            FinalizedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc),
            FinalizedBy = "seed",
            ItemCount = studentCodes.Count,
            CreatedBy = "seed"
        };

        foreach (var studentCode in studentCodes)
        {
            var studentProfile = await SeedStudentProfileAsync(dbContext, studentCode);
            dbContext.EnrollmentsSet.Add(new Enrollment
            {
                StudentProfileId = studentProfile.Id,
                CourseOfferingId = offering.Id,
                Status = EnrollmentStatus.Enrolled,
                EnrolledAtUtc = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "seed"
            });
            await dbContext.SaveChangesAsync();

            var enrollmentId = await dbContext.EnrollmentsSet
                .Where(x => x.StudentProfileId == studentProfile.Id && x.CourseOfferingId == offering.Id)
                .Select(x => x.Id)
                .SingleAsync();

            snapshot.Items.Add(new CourseOfferingRosterItem
            {
                EnrollmentId = enrollmentId,
                StudentProfileId = studentProfile.Id,
                StudentCode = studentProfile.StudentCode,
                StudentFullName = studentProfile.FullName,
                StudentClassName = studentProfile.StudentClass?.Name ?? string.Empty,
                CourseOfferingCode = offering.Code,
                CourseCode = offering.Course?.Code ?? string.Empty,
                CourseName = offering.Course?.Name ?? string.Empty,
                SemesterName = offering.Semester?.Name ?? string.Empty,
                CreatedBy = "seed"
            });
        }

        offering.IsRosterFinalized = true;
        offering.RosterFinalizedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc);
        dbContext.CourseOfferingRosterSnapshotsSet.Add(snapshot);
        await dbContext.SaveChangesAsync();
        return await dbContext.CourseOfferingRosterSnapshotsSet.Include(x => x.Items).SingleAsync(x => x.Id == snapshot.Id);
    }

    private static async Task SeedAttendanceSessionAsync(AppDbContext dbContext, CourseOffering offering, CourseOfferingRosterSnapshot snapshot)
    {
        var session = new AttendanceSession
        {
            CourseOfferingId = offering.Id,
            CourseOfferingRosterSnapshotId = snapshot.Id,
            SessionDate = new DateTime(2026, 3, 25),
            SessionNo = 1,
            Title = "Buoi 1",
            CreatedBy = "seed"
        };

        foreach (var item in snapshot.Items)
        {
            session.Records.Add(new AttendanceRecord
            {
                RosterItemId = item.Id,
                Status = item.StudentCode == "SV001" ? AttendanceStatus.Present : AttendanceStatus.Absent,
                CreatedBy = "seed"
            });
        }

        dbContext.AttendanceSessionsSet.Add(session);
        await dbContext.SaveChangesAsync();
    }

    private static async Task<GradeCategory> SeedGradeCategoryAsync(AppDbContext dbContext, CourseOffering offering, CourseOfferingRosterSnapshot snapshot)
    {
        var category = new GradeCategory
        {
            CourseOfferingId = offering.Id,
            CourseOfferingRosterSnapshotId = snapshot.Id,
            Name = "Quiz",
            Weight = 100m,
            MaxScore = 10m,
            OrderIndex = 1,
            IsActive = true,
            CreatedBy = "seed"
        };

        foreach (var item in snapshot.Items)
        {
            category.Entries.Add(new GradeEntry
            {
                RosterItemId = item.Id,
                CreatedBy = "seed"
            });
        }

        dbContext.GradeCategoriesSet.Add(category);
        await dbContext.SaveChangesAsync();
        return category;
    }

    private static async Task SetGradeScoreAsync(AppDbContext dbContext, Guid categoryId, Guid rosterItemId, decimal score)
    {
        var entry = await dbContext.GradeEntriesSet.SingleAsync(x => x.GradeCategoryId == categoryId && x.RosterItemId == rosterItemId);
        entry.Score = score;
        entry.Note = "seed";
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedGradeResultAsync(AppDbContext dbContext, CourseOffering offering, CourseOfferingRosterSnapshot snapshot, CourseOfferingRosterItem rosterItem, decimal weightedFinalScore)
    {
        dbContext.GradeResultsSet.Add(new GradeResult
        {
            CourseOfferingId = offering.Id,
            CourseOfferingRosterSnapshotId = snapshot.Id,
            RosterItemId = rosterItem.Id,
            WeightedFinalScore = weightedFinalScore,
            PassingScore = 50m,
            IsPassed = weightedFinalScore >= 50m,
            CalculatedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc),
            CalculatedBy = "seed",
            CreatedBy = "seed"
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedCourseMaterialAsync(AppDbContext dbContext, Guid offeringId, string title, bool isPublished)
    {
        dbContext.CourseMaterialsSet.Add(new CourseMaterial
        {
            CourseOfferingId = offeringId,
            FileMetadata = new FileMetadata
            {
                OriginalFileName = $"{title}.pdf",
                RelativePath = $"storage/course-materials/{offeringId}/{Guid.NewGuid():N}.pdf",
                ContentType = "application/pdf",
                SizeInBytes = 1024,
                UploadedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc),
                UploadedBy = "seed",
                CreatedBy = "seed"
            },
            Title = title,
            MaterialType = CourseMaterialType.Document,
            SortOrder = 0,
            IsPublished = isPublished,
            CreatedBy = "seed"
        });

        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeCurrentStudentContext : ICurrentStudentContext
    {
        private readonly Guid? _studentProfileId;

        public FakeCurrentStudentContext(Guid? studentProfileId)
        {
            _studentProfileId = studentProfileId;
        }

        public Task<Guid?> GetStudentProfileIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(_studentProfileId);

        public Task<Guid> GetRequiredStudentProfileIdAsync(CancellationToken cancellationToken = default)
        {
            if (!_studentProfileId.HasValue)
            {
                throw new AuthException("Current user is not mapped to a student profile.");
            }

            return Task.FromResult(_studentProfileId.Value);
        }
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public string? Username => "student-portal-user";
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions => [PermissionConstants.CourseMaterials.View, PermissionConstants.CourseMaterials.Download];
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "StudentPortalTests";
        public string ClientType => "Tests";
    }

    private sealed class FakeLocalFileStorage : ILocalFileStorage
    {
        public long MaxFileSizeInBytes => 10 * 1024 * 1024;

        public Task<StoredLocalFile> SaveAsync(LocalFileSaveRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StoredLocalFile
            {
                RelativePath = $"storage/course-materials/{request.CourseOfferingId}/{Guid.NewGuid():N}.pdf",
                StoredFileName = $"{Guid.NewGuid():N}.pdf"
            });
        }

        public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream([1, 2, 3]));

        public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
