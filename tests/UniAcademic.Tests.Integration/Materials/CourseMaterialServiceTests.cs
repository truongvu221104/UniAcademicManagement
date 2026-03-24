using Microsoft.EntityFrameworkCore;
using UniAcademic.Application.Abstractions.Common;
using UniAcademic.Application.Abstractions.Storage;
using UniAcademic.Application.Common;
using UniAcademic.Application.Features.Materials;
using UniAcademic.Application.Models.Materials;
using UniAcademic.Application.Security;
using UniAcademic.Domain.Entities.Academic;
using UniAcademic.Domain.Enums;
using UniAcademic.Infrastructure.Persistence;
using UniAcademic.Infrastructure.Services.Auth;
using Xunit;

namespace UniAcademic.Tests.Integration.Materials;

public sealed class CourseMaterialServiceTests
{
    [Fact]
    public async Task UploadAsync_ShouldCreateCourseMaterial_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var storage = new FakeLocalFileStorage();
        var service = CreateCourseMaterialService(dbContext, storage);

        var result = await service.UploadAsync(new UploadCourseMaterialCommand
        {
            CourseOfferingId = courseOffering.Id,
            Title = "  De cuong hoc phan  ",
            Description = "  Tai lieu mo dau  ",
            MaterialType = CourseMaterialType.Document,
            SortOrder = 1,
            IsPublished = false,
            OriginalFileName = "syllabus.pdf",
            ContentType = "application/pdf",
            SizeInBytes = 128,
            FileContent = CreateFileStream("pdf-content")
        });

        Assert.Equal("De cuong hoc phan", result.Title);
        Assert.Equal("Tai lieu mo dau", result.Description);
        Assert.Equal("syllabus.pdf", result.OriginalFileName);
        Assert.Equal(128, result.SizeInBytes);
        Assert.False(result.IsPublished);

        Assert.Single(dbContext.FileMetadatasSet);
        Assert.Single(dbContext.CourseMaterialsSet);
        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "coursematerial.upload");
        Assert.Equal(nameof(CourseMaterial), audit.EntityType);
    }

    [Fact]
    public async Task UploadAsync_ShouldFail_WhenCourseOfferingDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateCourseMaterialService(dbContext, new FakeLocalFileStorage());

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.UploadAsync(new UploadCourseMaterialCommand
        {
            CourseOfferingId = Guid.NewGuid(),
            Title = "Document",
            MaterialType = CourseMaterialType.Document,
            OriginalFileName = "a.pdf",
            ContentType = "application/pdf",
            SizeInBytes = 1,
            FileContent = CreateFileStream("x")
        }));

        Assert.Equal("Course offering was not found.", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_ShouldFail_WhenCourseOfferingIsSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        courseOffering.IsDeleted = true;
        courseOffering.DeletedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc);
        courseOffering.DeletedBy = "seed";
        await dbContext.SaveChangesAsync();
        var service = CreateCourseMaterialService(dbContext, new FakeLocalFileStorage());

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.UploadAsync(new UploadCourseMaterialCommand
        {
            CourseOfferingId = courseOffering.Id,
            Title = "Document",
            MaterialType = CourseMaterialType.Document,
            OriginalFileName = "a.pdf",
            ContentType = "application/pdf",
            SizeInBytes = 1,
            FileContent = CreateFileStream("x")
        }));

        Assert.Equal("Course offering was not found.", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_ShouldFail_WhenFileIsMissing()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var service = CreateCourseMaterialService(dbContext, new FakeLocalFileStorage());

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.UploadAsync(new UploadCourseMaterialCommand
        {
            CourseOfferingId = courseOffering.Id,
            Title = "Document",
            MaterialType = CourseMaterialType.Document,
            OriginalFileName = "a.pdf",
            ContentType = "application/pdf",
            SizeInBytes = 0,
            FileContent = Stream.Null
        }));

        Assert.Equal("Material file is required.", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_ShouldFail_WhenFileSizeExceedsLimit()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var service = CreateCourseMaterialService(dbContext, new FakeLocalFileStorage { MaxFileSizeInBytesValue = 4 });

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.UploadAsync(new UploadCourseMaterialCommand
        {
            CourseOfferingId = courseOffering.Id,
            Title = "Document",
            MaterialType = CourseMaterialType.Document,
            OriginalFileName = "a.pdf",
            ContentType = "application/pdf",
            SizeInBytes = 10,
            FileContent = CreateFileStream("1234567890")
        }));

        Assert.Equal("Material file exceeds the allowed size limit.", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_ShouldFail_WhenFileTypeIsNotAllowed()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var service = CreateCourseMaterialService(dbContext, new FakeLocalFileStorage());

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.UploadAsync(new UploadCourseMaterialCommand
        {
            CourseOfferingId = courseOffering.Id,
            Title = "Document",
            MaterialType = CourseMaterialType.Document,
            OriginalFileName = "malware.exe",
            ContentType = "application/octet-stream",
            SizeInBytes = 10,
            FileContent = CreateFileStream("1234567890")
        }));

        Assert.Equal("Material file type is not allowed.", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_ShouldCreateFileMetadataAndCourseMaterial()
    {
        await using var dbContext = CreateDbContext();
        var courseOffering = await SeedCourseOfferingAsync(dbContext, "CO-01");
        var storage = new FakeLocalFileStorage();
        var service = CreateCourseMaterialService(dbContext, storage);

        await service.UploadAsync(new UploadCourseMaterialCommand
        {
            CourseOfferingId = courseOffering.Id,
            Title = "Slides",
            MaterialType = CourseMaterialType.Slide,
            OriginalFileName = "slides.pptx",
            ContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            SizeInBytes = 16,
            FileContent = CreateFileStream("pptx-content-123")
        });

        var fileMetadata = await dbContext.FileMetadatasSet.SingleAsync();
        var material = await dbContext.CourseMaterialsSet.SingleAsync();
        Assert.Equal(fileMetadata.Id, material.FileMetadataId);
        Assert.False(string.IsNullOrWhiteSpace(fileMetadata.RelativePath));
        Assert.True(await storage.ExistsAsync(fileMetadata.RelativePath));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnMaterialWithMetadata()
    {
        await using var dbContext = CreateDbContext();
        var storage = new FakeLocalFileStorage();
        var material = await SeedMaterialAsync(dbContext, storage, "CO-01", "slides.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation");
        var service = CreateCourseMaterialService(dbContext, storage);

        var result = await service.GetByIdAsync(new GetCourseMaterialByIdQuery { Id = material.Id });

        Assert.Equal(material.Id, result.Id);
        Assert.Equal("slides.pptx", result.OriginalFileName);
        Assert.Equal("application/vnd.openxmlformats-officedocument.presentationml.presentation", result.ContentType);
        Assert.True(result.SizeInBytes > 0);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateMetadata_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var storage = new FakeLocalFileStorage();
        var material = await SeedMaterialAsync(dbContext, storage, "CO-01", "slides.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation");
        var service = CreateCourseMaterialService(dbContext, storage);

        var result = await service.UpdateAsync(new UpdateCourseMaterialCommand
        {
            Id = material.Id,
            Title = "  Lecture Slides  ",
            Description = "  Updated  ",
            MaterialType = CourseMaterialType.Slide,
            SortOrder = 3
        });

        Assert.Equal("Lecture Slides", result.Title);
        Assert.Equal("Updated", result.Description);
        Assert.Equal(3, result.SortOrder);
        Assert.Equal("slides.pptx", result.OriginalFileName);

        var audit = await dbContext.AuditLogs.SingleAsync(x => x.Action == "coursematerial.update");
        Assert.Equal(material.Id.ToString(), audit.EntityId);
    }

    [Fact]
    public async Task SetPublishStateAsync_ShouldPublishAndUnpublish_AndWriteAudit()
    {
        await using var dbContext = CreateDbContext();
        var storage = new FakeLocalFileStorage();
        var material = await SeedMaterialAsync(dbContext, storage, "CO-01", "slides.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation");
        var service = CreateCourseMaterialService(dbContext, storage);

        var published = await service.SetPublishStateAsync(new SetCourseMaterialPublishStateCommand
        {
            Id = material.Id,
            IsPublished = true
        });
        var hidden = await service.SetPublishStateAsync(new SetCourseMaterialPublishStateCommand
        {
            Id = material.Id,
            IsPublished = false
        });

        Assert.True(published.IsPublished);
        Assert.False(hidden.IsPublished);
        Assert.Single(dbContext.AuditLogs.Where(x => x.Action == "coursematerial.publish"));
        Assert.Single(dbContext.AuditLogs.Where(x => x.Action == "coursematerial.unpublish"));
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnFileContent()
    {
        await using var dbContext = CreateDbContext();
        var storage = new FakeLocalFileStorage();
        var material = await SeedMaterialAsync(dbContext, storage, "CO-01", "slides.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation", "download-content");
        var service = CreateCourseMaterialService(dbContext, storage);

        await using var result = await service.DownloadAsync(new DownloadCourseMaterialQuery { Id = material.Id });
        using var reader = new StreamReader(result.Content);
        var content = await reader.ReadToEndAsync();

        Assert.Equal("slides.pptx", result.FileName);
        Assert.Equal("download-content", content);
    }

    [Fact]
    public async Task DownloadAsync_ShouldFail_WhenFileIsMissingOnDisk()
    {
        await using var dbContext = CreateDbContext();
        var storage = new FakeLocalFileStorage();
        var material = await SeedMaterialAsync(dbContext, storage, "CO-01", "slides.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation");
        storage.Remove(material.FileMetadata!.RelativePath);
        var service = CreateCourseMaterialService(dbContext, storage);

        var exception = await Assert.ThrowsAsync<AuthException>(() => service.DownloadAsync(new DownloadCourseMaterialQuery { Id = material.Id }));

        Assert.Equal("Material file was not found on disk.", exception.Message);
    }

    private static MemoryStream CreateFileStream(string content)
    {
        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
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

    private static async Task<CourseMaterial> SeedMaterialAsync(AppDbContext dbContext, FakeLocalFileStorage storage, string courseOfferingCode, string fileName, string contentType, string content = "seed-content")
    {
        var courseOffering = await SeedCourseOfferingAsync(dbContext, courseOfferingCode);
        var saveRequest = new LocalFileSaveRequest
        {
            CourseOfferingId = courseOffering.Id,
            OriginalFileName = fileName,
            Content = CreateFileStream(content)
        };
        var stored = await storage.SaveAsync(saveRequest);

        var fileMetadata = new FileMetadata
        {
            OriginalFileName = fileName,
            RelativePath = stored.RelativePath,
            ContentType = contentType,
            SizeInBytes = System.Text.Encoding.UTF8.GetByteCount(content),
            UploadedAtUtc = new DateTime(2026, 3, 24, 0, 0, 0, DateTimeKind.Utc),
            UploadedBy = "seed",
            CreatedBy = "seed"
        };
        var material = new CourseMaterial
        {
            CourseOfferingId = courseOffering.Id,
            FileMetadata = fileMetadata,
            Title = "Seed material",
            MaterialType = CourseMaterialType.Other,
            SortOrder = 0,
            IsPublished = false,
            CreatedBy = "seed",
            CourseOffering = courseOffering
        };

        dbContext.CourseMaterialsSet.Add(material);
        await dbContext.SaveChangesAsync();
        return material;
    }

    private static CourseMaterialService CreateCourseMaterialService(AppDbContext dbContext, ILocalFileStorage localFileStorage)
    {
        return new CourseMaterialService(
            dbContext,
            new AuditService(dbContext, new FakeClientContextAccessor()),
            new FakeCurrentUser(),
            new FakeDateTimeProvider(),
            localFileStorage);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public string? Username => "materials-admin";
        public Guid? SessionId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Permissions => [PermissionConstants.CourseMaterials.View, PermissionConstants.CourseMaterials.Create, PermissionConstants.CourseMaterials.Edit, PermissionConstants.CourseMaterials.Download];
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 3, 24, 8, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeClientContextAccessor : IClientContextAccessor
    {
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "CourseMaterialTests";
        public string ClientType => "Tests";
    }

    private sealed class FakeLocalFileStorage : ILocalFileStorage
    {
        private readonly Dictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);

        public long MaxFileSizeInBytes => MaxFileSizeInBytesValue;

        public long MaxFileSizeInBytesValue { get; set; } = 10 * 1024 * 1024;

        public async Task<StoredLocalFile> SaveAsync(LocalFileSaveRequest request, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            request.Content.Position = 0;
            await request.Content.CopyToAsync(buffer, cancellationToken);
            var extension = Path.GetExtension(request.OriginalFileName);
            var relativePath = $"course-materials/{request.CourseOfferingId}/{Guid.NewGuid():N}{extension}";
            _files[relativePath] = buffer.ToArray();
            return new StoredLocalFile
            {
                RelativePath = relativePath,
                StoredFileName = Path.GetFileName(relativePath)
            };
        }

        public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            Stream stream = new MemoryStream(_files[relativePath], writable: false);
            return Task.FromResult(stream);
        }

        public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_files.ContainsKey(relativePath));
        }

        public void Remove(string relativePath)
        {
            _files.Remove(relativePath);
        }
    }
}
