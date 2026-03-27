# 04 - Demo Accounts and Seed Data

## 1. Mục tiêu của bộ demo seed data

Bộ demo seed data của UniAcademicManagement được thiết kế để phục vụ ba mục đích chính:

- giúp môi trường local có thể khởi tạo dữ liệu nhanh và nhất quán
- hỗ trợ demo các luồng nghiệp vụ vòng 1 mà không phải nhập tay toàn bộ dữ liệu nền
- tạo một bộ dữ liệu mẫu đủ nhỏ để dễ hiểu, nhưng vẫn đủ để kiểm thử các màn hình và quy trình chính

Về mặt tư duy, seed data trong dự án được tách thành hai lớp:

- **seed nền**: dữ liệu học vụ gốc cần có để hệ thống chạy được
- **seed nghiệp vụ sống**: dữ liệu vận hành như enrollment, roster, attendance, grades, grade results

Cách tách này giúp hệ thống vừa dễ reset/demo, vừa giữ rõ ranh giới giữa:

- dữ liệu cấu hình nền
- dữ liệu phát sinh trong quá trình học vụ

## 2. Danh sách account demo

Trong thực tế, file seed hiện tại có thể chứa tập user rộng hơn để phục vụ test. Tuy nhiên, nhóm account dưới đây là bộ tài khoản **đề xuất tối thiểu để demo nhanh**:

### 2.1. Admin

- `username`: `admin`
- `password`: `Demo@123456`
- vai trò sử dụng: quản trị toàn hệ thống

### 2.2. Staff

- `username`: `staff.ops`
- `password`: `Demo@123456`
- vai trò sử dụng: vận hành học vụ nội bộ

### 2.3. Student demo 1

- `username`: `student.an`
- `password`: `Demo@123456`
- vai trò sử dụng: demo student portal

### 2.4. Student demo 2

- `username`: `student.binh`
- `password`: `Demo@123456`
- vai trò sử dụng: demo student portal và đối chiếu ownership giữa nhiều sinh viên

### 2.5. Lecturer demo 1

- `username`: `lecturer.quang`
- `password`: `Demo@123456`
- vai trò sử dụng: demo lecturer portal

### 2.6. Lecturer demo 2

- `username`: `lecturer.thu`
- `password`: `Demo@123456`
- vai trò sử dụng: demo lecturer portal và đối chiếu ownership giữa nhiều giảng viên

## 3. Mapping tài khoản với profile nghiệp vụ

UniAcademicManagement đi theo hướng A: tài khoản người dùng được map trực tiếp sang profile nghiệp vụ trong domain.

### 3.1. Mapping student

- `student.an`
  - `User.StudentProfileId -> StudentProfile(STU001)`
- `student.binh`
  - trong dataset hiện tại đang map tới `StudentProfile(STU016)`

Ý nghĩa:

- student đăng nhập xong có thể vào ngay các luồng `My ...`
- hệ thống không cần nhận `studentProfileId` từ phía người dùng cuối
- ownership được suy ra từ chính account đang đăng nhập

### 3.2. Mapping lecturer

- `lecturer.quang`
  - `User.LecturerProfileId -> LecturerProfile(LEC001)`
- `lecturer.thu`
  - `User.LecturerProfileId -> LecturerProfile(LEC002)`

Ý nghĩa:

- lecturer chỉ thao tác trên các offering được assign cho profile của mình
- các màn attendance, grades, materials được gắn theo đúng ownership giảng dạy

### 3.3. Mapping account nội bộ

- `admin`
  - `StudentProfileId = null`
  - `LecturerProfileId = null`
- `staff.ops`
  - `StudentProfileId = null`
  - `LecturerProfileId = null`

Hai tài khoản này phục vụ vận hành, không đại diện cho student hay lecturer cụ thể trong domain học vụ.

## 4. Seed nền

Seed nền là lớp dữ liệu cần có trước để hệ thống có thể vận hành nghiệp vụ vòng 1. Trong solution hiện tại, phần này chủ yếu nằm ở:

- `seed-data/academic/faculties.json`
- `seed-data/academic/demo-foundation.json`

Các nhóm dữ liệu nền chính gồm:

### 4.1. Faculties

Dataset hiện có hai khoa nền:

- `FIT` - Information Technology
- `FBA` - Business Administration

### 4.2. Student classes

Seed nền có các lớp sinh viên để phục vụ phân nhóm học vụ, ví dụ:

- `SE2022A`
- `SE2022B`
- `BA2022A`
- `BA2022B`

### 4.3. Courses

Seed nền có tập môn học cơ bản thuộc các khoa chính, đủ để mở course offering trong học kỳ demo.

### 4.4. Semester

Seed nền có học kỳ demo chính để toàn bộ flow vòng 1 cùng chạy trên một trục thời gian thống nhất:

- `2025T1`

### 4.5. Student profiles

Seed nền có tập hồ sơ sinh viên để:

- phục vụ login student
- phục vụ enrollment
- phục vụ roster, attendance, grades và grade results

### 4.6. Lecturer profiles

Seed nền có tập hồ sơ giảng viên để:

- phục vụ login lecturer
- phục vụ lecturer assignment
- phục vụ ownership ở lecturer portal

### 4.7. Course offerings

Seed nền có các lớp học phần mở trong học kỳ demo, là trung tâm của các nghiệp vụ vận hành như:

- enrollment
- roster
- attendance
- grades
- materials

### 4.8. Lecturer assignments cơ bản

Seed nền cũng có dữ liệu phân công giảng viên tối thiểu cho các offering chính, giúp:

- lecturer portal có dữ liệu đúng ownership
- attendance, grades, materials có đúng người phụ trách

## 5. Seed nghiệp vụ sống

Seed nghiệp vụ sống là lớp dữ liệu phát sinh sau khi hệ thống đã có master data nền.

Về mặt nghiệp vụ, các loại dữ liệu này thường được tạo ra qua chính app trong quá trình vận hành:

- enrollments
- finalize roster
- attendance
- grades
- grade results
- materials

Tuy nhiên, để hỗ trợ demo và smoke test nhanh, solution hiện tại còn có thêm một dataset seed sống riêng:

- `seed-data/academic/demo-live.json`

Dataset này dùng để nạp sẵn một lượng nhỏ dữ liệu vận hành mẫu cho một số offering, nhằm giúp:

- không phải thao tác tay toàn bộ flow mỗi lần reset DB
- có sẵn dữ liệu để test nhanh các màn attendance, grades, grade results

Nói cách khác:

- về chiến lược sản phẩm, dữ liệu sống là loại dữ liệu phát sinh qua app
- về chiến lược demo, repo hiện tại có thêm seed sống tối thiểu để tiết kiệm thời gian setup

## 6. Dataset tối thiểu đề xuất

Để demo và test vòng 1 một cách thực dụng, dataset tối thiểu nên bao gồm:

### 6.1. Nhóm account

- `admin`
- `staff.ops`
- `student.an`
- `student.binh`
- `lecturer.quang`
- `lecturer.thu`

### 6.2. Nhóm dữ liệu nền

- 2 faculties
- một số student classes
- một số courses
- 1 semester chính
- student profiles đủ để login và enroll
- lecturer profiles đủ để assign và demo teaching flow
- một số course offerings
- lecturer assignments cơ bản

### 6.3. Nhóm dữ liệu sống tối thiểu

Nên có ít nhất:

- 1 offering đã có enrollment và roster finalized
- 1 offering có attendance session
- 1 offering có grade categories và grade entries
- 1 offering có grade results
- 1 offering có materials để sinh viên và giảng viên cùng xem được

Mức dữ liệu này là đủ để:

- test student portal
- test lecturer portal
- test management screens chính
- trình bày rõ luồng học vụ vòng 1

## 7. Vì sao tách seed nền và seed nghiệp vụ sống

Việc tách hai lớp seed có ý nghĩa rất thực tế.

### 7.1. Seed nền ổn định hơn

Dữ liệu như:

- faculties
- courses
- semester
- profiles
- course offerings

thường đóng vai trò cấu hình học vụ và thay đổi chậm hơn.

### 7.2. Seed sống biến động hơn

Dữ liệu như:

- enrollments
- attendance
- grades
- grade results

thường thay đổi theo từng đợt test, từng buổi demo hoặc từng kịch bản nghiệp vụ.

### 7.3. Tách riêng giúp reset và demo dễ hơn

Khi tách lớp seed:

- có thể reset dữ liệu sống mà không phải phá toàn bộ master data
- dễ xây dựng demo dataset nhỏ, tập trung vào đúng luồng muốn trình bày
- tránh làm file seed nền trở nên quá nặng và khó kiểm soát

### 7.4. Tách riêng giúp phản ánh đúng bản chất nghiệp vụ

Trong thế giới thật:

- master data là lớp cấu hình
- dữ liệu sống là lớp phát sinh

Việc tách seed theo đúng hai lớp này giúp solution dễ hiểu hơn và bám sát tư duy vận hành học vụ thực tế.

## 8. Kết luận

Seed data trong UniAcademicManagement không chỉ nhằm “đổ dữ liệu cho có”, mà được tổ chức để phục vụ đúng nhu cầu phát triển và demo của vòng 1:

- seed nền giúp hệ thống có cấu hình học vụ ổn định
- seed sống giúp kiểm thử và trình bày các luồng nghiệp vụ vận hành
- mapping `User -> StudentProfile/LecturerProfile` giúp các end-user flow hoạt động đúng vai trò và ownership

Với cách tổ chức này, người mới vào dự án có thể hiểu nhanh:

- dùng account nào để test
- dữ liệu nào là dữ liệu nền
- dữ liệu nào là dữ liệu nghiệp vụ sống
- vì sao repo lại tách chúng thành nhiều dataset khác nhau
