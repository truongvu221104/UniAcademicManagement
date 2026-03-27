# 06 - Lessons Learned and Release Notes

## 1. Giới thiệu ngắn

Tài liệu này tổng kết những bài học chính rút ra trong quá trình xây dựng UniAcademicManagement, đồng thời chốt lại trạng thái phát hành của **vòng 1** theo góc nhìn kỹ thuật và nghiệp vụ.

Mục tiêu của tài liệu không phải để mô tả hệ thống theo hướng quảng bá, mà để ghi lại một cách trung thực:

- dự án đã học được gì
- những quyết định nào tỏ ra đúng trong quá trình triển khai
- những khó khăn nào đã xuất hiện
- vòng 1 hiện đang ở mức hoàn thiện nào
- còn tồn tại những điểm nào chưa hoàn hảo nhưng chưa chặn release

## 2. Những gì học được khi làm dự án

## 2.1. Clean Architecture không chỉ là cấu trúc thư mục

Một bài học quan trọng là Clean Architecture chỉ có ý nghĩa khi kỷ luật đặt logic đúng chỗ được giữ nghiêm túc.

Trong dự án này, bài học rút ra là:

- `Domain` và `Application` phải là nơi giữ nghiệp vụ
- `Controller`, `Razor PageModel` và `WPF ViewModel` chỉ nên làm điều phối
- nếu không kiểm soát sớm, business logic rất dễ trôi sang UI layer

Điều này đặc biệt đúng với các nghiệp vụ học vụ có nhiều rule như enrollment, roster, attendance và grades.

## 2.2. Permission-based authorization phù hợp hơn role-based thuần túy

Ban đầu, nếu chỉ nghĩ theo role lớn như admin, staff, student, lecturer thì có thể dễ rơi vào việc:

- phân quyền quá rộng
- UI nhìn giống nhau giữa các role
- user có quyền vượt quá phạm vi cần thiết

Kinh nghiệm rút ra là:

- role giúp tổ chức nhóm người dùng
- permission mới là lớp kiểm soát thao tác cụ thể

Mô hình permission-based authorization tỏ ra phù hợp hơn với một hệ thống học vụ có nhiều loại hành vi và nhiều mức độ nhạy cảm khác nhau.

## 2.3. Ownership model là lớp bảo vệ rất quan trọng

Chỉ phân quyền theo permission là chưa đủ. Dự án đã cho thấy rằng cần thêm một lớp kiểm soát theo ownership, ví dụ:

- student chỉ thấy dữ liệu của chính mình
- lecturer chỉ thao tác trên offering được assign

Bài học quan trọng là ownership phải nằm ở `Application layer`, không được giao cho UI tự quyết định. Nếu ownership chỉ được xử lý ở màn hình hiển thị, hệ thống sẽ rất dễ bị sai logic hoặc lộ dữ liệu khi có thêm client mới.

## 2.4. Tách Web / API / AdminApp là quyết định đúng

Việc tách:

- `Web` cho end-user flow
- `Api` cho client token-based và integration
- `AdminApp` cho tác vụ nội bộ

đã giúp hệ thống rõ ràng hơn về vai trò của từng kênh sử dụng.

Bài học ở đây là:

- không nên cố gắng dùng một loại UI cho tất cả nhóm người dùng
- student/lecturer cần portal đơn giản, tập trung vào self-service
- admin/staff cần công cụ thao tác nhanh, trực tiếp và thiên về vận hành

## 2.5. EF Core migrations cần được quản lý có kỷ luật

Dự án cho thấy migration là khu vực rất dễ gây lỗi dây chuyền nếu không kiểm soát tốt.

Kinh nghiệm rút ra:

- mỗi thay đổi schema nên đi cùng migration rõ ràng
- migration dở dang hoặc sai thứ tự sẽ làm local setup rất dễ vỡ
- reset DB local là cách thực dụng trong giai đoạn hardening khi cần làm sạch trạng thái

Điều này đặc biệt quan trọng với solution đang đi theo Code First và có nhiều entity liên kết với nhau.

## 2.6. Roster snapshot thinking là một tư duy tốt cho nghiệp vụ học vụ

Một trong những bài học nghiệp vụ quan trọng nhất là cần tách rõ:

- dữ liệu enrollment đang sống
- danh sách lớp đã được chốt để vận hành

Từ đó, tư duy `roster snapshot` trở thành nền tảng hợp lý cho:

- attendance
- grades
- grade results
- transcript
- một số luồng handoff liên quan

Điểm rút ra là hệ thống học vụ không nên dựa toàn bộ vào dữ liệu “đang thay đổi” nếu phía sau còn có các bước đánh giá, thống kê hoặc tích hợp khác cần tính ổn định.

## 2.7. Attendance / grades / results pipeline cần nhất quán từ đầu

Khi xây dựng chuỗi:

- roster finalized
- attendance
- grades
- grade results

dự án cho thấy rằng nếu đầu chuỗi không chắc, phần sau sẽ rất dễ sai hoặc rỗng dữ liệu.

Bài học quan trọng là:

- không nên nhìn attendance hay grades như các module độc lập
- chúng là một pipeline phụ thuộc chặt vào dữ liệu roster đã chốt

Thiết kế và test cũng phải đi theo đúng tư duy pipeline này.

## 2.8. Hardening, build và test discipline quan trọng không kém viết feature

Trong quá trình làm dự án, nhiều vấn đề không đến từ business logic mà đến từ:

- build bị lock file
- seed dữ liệu chưa đồng bộ
- role/permission chưa siết đúng
- migration hoặc config local chưa sạch

Bài học rút ra là:

- hardening là một phần bắt buộc của vòng 1
- build pass chưa đủ, cần thêm smoke test theo role
- test discipline giúp chặn sớm những lỗi “nhỏ nhưng nguy hiểm” trong môi trường demo hoặc local setup

## 3. Những vấn đề quan trọng dự án đã giải quyết được

Ở trạng thái vòng 1, UniAcademicManagement đã giải quyết được các vấn đề có ý nghĩa thực tế sau:

- chuẩn hóa được master data học vụ cốt lõi
- tổ chức được luồng course offering theo học kỳ
- đưa enrollment về một luồng nghiệp vụ thống nhất
- tách rõ mốc roster finalized để làm nền cho vận hành lớp học phần
- xây dựng được chuỗi attendance, grades và grade results có liên kết logic với nhau
- triển khai được transcript ở mức đủ dùng cho student
- gắn lecturer assignment vào ownership thực tế
- đưa exam handoff vào sau roster finalize với log trạng thái rõ ràng
- đưa prerequisite vào enrollment engine thay vì để đăng ký học chỉ là thao tác CRUD
- tách được end-user flow cho student và lecturer
- giữ được nguyên tắc không để business logic nằm trong UI layer
- thiết lập được role + permission + ownership theo hướng có thể kiểm soát

Nói ngắn gọn, dự án đã đi được từ “một tập các màn hình CRUD” sang “một hệ thống có luồng học vụ cốt lõi tương đối hoàn chỉnh”.

## 4. Những khó khăn và điểm rút kinh nghiệm trong quá trình triển khai

Một số khó khăn nổi bật trong quá trình triển khai bao gồm:

- dễ để UI và nghiệp vụ dính vào nhau nếu không kiểm soát từ đầu
- phân quyền ban đầu có thể nhìn đúng ở mức ý tưởng nhưng sai ở mức dữ liệu seed
- dữ liệu demo nếu không tổ chức tốt sẽ làm smoke test trở nên rất khó lặp lại
- WPF lookup/form UX cần nhiều hardening hơn dự đoán ban đầu
- local setup với SQL Server và migration có thể làm người mới gặp vướng ngay từ bước đầu
- một số quyết định ban đầu về scope docs dễ bị chậm hơn code nếu không rà lại sau từng phase

Các điểm rút kinh nghiệm tương ứng là:

- cần chốt quy tắc đặt logic sớm và giữ kỷ luật trong suốt quá trình làm
- role/permission phải được kiểm từ cả seed, backend và UI visibility
- seed nền và seed sống nên được tách rõ
- desktop admin tool cần ưu tiên độ ổn định thao tác hơn là cố quá nhiều tương tác phức tạp
- tài liệu setup local và smoke test cần được viết sớm, không nên để đến cuối
- tài liệu trạng thái module cần được cập nhật lại sau mỗi phase có thêm module hoàn chỉnh như transcript hoặc exam handoff

## 5. Release notes vòng 1

## 5.1. Các module chính đã hoàn thành

Ở phạm vi vòng 1, các nhóm module chính đã được đưa vào solution và đủ dùng ở mức hiện tại gồm:

- Auth Foundation
- Faculty Management
- Faculty seed bootstrap
- StudentClass Management
- Course Management
- Semester Management
- CourseOffering Management
- StudentProfile Management
- Enrollment Management
- Finalize Roster / CourseOffering Roster Management
- Attendance Management
- Grades Management
- Course Materials Management
- Grade Result Foundation
- Transcript / Student Transcript
- LecturerProfile Management
- LecturerAssignment Management
- Exam Handoff
- Course Prerequisite Foundation
- Roster Reopen
- Web end-user flows cho Student/Lecturer
- AdminApp UI tối thiểu vòng 1

## 5.2. Trạng thái build

Ở thời điểm chốt vòng 1:

- các project chính có thể build được trên môi trường local phù hợp
- `Api`, `Web`, `AdminApp` đã được harden ở mức đủ để demo và kiểm thử
- local runbook, demo account và smoke test checklist đã được ghi lại thành tài liệu
- hệ thống docs đã được chỉnh lại để phản ánh đúng hơn trạng thái thực của code vòng 1

## 5.3. Trạng thái test và smoke test

Vòng 1 được xem là đã đi qua các lớp xác nhận cơ bản sau:

- build verification
- unit test ở các phần logic phù hợp
- integration test cho các luồng nhiều tầng
- smoke test theo role cho các flow chính

Mức độ “sẵn sàng” ở đây là:

- đủ để demo
- đủ để onboard người mới vào repo
- đủ để tiếp tục hardening hoặc mở rộng có kiểm soát

## 6. Known issues không blocker

Các điểm dưới đây được xem là tồn tại nhưng chưa chặn vòng 1.

## 6.1. Warning nền tảng ở AdminApp

WPF `AdminApp` có thể xuất hiện warning kiểu `CA1416` liên quan đến API chỉ hỗ trợ trên Windows, đặc biệt quanh `ProtectedTokenStore`.

Đây không phải blocker vì:

- `AdminApp` là ứng dụng WPF
- mục tiêu chạy chính là trên Windows

## 6.2. Warning Razor/generated file trong build Web

Khi build `Web`, có thể còn xuất hiện một số warning từ Razor/generated output hoặc thư mục build trung gian.

Các warning này cần được theo dõi, nhưng ở trạng thái hiện tại không được xem là chặn khả năng build và chạy của vòng 1.

## 6.3. Một số khu vực UI chưa thật sự polish

Một số phần UI, đặc biệt ở `AdminApp`, đã được harden để dùng được nhưng chưa phải mức polish cao. Ví dụ:

- UX chọn dữ liệu lookup
- một số chi tiết hiển thị form/list
- cảm giác nhất quán thị giác giữa các màn

Đây là điểm cần cải thiện tiếp, nhưng không làm mất giá trị cốt lõi của vòng 1.

## 6.4. Local setup vẫn phụ thuộc tương đối nhiều vào môi trường

Các vấn đề như:

- SQL Server local
- connection string
- DB reset/migration state

vẫn là những điểm dễ gây nhiễu cho người mới. Tuy nhiên, vì đã có runbook và seed strategy rõ hơn, mức độ rủi ro hiện nay đã giảm đáng kể so với giai đoạn đầu.

## 7. Hướng phát triển cho phase sau

Sau vòng 1, hướng phát triển hợp lý không nên là mở rộng tràn lan, mà nên đi theo từng bước có chủ đích.

Các hướng khả thi cho phase sau gồm:

- tiếp tục polish UI/UX cho `Web` và `AdminApp`
- tăng độ sâu của reporting ở mức thực sự cần thiết
- mở rộng các flow học vụ liên quan khi chuỗi lõi đã ổn định hơn
- tăng cường test coverage ở các khu vực có nhiều rule
- tiếp tục harden local setup, seed strategy và release discipline

Điểm quan trọng là phase sau nên kế thừa nền tảng vòng 1, không phá vỡ các nguyên tắc đã chứng minh là đúng:

- business logic ở application layer
- permission + ownership đi cùng nhau
- end-user flow và management flow được tách rõ

## 8. Kết luận

Vòng 1 của UniAcademicManagement cho thấy dự án đã đạt được điều quan trọng nhất: xây dựng được một nền tảng học vụ có cấu trúc, có luồng nghiệp vụ rõ và có khả năng kiểm soát quyền hạn tương đối chắc.

Những gì học được từ dự án không chỉ nằm ở feature đã làm xong, mà còn nằm ở cách tổ chức solution, cách siết logic đúng tầng, cách nghĩ theo ownership và cách harden hệ thống để nó thực sự chạy được trong môi trường local và demo.

Release vòng 1 vì vậy nên được nhìn nhận đúng bản chất:

- chưa phải hệ thống học vụ toàn diện cho mọi nhu cầu
- nhưng đã là một nền tảng thực dụng, có định hướng rõ và đủ chắc để bước sang giai đoạn tiếp theo
