# 02 - Module Status and Scope

## 1. Giới thiệu ngắn về cách đọc trạng thái module

Tài liệu này dùng để chốt phạm vi thực tế của UniAcademicManagement trong **vòng 1**. Mục tiêu là giúp người đọc hiểu rõ:

- module nào đã có và được xem là hoàn thành ở mức hiện tại
- module nào đang được giữ ổn định để demo, kiểm thử và tiếp tục phát triển theo hướng kiểm soát
- phần nào nằm ngoài phạm vi vòng 1 và chưa được xem là cam kết triển khai

Trong tài liệu này, trạng thái **đã hoàn thành/freeze** được hiểu là:

- module đã có luồng nghiệp vụ chính
- đã được đưa vào solution và gắn với kiến trúc hiện tại
- đủ để sử dụng, demo hoặc kiểm thử trong phạm vi vòng 1
- không có mục tiêu mở rộng lớn thêm về nghiệp vụ trong giai đoạn chốt scope hiện tại

Điểm quan trọng là “freeze” ở đây không có nghĩa là không bao giờ thay đổi, mà có nghĩa là module đã đạt mức đủ dùng và không phải trọng tâm mở rộng tiếp theo trừ khi có yêu cầu mới.

## 2. Danh sách module đã hoàn thành/freeze

### 2.1. Auth Foundation

**Trạng thái:** Đã hoàn thành/freeze

Module này giải quyết nền tảng xác thực và phân quyền của hệ thống, bao gồm:

- đăng nhập
- quản lý session/token theo loại client
- phân quyền theo permission
- tách vai trò giữa admin, staff, student, lecturer
- mapping người dùng tới profile nghiệp vụ tương ứng

Đây là lớp nền bắt buộc để các module học vụ phía trên hoạt động đúng quyền hạn.

### 2.2. Faculty Management

**Trạng thái:** Đã hoàn thành/freeze

Module này quản lý dữ liệu khoa, là một phần của master data học vụ. Nó tạo nền cho các module như:

- lớp sinh viên
- môn học
- hồ sơ giảng viên

### 2.3. Faculty seed bootstrap

**Trạng thái:** Đã hoàn thành/freeze

Phần này giải quyết việc bootstrap dữ liệu khoa ban đầu để môi trường phát triển và demo có thể khởi tạo dữ liệu nền nhất quán.

### 2.4. StudentClass Management

**Trạng thái:** Đã hoàn thành/freeze

Module này quản lý lớp hành chính của sinh viên. Nó giúp gắn sinh viên vào đúng đơn vị học tập nền, phục vụ cho:

- tổ chức dữ liệu sinh viên
- tra cứu học vụ
- liên kết với khoa

### 2.5. Course Management

**Trạng thái:** Đã hoàn thành/freeze

Module này quản lý dữ liệu môn học, là nền tảng để mở lớp học phần, xây dựng quy trình đăng ký học và đánh giá học tập.

### 2.6. Semester Management

**Trạng thái:** Đã hoàn thành/freeze

Module này quản lý học kỳ và là trục thời gian chính của toàn bộ quy trình học vụ, đặc biệt cho:

- mở lớp học phần
- đăng ký học
- theo dõi attendance, grades và results

### 2.7. CourseOffering Management

**Trạng thái:** Đã hoàn thành/freeze

Module này quản lý các lớp học phần cụ thể được mở trong từng học kỳ. Đây là một trong những module trung tâm của hệ thống, vì hầu hết nghiệp vụ học vụ đều xoay quanh course offering.

### 2.8. StudentProfile Management

**Trạng thái:** Đã hoàn thành/freeze

Module này quản lý hồ sơ sinh viên, là nền để:

- ánh xạ user sang student profile
- phục vụ student portal
- phục vụ enrollment, attendance, grades và grade results

### 2.9. Enrollment Management

**Trạng thái:** Đã hoàn thành/freeze

Module này xử lý đăng ký học. Đây là nơi bảo đảm sinh viên được đăng ký học phần theo một luồng thống nhất và có kiểm soát rule nghiệp vụ.

Về ý nghĩa, enrollment là nguồn xác định sinh viên có tham gia học phần hay không trước khi bước sang các khâu vận hành khác.

### 2.10. Finalize Roster / CourseOffering Roster Management

**Trạng thái:** Đã hoàn thành/freeze

Module này chốt danh sách lớp học phần sau giai đoạn đăng ký. Sau khi roster được finalize, hệ thống có thể sử dụng một danh sách ổn định để:

- điểm danh
- nhập điểm
- tính kết quả học tập
- bàn giao dữ liệu sang luồng liên quan

Đây là mốc nghiệp vụ quan trọng để chuyển từ giai đoạn đăng ký sang giai đoạn triển khai giảng dạy.

### 2.11. Attendance Management

**Trạng thái:** Đã hoàn thành/freeze

Module này quản lý điểm danh theo lớp học phần. Nó hỗ trợ việc ghi nhận sự tham gia học tập của sinh viên và cung cấp một lớp dữ liệu vận hành quan trọng cho giảng viên và bộ phận học vụ.

### 2.12. Grades Management

**Trạng thái:** Đã hoàn thành/freeze

Module này quản lý điểm thành phần của sinh viên trong từng lớp học phần. Nó là nền để tổng hợp kết quả học tập và là khu vực thao tác chính của giảng viên trong giai đoạn đánh giá.

### 2.13. Course Materials Management

**Trạng thái:** Đã hoàn thành/freeze

Module này quản lý tài liệu môn học gắn với course offering. Nó phục vụ:

- giảng viên trong việc cung cấp tài liệu
- sinh viên trong việc truy cập tài liệu đúng học phần

### 2.14. Grade Result Foundation

**Trạng thái:** Đã hoàn thành/freeze

Module này cung cấp nền tảng tổng hợp kết quả học tập từ dữ liệu điểm thành phần. Trong vòng 1, đây là lớp xử lý kết quả học phần ở mức thực dụng, đủ để phục vụ theo dõi kết quả và các màn hình liên quan.

### 2.15. LecturerProfile Management

**Trạng thái:** Đã hoàn thành/freeze

Module này quản lý hồ sơ giảng viên, là nền để:

- ánh xạ user sang lecturer profile
- phục vụ lecturer portal
- phục vụ lecturer assignment và ownership của giảng viên

### 2.16. LecturerAssignment Management

**Trạng thái:** Đã hoàn thành/freeze

Module này giải quyết việc phân công giảng viên vào course offering. Đây là cơ sở để xác định:

- giảng viên nào được thao tác trên offering nào
- ownership của lecturer trong attendance, grades và materials

### 2.17. Web end-user flows cho Student/Lecturer

**Trạng thái:** Đã hoàn thành/freeze

Phần này cung cấp các luồng sử dụng web cho người dùng cuối, tập trung vào:

- student portal
- lecturer portal

Mục tiêu là giúp sinh viên và giảng viên có thể sử dụng hệ thống theo đúng vai trò của mình mà không phải đi qua các màn hình quản trị nội bộ.

### 2.18. AdminApp UI tối thiểu vòng 1

**Trạng thái:** Đã hoàn thành/freeze

Phần này cung cấp bộ giao diện desktop tối thiểu cho admin/staff để thực hiện các tác vụ vận hành học vụ trong vòng 1.

Trọng tâm của AdminApp vòng 1 là:

- thao tác nhanh
- hỗ trợ dữ liệu đầu vào và vận hành nội bộ
- phục vụ demo và kiểm thử nghiệp vụ thực tế

AdminApp ở giai đoạn này không được xem là một nền tảng UI hoàn chỉnh ở mọi khía cạnh, nhưng đã đủ dùng cho mục tiêu vận hành và kiểm thử của vòng 1.

### 2.19. Transcript / Student Transcript

**Trạng thái:** Đã hoàn thành/freeze

Module này tổng hợp kết quả học tập của sinh viên theo học kỳ và toàn cục, bao gồm:

- gom kết quả theo semester
- tính GPA theo tín chỉ
- tính GPA tổng
- tổng tín chỉ đạt
- map grade symbol theo thang A/B/C/D/F

Ở trạng thái hiện tại, transcript đã có:

- service nghiệp vụ ở application layer
- API controller tương ứng
- trang web cho student

### 2.20. Exam Handoff

**Trạng thái:** Đã hoàn thành/freeze

Module này xử lý việc handoff dữ liệu roster sang hệ thống thi sau khi roster được finalize. Phần này hiện đã có:

- entity log handoff
- service handoff và retry
- endpoint xem trạng thái handoff
- rule nghiệp vụ liên quan tới reopen roster khi handoff đã thành công

### 2.21. Course Prerequisite Foundation

**Trạng thái:** Đã hoàn thành/freeze

Module này cung cấp nền tảng prerequisite ở mức dữ liệu và rule kiểm tra trong enrollment engine.

Ý nghĩa của phần này là:

- course có thể mang quan hệ prerequisite
- enrollment engine có thể kiểm tra điều kiện tiên quyết trước khi cho đăng ký học

### 2.22. Roster Reopen

**Trạng thái:** Đã hoàn thành/freeze

Module này cho phép mở lại roster trong các điều kiện được kiểm soát chặt. Đây không phải giả thuyết hay ý tưởng, mà là một luồng đã được implement với:

- command/service ở application layer
- API endpoint
- quyền riêng cho reopen
- rule chặn reopen khi đã có downstream data không an toàn hoặc exam handoff đã thành công

## 3. Mô tả ngắn các module đã giải quyết bài toán gì

Nhìn ở mức tổng thể, các module đã hoàn thành trong vòng 1 giải quyết được chuỗi bài toán học vụ cốt lõi sau:

- chuẩn hóa dữ liệu nền học vụ
- tổ chức lớp học phần theo học kỳ
- cho phép đăng ký học theo một luồng kiểm soát thống nhất
- chốt danh sách lớp chính thức để vận hành giảng dạy
- quản lý điểm danh trong quá trình học
- quản lý điểm thành phần
- tổng hợp kết quả học tập ở mức học phần
- tổng hợp transcript/GPA ở mức student
- quản lý tài liệu học tập theo lớp học phần
- xác định đúng giảng viên chịu trách nhiệm trên từng offering
- handoff roster sang luồng thi ở mức tích hợp cơ bản
- cung cấp cổng thao tác riêng cho sinh viên, giảng viên và công cụ nội bộ cho admin/staff

Điểm mạnh của phạm vi này là nó tạo thành một chuỗi tương đối hoàn chỉnh, từ dữ liệu nền đến quá trình học và đánh giá kết quả, thay vì chỉ dừng ở các module rời rạc.

## 4. Những phần ngoài scope vòng 1

Các nội dung dưới đây **không nằm trong phạm vi cam kết của vòng 1**:

- exam eligibility
- timetable
- workload
- payroll/HR
- advanced reporting
- notifications
- chat/discussion
- approval workflow phức tạp

Lưu ý:

- transcript và GPA ở mức transcript hiện đã có trong vòng 1
- phần ngoài scope ở đây nên được hiểu là các chức năng mở rộng hơn như đánh giá điều kiện dự thi, lịch học, khối lượng công việc, báo cáo nâng cao hoặc workflow phức tạp

Điều này không có nghĩa là các nội dung trên không có giá trị. Ngược lại, đó đều là các hướng mở rộng hợp lý cho các giai đoạn sau. Tuy nhiên, chúng chưa phải trọng tâm của vòng 1 vì dự án đang ưu tiên:

- hoàn thiện chuỗi học vụ cốt lõi
- giữ phạm vi vừa đủ để hệ thống ổn định
- tránh dàn trải sang các bài toán lớn hơn khi phần nền chưa cần mở rộng thêm

## 5. Trạng thái release/readiness hiện tại

Ở thời điểm chốt tài liệu này, UniAcademicManagement có thể được xem là đang ở trạng thái:

- đủ để demo nghiệp vụ vòng 1
- đủ để kiểm thử các luồng học vụ chính
- đủ để tiếp tục hardening và tinh chỉnh UI/UX

Nói cách khác, hệ thống chưa được mô tả như một sản phẩm học vụ toàn diện cho mọi nhu cầu của một trường đại học, nhưng đã đạt được mức **vòng 1 thực dụng và đủ dùng** cho:

- quản lý học vụ cốt lõi
- kiểm thử luồng vận hành chính
- trình bày kiến trúc và khả năng phát triển hệ thống

## 6. Kết luận

Phạm vi hiện tại của UniAcademicManagement được chốt theo hướng thực dụng: tập trung vào những module cần thiết nhất để tạo thành một chuỗi quản lý học vụ có thể vận hành được, thay vì cố gắng bao phủ toàn bộ bài toán học vụ ngay trong giai đoạn đầu.

Việc xác định rõ module nào đã hoàn thành và phần nào đang ngoài scope giúp:

- tránh hiểu sai về khả năng hiện tại của hệ thống
- giữ phạm vi phát triển có kiểm soát
- giúp người đọc, người phát triển và người đánh giá dự án có cùng một kỳ vọng về trạng thái thực tế của solution

Đây là nền tảng quan trọng để vòng 1 vừa đủ chắc, vừa đủ rõ, và có thể tiếp tục mở rộng một cách có chủ đích trong các giai đoạn sau.
