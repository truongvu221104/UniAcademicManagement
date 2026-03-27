# 03 - Local Setup and Run

## 1. Mục tiêu tài liệu

Tài liệu này hướng dẫn cách chạy UniAcademicManagement trên máy local cho người mới clone repo, bao gồm:

- restore package
- build solution
- apply migration vào SQL Server
- chạy `Api`
- chạy `Web`
- chạy `AdminApp`

Mục tiêu là giúp môi trường local có thể lên được hệ thống vòng 1 theo cách rõ ràng và ít vướng nhất.

## 2. Yêu cầu môi trường

Để chạy solution, máy local nên có các thành phần sau:

- `.NET SDK 8`
- `Visual Studio 2022`
- workload:
  - `ASP.NET and web development`
  - `.NET desktop development`
- `SQL Server`
  - có thể là SQL Server local, SQL Express hoặc instance tương đương
- `EF Core CLI`

### Cài EF Core CLI

```powershell
dotnet tool install --global dotnet-ef
```

Nếu đã cài rồi:

```powershell
dotnet tool update --global dotnet-ef
```

## 3. Cấu trúc solution cần biết

Các project chính cần quan tâm khi chạy local:

- `src/UniAcademic.Api`
  - API trung tâm
- `src/UniAcademic.Web`
  - web portal cho student/lecturer và một phần management web
- `src/UniAcademic.AdminApp`
  - WPF app cho admin/staff
- `src/UniAcademic.Infrastructure`
  - DbContext, migrations, seed data
- `tests/UniAcademic.Tests.Unit`
  - unit tests
- `tests/UniAcademic.Tests.Integration`
  - integration tests

## 4. Các bước chạy local

### 4.1. Restore

Từ thư mục root của repo:

```powershell
dotnet restore
```

### 4.2. Build

Có thể build toàn solution:

```powershell
dotnet build
```

Hoặc build từng project chính:

```powershell
dotnet build src\UniAcademic.Api\UniAcademic.Api.csproj -v minimal
dotnet build src\UniAcademic.Web\UniAcademic.Web.csproj -v minimal
dotnet build src\UniAcademic.AdminApp\UniAcademic.AdminApp.csproj -v minimal
```

### 4.3. Migration apply

Repo hiện dùng:

- SQL Server
- EF Core Code First
- migrations nằm ở `Infrastructure`

Command apply migration:

```powershell
dotnet ef database update --project src\UniAcademic.Infrastructure\UniAcademic.Infrastructure.csproj --startup-project src\UniAcademic.Api\UniAcademic.Api.csproj
```

### 4.4. Chạy API

Chạy API bằng command:

```powershell
dotnet run --project src\UniAcademic.Api\UniAcademic.Api.csproj
```

Môi trường `Development` hiện đang bật:

- `SeedData:ApplyMigrationsEnabled = true`
- `SeedData:AutoSyncEnabled = true`

Nghĩa là khi API chạy ở `Development`, hệ thống có thể:

- tự apply migration
- tự sync seed data

Swagger mặc định:

```text
https://localhost:7271/swagger
```

### 4.5. Chạy Web

```powershell
dotnet run --project src\UniAcademic.Web\UniAcademic.Web.csproj
```

Web mặc định:

```text
https://localhost:7008
```

### 4.6. Chạy AdminApp

Có thể mở bằng Visual Studio và chạy project:

- `UniAcademic.AdminApp`

Hoặc build bằng command:

```powershell
dotnet build src\UniAcademic.AdminApp\UniAcademic.AdminApp.csproj -v minimal
```

Lưu ý:

- `AdminApp` là WPF app cho `Admin/Staff`
- `Student/Lecturer` không được phép đăng nhập vào `AdminApp`

## 5. Command mẫu rõ ràng

### 5.1. Luồng chạy local tối thiểu

```powershell
dotnet restore
dotnet build
dotnet ef database update --project src\UniAcademic.Infrastructure\UniAcademic.Infrastructure.csproj --startup-project src\UniAcademic.Api\UniAcademic.Api.csproj
dotnet run --project src\UniAcademic.Api\UniAcademic.Api.csproj
```

Sau đó ở terminal khác:

```powershell
dotnet run --project src\UniAcademic.Web\UniAcademic.Web.csproj
```

### 5.2. Luồng build riêng từng project chính

```powershell
dotnet build src\UniAcademic.Api\UniAcademic.Api.csproj -v minimal
dotnet build src\UniAcademic.Web\UniAcademic.Web.csproj -v minimal
dotnet build src\UniAcademic.AdminApp\UniAcademic.AdminApp.csproj -v minimal
```

### 5.3. Chạy tests

```powershell
dotnet test tests\UniAcademic.Tests.Unit\UniAcademic.Tests.Unit.csproj -v minimal
dotnet test tests\UniAcademic.Tests.Integration\UniAcademic.Tests.Integration.csproj -v minimal
```

## 6. Lưu ý với Tests.Unit / AdminApp hardening

### 6.1. Tests.Unit

`Tests.Unit` dùng để kiểm tra logic nhỏ và authorization rule. Đây là lớp test nhanh nên nên chạy thường xuyên khi chỉnh:

- permission
- ownership
- application services

### 6.2. Tests.Integration

`Tests.Integration` phù hợp để kiểm tra luồng nghiệp vụ thực tế hơn, đặc biệt khi có thay đổi ở:

- DbContext
- migrations
- seed data
- service flow nhiều bước

### 6.3. AdminApp

`AdminApp` là WPF app nội bộ. Khi hardening local, cần nhớ:

- chỉ `Admin/Staff` được đăng nhập
- app gọi `Api`, không truy cập DB trực tiếp
- nếu giữ token cũ gây lỗi login/refresh thì có thể cần xóa session local và đăng nhập lại

## 7. Những lỗi thường gặp khi setup local và cách hiểu ngắn gọn

### 7.1. Lỗi SQL Server connection

Ví dụ:

- không connect được DB
- lỗi encryption/trust certificate

Cách hiểu:

- vấn đề nằm ở connection string hoặc instance SQL Server local
- không phải lỗi business logic

File cần kiểm:

- `src/UniAcademic.Api/appsettings.Development.json`

### 7.2. Lỗi migration / thiếu bảng

Ví dụ:

- lỗi foreign key
- bảng chưa tồn tại

Cách hiểu:

- DB local đang ở trạng thái migration dở dang hoặc chưa được apply sạch

Cách xử lý thường dùng:

- reset DB local
- chạy lại `dotnet ef database update`

### 7.3. Lỗi build do file bị lock

Ví dụ:

- `MSB3021`
- `MSB3027`

Cách hiểu:

- file output đang bị process giữ
- thường do API hoặc AdminApp vẫn đang chạy

Cách xử lý:

- stop debug
- đóng app đang chạy
- rebuild lại

### 7.4. Swagger lên nhưng không load definition

Cách hiểu:

- thường là có endpoint/request model nào đó khiến swagger json trả `500`
- đây là lỗi API metadata/contract, không nhất thiết là DB

### 7.5. Seed không cập nhật như mong muốn

Cách hiểu:

- DB local đang còn dữ liệu cũ
- hoặc cần restart API ở `Development`

Cách xử lý thực dụng:

- reset DB local
- chạy lại API để migration + seed sync chạy lại từ đầu

## 8. Kết luận

Với môi trường đúng và SQL Server local hoạt động ổn, UniAcademicManagement có thể được chạy trên máy local theo quy trình khá trực tiếp:

1. restore
2. build
3. apply migration
4. chạy API
5. chạy Web
6. chạy AdminApp nếu cần

Tài liệu này nên được dùng như runbook local tối thiểu cho thành viên mới tham gia dự án, trước khi đi sâu vào các tài liệu nghiệp vụ và kiến trúc khác.
