using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Common;
using UniAcademic.Application.Features.GradeResults;
using UniAcademic.Application.Models.GradeResults;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.GradeResults;

public sealed class GradeResultServiceTests
{
    [Fact]
    public async Task CalculateAsync_ShouldCreateResults_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, courseOffering, ["SV001", "SV002"]);
        await SeedActiveCategoryAsync(dbContext, courseOffering, snapshot, "Attendance", 40m, 10m, new Dictionary<string, decimal>
        {
            ["SV001"] = 8m,
            ["SV002"] = 9m
        });
        await SeedActiveCategoryAsync(dbContext, courseOffering, snapshot, "Final", 60m, 100m, new Dictionary<string, decimal>
        {
            ["SV001"] = 75m,
            ["SV002"] = 80m
        });

        var service = CreateGradeResultService(dbContext);
        var results = await service.CalculateAsync(new CalculateGradeResultsCommand
        {
            CourseOfferingId = courseOffering.Id,
            PassingScore = 50m
        });

        Assert.Equal(2, results.Count);
        var sv001 = Assert.Single(results, x => x.StudentCode == "SV001");
        Assert.Equal(77.0000m, sv001.WeightedFinalScore);
        Assert.True(sv001.IsPassed);
        Assert.Equal(snapshot.Id, sv001.CourseOfferingRosterSnapshotId);

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "graderesult.calculate");
        Assert.Equal(nameof(GradeResult), audit.EntityType);
        Assert.Equal(courseOffering.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task CalculateAsync_ShouldFail_WhenCourseOfferingDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateGradeResultService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CalculateAsync(new CalculateGradeResultsCommand
        {
            CourseOfferingId = Guid.NewGuid(),
            PassingScore = 50m
        }));

        Assert.Equal("Course offering was not found.", exception.Message);
    }

    [Fact]
    public async Task CalculateAsync_ShouldFail_WhenCourseOfferingIsSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        courseOffering.IsDeleted = true;
        courseOffering.DeletedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc);
        courseOffering.DeletedBy = "seed";
        await dbContext.SaveChangesAsync();
        var service = CreateGradeResultService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CalculateAsync(new CalculateGradeResultsCommand
        {
            CourseOfferingId = courseOffering.Id,
            PassingScore = 50m
        }));

        Assert.Equal("Course offering was not found.", exception.Message);
    }

    [Fact]
    public async Task CalculateAsync_ShouldFail_WhenRosterIsNotFinalized()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var service = CreateGradeResultService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CalculateAsync(new CalculateGradeResultsCommand
        {
            CourseOfferingId = courseOffering.Id,
            PassingScore = 50m
        }));

        Assert.Equal("Course offering roster was not finalized.", exception.Message);
    }

    [Fact]
    public async Task CalculateAsync_ShouldFail_WhenRosterSnapshotIsMissing()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        courseOffering.IsRosterFinalized = true;
        courseOffering.RosterFinalizedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc);
        await dbContext.SaveChangesAsync();
        var service = CreateGradeResultService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CalculateAsync(new CalculateGradeResultsCommand
        {
            CourseOfferingId = courseOffering.Id,
            PassingScore = 50m
        }));

        Assert.Equal("Course offering roster snapshot was not found.", exception.Message);
    }

    [Fact]
    public async Task CalculateAsync_ShouldFail_WhenNoActiveCategories()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, courseOffering, ["SV001"]);
        await SeedInactiveCategoryAsync(dbContext, courseOffering, snapshot, "Quiz", 100m, 10m, new Dictionary<string, decimal?> { ["SV001"] = 9m });
        var service = CreateGradeResultService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CalculateAsync(new CalculateGradeResultsCommand
        {
            CourseOfferingId = courseOffering.Id,
            PassingScore = 50m
        }));

        Assert.Equal("Active grade categories were not found.", exception.Message);
    }

    [Fact]
    public async Task CalculateAsync_ShouldFail_WhenActiveTotalWeightIsNot100()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, courseOffering, ["SV001"]);
        await SeedActiveCategoryAsync(dbContext, courseOffering, snapshot, "Quiz", 40m, 10m, new Dictionary<string, decimal> { ["SV001"] = 9m });
        await SeedActiveCategoryAsync(dbContext, courseOffering, snapshot, "Final", 50m, 100m, new Dictionary<string, decimal> { ["SV001"] = 80m });
        var service = CreateGradeResultService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CalculateAsync(new CalculateGradeResultsCommand
        {
            CourseOfferingId = courseOffering.Id,
            PassingScore = 50m
        }));

        Assert.Equal("Total active grade weight must equal 100.", exception.Message);
    }

    [Fact]
    public async Task CalculateAsync_ShouldFail_WhenAnyActiveScoreIsNull()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, courseOffering, ["SV001"]);
        await SeedActiveCategoryAsync(dbContext, courseOffering, snapshot, "Quiz", 100m, 10m, new Dictionary<string, decimal?> { ["SV001"] = null });
        var service = CreateGradeResultService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CalculateAsync(new CalculateGradeResultsCommand
        {
            CourseOfferingId = courseOffering.Id,
            PassingScore = 50m
        }));

        Assert.Equal("Active grade scores must be fully entered before calculation.", exception.Message);
    }

    [Fact]
    public async Task CalculateAsync_ShouldIgnoreInactiveCategories()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, courseOffering, ["SV001"]);
        await SeedActiveCategoryAsync(dbContext, courseOffering, snapshot, "Final", 100m, 100m, new Dictionary<string, decimal> { ["SV001"] = 80m });
        await SeedInactiveCategoryAsync(dbContext, courseOffering, snapshot, "Bonus", 10m, 10m, new Dictionary<string, decimal?> { ["SV001"] = 10m });
        var service = CreateGradeResultService(dbContext);

        var result = await service.CalculateAsync(new CalculateGradeResultsCommand
        {
            CourseOfferingId = courseOffering.Id,
            PassingScore = 50m
        });

        var only = Assert.Single(result);
        Assert.Equal(80.0000m, only.WeightedFinalScore);
    }

    [Fact]
    public async Task CalculateAsync_ShouldCreateResultForEveryRosterItemInSnapshot()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, courseOffering, ["SV001", "SV002", "SV003"]);
        await SeedActiveCategoryAsync(dbContext, courseOffering, snapshot, "Total", 100m, 10m, new Dictionary<string, decimal>
        {
            ["SV001"] = 8m,
            ["SV002"] = 7m,
            ["SV003"] = 9m
        });
        var service = CreateGradeResultService(dbContext);

        var result = await service.CalculateAsync(new CalculateGradeResultsCommand
        {
            CourseOfferingId = courseOffering.Id,
            PassingScore = 50m
        });

        Assert.Equal(3, result.Count);
        Assert.Equal(3, await dbContext.GradeResultsSet.CountAsync());
    }

    [Fact]
    public async Task CalculateAsync_ShouldRecalculateAndOverwriteExistingResults()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, courseOffering, ["SV001"]);
        var category = await SeedActiveCategoryAsync(dbContext, courseOffering, snapshot, "Total", 100m, 10m, new Dictionary<string, decimal>
        {
            ["SV001"] = 6m
        });
        var service = CreateGradeResultService(dbContext);

        await service.CalculateAsync(new CalculateGradeResultsCommand
        {
            CourseOfferingId = courseOffering.Id,
            PassingScore = 50m
        });

        category.Entries.Single().Score = 9m;
        await dbContext.SaveChangesAsync();

        var recalculated = await service.CalculateAsync(new CalculateGradeResultsCommand
        {
            CourseOfferingId = courseOffering.Id,
            PassingScore = 70m
        });

        var result = Assert.Single(recalculated);
        Assert.Equal(90.0000m, result.WeightedFinalScore);
        Assert.True(result.IsPassed);
        Assert.Equal(1, await dbContext.GradeResultsSet.CountAsync());
        Assert.Single(await dbContext.AuditLogs.Where(x => x.Action == "graderesult.recalculate").ToListAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnResultCorrectly()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, courseOffering, ["SV001"]);
        await SeedActiveCategoryAsync(dbContext, courseOffering, snapshot, "Total", 100m, 10m, new Dictionary<string, decimal>
        {
            ["SV001"] = 8m
        });
        var service = CreateGradeResultService(dbContext);
        var calculated = await service.CalculateAsync(new CalculateGradeResultsCommand
        {
            CourseOfferingId = courseOffering.Id,
            PassingScore = 50m
        });

        var result = await service.GetByIdAsync(new GetGradeResultByIdQuery { Id = calculated.Single().Id });

        Assert.Equal("SV001", result.StudentCode);
        Assert.Equal(80.0000m, result.WeightedFinalScore);
        Assert.Equal(50.0000m, result.PassingScore);
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

    private static async Task<Course> SeedCourseAsync(AppDbContext dbContext)
    {
        var course = new Course
        {
            Code = $"CS{100 + dbContext.CoursesSet.Count()}",
            Name = "Nhap mon lap trinh",
            Credits = 3,
            Status = CourseStatus.Active,
            CreatedBy = "seed"
        };

        dbContext.CoursesSet.Add(course);
        await dbContext.SaveChangesAsync();
        return course;
    }

    private static async Task<Semester> SeedSemesterAsync(AppDbContext dbContext)
    {
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

        dbContext.SemestersSet.Add(semester);
        await dbContext.SaveChangesAsync();
        return semester;
    }

    private static async Task<CourseOffering> SeedCourseOfferingAsync(AppDbContext dbContext, string code)
    {
        var course = await SeedCourseAsync(dbContext);
        var semester = await SeedSemesterAsync(dbContext);
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

    private static async Task<CourseOfferingRosterSnapshot> SeedFinalizedRosterAsync(AppDbContext dbContext, CourseOffering courseOffering, IReadOnlyCollection<string> studentCodes)
    {
        var snapshot = new CourseOfferingRosterSnapshot
        {
            CourseOfferingId = courseOffering.Id,
            FinalizedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc),
            FinalizedBy = "seed",
            ItemCount = studentCodes.Count,
            CreatedBy = "seed"
        };

        foreach (var studentCode in studentCodes)
        {
            var studentProfile = await SeedStudentProfileAsync(dbContext, studentCode);
            var enrollment = new Enrollment
            {
                StudentProfileId = studentProfile.Id,
                CourseOfferingId = courseOffering.Id,
                Status = EnrollmentStatus.Enrolled,
                EnrolledAtUtc = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "seed"
            };

            dbContext.EnrollmentsSet.Add(enrollment);
            await dbContext.SaveChangesAsync();

            snapshot.Items.Add(new CourseOfferingRosterItem
            {
                EnrollmentId = enrollment.Id,
                StudentProfileId = studentProfile.Id,
                StudentCode = studentProfile.StudentCode,
                StudentFullName = studentProfile.FullName,
                StudentClassName = studentProfile.StudentClass?.Name ?? string.Empty,
                CourseOfferingCode = courseOffering.Code,
                CourseCode = courseOffering.Course?.Code ?? string.Empty,
                CourseName = courseOffering.Course?.Name ?? string.Empty,
                SemesterName = courseOffering.Semester?.Name ?? string.Empty,
                CreatedBy = "seed"
            });
        }

        courseOffering.IsRosterFinalized = true;
        courseOffering.RosterFinalizedAtUtc = new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc);
        dbContext.CourseOfferingRosterSnapshotsSet.Add(snapshot);
        await dbContext.SaveChangesAsync();

        return snapshot;
    }

    private static async Task<GradeCategory> SeedActiveCategoryAsync(AppDbContext dbContext, CourseOffering courseOffering, CourseOfferingRosterSnapshot snapshot, string name, decimal weight, decimal maxScore, IReadOnlyDictionary<string, decimal> scores)
    {
        return await SeedCategoryAsync(dbContext, courseOffering, snapshot, name, weight, maxScore, true, scores.ToDictionary(x => x.Key, x => (decimal?)x.Value));
    }

    private static async Task<GradeCategory> SeedActiveCategoryAsync(AppDbContext dbContext, CourseOffering courseOffering, CourseOfferingRosterSnapshot snapshot, string name, decimal weight, decimal maxScore, IReadOnlyDictionary<string, decimal?> scores)
    {
        return await SeedCategoryAsync(dbContext, courseOffering, snapshot, name, weight, maxScore, true, scores);
    }

    private static async Task<GradeCategory> SeedInactiveCategoryAsync(AppDbContext dbContext, CourseOffering courseOffering, CourseOfferingRosterSnapshot snapshot, string name, decimal weight, decimal maxScore, IReadOnlyDictionary<string, decimal?> scores)
    {
        return await SeedCategoryAsync(dbContext, courseOffering, snapshot, name, weight, maxScore, false, scores);
    }

    private static async Task<GradeCategory> SeedCategoryAsync(AppDbContext dbContext, CourseOffering courseOffering, CourseOfferingRosterSnapshot snapshot, string name, decimal weight, decimal maxScore, bool isActive, IReadOnlyDictionary<string, decimal?> scores)
    {
        var category = new GradeCategory
        {
            CourseOfferingId = courseOffering.Id,
            CourseOfferingRosterSnapshotId = snapshot.Id,
            Name = name,
            Weight = weight,
            MaxScore = maxScore,
            OrderIndex = dbContext.GradeCategoriesSet.Count() + 1,
            IsActive = isActive,
            CreatedBy = "seed"
        };

        foreach (var item in snapshot.Items)
        {
            category.Entries.Add(new GradeEntry
            {
                RosterItemId = item.Id,
                Score = scores.TryGetValue(item.StudentCode, out var score) ? score : null,
                CreatedBy = "seed"
            });
        }

        dbContext.GradeCategoriesSet.Add(category);
        await dbContext.SaveChangesAsync();
        return await dbContext.GradeCategoriesSet
            .Include(x => x.Entries)
            .SingleAsync(x => x.Id == category.Id);
    }

    private static GradeResultService CreateGradeResultService(AppDbContext dbContext)
    {
        return new GradeResultService(
            dbContext,
            new AuditService(dbContext, new FakeClientContextAccessor()),
            new FakeCurrentUser(),
            new FakeDateTimeProvider());
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("66666666-6666-6666-6666-666666666666");
        public string? Username => "grade-results-admin";
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions => [PermissionConstants.GradeResults.View, PermissionConstants.GradeResults.Calculate];
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 3, 25, 8, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "GradeResultTests";
        public string ClientType => "Tests";
    }
}
