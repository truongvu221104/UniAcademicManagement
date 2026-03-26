using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Common;
using UniAcademic.Application.Features.Enrollments;
using UniAcademic.Application.Models.Enrollments;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.Enrollments;

public sealed class EnrollmentServiceTests
{
    [Fact]
    public async Task EnrollAsync_ShouldCreateEnrollment_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 2);
        var service = CreateEnrollmentService(dbContext);

        var result = await service.EnrollAsync(new EnrollStudentCommand
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = courseOffering.Id,
            Note = "  Dang ky moi  "
        });

        Assert.Equal(studentProfile.Id, result.StudentProfileId);
        Assert.Equal(courseOffering.Id, result.CourseOfferingId);
        Assert.Equal(EnrollmentStatus.Enrolled, result.Status);
        Assert.Equal("Dang ky moi", result.Note);

        var enrollment = await dbContext.EnrollmentsSet.SingleAsync();
        Assert.Equal(EnrollmentStatus.Enrolled, enrollment.Status);
        Assert.NotEqual(default, enrollment.EnrolledAtUtc);

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "enrollment.enroll");
        Assert.Equal(nameof(Enrollment), audit.EntityType);
        Assert.Equal(enrollment.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task EnrollAsync_ShouldFail_WhenEnrollmentAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 2);
        dbContext.EnrollmentsSet.Add(new Enrollment
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = courseOffering.Id,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateEnrollmentService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.EnrollAsync(new EnrollStudentCommand
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = courseOffering.Id
        }));

        Assert.Equal("Enrollment already exists.", exception.Message);
    }

    [Fact]
    public async Task EnrollAsync_ShouldReactivateDroppedEnrollment()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 2);
        var enrollment = new Enrollment
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = courseOffering.Id,
            Status = EnrollmentStatus.Dropped,
            EnrolledAtUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            DroppedAtUtc = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc),
            Note = "Old note",
            CreatedBy = "seed"
        };
        dbContext.EnrollmentsSet.Add(enrollment);
        await dbContext.SaveChangesAsync();

        var service = CreateEnrollmentService(dbContext);
        var result = await service.EnrollAsync(new EnrollStudentCommand
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = courseOffering.Id,
            Note = "  Reactivate  "
        });

        Assert.Equal(enrollment.Id, result.Id);
        Assert.Equal(EnrollmentStatus.Enrolled, result.Status);
        Assert.Null(result.DroppedAtUtc);
        Assert.Equal("Reactivate", result.Note);
        Assert.Single(dbContext.EnrollmentsSet);
    }

    [Fact]
    public async Task EnrollAsync_ShouldFail_WhenStudentProfileDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 2);
        var service = CreateEnrollmentService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.EnrollAsync(new EnrollStudentCommand
        {
            StudentProfileId = Guid.NewGuid(),
            CourseOfferingId = courseOffering.Id
        }));
    }

    [Fact]
    public async Task EnrollAsync_ShouldFail_WhenStudentProfileIsSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 2);
        studentProfile.IsDeleted = true;
        studentProfile.DeletedAtUtc = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);
        studentProfile.DeletedBy = "seed";
        await dbContext.SaveChangesAsync();

        var service = CreateEnrollmentService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.EnrollAsync(new EnrollStudentCommand
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = courseOffering.Id
        }));

        Assert.Equal("Student profile was not found.", exception.Message);
        Assert.False(await dbContext.EnrollmentsSet.AnyAsync());
    }

    [Fact]
    public async Task EnrollAsync_ShouldFail_WhenCourseOfferingDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var service = CreateEnrollmentService(dbContext);

        await Assert.ThrowsAsync<AuthException>(() => service.EnrollAsync(new EnrollStudentCommand
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = Guid.NewGuid()
        }));
    }

    [Fact]
    public async Task EnrollAsync_ShouldFail_WhenCourseOfferingIsSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 2);
        courseOffering.IsDeleted = true;
        courseOffering.DeletedAtUtc = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);
        courseOffering.DeletedBy = "seed";
        await dbContext.SaveChangesAsync();

        var service = CreateEnrollmentService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.EnrollAsync(new EnrollStudentCommand
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = courseOffering.Id
        }));

        Assert.Equal("Course offering was not found.", exception.Message);
        Assert.False(await dbContext.EnrollmentsSet.AnyAsync());
    }

    [Fact]
    public async Task EnrollAsync_ShouldFail_WhenCapacityIsExceeded()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile1 = await SeedStudentProfileAsync(dbContext, "SV001");
        var studentProfile2 = await SeedStudentProfileAsync(dbContext, "SV002");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 1);
        dbContext.EnrollmentsSet.Add(new Enrollment
        {
            StudentProfileId = studentProfile1.Id,
            CourseOfferingId = courseOffering.Id,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateEnrollmentService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.EnrollAsync(new EnrollStudentCommand
        {
            StudentProfileId = studentProfile2.Id,
            CourseOfferingId = courseOffering.Id
        }));

        Assert.Equal("Course offering capacity has been reached.", exception.Message);
    }

    [Fact]
    public async Task EnrollAsync_ShouldFail_WhenPrerequisiteIsNotPassed()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var prerequisiteCourse = await SeedCourseAsync(dbContext, "MATH101", credits: 3);
        var targetCourse = await SeedCourseAsync(dbContext, "CS201", credits: 3);
        dbContext.CoursePrerequisitesSet.Add(new CoursePrerequisite
        {
            CourseId = targetCourse.Id,
            PrerequisiteCourseId = prerequisiteCourse.Id,
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var semester = await SeedSemesterAsync(dbContext);
        var targetOffering = await SeedCourseOfferingAsync(dbContext, "CS201-HK1-01", 2, targetCourse, semester);
        var service = CreateEnrollmentService(dbContext);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.EnrollAsync(new EnrollStudentCommand
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = targetOffering.Id
        }));

        Assert.Equal("Student has not satisfied course prerequisites.", exception.Message);
    }

    [Fact]
    public async Task EnrollAsync_ShouldFail_WhenScheduleConflicts()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var semester = await SeedSemesterAsync(dbContext);
        var offeringA = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 10, semester: semester, dayOfWeek: 2, startPeriod: 1, endPeriod: 3);
        var offeringB = await SeedCourseOfferingAsync(dbContext, "CS102-HK1-01", 10, semester: semester, dayOfWeek: 2, startPeriod: 3, endPeriod: 5);

        dbContext.EnrollmentsSet.Add(new Enrollment
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = offeringA.Id,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateEnrollmentService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.EnrollAsync(new EnrollStudentCommand
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = offeringB.Id
        }));

        Assert.Equal("Course offering schedule conflicts with another enrolled course offering.", exception.Message);
    }

    [Fact]
    public async Task EnrollAsync_ShouldFail_WhenCreditLimitIsExceeded()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var semester = await SeedSemesterAsync(dbContext);
        var existingCourse = await SeedCourseAsync(dbContext, "CS301", credits: 22);
        var targetCourse = await SeedCourseAsync(dbContext, "CS302", credits: 4);
        var existingOffering = await SeedCourseOfferingAsync(dbContext, "CS301-HK1-01", 10, existingCourse, semester);
        var targetOffering = await SeedCourseOfferingAsync(dbContext, "CS302-HK1-01", 10, targetCourse, semester);

        dbContext.EnrollmentsSet.Add(new Enrollment
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = existingOffering.Id,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateEnrollmentService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.EnrollAsync(new EnrollStudentCommand
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = targetOffering.Id
        }));

        Assert.Equal("Enrollment exceeds the semester credit limit of 24.", exception.Message);
    }

    [Fact]
    public async Task EnrollAsync_ShouldFail_WhenStudentAlreadyPassedCourse()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var course = await SeedCourseAsync(dbContext, "CS401", credits: 3);
        var priorOffering = await SeedCourseOfferingAsync(dbContext, "CS401-HK0-01", 10, course);
        var targetOffering = await SeedCourseOfferingAsync(dbContext, "CS401-HK1-01", 10, course);

        await SeedPassedGradeResultAsync(dbContext, studentProfile, priorOffering);

        var service = CreateEnrollmentService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.EnrollAsync(new EnrollStudentCommand
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = targetOffering.Id
        }));

        Assert.Equal("Student has already passed this course.", exception.Message);
    }

    [Fact]
    public async Task EnrollAsync_ShouldAllowOverride_AndWriteOverrideAudit()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var semester = await SeedSemesterAsync(dbContext);
        var existingOffering = await SeedCourseOfferingAsync(dbContext, "CS501-HK1-01", 10, semester: semester, dayOfWeek: 2, startPeriod: 1, endPeriod: 3);
        var targetOffering = await SeedCourseOfferingAsync(dbContext, "CS502-HK1-01", 10, semester: semester, dayOfWeek: 2, startPeriod: 2, endPeriod: 4);

        dbContext.EnrollmentsSet.Add(new Enrollment
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = existingOffering.Id,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateEnrollmentService(dbContext);
        var result = await service.EnrollAsync(new EnrollStudentCommand
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = targetOffering.Id,
            IsOverride = true,
            OverrideReason = "Dean approval"
        });

        Assert.Equal(targetOffering.Id, result.CourseOfferingId);
        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "enrollment.override");
        Assert.Equal(nameof(Enrollment), audit.EntityType);
    }

    [Fact]
    public async Task EnrollAsync_ShouldFailToReactivate_WhenCapacityIsExceeded()
    {
        await using var dbContext = CreateDbContext();
        var studentProfileA = await SeedStudentProfileAsync(dbContext, "SV001");
        var studentProfileB = await SeedStudentProfileAsync(dbContext, "SV002");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 1);
        var droppedEnrollment = new Enrollment
        {
            StudentProfileId = studentProfileA.Id,
            CourseOfferingId = courseOffering.Id,
            Status = EnrollmentStatus.Dropped,
            EnrolledAtUtc = new DateTime(2026, 3, 18, 0, 0, 0, DateTimeKind.Utc),
            DroppedAtUtc = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        };
        var activeEnrollment = new Enrollment
        {
            StudentProfileId = studentProfileB.Id,
            CourseOfferingId = courseOffering.Id,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        };
        dbContext.EnrollmentsSet.AddRange(droppedEnrollment, activeEnrollment);
        await dbContext.SaveChangesAsync();

        var service = CreateEnrollmentService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.EnrollAsync(new EnrollStudentCommand
        {
            StudentProfileId = studentProfileA.Id,
            CourseOfferingId = courseOffering.Id
        }));

        Assert.Equal("Course offering capacity has been reached.", exception.Message);

        var enrollments = await dbContext.EnrollmentsSet
            .Where(x => x.CourseOfferingId == courseOffering.Id)
            .OrderBy(x => x.StudentProfileId)
            .ToListAsync();

        Assert.Equal(2, enrollments.Count);
        var unchangedDroppedEnrollment = Assert.Single(enrollments, x => x.StudentProfileId == studentProfileA.Id);
        Assert.Equal(EnrollmentStatus.Dropped, unchangedDroppedEnrollment.Status);
        Assert.NotNull(unchangedDroppedEnrollment.DroppedAtUtc);
    }

    [Fact]
    public async Task DropAsync_ShouldMarkEnrollmentAsDropped_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 2);
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

        var service = CreateEnrollmentService(dbContext);
        await service.DropAsync(new DropEnrollmentCommand { Id = enrollment.Id });

        var dropped = await dbContext.EnrollmentsSet.SingleAsync();
        Assert.Equal(EnrollmentStatus.Dropped, dropped.Status);
        Assert.NotNull(dropped.DroppedAtUtc);

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "enrollment.drop");
        Assert.Equal(enrollment.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task EnrollAsync_ShouldFail_WhenCourseOfferingRosterIsFinalized()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 2);
        courseOffering.IsRosterFinalized = true;
        courseOffering.RosterFinalizedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc);
        await dbContext.SaveChangesAsync();

        var service = CreateEnrollmentService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.EnrollAsync(new EnrollStudentCommand
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = courseOffering.Id
        }));

        Assert.Equal("Course offering roster was already finalized.", exception.Message);
        Assert.False(await dbContext.EnrollmentsSet.AnyAsync());
    }

    [Fact]
    public async Task EnrollAsync_ShouldFailToReactivate_WhenCourseOfferingRosterIsFinalized()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 2);
        var droppedEnrollment = new Enrollment
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = courseOffering.Id,
            Status = EnrollmentStatus.Dropped,
            EnrolledAtUtc = new DateTime(2026, 3, 18, 0, 0, 0, DateTimeKind.Utc),
            DroppedAtUtc = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        };
        dbContext.EnrollmentsSet.Add(droppedEnrollment);
        await dbContext.SaveChangesAsync();

        courseOffering.IsRosterFinalized = true;
        courseOffering.RosterFinalizedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc);
        await dbContext.SaveChangesAsync();

        var service = CreateEnrollmentService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.EnrollAsync(new EnrollStudentCommand
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = courseOffering.Id
        }));

        Assert.Equal("Course offering roster was already finalized.", exception.Message);

        var enrollments = await dbContext.EnrollmentsSet
            .Where(x => x.CourseOfferingId == courseOffering.Id)
            .ToListAsync();

        Assert.Single(enrollments);
        var unchangedDroppedEnrollment = enrollments.Single();
        Assert.Equal(EnrollmentStatus.Dropped, unchangedDroppedEnrollment.Status);
        Assert.NotNull(unchangedDroppedEnrollment.DroppedAtUtc);
    }

    [Fact]
    public async Task DropAsync_ShouldFail_WhenCourseOfferingRosterIsFinalized()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile = await SeedStudentProfileAsync(dbContext, "SV001");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 2);
        courseOffering.IsRosterFinalized = true;
        courseOffering.RosterFinalizedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc);
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

        var service = CreateEnrollmentService(dbContext);
        var exception = await Assert.ThrowsAsync<AuthException>(() => service.DropAsync(new DropEnrollmentCommand
        {
            Id = enrollment.Id
        }));

        Assert.Equal("Course offering roster was already finalized.", exception.Message);
        var unchangedEnrollment = await dbContext.EnrollmentsSet.SingleAsync(x => x.Id == enrollment.Id);
        Assert.Equal(EnrollmentStatus.Enrolled, unchangedEnrollment.Status);
        Assert.Null(unchangedEnrollment.DroppedAtUtc);
    }

    [Fact]
    public async Task GetListAsync_ShouldFilterByStatus()
    {
        await using var dbContext = CreateDbContext();
        var studentProfile1 = await SeedStudentProfileAsync(dbContext, "SV001");
        var studentProfile2 = await SeedStudentProfileAsync(dbContext, "SV002");
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CS101-HK1-01", 3);
        dbContext.EnrollmentsSet.AddRange(
            new Enrollment
            {
                StudentProfileId = studentProfile1.Id,
                CourseOfferingId = courseOffering.Id,
                Status = EnrollmentStatus.Enrolled,
                EnrolledAtUtc = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "seed"
            },
            new Enrollment
            {
                StudentProfileId = studentProfile2.Id,
                CourseOfferingId = courseOffering.Id,
                Status = EnrollmentStatus.Dropped,
                EnrolledAtUtc = new DateTime(2026, 3, 18, 0, 0, 0, DateTimeKind.Utc),
                DroppedAtUtc = new DateTime(2026, 3, 19, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "seed"
            });
        await dbContext.SaveChangesAsync();

        var service = CreateEnrollmentService(dbContext);
        var result = await service.GetListAsync(new GetEnrollmentsQuery
        {
            Status = EnrollmentStatus.Enrolled
        });

        Assert.Single(result);
        Assert.Equal("SV001", result.Single().StudentCode);
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
            Code = "CNTT",
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
            CreatedBy = "seed"
        };

        dbContext.StudentProfilesSet.Add(studentProfile);
        await dbContext.SaveChangesAsync();
        return studentProfile;
    }

    private static async Task<Course> SeedCourseAsync(AppDbContext dbContext, string? code = null, int credits = 3)
    {
        var course = new Course
        {
            Code = code ?? $"CS{100 + dbContext.CoursesSet.Count()}",
            Name = "Nhap mon lap trinh",
            Credits = credits,
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

    private static async Task<CourseOffering> SeedCourseOfferingAsync(
        AppDbContext dbContext,
        string code,
        int capacity,
        Course? course = null,
        Semester? semester = null,
        int dayOfWeek = 0,
        int startPeriod = 0,
        int endPeriod = 0)
    {
        course ??= await SeedCourseAsync(dbContext);
        semester ??= await SeedSemesterAsync(dbContext);
        var offering = new CourseOffering
        {
            Code = code,
            CourseId = course.Id,
            SemesterId = semester.Id,
            DisplayName = $"Offering {code}",
            Capacity = capacity,
            DayOfWeek = dayOfWeek,
            StartPeriod = startPeriod,
            EndPeriod = endPeriod,
            Status = CourseOfferingStatus.Active,
            CreatedBy = "seed",
            Course = course,
            Semester = semester
        };

        dbContext.CourseOfferingsSet.Add(offering);
        await dbContext.SaveChangesAsync();
        return offering;
    }

    private static EnrollmentService CreateEnrollmentService(AppDbContext dbContext)
    {
        return new EnrollmentService(
            dbContext,
            new AuditService(dbContext, new FakeClientContextAccessor()),
            new FakeCurrentUser(),
            new FakeDateTimeProvider(),
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Enrollment:MaxCreditsPerSemester"] = "24"
                })
                .Build());
    }

    private static async Task SeedPassedGradeResultAsync(AppDbContext dbContext, StudentProfile studentProfile, CourseOffering courseOffering)
    {
        var enrollment = new Enrollment
        {
            StudentProfileId = studentProfile.Id,
            CourseOfferingId = courseOffering.Id,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAtUtc = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed"
        };
        dbContext.EnrollmentsSet.Add(enrollment);
        await dbContext.SaveChangesAsync();

        var snapshot = new CourseOfferingRosterSnapshot
        {
            CourseOfferingId = courseOffering.Id,
            FinalizedAtUtc = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            FinalizedBy = "seed",
            ItemCount = 1,
            CreatedBy = "seed"
        };
        dbContext.CourseOfferingRosterSnapshotsSet.Add(snapshot);
        await dbContext.SaveChangesAsync();

        var rosterItem = new CourseOfferingRosterItem
        {
            RosterSnapshotId = snapshot.Id,
            EnrollmentId = enrollment.Id,
            StudentProfileId = studentProfile.Id,
            StudentCode = studentProfile.StudentCode,
            StudentFullName = studentProfile.FullName,
            StudentClassName = "KTPM",
            CourseOfferingCode = courseOffering.Code,
            CourseCode = courseOffering.Course?.Code ?? string.Empty,
            CourseName = courseOffering.Course?.Name ?? string.Empty,
            SemesterName = courseOffering.Semester?.Name ?? string.Empty,
            CreatedBy = "seed"
        };
        dbContext.CourseOfferingRosterItemsSet.Add(rosterItem);
        await dbContext.SaveChangesAsync();

        dbContext.GradeResultsSet.Add(new GradeResult
        {
            CourseOfferingId = courseOffering.Id,
            CourseOfferingRosterSnapshotId = snapshot.Id,
            RosterItemId = rosterItem.Id,
            WeightedFinalScore = 8.5m,
            PassingScore = 5m,
            IsPassed = true,
            CalculatedAtUtc = new DateTime(2026, 3, 16, 0, 0, 0, DateTimeKind.Utc),
            CalculatedBy = "seed",
            CreatedBy = "seed"
        });
        await dbContext.SaveChangesAsync();
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("88888888-8888-8888-8888-888888888888");
        public string? Username => "enrollment-admin";
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions => [PermissionConstants.Enrollments.View, PermissionConstants.Enrollments.Create, PermissionConstants.Enrollments.Override, PermissionConstants.Enrollments.Delete];
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "EnrollmentTests";
        public string ClientType => "Tests";
    }
}
