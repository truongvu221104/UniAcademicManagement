using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Common;
using UniAcademic.Application.Features.Grades;
using UniAcademic.Application.Models.Grades;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.Grades;

public sealed class GradeServiceTests
{
    [Fact]
    public async Task CreateCategoryAsync_ShouldCreateGradeCategory_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, courseOffering, ["SV001", "SV002"]);
        var service = CreateGradeService(dbContext);

        var result = await service.CreateCategoryAsync(new CreateGradeCategoryCommand
        {
            CourseOfferingId = courseOffering.Id,
            Name = "  Chuyen can  ",
            Weight = 10m,
            MaxScore = 10m,
            OrderIndex = 1,
            IsActive = true
        });

        Assert.Equal(courseOffering.Id, result.CourseOfferingId);
        Assert.Equal(snapshot.Id, result.CourseOfferingRosterSnapshotId);
        Assert.Equal("Chuyen can", result.Name);
        Assert.Equal(10m, result.Weight);
        Assert.Equal(10m, result.MaxScore);
        Assert.Equal(2, result.EntryCount);
        Assert.All(result.Entries, x => Assert.Null(x.Score));
        Assert.All(result.Entries, x => Assert.Null(x.Note));

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "gradecategory.create");
        Assert.Equal(nameof(GradeCategory), audit.EntityType);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldFail_WhenCourseOfferingDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateGradeService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateCategoryAsync(new CreateGradeCategoryCommand
        {
            CourseOfferingId = Guid.NewGuid(),
            Name = "Quiz",
            Weight = 10m,
            MaxScore = 10m,
            OrderIndex = 1
        }));

        Assert.Equal("Course offering was not found.", exception.Message);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldFail_WhenCourseOfferingIsSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        courseOffering.IsDeleted = true;
        courseOffering.DeletedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc);
        courseOffering.DeletedBy = "seed";
        await dbContext.SaveChangesAsync();
        var service = CreateGradeService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateCategoryAsync(new CreateGradeCategoryCommand
        {
            CourseOfferingId = courseOffering.Id,
            Name = "Quiz",
            Weight = 10m,
            MaxScore = 10m,
            OrderIndex = 1
        }));

        Assert.Equal("Course offering was not found.", exception.Message);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldFail_WhenRosterIsNotFinalized()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var service = CreateGradeService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateCategoryAsync(new CreateGradeCategoryCommand
        {
            CourseOfferingId = courseOffering.Id,
            Name = "Quiz",
            Weight = 10m,
            MaxScore = 10m,
            OrderIndex = 1
        }));

        Assert.Equal("Course offering roster was not finalized.", exception.Message);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldFail_WhenRosterSnapshotIsMissing()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        courseOffering.IsRosterFinalized = true;
        courseOffering.RosterFinalizedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc);
        await dbContext.SaveChangesAsync();
        var service = CreateGradeService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateCategoryAsync(new CreateGradeCategoryCommand
        {
            CourseOfferingId = courseOffering.Id,
            Name = "Quiz",
            Weight = 10m,
            MaxScore = 10m,
            OrderIndex = 1
        }));

        Assert.Equal("Course offering roster snapshot was not found.", exception.Message);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldFail_WhenCategoryNameAlreadyExistsInOffering()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, courseOffering, ["SV001"]);
        dbContext.GradeCategoriesSet.Add(new GradeCategory
        {
            CourseOfferingId = courseOffering.Id,
            CourseOfferingRosterSnapshotId = snapshot.Id,
            Name = "Quiz 1",
            Weight = 10m,
            MaxScore = 10m,
            OrderIndex = 1,
            IsActive = true,
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();
        var service = CreateGradeService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateCategoryAsync(new CreateGradeCategoryCommand
        {
            CourseOfferingId = courseOffering.Id,
            Name = "  Quiz 1  ",
            Weight = 20m,
            MaxScore = 10m,
            OrderIndex = 2
        }));

        Assert.Equal("Grade category name already exists.", exception.Message);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldFail_WhenTotalWeightExceeds100()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var snapshot = await SeedFinalizedRosterAsync(dbContext, courseOffering, ["SV001"]);
        dbContext.GradeCategoriesSet.Add(new GradeCategory
        {
            CourseOfferingId = courseOffering.Id,
            CourseOfferingRosterSnapshotId = snapshot.Id,
            Name = "Quiz 1",
            Weight = 60m,
            MaxScore = 10m,
            OrderIndex = 1,
            IsActive = true,
            CreatedBy = "seed"
        });
        dbContext.GradeCategoriesSet.Add(new GradeCategory
        {
            CourseOfferingId = courseOffering.Id,
            CourseOfferingRosterSnapshotId = snapshot.Id,
            Name = "Quiz 2",
            Weight = 30m,
            MaxScore = 10m,
            OrderIndex = 2,
            IsActive = true,
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();
        var service = CreateGradeService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.CreateCategoryAsync(new CreateGradeCategoryCommand
        {
            CourseOfferingId = courseOffering.Id,
            Name = "Final",
            Weight = 20m,
            MaxScore = 100m,
            OrderIndex = 3
        }));

        Assert.Equal("Total active grade weight exceeds 100.", exception.Message);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldGenerateEntriesForAllRosterItems()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        await SeedFinalizedRosterAsync(dbContext, courseOffering, ["SV001", "SV002", "SV003"]);
        var service = CreateGradeService(dbContext);

        var result = await service.CreateCategoryAsync(new CreateGradeCategoryCommand
        {
            CourseOfferingId = courseOffering.Id,
            Name = "Quiz",
            Weight = 10m,
            MaxScore = 10m,
            OrderIndex = 1
        });

        Assert.Equal(3, result.EntryCount);
        Assert.Equal(3, await dbContext.GradeEntriesSet.CountAsync());
    }

    [Fact]
    public async Task UpdateCategoryAsync_ShouldUpdateCategory_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var category = await SeedGradeCategoryAsync(dbContext, "CO-01", "Quiz 1", 10m, 10m, ["SV001", "SV002"]);
        var service = CreateGradeService(dbContext);

        var result = await service.UpdateCategoryAsync(new UpdateGradeCategoryCommand
        {
            Id = category.Id,
            Name = "  Midterm  ",
            Weight = 40m,
            MaxScore = 100m,
            OrderIndex = 2,
            IsActive = true
        });

        Assert.Equal("Midterm", result.Name);
        Assert.Equal(40m, result.Weight);
        Assert.Equal(100m, result.MaxScore);
        Assert.Equal(2, result.OrderIndex);

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "gradecategory.update");
        Assert.Equal(category.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task UpdateCategoryAsync_ShouldFail_WhenTotalWeightExceeds100()
    {
        await using var dbContext = CreateDbContext();
        var categoryA = await SeedGradeCategoryAsync(dbContext, "CO-01", "Quiz", 40m, 10m, ["SV001"]);
        var categoryB = new GradeCategory
        {
            CourseOfferingId = categoryA.CourseOfferingId,
            CourseOfferingRosterSnapshotId = categoryA.CourseOfferingRosterSnapshotId,
            Name = "Final",
            Weight = 50m,
            MaxScore = 100m,
            OrderIndex = 2,
            IsActive = true,
            CreatedBy = "seed"
        };
        dbContext.GradeCategoriesSet.Add(categoryB);
        await dbContext.SaveChangesAsync();
        var service = CreateGradeService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.UpdateCategoryAsync(new UpdateGradeCategoryCommand
        {
            Id = categoryA.Id,
            Name = categoryA.Name,
            Weight = 60m,
            MaxScore = categoryA.MaxScore,
            OrderIndex = categoryA.OrderIndex,
            IsActive = true
        }));

        Assert.Equal("Total active grade weight exceeds 100.", exception.Message);
    }

    [Fact]
    public async Task UpdateEntriesAsync_ShouldUpdateEntries_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var category = await SeedGradeCategoryAsync(dbContext, "CO-01", "Quiz", 10m, 10m, ["SV001", "SV002"]);
        var service = CreateGradeService(dbContext);

        var result = await service.UpdateEntriesAsync(new UpdateGradeEntriesCommand
        {
            Id = category.Id,
            Entries = category.Entries.Select(x => new UpdateGradeEntryItemCommand
            {
                RosterItemId = x.RosterItemId,
                Score = x.RosterItem!.StudentCode == "SV001" ? 9.5m : 8m,
                Note = "  Da nhap diem  "
            }).ToList()
        });

        Assert.Contains(result.Entries, x => x.StudentCode == "SV001" && x.Score == 9.5m);
        Assert.Contains(result.Entries, x => x.StudentCode == "SV002" && x.Score == 8m);
        Assert.All(result.Entries, x => Assert.Equal("Da nhap diem", x.Note));

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "gradeentries.update");
        Assert.Equal(category.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task UpdateEntriesAsync_ShouldFail_WhenRosterItemDoesNotBelongToCategorySnapshot()
    {
        await using var dbContext = CreateDbContext();
        var categoryA = await SeedGradeCategoryAsync(dbContext, "CO-01", "Quiz", 10m, 10m, ["SV001"]);
        var categoryB = await SeedGradeCategoryAsync(dbContext, "CO-02", "Quiz", 10m, 10m, ["SV002"]);
        var foreignRosterItemId = categoryB.Entries.Single().RosterItemId;
        var service = CreateGradeService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.UpdateEntriesAsync(new UpdateGradeEntriesCommand
        {
            Id = categoryA.Id,
            Entries =
            [
                new UpdateGradeEntryItemCommand
                {
                    RosterItemId = foreignRosterItemId,
                    Score = 5m
                }
            ]
        }));

        Assert.Equal("Grade entry does not belong to the category snapshot.", exception.Message);
    }

    [Fact]
    public async Task UpdateEntriesAsync_ShouldFail_WhenScoreExceedsMaxScore()
    {
        await using var dbContext = CreateDbContext();
        var category = await SeedGradeCategoryAsync(dbContext, "CO-01", "Quiz", 10m, 10m, ["SV001"]);
        var rosterItemId = category.Entries.Single().RosterItemId;
        var service = CreateGradeService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.UpdateEntriesAsync(new UpdateGradeEntriesCommand
        {
            Id = category.Id,
            Entries =
            [
                new UpdateGradeEntryItemCommand
                {
                    RosterItemId = rosterItemId,
                    Score = 11m
                }
            ]
        }));

        Assert.Equal("Grade score is invalid.", exception.Message);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCategoryWithEntries()
    {
        await using var dbContext = CreateDbContext();
        var category = await SeedGradeCategoryAsync(dbContext, "CO-01", "Quiz", 10m, 10m, ["SV001", "SV002"]);
        var service = CreateGradeService(dbContext);

        var result = await service.GetByIdAsync(new GetGradeCategoryByIdQuery { Id = category.Id });

        Assert.Equal(category.Id, result.Id);
        Assert.Equal(2, result.EntryCount);
        Assert.Contains(result.Entries, x => x.StudentCode == "SV001");
        Assert.Contains(result.Entries, x => x.StudentCode == "SV002");
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
            FinalizedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc),
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
        courseOffering.RosterFinalizedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc);
        dbContext.CourseOfferingRosterSnapshotsSet.Add(snapshot);
        await dbContext.SaveChangesAsync();

        return snapshot;
    }

    private static async Task<GradeCategory> SeedGradeCategoryAsync(AppDbContext dbContext, string courseOfferingCode, string name, decimal weight, decimal maxScore, IReadOnlyCollection<string> studentCodes)
    {
        var courseOffering = await SeedCourseOfferingAsync(dbContext, courseOfferingCode);
        var snapshot = await SeedFinalizedRosterAsync(dbContext, courseOffering, studentCodes);
        var category = new GradeCategory
        {
            CourseOfferingId = courseOffering.Id,
            CourseOfferingRosterSnapshotId = snapshot.Id,
            Name = name,
            Weight = weight,
            MaxScore = maxScore,
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
        return await dbContext.GradeCategoriesSet
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CourseOffering)
                .ThenInclude(x => x!.Semester)
            .Include(x => x.Entries)
                .ThenInclude(x => x.RosterItem)
            .SingleAsync(x => x.Id == category.Id);
    }

    private static GradeService CreateGradeService(AppDbContext dbContext)
    {
        return new GradeService(
            dbContext,
            new AuditService(dbContext, new FakeClientContextAccessor()),
            new FakeCurrentUser());
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("77777777-7777-7777-7777-777777777777");
        public string? Username => "grades-admin";
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions => [PermissionConstants.Grades.View, PermissionConstants.Grades.Create, PermissionConstants.Grades.Edit];
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "GradeTests";
        public string ClientType => "Tests";
    }
}
