# 01 - Architecture and Solution Overview

## 1. Tổng quan kiến trúc

UniAcademicManagement được tổ chức theo hướng **Clean Architecture**, với mục tiêu tách bạch rõ giữa:

- phần mô hình nghiệp vụ
- phần xử lý use case
- phần hạ tầng kỹ thuật
- phần giao diện người dùng

Kiến trúc này giúp hệ thống giữ được sự rõ ràng về trách nhiệm của từng lớp, hạn chế việc business logic bị rải ra nhiều nơi, và giúp solution có thể mở rộng dần theo thời gian mà không bị rối.

Ở mức tổng thể, hệ thống có ba đầu vào chính:

- `UniAcademic.Web`: cổng thông tin web cho end-user
- `UniAcademic.Api`: điểm vào HTTP trung tâm cho các tác vụ qua API
- `UniAcademic.AdminApp`: ứng dụng desktop nội bộ cho admin/staff

Các thành phần này không đi trực tiếp xuống cơ sở dữ liệu theo cách tùy ý, mà đều đi qua các lớp nghiệp vụ và hạ tầng đã được tổ chức sẵn.

## 2. Lý do chọn Clean Architecture

Clean Architecture được chọn vì dự án có đặc điểm rất phù hợp với mô hình này:

- có nhiều nhóm người dùng khác nhau
- có nhiều luồng nghiệp vụ học vụ cần kiểm soát chặt
- cần tách rõ business logic khỏi UI
- cần tránh để Web, API hay WPF tự ý xử lý luật nghiệp vụ
- cần khả năng kiểm thử logic ở mức đơn vị và tích hợp

Trong bối cảnh của UniAcademicManagement, lựa chọn này mang lại một số lợi ích thực tế:

- business logic được đặt tập trung ở `Application` và `Domain`
- UI chỉ đóng vai trò gọi use case và hiển thị dữ liệu
- Infrastructure có thể thay đổi cách lưu trữ hoặc tích hợp mà ít ảnh hưởng tới nghiệp vụ
- solution dễ đọc hơn với người mới vì mỗi project có vai trò tương đối rõ

Nói ngắn gọn, đây là cách tiếp cận giúp hệ thống vừa thực dụng trong triển khai, vừa đủ kỷ luật để không bị trôi logic vào các tầng không phù hợp.

## 3. Cấu trúc solution và vai trò của từng project

### 3.1. UniAcademic.SharedKernel

Đây là project chứa các thành phần nền tảng dùng chung cho toàn hệ thống, thường là các kiểu hoặc khái niệm cơ sở không mang nghiệp vụ học vụ cụ thể.

Vai trò chính:

- chia sẻ các khái niệm kỹ thuật dùng chung
- làm nền cho các project lõi khác

### 3.2. UniAcademic.Domain

`Domain` là nơi chứa mô hình nghiệp vụ cốt lõi của hệ thống. Đây là tầng biểu diễn các thực thể học vụ và các quan hệ nghiệp vụ nền tảng.

Vai trò chính:

- định nghĩa entity nghiệp vụ
- thể hiện cấu trúc dữ liệu cốt lõi của domain học vụ
- giữ những quy tắc nghiệp vụ phù hợp với mô hình domain

Ví dụ các thực thể quan trọng:

- User
- StudentProfile
- LecturerProfile
- Course
- CoursePrerequisite
- CourseOffering
- Enrollment
- Attendance
- Grade
- GradeResult
- ExamHandoffLog

### 3.3. UniAcademic.Application

`Application` là tầng xử lý use case. Đây là nơi đặt phần lớn business logic của hệ thống.

Vai trò chính:

- điều phối các nghiệp vụ theo từng use case
- áp dụng rule nghiệp vụ
- kiểm tra ownership và permission ở mức logic ứng dụng
- định nghĩa abstraction cho các hạ tầng cần dùng

Đây là tầng quan trọng nhất trong việc bảo đảm rằng:

- controller không chứa logic nghiệp vụ
- Razor PageModel không chứa logic nghiệp vụ
- WPF ViewModel không chứa logic nghiệp vụ

Ngoài các use case CRUD và vận hành học vụ cơ bản, `Application` hiện cũng chứa các service và command có ý nghĩa nghiệp vụ rõ ràng như:

- enrollment engine với kiểm tra prerequisite, credit limit và repeat rule
- transcript service cho student
- roster finalize và roster reopen
- exam handoff sau khi roster được finalize

### 3.4. UniAcademic.Contracts

`Contracts` chứa các model trao đổi dữ liệu giữa các boundary, đặc biệt là giữa API và client.

Vai trò chính:

- định nghĩa request/response DTO
- làm hợp đồng dữ liệu giữa các thành phần giao tiếp qua HTTP
- giúp tách model giao tiếp khỏi entity domain

Điều này giúp tránh việc UI hoặc client phụ thuộc trực tiếp vào entity nội bộ.

### 3.5. UniAcademic.Infrastructure

`Infrastructure` là tầng hiện thực các phụ thuộc kỹ thuật mà `Application` cần dùng.

Vai trò chính:

- EF Core DbContext
- migrations
- cấu hình persistence
- triển khai các service hạ tầng
- file storage
- seed data
- tích hợp kỹ thuật với các hệ thống ngoài

Đây là nơi kết nối logic ứng dụng với SQL Server, local disk và các công nghệ cụ thể khác.

### 3.6. UniAcademic.Api

`Api` là điểm vào HTTP cho các luồng làm việc qua token, đặc biệt là:

- `AdminApp`
- các tích hợp nội bộ hoặc bên ngoài

Vai trò chính:

- nhận request từ client
- xác thực và phân quyền ở mức API
- gọi các service ở `Application`
- trả về response qua `Contracts`

API không phải nơi viết business logic. Nó chỉ là lớp điều phối đầu vào theo đúng boundary.

Ngoài các endpoint quản lý học vụ cơ bản, API hiện cũng có các nhóm endpoint đáng chú ý như:

- transcript cho student
- roster reopen
- retry handoff và xem trạng thái exam handoff

### 3.7. UniAcademic.Web

`Web` là cổng thông tin web phục vụ end-user, đặc biệt cho:

- student
- lecturer
- một phần management web cho admin/staff

Vai trò chính:

- cung cấp giao diện MVC/Razor Pages
- dùng Cookie Authentication cho trải nghiệm web
- gọi vào các service ứng dụng theo pattern đã chốt
- hiển thị dữ liệu và luồng thao tác cho người dùng cuối

`Web` là lớp UI web, không phải nơi đặt luật nghiệp vụ.

### 3.8. UniAcademic.AdminApp

`AdminApp` là ứng dụng WPF dành cho sử dụng nội bộ, chủ yếu cho:

- admin
- staff

Vai trò chính:

- hỗ trợ các tác vụ vận hành học vụ bằng giao diện desktop
- gọi `UniAcademic.Api` qua HTTP
- không truy cập database trực tiếp

Điểm này rất quan trọng: `AdminApp` không đi tắt xuống DB. Toàn bộ luồng vẫn phải đi qua API và Application để giữ thống nhất rule nghiệp vụ.

### 3.9. UniAcademic.Tests.Unit

Project này dùng để kiểm thử ở mức đơn vị.

Vai trò chính:

- kiểm tra logic nhỏ, độc lập
- kiểm tra permission/authorization rule
- kiểm tra các use case hoặc helper có thể test tách rời

### 3.10. UniAcademic.Tests.Integration

Project này dùng để kiểm thử tích hợp.

Vai trò chính:

- kiểm tra luồng nghiệp vụ qua nhiều lớp
- xác nhận Application, Infrastructure và DB phối hợp đúng
- giảm rủi ro sai lệch giữa logic viết ra và hành vi thực tế khi chạy hệ thống

## 4. Vai trò của UniAcademic.Api, UniAcademic.Web, UniAcademic.AdminApp

Ba project này cùng phục vụ hệ thống nhưng có trách nhiệm khác nhau.

### 4.1. UniAcademic.Api

`UniAcademic.Api` là lớp API trung tâm cho các client dùng token.

Phù hợp cho:

- WPF AdminApp
- luồng tích hợp hoặc truy cập chương trình hóa

Nó chịu trách nhiệm chuẩn hóa đầu vào và bảo đảm các request được đưa vào đúng use case ứng dụng.

### 4.2. UniAcademic.Web

`UniAcademic.Web` là giao diện web dành cho end-user flow.

Phù hợp cho:

- student portal
- lecturer portal
- một phần thao tác web của admin/staff

Nó mang tính cổng thông tin hơn là công cụ kỹ thuật. Vì vậy, layout, điều hướng và trải nghiệm sử dụng được tổ chức theo vai trò người dùng.

### 4.3. UniAcademic.AdminApp

`UniAcademic.AdminApp` là công cụ desktop nội bộ phục vụ vận hành học vụ.

Phù hợp cho:

- thao tác nhanh của admin/staff
- các màn hình quản lý và xử lý nghiệp vụ nội bộ

Về mặt kỹ thuật, `AdminApp` là client của API, không phải một tầng nghiệp vụ riêng biệt.

## 5. Mô hình xác thực và phân quyền

UniAcademicManagement sử dụng hai mô hình xác thực khác nhau tùy theo loại client.

### 5.1. Cookie Authentication cho Web

`UniAcademic.Web` sử dụng **Cookie Authentication**.

Cách làm này phù hợp với:

- trải nghiệm đăng nhập trên trình duyệt
- session-based web portal
- điều hướng liên tục giữa các trang MVC/Razor Pages

### 5.2. JWT + Refresh Token cho API và WPF

`UniAcademic.Api` và `UniAcademic.AdminApp` sử dụng mô hình:

- JWT Access Token
- Refresh Token

Mô hình này phù hợp với:

- client gọi API
- ứng dụng desktop
- kiểm soát phiên đăng nhập theo hướng rõ ràng và dễ mở rộng

### 5.3. Permission-based Authorization

Hệ thống dùng **permission-based authorization** cho các thao tác nhạy cảm và các nhóm chức năng chính.

Ý nghĩa của cách làm này:

- phân quyền chi tiết hơn role thuần túy
- hỗ trợ tách rõ admin, staff, student, lecturer
- tránh việc mọi tài khoản có cùng quyền chỉ vì cùng một vai trò lớn

Role vẫn có ý nghĩa ở mức tổ chức người dùng, nhưng permission là lớp quyết định cụ thể user được làm gì trong hệ thống.

## 6. User mapping hướng A

Một điểm thiết kế quan trọng của solution là mapping trực tiếp từ `User` sang hồ sơ nghiệp vụ tương ứng.

### 6.1. User.StudentProfileId

Nếu tài khoản là tài khoản sinh viên, `User.StudentProfileId` sẽ trỏ đến hồ sơ sinh viên tương ứng.

### 6.2. User.LecturerProfileId

Nếu tài khoản là tài khoản giảng viên, `User.LecturerProfileId` sẽ trỏ đến hồ sơ giảng viên tương ứng.

### 6.3. Ý nghĩa của mapping này

Mapping này giúp hệ thống biết ngay tài khoản hiện tại đại diện cho ai trong domain học vụ.

Từ đó:

- student portal có thể tự xác định sinh viên hiện tại
- lecturer portal có thể tự xác định giảng viên hiện tại
- tránh việc nhận `studentProfileId` hay `lecturerProfileId` trực tiếp từ người dùng cuối
- giảm rủi ro truy cập sai dữ liệu

Đây là nền tảng cho các end-user flow như:

- My Course Offerings
- My Attendance
- My Grades
- My Grade Results
- My Transcript
- My Materials
- My Teaching Offerings

## 7. Ownership model

Ngoài permission, hệ thống còn áp dụng **ownership model** để kiểm soát dữ liệu theo ngữ cảnh người dùng.

### 7.1. Student chỉ thấy dữ liệu của mình

Sinh viên chỉ được xem hoặc thao tác trên dữ liệu gắn với chính hồ sơ sinh viên của mình, ví dụ:

- enrollments của bản thân
- attendance của bản thân
- grades và grade results của bản thân
- materials thuộc các offering mà mình có liên quan

### 7.2. Lecturer chỉ thao tác trên offering được assign

Giảng viên chỉ được thao tác trên các lớp học phần mà mình được phân công.

Điều này áp dụng cho các nghiệp vụ như:

- attendance
- grades
- grade results
- materials

### 7.3. Ownership rule nằm ở application layer

Điểm quan trọng là các rule ownership không nằm ở UI.

Chúng được đặt ở `Application`, vì:

- UI không phải nơi đáng tin cậy để kiểm soát quyền truy cập
- cùng một rule phải áp dụng thống nhất cho Web, API và AdminApp
- business logic phải sống ở tầng ứng dụng thay vì rải trong controller hay ViewModel

## 8. Luồng dữ liệu tổng quát

Luồng kỹ thuật tổng quát của hệ thống có thể hiểu đơn giản như sau:

`Web / AdminApp -> API / Application -> Infrastructure -> Database`

Diễn giải cụ thể hơn:

1. Người dùng thao tác trên `Web` hoặc `AdminApp`
2. Request được chuyển vào `Api` hoặc vào service phù hợp trong web flow
3. `Application` xử lý use case và áp dụng rule nghiệp vụ
4. `Infrastructure` thực hiện persistence, file storage hoặc tích hợp kỹ thuật
5. Dữ liệu được lưu hoặc đọc từ SQL Server / local disk

Ý nghĩa của luồng này là:

- mỗi tầng có trách nhiệm rõ
- UI không chạm trực tiếp vào DB
- logic nghiệp vụ được giữ tập trung
- dễ kiểm soát thay đổi và kiểm thử

## 9. Cách hệ thống lưu file

Hệ thống lưu file theo mô hình tách metadata và binary.

### 9.1. Metadata trong DB

Các thông tin mô tả file được lưu trong cơ sở dữ liệu, ví dụ:

- tên file
- loại file
- liên kết với course material hoặc đối tượng nghiệp vụ
- thông tin cần truy vấn và kiểm soát quyền truy cập

### 9.2. Binary trên local disk

Nội dung nhị phân thực tế của file được lưu trên local disk.

Cách làm này có lợi ở chỗ:

- DB không phải gánh phần binary lớn
- truy vấn metadata nhanh và rõ ràng hơn
- thuận tiện cho việc quản lý file ở mức hạ tầng

Đây là chiến lược phù hợp với phạm vi vòng 1 của dự án.

## 10. Kết luận ngắn

Kiến trúc của UniAcademicManagement được thiết kế theo hướng rõ tầng, rõ trách nhiệm và ưu tiên tính nhất quán của business logic. `Clean Architecture` trong solution này không nhằm tạo ra sự phức tạp học thuật, mà để bảo đảm rằng các luồng học vụ quan trọng được triển khai đúng chỗ, dễ kiểm soát và có thể mở rộng.

Người đọc có thể hiểu solution theo cách ngắn gọn như sau:

- `Domain` mô tả bài toán
- `Application` xử lý nghiệp vụ
- `Infrastructure` kết nối công nghệ cụ thể
- `Api`, `Web`, `AdminApp` là các cửa vào của hệ thống
- `Tests` giúp xác nhận logic và luồng kỹ thuật đang hoạt động đúng

Tài liệu này là bước nối giữa phần tổng quan nghiệp vụ và phần chi tiết kỹ thuật, giúp người đọc hình dung được hệ thống được tổ chức như thế nào trước khi đi sâu vào từng module cụ thể.
