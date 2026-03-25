using UniAcademic.AdminApp.Dialogs;
using UniAcademic.AdminApp.Services.Attendance;
using UniAcademic.AdminApp.Services.CourseOfferings;
using UniAcademic.AdminApp.Services.Courses;
using UniAcademic.AdminApp.Services.Enrollments;
using UniAcademic.AdminApp.Services.Faculties;
using UniAcademic.AdminApp.Services.GradeResults;
using UniAcademic.AdminApp.Services.Grades;
using UniAcademic.AdminApp.Services.LecturerAssignments;
using UniAcademic.AdminApp.Services.LecturerProfiles;
using UniAcademic.AdminApp.Services.Materials;
using UniAcademic.AdminApp.Services.Rosters;
using UniAcademic.AdminApp.Services.Semesters;
using UniAcademic.AdminApp.Services.StudentClasses;
using UniAcademic.AdminApp.Services.StudentProfiles;
using UniAcademic.AdminApp.ViewModels;
using UniAcademic.Contracts.Attendance;
using UniAcademic.Contracts.CourseOfferings;
using UniAcademic.Contracts.Courses;
using UniAcademic.Contracts.Enrollments;
using UniAcademic.Contracts.Faculties;
using UniAcademic.Contracts.GradeResults;
using UniAcademic.Contracts.Grades;
using UniAcademic.Contracts.LecturerAssignments;
using UniAcademic.Contracts.LecturerProfiles;
using UniAcademic.Contracts.Materials;
using UniAcademic.Contracts.Rosters;
using UniAcademic.Contracts.Semesters;
using UniAcademic.Contracts.StudentClasses;
using UniAcademic.Contracts.StudentProfiles;

namespace UniAcademic.AdminApp.Navigation;

public sealed class AdminModuleCatalog
{
    private readonly IFacultyApiClient _facultyApiClient;
    private readonly IStudentClassApiClient _studentClassApiClient;
    private readonly ICourseApiClient _courseApiClient;
    private readonly ISemesterApiClient _semesterApiClient;
    private readonly IStudentProfileApiClient _studentProfileApiClient;
    private readonly ILecturerProfileApiClient _lecturerProfileApiClient;
    private readonly ICourseOfferingApiClient _courseOfferingApiClient;
    private readonly IEnrollmentApiClient _enrollmentApiClient;
    private readonly ICourseOfferingRosterApiClient _rosterApiClient;
    private readonly IAttendanceApiClient _attendanceApiClient;
    private readonly IGradeApiClient _gradeApiClient;
    private readonly IGradeResultApiClient _gradeResultApiClient;
    private readonly ICourseMaterialApiClient _courseMaterialApiClient;
    private readonly ILecturerAssignmentApiClient _lecturerAssignmentApiClient;
    private readonly IFormDialogService _formDialogService;
    private readonly IMessageDialogService _messageDialogService;
    private readonly ITextEditorDialogService _textEditorDialogService;
    private readonly IFileDialogService _fileDialogService;

    public AdminModuleCatalog(
        IFacultyApiClient facultyApiClient,
        IStudentClassApiClient studentClassApiClient,
        ICourseApiClient courseApiClient,
        ISemesterApiClient semesterApiClient,
        IStudentProfileApiClient studentProfileApiClient,
        ILecturerProfileApiClient lecturerProfileApiClient,
        ICourseOfferingApiClient courseOfferingApiClient,
        IEnrollmentApiClient enrollmentApiClient,
        ICourseOfferingRosterApiClient rosterApiClient,
        IAttendanceApiClient attendanceApiClient,
        IGradeApiClient gradeApiClient,
        IGradeResultApiClient gradeResultApiClient,
        ICourseMaterialApiClient courseMaterialApiClient,
        ILecturerAssignmentApiClient lecturerAssignmentApiClient,
        IFormDialogService formDialogService,
        IMessageDialogService messageDialogService,
        ITextEditorDialogService textEditorDialogService,
        IFileDialogService fileDialogService)
    {
        _facultyApiClient = facultyApiClient;
        _studentClassApiClient = studentClassApiClient;
        _courseApiClient = courseApiClient;
        _semesterApiClient = semesterApiClient;
        _studentProfileApiClient = studentProfileApiClient;
        _lecturerProfileApiClient = lecturerProfileApiClient;
        _courseOfferingApiClient = courseOfferingApiClient;
        _enrollmentApiClient = enrollmentApiClient;
        _rosterApiClient = rosterApiClient;
        _attendanceApiClient = attendanceApiClient;
        _gradeApiClient = gradeApiClient;
        _gradeResultApiClient = gradeResultApiClient;
        _courseMaterialApiClient = courseMaterialApiClient;
        _lecturerAssignmentApiClient = lecturerAssignmentApiClient;
        _formDialogService = formDialogService;
        _messageDialogService = messageDialogService;
        _textEditorDialogService = textEditorDialogService;
        _fileDialogService = fileDialogService;
    }

    public IReadOnlyCollection<ModuleDefinition> GetModules()
    {
        return
        [
            new ModuleDefinition { Group = "Master Data", Title = "Faculties", Create = CreateFacultyModule },
            new ModuleDefinition { Group = "Master Data", Title = "Student Classes", Create = CreateStudentClassModule },
            new ModuleDefinition { Group = "Master Data", Title = "Courses", Create = CreateCourseModule },
            new ModuleDefinition { Group = "Master Data", Title = "Semesters", Create = CreateSemesterModule },
            new ModuleDefinition { Group = "Master Data", Title = "Student Profiles", Create = CreateStudentProfileModule },
            new ModuleDefinition { Group = "Master Data", Title = "Lecturer Profiles", Create = CreateLecturerProfileModule },
            new ModuleDefinition { Group = "Academic Operations", Title = "Course Offerings", Create = CreateCourseOfferingModule },
            new ModuleDefinition { Group = "Academic Operations", Title = "Enrollments", Create = CreateEnrollmentModule },
            new ModuleDefinition { Group = "Academic Operations", Title = "Rosters", Create = CreateRosterModule },
            new ModuleDefinition { Group = "Academic Operations", Title = "Attendance", Create = CreateAttendanceModule },
            new ModuleDefinition { Group = "Academic Operations", Title = "Grades", Create = CreateGradeModule },
            new ModuleDefinition { Group = "Academic Operations", Title = "Grade Results", Create = CreateGradeResultModule },
            new ModuleDefinition { Group = "Academic Operations", Title = "Course Materials", Create = CreateMaterialModule },
            new ModuleDefinition { Group = "Academic Operations", Title = "Lecturer Assignments", Create = CreateLecturerAssignmentModule }
        ];
    }

    private ModulePageViewModel CreateFacultyModule()
    {
        var module = CreateModule("Faculties", ct => LoadObjectsAsync(_facultyApiClient.GetListAsync(cancellationToken: ct)), (item, ct) => AsObjectTask(_facultyApiClient.GetByIdAsync(GetId(item), ct)!));
        module.AddAction(new ModuleActionViewModel("Create", module, async vm =>
        {
            var fields = Fields(("Code", typeof(string)), ("Name", typeof(string)), ("Description", typeof(string), (object?)null, true), ("Status", typeof(UniAcademic.Domain.Enums.FacultyStatus)));
            if (!_formDialogService.Show("Create Faculty", fields)) return;
            await _facultyApiClient.CreateAsync(new CreateFacultyRequest
            {
                Code = Get<string>(fields, "Code"),
                Name = Get<string>(fields, "Name"),
                Description = Get<string?>(fields, "Description"),
                Status = Get<UniAcademic.Domain.Enums.FacultyStatus>(fields, "Status")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Faculty created.");
        }));
        module.AddAction(new ModuleActionViewModel("Edit", module, async vm =>
        {
            if (vm.SelectedItem is not FacultyListItemResponse selected) return;
            var detail = await _facultyApiClient.GetByIdAsync(selected.Id);
            var fields = Fields(("Code", typeof(string), detail.Code), ("Name", typeof(string), detail.Name), ("Description", typeof(string), detail.Description, true), ("Status", typeof(UniAcademic.Domain.Enums.FacultyStatus), detail.Status));
            if (!_formDialogService.Show("Edit Faculty", fields)) return;
            await _facultyApiClient.UpdateAsync(selected.Id, new UpdateFacultyRequest
            {
                Code = Get<string>(fields, "Code"),
                Name = Get<string>(fields, "Name"),
                Description = Get<string?>(fields, "Description"),
                Status = Get<UniAcademic.Domain.Enums.FacultyStatus>(fields, "Status")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Faculty updated.");
        }, vm => vm.SelectedItem is FacultyListItemResponse));
        return module;
    }

    private ModulePageViewModel CreateStudentClassModule()
    {
        var module = CreateModule("Student Classes", ct => LoadObjectsAsync(_studentClassApiClient.GetListAsync(cancellationToken: ct)), (item, ct) => AsObjectTask(_studentClassApiClient.GetByIdAsync(GetId(item), ct)!));
        module.AddAction(new ModuleActionViewModel("Create", module, async vm =>
        {
            var fields = Fields(("Code", typeof(string)), ("Name", typeof(string)), ("FacultyId", typeof(Guid)), ("IntakeYear", typeof(int), DateTime.UtcNow.Year), ("Status", typeof(UniAcademic.Domain.Enums.StudentClassStatus)), ("Description", typeof(string), (object?)null, true));
            if (!_formDialogService.Show("Create Student Class", fields)) return;
            await _studentClassApiClient.CreateAsync(new CreateStudentClassRequest
            {
                Code = Get<string>(fields, "Code"),
                Name = Get<string>(fields, "Name"),
                FacultyId = Get<Guid>(fields, "FacultyId"),
                IntakeYear = Get<int>(fields, "IntakeYear"),
                Status = Get<UniAcademic.Domain.Enums.StudentClassStatus>(fields, "Status"),
                Description = Get<string?>(fields, "Description")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Student class created.");
        }));
        module.AddAction(new ModuleActionViewModel("Edit", module, async vm =>
        {
            if (vm.SelectedItem is not StudentClassListItemResponse selected) return;
            var detail = await _studentClassApiClient.GetByIdAsync(selected.Id);
            var fields = Fields(("Code", typeof(string), detail.Code), ("Name", typeof(string), detail.Name), ("FacultyId", typeof(Guid), detail.FacultyId), ("IntakeYear", typeof(int), detail.IntakeYear), ("Status", typeof(UniAcademic.Domain.Enums.StudentClassStatus), detail.Status), ("Description", typeof(string), detail.Description, true));
            if (!_formDialogService.Show("Edit Student Class", fields)) return;
            await _studentClassApiClient.UpdateAsync(selected.Id, new UpdateStudentClassRequest
            {
                Code = Get<string>(fields, "Code"),
                Name = Get<string>(fields, "Name"),
                FacultyId = Get<Guid>(fields, "FacultyId"),
                IntakeYear = Get<int>(fields, "IntakeYear"),
                Status = Get<UniAcademic.Domain.Enums.StudentClassStatus>(fields, "Status"),
                Description = Get<string?>(fields, "Description")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Student class updated.");
        }, vm => vm.SelectedItem is StudentClassListItemResponse));
        return module;
    }

    private ModulePageViewModel CreateCourseModule()
    {
        var module = CreateModule("Courses", ct => LoadObjectsAsync(_courseApiClient.GetListAsync(cancellationToken: ct)), (item, ct) => AsObjectTask(_courseApiClient.GetByIdAsync(GetId(item), ct)!));
        module.AddAction(new ModuleActionViewModel("Create", module, async vm =>
        {
            var fields = Fields(("Code", typeof(string)), ("Name", typeof(string)), ("Credits", typeof(int), 3), ("FacultyId", typeof(Guid?)), ("Status", typeof(UniAcademic.Domain.Enums.CourseStatus)), ("Description", typeof(string), (object?)null, true));
            if (!_formDialogService.Show("Create Course", fields)) return;
            await _courseApiClient.CreateAsync(new CreateCourseRequest
            {
                Code = Get<string>(fields, "Code"),
                Name = Get<string>(fields, "Name"),
                Credits = Get<int>(fields, "Credits"),
                FacultyId = Get<Guid?>(fields, "FacultyId"),
                Status = Get<UniAcademic.Domain.Enums.CourseStatus>(fields, "Status"),
                Description = Get<string?>(fields, "Description")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Course created.");
        }));
        module.AddAction(new ModuleActionViewModel("Edit", module, async vm =>
        {
            if (vm.SelectedItem is not CourseListItemResponse selected) return;
            var detail = await _courseApiClient.GetByIdAsync(selected.Id);
            var fields = Fields(("Code", typeof(string), detail.Code), ("Name", typeof(string), detail.Name), ("Credits", typeof(int), detail.Credits), ("FacultyId", typeof(Guid?), detail.FacultyId), ("Status", typeof(UniAcademic.Domain.Enums.CourseStatus), detail.Status), ("Description", typeof(string), detail.Description, true));
            if (!_formDialogService.Show("Edit Course", fields)) return;
            await _courseApiClient.UpdateAsync(selected.Id, new UpdateCourseRequest
            {
                Code = Get<string>(fields, "Code"),
                Name = Get<string>(fields, "Name"),
                Credits = Get<int>(fields, "Credits"),
                FacultyId = Get<Guid?>(fields, "FacultyId"),
                Status = Get<UniAcademic.Domain.Enums.CourseStatus>(fields, "Status"),
                Description = Get<string?>(fields, "Description")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Course updated.");
        }, vm => vm.SelectedItem is CourseListItemResponse));
        return module;
    }

    private ModulePageViewModel CreateSemesterModule()
    {
        var module = CreateModule("Semesters", ct => LoadObjectsAsync(_semesterApiClient.GetListAsync(cancellationToken: ct)), (item, ct) => AsObjectTask(_semesterApiClient.GetByIdAsync(GetId(item), ct)!));
        module.AddAction(new ModuleActionViewModel("Create", module, async vm =>
        {
            var fields = Fields(("Code", typeof(string)), ("Name", typeof(string)), ("AcademicYear", typeof(string), "2025-2026"), ("TermNo", typeof(int), 1), ("StartDate", typeof(DateTime), DateTime.Today), ("EndDate", typeof(DateTime), DateTime.Today.AddMonths(4)), ("Status", typeof(UniAcademic.Domain.Enums.SemesterStatus)), ("Description", typeof(string), (object?)null, true));
            if (!_formDialogService.Show("Create Semester", fields)) return;
            await _semesterApiClient.CreateAsync(new CreateSemesterRequest
            {
                Code = Get<string>(fields, "Code"),
                Name = Get<string>(fields, "Name"),
                AcademicYear = Get<string>(fields, "AcademicYear"),
                TermNo = Get<int>(fields, "TermNo"),
                StartDate = Get<DateTime>(fields, "StartDate"),
                EndDate = Get<DateTime>(fields, "EndDate"),
                Status = Get<UniAcademic.Domain.Enums.SemesterStatus>(fields, "Status"),
                Description = Get<string?>(fields, "Description")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Semester created.");
        }));
        module.AddAction(new ModuleActionViewModel("Edit", module, async vm =>
        {
            if (vm.SelectedItem is not SemesterListItemResponse selected) return;
            var detail = await _semesterApiClient.GetByIdAsync(selected.Id);
            var fields = Fields(("Code", typeof(string), detail.Code), ("Name", typeof(string), detail.Name), ("AcademicYear", typeof(string), detail.AcademicYear), ("TermNo", typeof(int), detail.TermNo), ("StartDate", typeof(DateTime), detail.StartDate), ("EndDate", typeof(DateTime), detail.EndDate), ("Status", typeof(UniAcademic.Domain.Enums.SemesterStatus), detail.Status), ("Description", typeof(string), detail.Description, true));
            if (!_formDialogService.Show("Edit Semester", fields)) return;
            await _semesterApiClient.UpdateAsync(selected.Id, new UpdateSemesterRequest
            {
                Code = Get<string>(fields, "Code"),
                Name = Get<string>(fields, "Name"),
                AcademicYear = Get<string>(fields, "AcademicYear"),
                TermNo = Get<int>(fields, "TermNo"),
                StartDate = Get<DateTime>(fields, "StartDate"),
                EndDate = Get<DateTime>(fields, "EndDate"),
                Status = Get<UniAcademic.Domain.Enums.SemesterStatus>(fields, "Status"),
                Description = Get<string?>(fields, "Description")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Semester updated.");
        }, vm => vm.SelectedItem is SemesterListItemResponse));
        return module;
    }

    private ModulePageViewModel CreateStudentProfileModule()
    {
        var module = CreateModule("Student Profiles", ct => LoadObjectsAsync(_studentProfileApiClient.GetListAsync(cancellationToken: ct)), (item, ct) => AsObjectTask(_studentProfileApiClient.GetByIdAsync(GetId(item), ct)!));
        module.AddAction(new ModuleActionViewModel("Create", module, async vm =>
        {
            var fields = Fields(("StudentCode", typeof(string)), ("FullName", typeof(string)), ("StudentClassId", typeof(Guid)), ("Email", typeof(string)), ("Phone", typeof(string)), ("DateOfBirth", typeof(DateTime?)), ("Gender", typeof(UniAcademic.Domain.Enums.StudentGender)), ("Address", typeof(string), (object?)null, true), ("Status", typeof(UniAcademic.Domain.Enums.StudentProfileStatus)), ("Note", typeof(string), (object?)null, true));
            if (!_formDialogService.Show("Create Student Profile", fields)) return;
            await _studentProfileApiClient.CreateAsync(new CreateStudentProfileRequest
            {
                StudentCode = Get<string>(fields, "StudentCode"),
                FullName = Get<string>(fields, "FullName"),
                StudentClassId = Get<Guid>(fields, "StudentClassId"),
                Email = Get<string?>(fields, "Email"),
                Phone = Get<string?>(fields, "Phone"),
                DateOfBirth = Get<DateTime?>(fields, "DateOfBirth"),
                Gender = Get<UniAcademic.Domain.Enums.StudentGender>(fields, "Gender"),
                Address = Get<string?>(fields, "Address"),
                Status = Get<UniAcademic.Domain.Enums.StudentProfileStatus>(fields, "Status"),
                Note = Get<string?>(fields, "Note")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Student profile created.");
        }));
        module.AddAction(new ModuleActionViewModel("Edit", module, async vm =>
        {
            if (vm.SelectedItem is not StudentProfileListItemResponse selected) return;
            var detail = await _studentProfileApiClient.GetByIdAsync(selected.Id);
            var fields = Fields(("StudentCode", typeof(string), detail.StudentCode), ("FullName", typeof(string), detail.FullName), ("StudentClassId", typeof(Guid), detail.StudentClassId), ("Email", typeof(string), detail.Email), ("Phone", typeof(string), detail.Phone), ("DateOfBirth", typeof(DateTime?), detail.DateOfBirth), ("Gender", typeof(UniAcademic.Domain.Enums.StudentGender), detail.Gender), ("Address", typeof(string), detail.Address, true), ("Status", typeof(UniAcademic.Domain.Enums.StudentProfileStatus), detail.Status), ("Note", typeof(string), detail.Note, true));
            if (!_formDialogService.Show("Edit Student Profile", fields)) return;
            await _studentProfileApiClient.UpdateAsync(selected.Id, new UpdateStudentProfileRequest
            {
                StudentCode = Get<string>(fields, "StudentCode"),
                FullName = Get<string>(fields, "FullName"),
                StudentClassId = Get<Guid>(fields, "StudentClassId"),
                Email = Get<string?>(fields, "Email"),
                Phone = Get<string?>(fields, "Phone"),
                DateOfBirth = Get<DateTime?>(fields, "DateOfBirth"),
                Gender = Get<UniAcademic.Domain.Enums.StudentGender>(fields, "Gender"),
                Address = Get<string?>(fields, "Address"),
                Status = Get<UniAcademic.Domain.Enums.StudentProfileStatus>(fields, "Status"),
                Note = Get<string?>(fields, "Note")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Student profile updated.");
        }, vm => vm.SelectedItem is StudentProfileListItemResponse));
        return module;
    }

    private ModulePageViewModel CreateLecturerProfileModule()
    {
        var module = CreateModule("Lecturer Profiles", ct => LoadObjectsAsync(_lecturerProfileApiClient.GetListAsync(cancellationToken: ct)), (item, ct) => AsObjectTask(_lecturerProfileApiClient.GetByIdAsync(GetId(item), ct)!));
        module.AddAction(new ModuleActionViewModel("Create", module, async vm =>
        {
            var fields = Fields(("Code", typeof(string)), ("FullName", typeof(string)), ("Email", typeof(string)), ("PhoneNumber", typeof(string)), ("FacultyId", typeof(Guid)), ("IsActive", typeof(bool), true), ("Note", typeof(string), (object?)null, true));
            if (!_formDialogService.Show("Create Lecturer Profile", fields)) return;
            await _lecturerProfileApiClient.CreateAsync(new CreateLecturerProfileRequest
            {
                Code = Get<string>(fields, "Code"),
                FullName = Get<string>(fields, "FullName"),
                Email = Get<string?>(fields, "Email"),
                PhoneNumber = Get<string?>(fields, "PhoneNumber"),
                FacultyId = Get<Guid>(fields, "FacultyId"),
                IsActive = Get<bool>(fields, "IsActive"),
                Note = Get<string?>(fields, "Note")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Lecturer profile created.");
        }));
        module.AddAction(new ModuleActionViewModel("Edit", module, async vm =>
        {
            if (vm.SelectedItem is not LecturerProfileListItemResponse selected) return;
            var detail = await _lecturerProfileApiClient.GetByIdAsync(selected.Id);
            var fields = Fields(("Code", typeof(string), detail.Code), ("FullName", typeof(string), detail.FullName), ("Email", typeof(string), detail.Email), ("PhoneNumber", typeof(string), detail.PhoneNumber), ("FacultyId", typeof(Guid), detail.FacultyId), ("IsActive", typeof(bool), detail.IsActive), ("Note", typeof(string), detail.Note, true));
            if (!_formDialogService.Show("Edit Lecturer Profile", fields)) return;
            await _lecturerProfileApiClient.UpdateAsync(selected.Id, new UpdateLecturerProfileRequest
            {
                Code = Get<string>(fields, "Code"),
                FullName = Get<string>(fields, "FullName"),
                Email = Get<string?>(fields, "Email"),
                PhoneNumber = Get<string?>(fields, "PhoneNumber"),
                FacultyId = Get<Guid>(fields, "FacultyId"),
                IsActive = Get<bool>(fields, "IsActive"),
                Note = Get<string?>(fields, "Note")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Lecturer profile updated.");
        }, vm => vm.SelectedItem is LecturerProfileListItemResponse));
        module.AddAction(new ModuleActionViewModel("Soft Delete", module, async vm =>
        {
            if (vm.SelectedItem is not LecturerProfileListItemResponse selected) return;
            if (!_messageDialogService.Confirm($"Soft delete lecturer '{selected.FullName}'?")) return;
            await _lecturerProfileApiClient.DeleteAsync(selected.Id);
            await vm.RefreshAsync();
            vm.NotifySuccess("Lecturer profile deleted.");
        }, vm => vm.SelectedItem is LecturerProfileListItemResponse));
        return module;
    }

    private ModulePageViewModel CreateCourseOfferingModule()
    {
        var module = CreateModule("Course Offerings", ct => LoadObjectsAsync(_courseOfferingApiClient.GetListAsync(cancellationToken: ct)), (item, ct) => AsObjectTask(_courseOfferingApiClient.GetByIdAsync(GetId(item), ct)!));
        module.AddAction(new ModuleActionViewModel("Create", module, async vm =>
        {
            var fields = Fields(("Code", typeof(string)), ("CourseId", typeof(Guid)), ("SemesterId", typeof(Guid)), ("DisplayName", typeof(string)), ("Capacity", typeof(int), 30), ("Status", typeof(UniAcademic.Domain.Enums.CourseOfferingStatus)), ("Description", typeof(string), (object?)null, true));
            if (!_formDialogService.Show("Create Course Offering", fields)) return;
            await _courseOfferingApiClient.CreateAsync(new CreateCourseOfferingRequest
            {
                Code = Get<string>(fields, "Code"),
                CourseId = Get<Guid>(fields, "CourseId"),
                SemesterId = Get<Guid>(fields, "SemesterId"),
                DisplayName = Get<string>(fields, "DisplayName"),
                Capacity = Get<int>(fields, "Capacity"),
                Status = Get<UniAcademic.Domain.Enums.CourseOfferingStatus>(fields, "Status"),
                Description = Get<string?>(fields, "Description")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Course offering created.");
        }));
        module.AddAction(new ModuleActionViewModel("Edit", module, async vm =>
        {
            if (vm.SelectedItem is not CourseOfferingListItemResponse selected) return;
            var detail = await _courseOfferingApiClient.GetByIdAsync(selected.Id);
            var fields = Fields(("Code", typeof(string), detail.Code), ("CourseId", typeof(Guid), detail.CourseId), ("SemesterId", typeof(Guid), detail.SemesterId), ("DisplayName", typeof(string), detail.DisplayName), ("Capacity", typeof(int), detail.Capacity), ("Status", typeof(UniAcademic.Domain.Enums.CourseOfferingStatus), detail.Status), ("Description", typeof(string), detail.Description, true));
            if (!_formDialogService.Show("Edit Course Offering", fields)) return;
            await _courseOfferingApiClient.UpdateAsync(selected.Id, new UpdateCourseOfferingRequest
            {
                Code = Get<string>(fields, "Code"),
                CourseId = Get<Guid>(fields, "CourseId"),
                SemesterId = Get<Guid>(fields, "SemesterId"),
                DisplayName = Get<string>(fields, "DisplayName"),
                Capacity = Get<int>(fields, "Capacity"),
                Status = Get<UniAcademic.Domain.Enums.CourseOfferingStatus>(fields, "Status"),
                Description = Get<string?>(fields, "Description")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Course offering updated.");
        }, vm => vm.SelectedItem is CourseOfferingListItemResponse));
        return module;
    }

    private ModulePageViewModel CreateEnrollmentModule()
    {
        var module = CreateModule("Enrollments", ct => LoadObjectsAsync(_enrollmentApiClient.GetListAsync(cancellationToken: ct)), (item, ct) => AsObjectTask(_enrollmentApiClient.GetByIdAsync(GetId(item), ct)!));
        module.AddAction(new ModuleActionViewModel("Enroll", module, async vm =>
        {
            var fields = Fields(("StudentProfileId", typeof(Guid)), ("CourseOfferingId", typeof(Guid)), ("Note", typeof(string), (object?)null, true));
            if (!_formDialogService.Show("Enroll Student", fields)) return;
            await _enrollmentApiClient.CreateAsync(new CreateEnrollmentRequest
            {
                StudentProfileId = Get<Guid>(fields, "StudentProfileId"),
                CourseOfferingId = Get<Guid>(fields, "CourseOfferingId"),
                Note = Get<string?>(fields, "Note")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Enrollment created.");
        }));
        module.AddAction(new ModuleActionViewModel("Drop", module, async vm =>
        {
            if (vm.SelectedItem is not EnrollmentListItemResponse selected) return;
            if (!_messageDialogService.Confirm($"Drop enrollment for {selected.StudentFullName}?")) return;
            await _enrollmentApiClient.DeleteAsync(selected.Id);
            await vm.RefreshAsync();
            vm.NotifySuccess("Enrollment dropped.");
        }, vm => vm.SelectedItem is EnrollmentListItemResponse));
        module.AddAction(new ModuleActionViewModel("Reactivate", module, async vm =>
        {
            if (vm.SelectedItem is not EnrollmentListItemResponse selected) return;
            await _enrollmentApiClient.CreateAsync(new CreateEnrollmentRequest
            {
                StudentProfileId = selected.StudentProfileId,
                CourseOfferingId = selected.CourseOfferingId,
                Note = "Reactivated from AdminApp"
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Enrollment reactivated.");
        }, vm => vm.SelectedItem is EnrollmentListItemResponse selected && selected.Status == UniAcademic.Domain.Enums.EnrollmentStatus.Dropped));
        return module;
    }

    private ModulePageViewModel CreateRosterModule()
    {
        var module = CreateModule("Rosters", ct => LoadObjectsAsync(_courseOfferingApiClient.GetListAsync(cancellationToken: ct)), (item, ct) => AsObjectTask(_rosterApiClient.GetAsync(GetId(item), ct)!));
        module.AddAction(new ModuleActionViewModel("Finalize", module, async vm =>
        {
            if (vm.SelectedItem is null) return;
            var fields = Fields(("Note", typeof(string), (object?)null, true));
            if (!_formDialogService.Show("Finalize Roster", fields)) return;
            await _rosterApiClient.FinalizeAsync(GetId(vm.SelectedItem), new FinalizeCourseOfferingRosterRequest
            {
                Note = Get<string?>(fields, "Note")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Roster finalized.");
        }, vm => vm.SelectedItem is CourseOfferingListItemResponse));
        return module;
    }

    private ModulePageViewModel CreateAttendanceModule()
    {
        var module = CreateModule("Attendance", ct => LoadObjectsAsync(_attendanceApiClient.GetListAsync(cancellationToken: ct)), (item, ct) => AsObjectTask(_attendanceApiClient.GetByIdAsync(GetId(item), ct)!));
        module.AddAction(new ModuleActionViewModel("Create Session", module, async vm =>
        {
            var fields = Fields(("CourseOfferingId", typeof(Guid)), ("SessionDate", typeof(DateTime), DateTime.Today), ("SessionNo", typeof(int), 1), ("Title", typeof(string)), ("Note", typeof(string), (object?)null, true));
            if (!_formDialogService.Show("Create Attendance Session", fields)) return;
            await _attendanceApiClient.CreateAsync(new CreateAttendanceSessionRequest
            {
                CourseOfferingId = Get<Guid>(fields, "CourseOfferingId"),
                SessionDate = Get<DateTime>(fields, "SessionDate"),
                SessionNo = Get<int>(fields, "SessionNo"),
                Title = Get<string?>(fields, "Title"),
                Note = Get<string?>(fields, "Note")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Attendance session created.");
        }));
        module.AddAction(new ModuleActionViewModel("Edit Records", module, async vm =>
        {
            if (vm.SelectedItem is not AttendanceSessionListItemResponse selected) return;
            var detail = await _attendanceApiClient.GetByIdAsync(selected.Id);
            var request = new UpdateAttendanceRecordsRequest
            {
                Records = detail.Records.Select(x => new UpdateAttendanceRecordItemRequest
                {
                    RosterItemId = x.RosterItemId,
                    Status = x.Status,
                    Note = x.Note
                }).ToList()
            };
            var json = UniAcademic.AdminApp.Infrastructure.JsonFormatter.Format(request);
            if (!_textEditorDialogService.Edit("Edit Attendance Records JSON", ref json)) return;
            await _attendanceApiClient.UpdateRecordsAsync(selected.Id, UniAcademic.AdminApp.Infrastructure.JsonFormatter.Deserialize<UpdateAttendanceRecordsRequest>(json));
            await vm.RefreshAsync();
            vm.NotifySuccess("Attendance records updated.");
        }, vm => vm.SelectedItem is AttendanceSessionListItemResponse));
        return module;
    }

    private ModulePageViewModel CreateGradeModule()
    {
        var module = CreateModule("Grades", ct => LoadObjectsAsync(_gradeApiClient.GetListAsync(cancellationToken: ct)), (item, ct) => AsObjectTask(_gradeApiClient.GetByIdAsync(GetId(item), ct)!));
        module.AddAction(new ModuleActionViewModel("Create Category", module, async vm =>
        {
            var fields = Fields(("CourseOfferingId", typeof(Guid)), ("Name", typeof(string)), ("Weight", typeof(decimal), 10m), ("MaxScore", typeof(decimal), 10m), ("OrderIndex", typeof(int), 1), ("IsActive", typeof(bool), true));
            if (!_formDialogService.Show("Create Grade Category", fields)) return;
            await _gradeApiClient.CreateCategoryAsync(new CreateGradeCategoryRequest
            {
                CourseOfferingId = Get<Guid>(fields, "CourseOfferingId"),
                Name = Get<string>(fields, "Name"),
                Weight = Get<decimal>(fields, "Weight"),
                MaxScore = Get<decimal>(fields, "MaxScore"),
                OrderIndex = Get<int>(fields, "OrderIndex"),
                IsActive = Get<bool>(fields, "IsActive")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Grade category created.");
        }));
        module.AddAction(new ModuleActionViewModel("Edit Category", module, async vm =>
        {
            if (vm.SelectedItem is not GradeCategoryListItemResponse selected) return;
            var detail = await _gradeApiClient.GetByIdAsync(selected.Id);
            var fields = Fields(("Name", typeof(string), detail.Name), ("Weight", typeof(decimal), detail.Weight), ("MaxScore", typeof(decimal), detail.MaxScore), ("OrderIndex", typeof(int), detail.OrderIndex), ("IsActive", typeof(bool), detail.IsActive));
            if (!_formDialogService.Show("Edit Grade Category", fields)) return;
            await _gradeApiClient.UpdateCategoryAsync(selected.Id, new UpdateGradeCategoryRequest
            {
                Name = Get<string>(fields, "Name"),
                Weight = Get<decimal>(fields, "Weight"),
                MaxScore = Get<decimal>(fields, "MaxScore"),
                OrderIndex = Get<int>(fields, "OrderIndex"),
                IsActive = Get<bool>(fields, "IsActive")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Grade category updated.");
        }, vm => vm.SelectedItem is GradeCategoryListItemResponse));
        module.AddAction(new ModuleActionViewModel("Edit Entries", module, async vm =>
        {
            if (vm.SelectedItem is not GradeCategoryListItemResponse selected) return;
            var detail = await _gradeApiClient.GetByIdAsync(selected.Id);
            var request = new UpdateGradeEntriesRequest
            {
                Entries = detail.Entries.Select(x => new UpdateGradeEntryItemRequest
                {
                    RosterItemId = x.RosterItemId,
                    Score = x.Score,
                    Note = x.Note
                }).ToList()
            };
            var json = UniAcademic.AdminApp.Infrastructure.JsonFormatter.Format(request);
            if (!_textEditorDialogService.Edit("Edit Grade Entries JSON", ref json)) return;
            await _gradeApiClient.UpdateEntriesAsync(selected.Id, UniAcademic.AdminApp.Infrastructure.JsonFormatter.Deserialize<UpdateGradeEntriesRequest>(json));
            await vm.RefreshAsync();
            vm.NotifySuccess("Grade entries updated.");
        }, vm => vm.SelectedItem is GradeCategoryListItemResponse));
        return module;
    }

    private ModulePageViewModel CreateGradeResultModule()
    {
        var module = CreateModule("Grade Results", ct => LoadObjectsAsync(_gradeResultApiClient.GetListAsync(cancellationToken: ct)), (item, ct) => AsObjectTask(_gradeResultApiClient.GetByIdAsync(GetId(item), ct)!));
        module.AddAction(new ModuleActionViewModel("Calculate", module, async vm =>
        {
            var fields = Fields(("CourseOfferingId", typeof(Guid)), ("PassingScore", typeof(decimal), 50m));
            if (!_formDialogService.Show("Calculate Grade Results", fields)) return;
            await _gradeResultApiClient.CalculateAsync(new CalculateGradeResultsRequest
            {
                CourseOfferingId = Get<Guid>(fields, "CourseOfferingId"),
                PassingScore = Get<decimal>(fields, "PassingScore")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Grade results calculated.");
        }));
        return module;
    }

    private ModulePageViewModel CreateMaterialModule()
    {
        var module = CreateModule("Course Materials", ct => LoadObjectsAsync(_courseMaterialApiClient.GetListAsync(cancellationToken: ct)), (item, ct) => AsObjectTask(_courseMaterialApiClient.GetByIdAsync(GetId(item), ct)!));
        module.AddAction(new ModuleActionViewModel("Upload", module, async vm =>
        {
            var filePath = _fileDialogService.SelectOpenFile();
            if (string.IsNullOrWhiteSpace(filePath)) return;
            var fields = Fields(("CourseOfferingId", typeof(Guid)), ("Title", typeof(string), Path.GetFileNameWithoutExtension(filePath), false), ("Description", typeof(string), (object?)null, true), ("MaterialType", typeof(UniAcademic.Domain.Enums.CourseMaterialType), (object?)null, false), ("SortOrder", typeof(int), 0, false), ("IsPublished", typeof(bool), false, false));
            if (!_formDialogService.Show("Upload Material", fields)) return;
            await _courseMaterialApiClient.UploadAsync(new UploadCourseMaterialRequest
            {
                CourseOfferingId = Get<Guid>(fields, "CourseOfferingId"),
                Title = Get<string>(fields, "Title"),
                Description = Get<string?>(fields, "Description"),
                MaterialType = Get<UniAcademic.Domain.Enums.CourseMaterialType>(fields, "MaterialType"),
                SortOrder = Get<int>(fields, "SortOrder"),
                IsPublished = Get<bool>(fields, "IsPublished")
            }, filePath);
            await vm.RefreshAsync();
            vm.NotifySuccess("Material uploaded.");
        }));
        module.AddAction(new ModuleActionViewModel("Edit", module, async vm =>
        {
            if (vm.SelectedItem is not CourseMaterialListItemResponse selected) return;
            var detail = await _courseMaterialApiClient.GetByIdAsync(selected.Id);
            var fields = Fields(("Title", typeof(string), detail.Title, false), ("Description", typeof(string), detail.Description, true), ("MaterialType", typeof(UniAcademic.Domain.Enums.CourseMaterialType), detail.MaterialType, false), ("SortOrder", typeof(int), detail.SortOrder, false));
            if (!_formDialogService.Show("Edit Material", fields)) return;
            await _courseMaterialApiClient.UpdateAsync(selected.Id, new UpdateCourseMaterialRequest
            {
                Title = Get<string>(fields, "Title"),
                Description = Get<string?>(fields, "Description"),
                MaterialType = Get<UniAcademic.Domain.Enums.CourseMaterialType>(fields, "MaterialType"),
                SortOrder = Get<int>(fields, "SortOrder")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Material updated.");
        }, vm => vm.SelectedItem is CourseMaterialListItemResponse));
        module.AddAction(new ModuleActionViewModel("Publish/Unpublish", module, async vm =>
        {
            if (vm.SelectedItem is not CourseMaterialListItemResponse selected) return;
            await _courseMaterialApiClient.SetPublishStateAsync(selected.Id, new SetCourseMaterialPublishStateRequest
            {
                IsPublished = !selected.IsPublished
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Publish state updated.");
        }, vm => vm.SelectedItem is CourseMaterialListItemResponse));
        module.AddAction(new ModuleActionViewModel("Download", module, async vm =>
        {
            if (vm.SelectedItem is not CourseMaterialListItemResponse selected) return;
            var targetPath = _fileDialogService.SelectSaveFile(selected.OriginalFileName);
            if (string.IsNullOrWhiteSpace(targetPath)) return;
            var bytes = await _courseMaterialApiClient.DownloadAsync(selected.Id);
            await File.WriteAllBytesAsync(targetPath, bytes);
            vm.NotifySuccess($"Saved to {targetPath}");
        }, vm => vm.SelectedItem is CourseMaterialListItemResponse));
        return module;
    }

    private ModulePageViewModel CreateLecturerAssignmentModule()
    {
        var module = CreateModule("Lecturer Assignments", ct => LoadObjectsAsync(_lecturerAssignmentApiClient.GetListAsync(cancellationToken: ct)));
        module.AddAction(new ModuleActionViewModel("Assign", module, async vm =>
        {
            var fields = Fields(("CourseOfferingId", typeof(Guid)), ("LecturerProfileId", typeof(Guid)), ("IsPrimary", typeof(bool), false));
            if (!_formDialogService.Show("Assign Lecturer", fields)) return;
            await _lecturerAssignmentApiClient.AssignAsync(new AssignLecturerRequest
            {
                CourseOfferingId = Get<Guid>(fields, "CourseOfferingId"),
                LecturerProfileId = Get<Guid>(fields, "LecturerProfileId"),
                IsPrimary = Get<bool>(fields, "IsPrimary")
            });
            await vm.RefreshAsync();
            vm.NotifySuccess("Lecturer assigned.");
        }));
        module.AddAction(new ModuleActionViewModel("Unassign", module, async vm =>
        {
            if (vm.SelectedItem is not LecturerAssignmentResponse selected) return;
            if (!_messageDialogService.Confirm($"Unassign {selected.LecturerFullName} from {selected.CourseOfferingCode}?")) return;
            await _lecturerAssignmentApiClient.UnassignAsync(selected.Id);
            await vm.RefreshAsync();
            vm.NotifySuccess("Lecturer unassigned.");
        }, vm => vm.SelectedItem is LecturerAssignmentResponse));
        return module;
    }

    private ModulePageViewModel CreateModule(
        string title,
        Func<CancellationToken, Task<IReadOnlyCollection<object>>> loadListAsync,
        Func<object, CancellationToken, Task<object?>>? loadDetailAsync = null)
        => new(title, loadListAsync, loadDetailAsync);

    private static async Task<IReadOnlyCollection<object>> LoadObjectsAsync<T>(Task<IReadOnlyCollection<T>> task)
        => (await task).Cast<object>().ToList();

    private static Guid GetId(object item)
        => (Guid)item.GetType().GetProperty("Id")!.GetValue(item)!;

    private static List<FormFieldViewModel> Fields(params object[] values)
    {
        var fields = new List<FormFieldViewModel>();

        foreach (var value in values)
        {
            switch (value)
            {
                case ValueTuple<string, Type> tuple:
                    fields.Add(new FormFieldViewModel(tuple.Item1, tuple.Item2));
                    break;
                case ValueTuple<string, Type, object?> tuple:
                    fields.Add(new FormFieldViewModel(tuple.Item1, tuple.Item2, tuple.Item3));
                    break;
                case ValueTuple<string, Type, object?, bool> tuple:
                    fields.Add(new FormFieldViewModel(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4));
                    break;
                default:
                    throw new InvalidOperationException("Unsupported field definition.");
            }
        }

        return fields;
    }

    private static async Task<object?> AsObjectTask<T>(Task<T> task)
        => await task;

    private static T Get<T>(IEnumerable<FormFieldViewModel> fields, string label)
    {
        var field = fields.First(x => x.Label == label);
        field.TryGetValue(out var value, out _);
        if (value is null)
        {
            return default!;
        }

        return (T)value;
    }
}
