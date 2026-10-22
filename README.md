# Vietnam Discovery Backend

ASP.NET Core Web API (.NET 9), tổ chức Controller → Service. Hiện có API health và WeatherForecast mẫu; chưa tích hợp PostgreSQL/EF Core.

## 1. Chuẩn bị và chạy từ bản clone mới

Cài .NET SDK 9.0 (kiểm tra bằng `dotnet --list-sdks`), Git và một editor. Các lệnh dưới đây chạy từ thư mục chứa `VietnamDiscovery.sln`.

```powershell
git clone https://github.com/Lee-Quang/VietNamDiscovery.git
cd VietNamDiscovery
# Nhánh triển khai VDB-12; sau khi merge có thể dùng master.
git switch feature/VDB-12
dotnet restore VietnamDiscovery.sln
dotnet build VietnamDiscovery.sln --no-restore
dotnet test VietnamDiscovery.sln --no-build
dotnet run --project backend/VietnamDiscovery.Api --launch-profile http
```

Profile `http` đặt môi trường `Development`, chạy ở `http://localhost:5026`. Dừng bằng Ctrl+C.

Nếu build báo file `VietnamDiscovery.Api.exe` đang bị khóa, dừng phiên API/Debug đang chạy rồi build lại; hoặc kiểm tra riêng bằng `dotnet build VietnamDiscovery.sln -c Release` và `dotnet test VietnamDiscovery.sln -c Release --no-build`.

- Swagger UI: http://localhost:5026/swagger/index.html
- OpenAPI JSON: http://localhost:5026/swagger/v1/swagger.json
- Health: http://localhost:5026/api/v1/health
- API mẫu: http://localhost:5026/WeatherForecast

Nếu chạy HTTPS lần đầu, dùng `dotnet dev-certs https --trust`, sau đó `dotnet run --project backend/VietnamDiscovery.Api --launch-profile https`. Swagger ở `https://localhost:7171/swagger/index.html`. `UseHttpsRedirection` giữ nguyên trong pipeline; profile HTTP không cấu hình cổng HTTPS nên có thể ghi cảnh báo và tiếp tục HTTP khi phát triển.

Chạy Production cục bộ để kiểm tra cấu hình môi trường (Swagger phải trả 404):

```powershell
dotnet run --project backend/VietnamDiscovery.Api --no-launch-profile -- --environment Production --urls http://localhost:5026
```

## 2. Cấu hình và secrets

`appsettings.json` và `appsettings.Development.json` chỉ chứa cấu hình không nhạy cảm. File `appsettings.Development.example.json` là bản mẫu đã làm sạch, có thể dùng để khôi phục file Development:

```powershell
Copy-Item backend/VietnamDiscovery.Api/appsettings.Development.example.json backend/VietnamDiscovery.Api/appsettings.Development.json
```

Không ghi mật khẩu, token hoặc connection string thật vào các file được Git theo dõi, kể cả `launchSettings.json`. `UserSecretsId` đã có trong `.csproj`; không cần chạy `user-secrets init` lại.

Ví dụ cấu hình kết nối cho bước tích hợp database sau này (thay placeholder trên máy bạn; hiện API chưa đọc kết nối này):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=VietnamDiscovery;Username=YOUR_USER;Password=YOUR_PASSWORD" --project backend/VietnamDiscovery.Api
```

User-secrets được nạp tự động trong `Development`, lưu ngoài repository trong hồ sơ người dùng và không được mã hóa. Không dùng chúng làm kho secrets cho Production. Khi triển khai, truyền qua secret store hoặc biến môi trường, ví dụ tên `ConnectionStrings__DefaultConnection` (hai dấu gạch dưới biểu diễn `:`).

Với cùng một key, độ ưu tiên mặc định từ cao xuống thấp: tham số dòng lệnh → biến môi trường → user-secrets (Development) → `appsettings.{Environment}.json` → `appsettings.json`.

`.gitignore` bỏ qua `bin/`, `obj/`, cấu hình IDE, `.env*` (trừ `.env.example`), `secrets.json`, `appsettings.*.Local.json`, chứng chỉ riêng, log và kết quả test. ASP.NET Core không tự nạp `.env` hoặc `appsettings.*.Local.json`; dùng user-secrets/biến môi trường như trên. `.gitignore` không bảo vệ file đã được Git theo dõi: luôn kiểm tra `git diff --cached` trước commit. Nếu lộ secret, thu hồi/đổi giá trị ngay, không chỉ xóa khỏi file.

## 3. Phản hồi lỗi thống nhất

API dùng `application/problem+json` theo ProblemDetails của ASP.NET Core. Ví dụ một route không tồn tại:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "instance": "/api/v1/missing",
  "traceId": "00-example-trace-id"
}
```

Các trường chung: `type`, `title`, `status`, `instance` (đường dẫn request) và `traceId`. `detail` có thể có; lỗi validation 400 thêm `errors` theo từng trường. Giữ nguyên HTTP status tương ứng:

| Tình huống | Status | Xử lý |
| --- | --- | --- |
| Model không hợp lệ / JSON sai | 400 | `[ApiController]` trả ValidationProblemDetails |
| Route không tồn tại | 404 | Status code middleware trả ProblemDetails |
| HTTP method không hỗ trợ | 405 | ProblemDetails, giữ header `Allow` |
| Exception chưa được xử lý | 500 | Exception handler ghi log server và trả thông báo chung |

`GlobalExceptionHandler` chạy trong cả Development và Production; response 500 không chứa message exception, stack trace hoặc thông tin nội bộ. Dùng `traceId` để đối chiếu log. `UseStatusCodePages` bổ sung body cho lỗi chưa có body; endpoint mới nên dùng `Problem(...)` / `ValidationProblem(...)` thay vì tự tạo chuỗi lỗi hoặc một JSON khác định dạng. Handler không thể thay body sau khi response đã bắt đầu gửi.

## 4. Kiểm thử

`dotnet test VietnamDiscovery.sln` chạy integration tests qua `WebApplicationFactory`, dùng pipeline API thật và TestServer trong bộ nhớ. Bao phủ health/WeatherForecast, Swagger Development, Swagger bị tắt ở Production, 400 (validation và JSON sai), 404, 405 và 500 không lộ exception (cả Development/Production, kể cả `Accept: text/html`). Controller validation và service ném lỗi chỉ được gắn trong test host, không xuất hiện trong API triển khai.

Kiểm tra thủ công bằng Swagger UI:

1. Chạy profile `http`, mở `/swagger/index.html`.
2. Mở `GET /api/v1/health` → **Try it out** → **Execute**: status 200, `status` là `Healthy`.
3. Chạy `GET /WeatherForecast`: status 200, trả 5 phần tử.

File `backend/VietnamDiscovery.Api/VietnamDiscovery.Api.http` có request health, OpenAPI, 404 và 405. Mở bằng HTTP editor của Visual Studio hoặc nhập các URL/method tương tự vào Postman. Có thể kiểm tra nhanh từ terminal trong lúc server chạy:

```powershell
curl.exe -i http://localhost:5026/api/v1/health
curl.exe -i http://localhost:5026/swagger/v1/swagger.json
curl.exe -i -H "Accept: text/html" http://localhost:5026/api/v1/missing
curl.exe -i -X POST http://localhost:5026/api/v1/health
```

Lỗi validation 400 được kích hoạt trong integration tests. Để chạy bộ Postman có case exception 500 và recovery, xem [hướng dẫn Postman](postman/README.md). Endpoint `/api/v1/test-error` mặc định tắt, chỉ được đăng ký trong Development khi truyền `--Diagnostics:EnableTestEndpoints=true`; không xuất hiện trong Production. Collection và environment JSON để import nằm trong `postman/exports/`, bên cạnh collection YAML cho Postman Native Git.

## 5. Quy trình Git và Jira

`master` là nhánh backend chung. Mỗi ticket có nhánh riêng, ví dụ `feature/VDB-12`; commit mang mã ticket. Sau khi hoàn tất, tạo Pull Request về `master`, review rồi merge.

```powershell
git switch master
git pull --ff-only
git switch -c feature/VDB-<SO_TICKET>
# Sửa code và chạy build/test
git add <cac-file-can-commit>
git diff --cached
git commit -m "VDB-<SO_TICKET>: Mo ta thay doi"
git push -u origin feature/VDB-<SO_TICKET>
```

Với nhánh đã tồn tại, dùng `git switch feature/VDB-12` rồi `git pull --ff-only`, không tạo lại. Comment trên Jira gồm link commit/PR, kết quả build/test và các phần chưa kiểm tra; chỉ xác nhận Done khi đáp ứng Acceptance Criteria.

## Tham khảo

- [ASP.NET Core error handling](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling?view=aspnetcore-9.0)
- [User-secrets trong Development](https://learn.microsoft.com/aspnet/core/security/app-secrets?view=aspnetcore-9.0)
- [Integration tests với WebApplicationFactory](https://learn.microsoft.com/aspnet/core/test/integration-tests?view=aspnetcore-9.0)
