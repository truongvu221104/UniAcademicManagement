# 05 - End-to-End Flows and Smoke Test Checklist

## 1. Giới thiệu ngắn về mục tiêu smoke test vòng 1

Smoke test vòng 1 của UniAcademicManagement nhằm xác nhận rằng các luồng học vụ cốt lõi của hệ thống đang hoạt động đúng ở mức đủ dùng để:

- demo sản phẩm
- kiểm thử nhanh sau khi setup local hoặc reset DB
- kiểm tra xem các thay đổi gần đây có làm vỡ flow chính hay không

Mục tiêu của smoke test không phải là thay thế toàn bộ test chi tiết, mà là xác nhận nhanh các câu hỏi quan trọng:

- hệ thống có lên được không
- dữ liệu nền có đúng không
- các role chính có vào đúng khu vực của mình không
- các flow học vụ cốt lõi có chạy xuyên suốt không

## 2. Luồng tổng thể của hệ thống

Luồng end-to-end của vòng 1 có thể hiểu ngắn gọn như sau:

1. chuẩn bị `master data`
2. mở `course offering`
3. phân công `lecturer assignment`
4. thực hiện `enrollment`
5. `finalize roster`
6. tạo và cập nhật `attendance`
7. tạo và cập nhật `grades`
8. tính `grade result`
9. tổng hợp `transcript`
10. quản lý `materials`
11. handoff dữ liệu thi nếu có
12. student và lecturer sử dụng dữ liệu đó qua portal

### 2.1. Master data

Đây là lớp dữ liệu nền để toàn bộ hệ thống vận hành, gồm:

- faculties
- student classes
- courses
- semester
- student profiles
- lecturer profiles

Nếu lớp dữ liệu này sai hoặc thiếu, toàn bộ flow phía sau thường sẽ lỗi dây chuyền.

### 2.2. Course offering

Sau khi có master data, hệ thống cần có lớp học phần cụ thể trong học kỳ để làm trung tâm cho mọi nghiệp vụ:

- enrollment
- roster
- attendance
- grades
- materials

### 2.3. Lecturer assignment

Giảng viên phải được assign vào offering thì các luồng lecturer mới có ý nghĩa. Đây là cơ sở để xác định ownership trong lecturer portal.

### 2.4. Enrollment

Sinh viên hoặc admin/staff thực hiện enrollment vào offering. Đây là nguồn dữ liệu gốc xác định ai đang học lớp học phần nào.

### 2.5. Roster finalize

Sau khi enrollment đã ổn, offering được finalize roster. Từ mốc này, hệ thống khóa danh sách lớp để phục vụ attendance, grades và các bước tiếp theo.

### 2.6. Attendance

Attendance được tạo theo roster đã finalize. Giảng viên hoặc admin/staff có thể ghi nhận tình trạng tham gia học của sinh viên.

### 2.7. Grades

Grades dùng để quản lý điểm thành phần theo offering. Đây là lớp dữ liệu đầu vào cho kết quả học tập cuối cùng.

### 2.8. Grade result

Grade result tổng hợp dữ liệu từ grades thành kết quả học tập cuối cùng trên offering.

### 2.9. Materials

Materials cho phép gắn tài liệu vào offering. Dữ liệu này dùng để lecturer quản lý tài liệu và student truy cập tài liệu đúng học phần.

### 2.10. Transcript

Transcript tổng hợp kết quả học tập của student theo học kỳ và toàn cục. Đây là lớp đọc dữ liệu cao hơn `grade result`, giúp người dùng cuối nhìn được bức tranh tích lũy học tập thay vì chỉ xem từng offering riêng lẻ.

### 2.11. Exam handoff

Sau khi roster được finalize, hệ thống có thể ghi nhận và theo dõi handoff dữ liệu roster sang luồng thi. Đây là phần tích hợp có log trạng thái riêng và có ảnh hưởng trực tiếp tới rule reopen roster.

### 2.12. Student flows

Student dùng web portal để:

- xem course offerings liên quan
- xem enrollments
- xem attendance
- xem grades
- xem grade results
- xem transcript
- xem materials

### 2.13. Lecturer flows

Lecturer dùng web portal để:

- xem teaching offerings
- thao tác attendance
- thao tác grades
- xem grade results
- quản lý materials

## 3. Smoke test checklist theo role

## 3.1. Admin/Staff

### Nhóm mục tiêu test

- xác nhận dữ liệu nền đã có
- xác nhận management flow hoạt động
- xác nhận lifecycle của một offering chạy được từ enrollment đến results

### Checklist

| Bước test | Expected result | Bảng DB nên kiểm nếu cần |
|---|---|---|
| Đăng nhập bằng `admin` hoặc `staff.ops` | Đăng nhập thành công vào đúng khu vực quản trị | `Users`, `UserRoles` |
| Mở các màn master data như Faculties, Courses, Semesters, Student Profiles, Lecturer Profiles | Danh sách tải được, không lỗi quyền | `Faculties`, `Courses`, `Semesters`, `StudentProfiles`, `LecturerProfiles` |
| Mở danh sách Course Offerings | Nhìn thấy offering seed nền | `CourseOfferings` |
| Kiểm tra Lecturer Assignments | Offering có lecturer được assign đúng | `LecturerAssignments` |
| Tạo hoặc kiểm tra Enrollment cho một offering demo | Enrollment được tạo hoặc đã có sẵn | `Enrollments` |
| Finalize roster cho offering chưa finalize hoặc kiểm tra offering đã finalize | Offering chuyển sang trạng thái roster finalized | `CourseOfferings`, `CourseOfferingRosterSnapshots`, `CourseOfferingRosterItems` |
| Tạo hoặc mở Attendance Session | Session hiển thị đúng roster items | `AttendanceSessions`, `AttendanceRecords` |
| Tạo hoặc mở Grade Categories và Grade Entries | Categories và entries hiển thị đúng sinh viên | `GradeCategories`, `GradeEntries` |
| Tính Grade Results hoặc mở Grade Results có sẵn | Result được tính và hiển thị đúng | `GradeResults` |
| Kiểm tra trạng thái Exam Handoff của offering đã finalize | Có thể xem được trạng thái handoff; retry chỉ dùng khi đúng quyền và đúng nhu cầu test | `ExamHandoffLogs` |
| Upload hoặc kiểm tra Materials | Material được tạo và hiển thị đúng | `CourseMaterials` |

### Gợi ý offering smoke test

Nếu dùng seed hiện tại, nên ưu tiên kiểm trên:

- `SE101-2025T1-02`
- `BA101-2025T1-01`

Vì đây là các offering có dữ liệu sống mẫu phù hợp để demo nhanh.

## 3.2. Student

### Nhóm mục tiêu test

- xác nhận student chỉ thấy dữ liệu của mình
- xác nhận portal đọc được dữ liệu nghiệp vụ đã phát sinh

### Checklist

| Bước test | Expected result | Bảng DB nên kiểm nếu cần |
|---|---|---|
| Đăng nhập bằng `student.an` | Vào được Student Portal | `Users`, `StudentProfiles` |
| Mở `My Course Offerings` | Chỉ thấy offering liên quan tới student hiện tại | `Enrollments`, `CourseOfferings` |
| Mở `My Enrollments` | Chỉ thấy enrollment của chính mình | `Enrollments` |
| Mở `My Attendance` | Chỉ thấy attendance record của chính mình | `AttendanceRecords`, `CourseOfferingRosterItems` |
| Mở `My Grades` | Chỉ thấy grade entries của chính mình | `GradeEntries` |
| Mở `My Grade Results` | Chỉ thấy grade result của chính mình | `GradeResults` |
| Mở `My Transcript` | Thấy transcript theo đúng dữ liệu grade results của chính mình | `GradeResults`, `Courses`, `Semesters` |
| Mở `My Materials` | Chỉ thấy material của offering liên quan và đúng quyền hiển thị | `CourseMaterials` |
| Đăng nhập bằng `student.binh` và đối chiếu | Không nhìn thấy dữ liệu của `student.an` | các bảng tương ứng theo offering/student |

### Account gợi ý

- `student.an`
- `student.binh`

Hai account này phù hợp để đối chiếu ownership giữa hai sinh viên khác nhau.

## 3.3. Lecturer

### Nhóm mục tiêu test

- xác nhận lecturer chỉ thao tác trên offering được assign
- xác nhận lecturer portal đọc và ghi đúng dữ liệu teaching flow

### Checklist

| Bước test | Expected result | Bảng DB nên kiểm nếu cần |
|---|---|---|
| Đăng nhập bằng `lecturer.quang` hoặc `lecturer.thu` | Vào được Lecturer Portal | `Users`, `LecturerProfiles` |
| Mở `My Teaching Offerings` | Chỉ thấy offering được assign cho lecturer hiện tại | `LecturerAssignments`, `CourseOfferings` |
| Mở `Attendance` của một offering thuộc quyền | Thấy đúng roster items và session liên quan | `AttendanceSessions`, `AttendanceRecords`, `CourseOfferingRosterItems` |
| Mở `Grades` của một offering thuộc quyền | Thấy đúng categories và entries | `GradeCategories`, `GradeEntries` |
| Mở `Grade Results` | Thấy đúng result của offering thuộc quyền | `GradeResults` |
| Mở `Materials` | Thấy đúng material của offering thuộc quyền | `CourseMaterials` |
| Đăng nhập bằng lecturer khác để đối chiếu | Không thao tác được trên offering không thuộc assignment của mình | `LecturerAssignments` |

### Account gợi ý

- `lecturer.quang`
- `lecturer.thu`

Hai account này giúp kiểm ownership giữa nhiều lecturer dễ hơn.

## 4. Cross-check quan trọng

## 4.1. Ownership / authorization

Các điểm phải kiểm:

- student không thấy dữ liệu của student khác
- lecturer không thao tác được trên offering không được assign
- student/lecturer không vào được khu management
- student/lecturer không đăng nhập được vào `AdminApp`

## 4.2. Roster lock

Sau khi roster đã finalize:

- enrollment mới phải bị chặn theo rule hiện hành
- attendance, grades và results phải dựa trên roster snapshot đã chốt
- reopen roster là một flow đã implement và nếu được dùng thì phải đúng quyền, đúng điều kiện nghiệp vụ, và không được vượt qua các guard liên quan tới downstream data hoặc handoff success

## 4.3. Attendance / grades / results consistency

Các lớp dữ liệu phải khớp với nhau:

- attendance records phải bám đúng roster items
- grade entries phải bám đúng roster items
- grade results phải tính trên cùng offering và cùng snapshot hợp lệ

Nếu một offering không có enrollment hoặc roster hợp lệ, các màn phía sau thường sẽ không có dữ liệu đúng.

## 4.4. Transcript consistency

Transcript phải phản ánh đúng grade results đã được tính. Nếu grade result chưa có hoặc sai, transcript cũng sẽ sai theo.

## 4.5. Exam handoff status

Với offering đã finalize, nếu có handoff:

- trạng thái handoff phải truy ra được
- retry handoff phải đi theo đúng quyền
- offering đã handoff thành công không được reopen roster trái rule

## 4.6. Published materials visibility

Student chỉ nên thấy materials phù hợp với quyền hiển thị/publish state hiện hành. Lecturer và admin/staff thao tác material ở phạm vi rộng hơn theo quyền của mình.

## 4.7. Lecturer assignment ownership

Lecturer assignment là điều kiện rất quan trọng. Nếu offering không được assign đúng:

- lecturer portal sẽ không có dữ liệu đúng
- attendance/grades/materials ownership sẽ sai

## 5. Mức ưu tiên

## 5.1. P0

Đây là các bước bắt buộc phải pass để coi hệ thống vòng 1 còn sử dụng được:

- API lên được
- Web lên được
- AdminApp lên được
- admin/staff đăng nhập được
- student đăng nhập được vào web portal
- lecturer đăng nhập được vào web portal
- dữ liệu nền tải được
- enrollment hoạt động hoặc dữ liệu enrollment seed sống có sẵn đúng
- roster finalized hoạt động hoặc dữ liệu roster seed sống đúng
- attendance hiển thị được
- grades hiển thị được
- grade results hiển thị được
- transcript hiển thị được
- ownership theo student/lecturer không bị sai

## 5.2. P1

Đây là các bước nên kiểm thêm để tăng độ tự tin:

- đối chiếu nhiều account student với nhau
- đối chiếu nhiều account lecturer với nhau
- kiểm materials theo publish state
- kiểm DB bảng nền và bảng sống sau từng bước
- kiểm các action bị khóa đúng sau roster finalize

## 6. Điều kiện để coi smoke test pass

Smoke test vòng 1 có thể được coi là pass khi đồng thời thỏa các điều kiện sau:

- môi trường local chạy được `Api`, `Web`, `AdminApp`
- login theo role hoạt động đúng
- management flow cơ bản không lỗi
- student portal hiển thị đúng dữ liệu theo ownership
- lecturer portal hiển thị đúng dữ liệu theo assignment ownership
- dữ liệu attendance, grades, grade results nhất quán với roster
- materials hiển thị đúng theo quyền truy cập
- không phát hiện lỗi quyền nghiêm trọng như:
  - student thấy dữ liệu của người khác
  - lecturer sửa được offering không thuộc mình
  - student/lecturer vào được khu quản trị nội bộ

## 7. Kết luận

Smoke test vòng 1 của UniAcademicManagement nên được hiểu như một bài kiểm tra thực dụng cho toàn bộ chuỗi học vụ cốt lõi. Nó không cần quá nặng về số lượng test case, nhưng phải đủ để xác nhận rằng:

- hệ thống lên được
- dữ liệu chạy được
- role chạy đúng
- ownership đúng
- chuỗi từ enrollment đến results không bị đứt

Nếu checklist trong tài liệu này pass ổn định, có thể xem hệ thống đã đạt mức sẵn sàng tốt cho demo và kiểm thử nhanh trong phạm vi vòng 1.
