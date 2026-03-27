# 00 - Project Overview and Business Value

## 1. Giới thiệu dự án

**UniAcademicManagement** là hệ thống quản lý học vụ đại học được xây dựng với mục tiêu chuẩn hóa và số hóa các quy trình học tập cốt lõi trong phạm vi một trường hoặc một đơn vị đào tạo. Dự án tập trung vào các nghiệp vụ học vụ có tính nền tảng như quản lý dữ liệu học thuật, mở lớp học phần, đăng ký học, chốt danh sách lớp, điểm danh, nhập điểm, tổng hợp kết quả học tập và quản lý tài liệu môn học.

Hệ thống được định hướng như một nền tảng thực dụng, dễ mở rộng, phục vụ đồng thời cho ba nhóm người dùng chính:

- bộ phận quản trị và vận hành học vụ
- sinh viên
- giảng viên

Về mặt sản phẩm, UniAcademicManagement không chỉ là một phần mềm lưu trữ dữ liệu, mà còn là công cụ giúp tổ chức lại luồng vận hành học vụ theo cách nhất quán, có kiểm soát quyền hạn và giảm phụ thuộc vào các thao tác thủ công rời rạc.

## 2. Bối cảnh và vấn đề thực tế trong quản lý học vụ

Trong thực tế, công tác quản lý học vụ tại nhiều đơn vị đào tạo thường gặp các vấn đề phổ biến sau:

- dữ liệu học vụ phân tán ở nhiều nơi, khó đồng bộ và khó truy vết
- quy trình mở lớp, đăng ký học, chốt danh sách lớp và nhập điểm thiếu tính liên thông
- thao tác nghiệp vụ phụ thuộc nhiều vào con người, dễ phát sinh sai sót
- khó kiểm soát quyền truy cập giữa cán bộ học vụ, giảng viên và sinh viên
- sinh viên và giảng viên thiếu một cổng thông tin rõ ràng để tự phục vụ các nhu cầu thường xuyên
- việc tổng hợp kết quả học tập và theo dõi tiến độ học vụ tốn thời gian, dễ lệch dữ liệu

Khi quy mô đào tạo tăng lên, các vấn đề trên không chỉ làm giảm hiệu quả vận hành mà còn ảnh hưởng trực tiếp đến trải nghiệm của người học, giảng viên và bộ phận quản lý.

## 3. Mục tiêu của UniAcademicManagement

UniAcademicManagement được xây dựng để giải quyết các vấn đề đó thông qua các mục tiêu chính sau:

- chuẩn hóa dữ liệu học vụ và các thực thể cốt lõi trong hệ thống
- số hóa quy trình học vụ theo luồng nghiệp vụ rõ ràng, nhất quán
- tách biệt vai trò và quyền hạn của từng nhóm người dùng
- hỗ trợ sinh viên và giảng viên tự thực hiện các tác vụ thuộc phạm vi của mình
- giảm thao tác thủ công, giảm sai sót và tăng khả năng kiểm soát
- tạo nền tảng cho việc mở rộng các chức năng học vụ trong các giai đoạn tiếp theo

Nói cách khác, dự án hướng đến việc biến các quy trình học vụ vốn rời rạc thành một hệ thống thống nhất, có thể vận hành, kiểm tra và mở rộng một cách có tổ chức.

## 4. Các nhóm người dùng chính

### 4.1. Admin/Staff

Đây là nhóm người dùng chịu trách nhiệm vận hành và kiểm soát các nghiệp vụ học vụ ở mức hệ thống.

Vai trò chính:

- quản lý dữ liệu nền của học vụ
- thiết lập và vận hành học kỳ, môn học, lớp học phần
- xử lý đăng ký học và các nghiệp vụ liên quan
- chốt danh sách lớp để phục vụ các bước học vụ tiếp theo
- theo dõi và điều phối dữ liệu điểm danh, điểm số và kết quả học tập

Nhóm này là trung tâm của luồng vận hành, bảo đảm dữ liệu đầu vào đúng, cấu hình học vụ đầy đủ và các quy trình được thực hiện theo thứ tự hợp lệ.

### 4.2. Student

Sinh viên là nhóm người dùng cuối sử dụng hệ thống để theo dõi và tương tác với quá trình học tập của mình.

Nhu cầu chính:

- xem các lớp học phần liên quan
- đăng ký hoặc theo dõi tình trạng đăng ký học
- xem điểm danh
- xem điểm thành phần và kết quả học tập
- xem tài liệu môn học

Hệ thống giúp sinh viên có một cổng thông tin rõ ràng, tập trung và dễ truy cập thay vì phải phụ thuộc vào nhiều kênh thông tin rời rạc.

### 4.3. Lecturer

Giảng viên là nhóm người dùng chịu trách nhiệm quản lý hoạt động giảng dạy trên các lớp học phần được phân công.

Nhu cầu chính:

- xem các lớp học phần mình phụ trách
- thực hiện điểm danh
- nhập và quản lý điểm
- theo dõi kết quả học tập của lớp
- quản lý và cung cấp tài liệu môn học

Với giảng viên, hệ thống đóng vai trò như một không gian làm việc học vụ tập trung, giúp các tác vụ giảng dạy được thực hiện nhất quán và dễ kiểm soát hơn.

## 5. Những nghiệp vụ quan trọng mà hệ thống xử lý

### 5.1. Quản lý master data học vụ

Hệ thống quản lý các dữ liệu nền cần thiết để toàn bộ quy trình học vụ có thể vận hành, bao gồm:

- khoa
- lớp sinh viên
- môn học
- học kỳ
- hồ sơ sinh viên
- hồ sơ giảng viên

Đây là lớp dữ liệu gốc, có vai trò quyết định đến tính chính xác của các nghiệp vụ phía sau.

### 5.2. Course Offering

Từ dữ liệu học kỳ và môn học, hệ thống hỗ trợ mở các lớp học phần cụ thể cho từng kỳ đào tạo. Mỗi lớp học phần là đơn vị vận hành trực tiếp trong quá trình giảng dạy và học tập.

Thông qua lớp học phần, hệ thống xác định:

- môn học nào đang được mở
- mở trong học kỳ nào
- quy mô tiếp nhận
- giảng viên nào phụ trách

### 5.3. Enrollment

Enrollment là nghiệp vụ đăng ký học của sinh viên vào lớp học phần. Đây là một trong những luồng cốt lõi nhất của dự án vì nó quyết định sinh viên có đủ điều kiện tham gia học phần hay không.

Việc đăng ký học được kiểm soát thông qua một luồng thống nhất, nhằm bảo đảm:

- giảm nguy cơ đăng ký sai
- kiểm tra được các ràng buộc học vụ
- làm nền cho các bước sau như chốt danh sách lớp, điểm danh và nhập điểm

### 5.4. Finalized Roster

Sau khi hoàn tất giai đoạn đăng ký, danh sách lớp học phần được chốt lại thành roster chính thức. Đây là mốc quan trọng trong quy trình học vụ vì từ thời điểm này, hệ thống có cơ sở ổn định để:

- điểm danh theo danh sách lớp thực tế
- nhập điểm cho đúng đối tượng học
- tổng hợp kết quả học tập
- bàn giao dữ liệu liên quan cho các bước xử lý tiếp theo

Việc chốt roster giúp phân tách rõ giai đoạn đăng ký học với giai đoạn triển khai giảng dạy và đánh giá.

### 5.5. Attendance

Hệ thống hỗ trợ quản lý điểm danh theo lớp học phần, giúp ghi nhận tình trạng tham gia học tập của sinh viên trong quá trình học.

Nghiệp vụ này có ý nghĩa thực tiễn lớn vì:

- phản ánh mức độ tham gia của sinh viên
- hỗ trợ giảng viên theo dõi tình hình lớp học
- tạo thêm dữ liệu đầu vào cho công tác đánh giá và quản lý học vụ

### 5.6. Grades

Hệ thống hỗ trợ quản lý điểm thành phần trong từng lớp học phần. Giảng viên có thể cập nhật các dữ liệu đánh giá phù hợp với cấu trúc chấm điểm của môn học.

Điểm thành phần là nền tảng để chuyển từ dữ liệu đánh giá rời rạc sang kết quả học tập tổng hợp.

### 5.7. Grade Result

Từ các điểm thành phần, hệ thống tổng hợp và xác lập kết quả học tập cuối cùng cho sinh viên trên từng lớp học phần.

Nghiệp vụ này giúp:

- chuẩn hóa cách tổng hợp kết quả
- giảm sai lệch khi xử lý thủ công
- tạo dữ liệu tin cậy cho việc xem kết quả học tập và báo cáo học vụ

### 5.8. Materials

Hệ thống cho phép quản lý tài liệu học tập gắn với lớp học phần. Điều này hỗ trợ quá trình dạy và học theo hướng tập trung, rõ nguồn và dễ truy cập.

Giá trị của nghiệp vụ này nằm ở việc:

- giảm phân tán tài liệu
- giúp sinh viên tiếp cận đúng tài liệu của môn học
- giúp giảng viên quản lý việc cung cấp tài liệu một cách có tổ chức

### 5.9. Lecturer Assignment

Việc phân công giảng viên vào lớp học phần là mắt xích quan trọng để hệ thống biết ai là người chịu trách nhiệm giảng dạy trên từng offering.

Nghiệp vụ này là cơ sở để:

- xác định quyền thao tác của giảng viên
- gắn đúng trách nhiệm giảng dạy với đúng lớp học phần
- bảo đảm các luồng attendance, grades và materials đi đúng người phụ trách

## 6. Giá trị và ý nghĩa của dự án

UniAcademicManagement mang lại giá trị ở cả góc độ vận hành và góc độ chuyển đổi số.

Về vận hành, hệ thống giúp:

- chuẩn hóa quy trình học vụ từ dữ liệu nền đến kết quả học tập
- giảm phụ thuộc vào bảng tính, trao đổi thủ công hoặc các hệ thống rời rạc
- cải thiện khả năng kiểm soát, truy vết và phân quyền
- giúp các bên liên quan làm việc trên cùng một nguồn dữ liệu

Về chuyển đổi số, hệ thống giúp:

- biến các bước nghiệp vụ thành luồng thao tác có cấu trúc
- tách bạch rõ vai trò giữa quản trị, giảng viên và sinh viên
- tạo nền móng để mở rộng sang các nghiệp vụ học vụ sâu hơn trong tương lai

Về mặt đào tạo và kỹ thuật, dự án còn có ý nghĩa như một mô hình tham chiếu cho việc thiết kế hệ thống quản lý học vụ theo hướng rõ domain, rõ ràng luồng nghiệp vụ và có khả năng kiểm soát tốt.

## 7. Phạm vi vòng 1

Trong vòng 1, UniAcademicManagement tập trung vào các năng lực cốt lõi nhất của quản lý học vụ, bao gồm:

- quản lý master data học vụ
- mở và quản lý lớp học phần
- đăng ký học
- chốt danh sách lớp
- điểm danh
- nhập điểm
- tổng hợp kết quả học tập
- quản lý tài liệu môn học
- phân công giảng viên
- cổng thông tin cơ bản cho sinh viên và giảng viên

Phạm vi này đủ để tạo nên một chuỗi học vụ hoàn chỉnh từ chuẩn bị dữ liệu, tổ chức giảng dạy đến theo dõi kết quả học tập. Đồng thời, phạm vi vòng 1 vẫn được giữ ở mức thực dụng, tránh dàn trải sang các chức năng ngoài trọng tâm.

## 8. Kết luận ngắn

UniAcademicManagement là dự án hướng đến việc chuẩn hóa và số hóa quy trình học vụ đại học theo cách rõ ràng, có cấu trúc và phù hợp với thực tế vận hành. Giá trị cốt lõi của hệ thống không nằm ở việc số hóa từng màn hình riêng lẻ, mà ở việc kết nối các nghiệp vụ học vụ thành một luồng thống nhất, có kiểm soát quyền hạn, có dữ liệu nhất quán và có khả năng phục vụ đồng thời cho quản trị, giảng viên và sinh viên.

Tài liệu này là điểm bắt đầu để người đọc mới hiểu dự án dùng để làm gì, vì sao dự án tồn tại và những bài toán nghiệp vụ nào đang được giải quyết trước khi đi sâu vào kiến trúc, kỹ thuật và quy tắc phát triển.
