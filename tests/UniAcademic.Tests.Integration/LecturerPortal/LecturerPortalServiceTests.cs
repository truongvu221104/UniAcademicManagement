using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Attendance;
using UniAcademic.Application.Abstractions.Auth;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.Grades;
using UniAcademic.Application.Abstractions.GradeResults;
using UniAcademic.Application.Abstractions.Materials;
using UniAcademic.Application.Abstractions.Storage;
using UniAcademic.Application.Common;
using UniAcademic.Application.Features.Attendance;
using UniAcademic.Application.Features.Grades;
using UniAcademic.Application.Features.GradeResults;
using UniAcademic.Application.Features.LecturerPortal;
using UniAcademic.Application.Features.Materials;
using UniAcademic.Application.Models.Attendance;
using UniAcademic.Application.Models.Grades;
using UniAcademic.Application.Models.LecturerPortal;
using UniAcademic.Application.Models.Materials;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.LecturerPortal;

public sealed class LecturerPortalServiceTests
{
    [Fact]
    public async Task GetMyTeachingOfferingsAsync_ShouldReturnOnlyAssignedOfferings()
    {
        await using var dbContext = CreateDbContext();
        var lecturer = await SeedLecturerProfileAsync(dbContext, "GV001");
        var otherLecturer = await SeedLecturerProfileAsync(dbContext, "GV002");
        var offeringA = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var offeringB = await SeedCourseOfferingAsync(dbContext, "CO-02");

        await SeedAssignmentAsync(dbContext, lecturer.Id, offeringA.Id, true);
        await SeedAssignmentAsync(dbContext, otherLecturer.Id, offeringB.Id, true);

        var service = CreateLecturerPortalService(dbContext, lecturer.Id);
        var result = await service.GetMyTeachingOfferingsAsync(new GetMyTeachingOfferingsQuery());

        Assert.Single(result);
        Assert.Equal(offeringA.Id, result.Single().Id);
    }

    [Fact]
    public async Task UpdateAttendanceRecordsAsync_ShouldAllowAssignedLecturer()
    {
        await using var dbContext = CreateDbContext();
        var lecturer = await SeedLecturerProfileAsync(dbContext, "GV001");
        var offering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, offering, ["SV001"]);
        var session = await SeedAttendanceSessionAsync(dbContext, offering, snapshot);
        await SeedAssignmentAsync(dbContext, lecturer.Id, offering.Id, true);

        var service = CreateLecturerPortalService(dbContext, lecturer.Id);
        var result = await service.UpdateAttendanceRecordsAsync(new UpdateAttendanceRecordsCommand
        {
            Id = session.Id,
            Records =
            [
                new UpdateAttendanceRecordItemCommand
                {
                    RosterItemId = snapshot.Items.Single().Id,
                    Status = AttendanceStatus.Present,
                    Note = "Da diem danh"
                }
            ]
        });

        Assert.Single(result.Records);
        Assert.Equal(AttendanceStatus.Present, result.Records.Single().Status);
    }

    [Fact]
    public async Task UpdateAttendanceRecordsAsync_ShouldFailForUnassignedLecturer()
    {
        await using var dbContext = CreateDbContext();
        var lecturer = await SeedLecturerProfileAsync(dbContext, "GV001");
        var offering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, offering, ["SV001"]);
        var session = await SeedAttendanceSessionAsync(dbContext, offering, snapshot);

        var service = CreateLecturerPortalService(dbContext, lecturer.Id);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.UpdateAttendanceRecordsAsync(new UpdateAttendanceRecordsCommand
        {
            Id = session.Id,
            Records =
            [
                new UpdateAttendanceRecordItemCommand
                {
                    RosterItemId = snapshot.Items.Single().Id,
                    Status = AttendanceStatus.Present
                }
            ]
        }));

        Assert.Equal("Current lecturer is not assigned to the course offering.", exception.Message);
    }

    [Fact]
    public async Task UpdateGradeEntriesAndUpdateCourseMaterialAsync_ShouldAllowAssignedLecturer()
    {
        await using var dbContext = CreateDbContext();
        var lecturer = await SeedLecturerProfileAsync(dbContext, "GV001");
        var offering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, offering, ["SV001"]);
        var category = await SeedGradeCategoryAsync(dbContext, offering, snapshot);
        var material = await SeedCourseMaterialAsync(dbContext, offering.Id, "Lecture 1", true);
        await SeedAssignmentAsync(dbContext, lecturer.Id, offering.Id, true);

        var service = CreateLecturerPortalService(dbContext, lecturer.Id);
        var updatedCategory = await service.UpdateGradeEntriesAsync(new UpdateGradeEntriesCommand
        {
            Id = category.Id,
            Entries =
            [
                new UpdateGradeEntryItemCommand
                {
                    RosterItemId = snapshot.Items.Single().Id,
                    Score = 9m,
                    Note = "good"
                }
            ]
        });
        var updatedMaterial = await service.UpdateCourseMaterialAsync(new UpdateCourseMaterialCommand
        {
            Id = material.Id,
            Title = "Lecture 1 Updated",
            Description = "Updated",
            MaterialType = CourseMaterialType.Document,
            SortOrder = 1
        });

        Assert.Equal(9m, updatedCategory.Entries.Single().Score);
        Assert.Equal("Lecture 1 Updated", updatedMaterial.Title);
    }

    [Fact]
    public async Task UpdateGradeEntriesAndUpdateCourseMaterialAsync_ShouldFailForUnassignedLecturer()
    {
        await using var dbContext = CreateDbContext();
        var lecturer = await SeedLecturerProfileAsync(dbContext, "GV001");
        var offering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, offering, ["SV001"]);
        var category = await SeedGradeCategoryAsync(dbContext, offering, snapshot);
        var material = await SeedCourseMaterialAsync(dbContext, offering.Id, "Lecture 1", true);

        var service = CreateLecturerPortalService(dbContext, lecturer.Id);

        var gradeException = await Assert.ThrowsAsync<AuthException>(() => service.UpdateGradeEntriesAsync(new UpdateGradeEntriesCommand
        {
            Id = category.Id,
            Entries =
            [
                new UpdateGradeEntryItemCommand
                {
                    RosterItemId = snapshot.Items.Single().Id,
                    Score = 8m
                }
            ]
        }));

        var materialException = await Assert.ThrowsAsync<AuthException>(() => service.UpdateCourseMaterialAsync(new UpdateCourseMaterialCommand
        {
            Id = material.Id,
            Title = "Lecture 1 Updated",
            Description = "Updated",
            MaterialType = CourseMaterialType.Document,
            SortOrder = 1
        }));

        Assert.Equal("Current lecturer is not assigned to the course offering.", gradeException.Message);
        Assert.Equal("Current lecturer is not assigned to the course offering.", materialException.Message);
    }

    [Fact]
    public async Task GetMyTeachingOfferingsAsync_ShouldFail_WhenCurrentUserIsNotMappedToLecturer()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateLecturerPortalService(dbContext, null);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.GetMyTeachingOfferingsAsync(new GetMyTeachingOfferingsQuery()));

        Assert.Equal("Current user is not mapped to a lecturer profile.", exception.Message);
    }

    private static LecturerPortalService CreateLecturerPortalService(AppDbContext dbContext, Guid? lecturerProfileId)
    {
        var auditService = new AuditService(dbContext, new FakeClientContextAccessor());
        var currentUser = new FakeCurrentUser();
        var dateTimeProvider = new FakeDateTimeProvider();
        IAttendanceService attendanceService = new AttendanceService(dbContext, auditService, currentUser, dateTimeProvider);
        IGradeService gradeService = new GradeService(dbContext, auditService, currentUser);
        ICourseMaterialService materialService = new CourseMaterialService(dbContext, auditService, currentUser, dateTimeProvider, new FakeLocalFileStorage());
        IGradeResultService gradeResultService = new GradeResultService(dbContext, auditService, currentUser, dateTimeProvider);

        return new LecturerPortalService(dbContext, new FakeCurrentLecturerContext(lecturerProfileId), attendanceService, gradeService, materialService, gradeResultService);
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

    private static async Task<LecturerProfile> SeedLecturerProfileAsync(AppDbContext dbContext, string code)
    {
        var faculty = await SeedFacultyAsync(dbContext);
        var lecturer = new LecturerProfile
        {
            Code = code,
            FullName = $"Lecturer {code}",
            FacultyId = faculty.Id,
            IsActive = true,
            CreatedBy = "seed"
        };

        dbContext.LecturerProfilesSet.Add(lecturer);
        await dbContext.SaveChangesAsync();
        return lecturer;
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

    private static async Task SeedAssignmentAsync(AppDbContext dbContext, Guid lecturerProfileId, Guid offeringId, bool isPrimary)
    {
        dbContext.LecturerAssignmentsSet.Add(new LecturerAssignment
        {
            CourseOfferingId = offeringId,
            LecturerProfileId = lecturerProfileId,
            IsPrimary = isPrimary,
            AssignedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc),
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

    private static async Task<AttendanceSession> SeedAttendanceSessionAsync(AppDbContext dbContext, CourseOffering offering, CourseOfferingRosterSnapshot snapshot)
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
                Status = AttendanceStatus.Unmarked,
                CreatedBy = "seed"
            });
        }

        dbContext.AttendanceSessionsSet.Add(session);
        await dbContext.SaveChangesAsync();
        return session;
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

    private static async Task<CourseMaterial> SeedCourseMaterialAsync(AppDbContext dbContext, Guid offeringId, string title, bool isPublished)
    {
        var material = new CourseMaterial
        {
            CourseOfferingId = offeringId,
            FileMetadata = new FileMetadata
            {
                OriginalFileName = "lecture1.pdf",
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
        };

        dbContext.CourseMaterialsSet.Add(material);
        await dbContext.SaveChangesAsync();
        return material;
    }

    private sealed class FakeCurrentLecturerContext : ICurrentLecturerContext
    {
        private readonly Guid? _lecturerProfileId;

        public FakeCurrentLecturerContext(Guid? lecturerProfileId)
        {
            _lecturerProfileId = lecturerProfileId;
        }

        public Task<Guid?> GetLecturerProfileIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(_lecturerProfileId);

        public Task<Guid> GetRequiredLecturerProfileIdAsync(CancellationToken cancellationToken = default)
        {
            if (!_lecturerProfileId.HasValue)
            {
                throw new AuthException("Current user is not mapped to a lecturer profile.");
            }

            return Task.FromResult(_lecturerProfileId.Value);
        }
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public string? Username => "lecturer-portal-user";
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions =>
        [
            PermissionConstants.Attendance.View,
            PermissionConstants.Attendance.Create,
            PermissionConstants.Attendance.Edit,
            PermissionConstants.Grades.View,
            PermissionConstants.Grades.Create,
            PermissionConstants.Grades.Edit,
            PermissionConstants.CourseMaterials.View,
            PermissionConstants.CourseMaterials.Create,
            PermissionConstants.CourseMaterials.Edit,
            PermissionConstants.CourseMaterials.Download,
            PermissionConstants.GradeResults.View
        ];
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "LecturerPortalTests";
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
