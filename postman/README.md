# Postman – VDB-11 và VDB-12

## Kết nối nhanh với backend

Postman gửi HTTP request tới backend đang chạy; không cần cài package Postman vào ASP.NET Core và không cần kết nối database cho các case này.

### Bước 1: Chạy backend

Mở PowerShell tại thư mục chứa `VietnamDiscovery.sln`:

```powershell
dotnet restore VietnamDiscovery.sln
dotnet build VietnamDiscovery.sln -c Release --no-restore
dotnet run --project backend/VietnamDiscovery.Api -c Release --no-build --launch-profile http
```

Giữ terminal này chạy. Log phải có `Now listening on: http://localhost:5026`. URL trong Postman phải trùng host/port thực tế của server. Có thể mở `http://localhost:5026/api/v1/health` để kiểm tra nhanh.

### Bước 2: Mở collection trong Postman

Chọn **một** trong hai cách sau để tránh có hai collection trùng tên:

**Cách A – Import JSON, phù hợp để chia sẻ/nộp bài:**

1. Trong Postman chọn **Import → Files** (hoặc kéo thả file).
2. Chọn hai file trong `postman/exports/`:
   - `VietnamDiscovery.postman_collection.json`
   - `Local.postman_environment.json`
3. Chọn environment **Vietnam Discovery - Local** trong ô environment của workspace/request.
4. Kiểm tra các biến dưới đây. Lưu lại nếu bạn sửa giá trị.

| Biến | Mặc định | Ý nghĩa |
| --- | --- | --- |
| `baseUrl` | `http://localhost:5026` | Không có dấu `/` cuối URL; không thêm `/api/v1` ở đây |
| `maxResponseTimeMs` | `2000` | Ngưỡng TC06; đo sau khi server đã khởi động |
| `enableErrorTests` | `false` | Bỏ qua TC08–TC11 khi backend chưa bật diagnostics |

Collection cũng có giá trị mặc định. Biến environment cùng tên sẽ được ưu tiên; nếu sửa `baseUrl`, sửa ở environment đang chọn. Không dùng token/accessToken cho health API này.

**Cách B – Postman Native Git/local workspace đang liên kết project:**

Bạn đã có thư mục `postman/collections/` và manifest `.postman/resources.yaml`. Các request YAML đã được sửa ngay tại đây. Mở lại/reload collection **Vietnam Discovery Backend API** trong workspace đang liên kết project; environment cũ đã được đặt tên **Vietnam Discovery - Local** và điền giá trị mặc định. Không cần import JSON lần nữa nếu Postman đã đọc các file local. Nếu workspace chưa liên kết, dùng luồng **Import → Connect Local Git Repo** (nếu phiên bản hỗ trợ) và chọn repository này; cách A vẫn dùng được cho workspace hỗ trợ import collection.

Không sửa `.postman/resources.yaml` chỉ để gọi API; file đó chứa ánh xạ workspace/cloud, không phải địa chỉ backend. Collection JSON và YAML có cùng request/script tại thời điểm bàn giao; sau khi sửa bằng Postman, hãy cập nhật bản còn lại nếu muốn duy trì cả hai.

### Bước 3: Gửi request và chạy toàn bộ

1. Mở nhóm **01 – Health Check API → 01 – Health Check**.
2. Request phải hiển thị `GET {{baseUrl}}/api/v1/health`.
3. Nhấn **Send**: mong đợi HTTP 200 và body có `status: Healthy`.
4. Mở **Test Results**: TC01–TC04 phải PASS. Script nằm trong **Scripts → Post-response**.
5. Chọn collection → **Run collection**; giữ nguyên thứ tự và chọn 1 iteration.

Với server mặc định và `enableErrorTests=false`: có **8 request được gửi, 13 assertion**; các nhóm exception/recovery bị skip. Skip không được ghi thành PASS cho TC08–TC11. Các nhóm checklist không có HTTP request nên không tạo test giả.

Nếu dùng Postman web để gọi `localhost`, chọn **Desktop Agent** đang chạy trên cùng máy backend. Cloud Agent không truy cập được localhost của máy bạn. Postman desktop app có thể gửi request trực tiếp.

## Chạy TC08–TC11: Exception và Recovery

Endpoint `/api/v1/test-error` chỉ được đăng ký khi **cả hai** điều kiện đúng: môi trường `Development` và `Diagnostics:EnableTestEndpoints=true`. Không có endpoint này ở Production, kể cả khi truyền flag `true`.

1. Dừng phiên backend mặc định bằng Ctrl+C.
2. Chạy backend với flag thử nghiệm:

```powershell
dotnet run --project backend/VietnamDiscovery.Api -c Release --no-build --launch-profile http -- --Diagnostics:EnableTestEndpoints=true
```

3. Trong environment Postman, đặt `enableErrorTests` thành chuỗi `true`.
4. Chạy **toàn bộ collection theo thứ tự**, hoặc chạy request trong nhóm 03 rồi nhóm 04. Không restart server giữa hai request.
5. TC08 nhận 500, TC09 xác minh ProblemDetails, TC10 xác minh không lộ exception, TC11 gọi lại health để kiểm tra 200/Healthy.

Ở chế độ này: **9 request được gửi, 16 assertion**; TC23 (endpoint tắt mặc định) bị skip. Request TC08 lưu dấu đã nhận 500 theo `baseUrl`; TC11 sẽ FAIL nếu chưa chạy request gây lỗi trước đó. Sau TC11, dấu này được xóa để tránh dùng kết quả cũ.

Kết thúc kiểm thử: Ctrl+C, chạy lại lệnh mặc định và đặt `enableErrorTests=false`. Flag backend và biến Postman là hai cấu hình riêng: biến Postman chỉ điều khiển có gửi request thử lỗi hay không, không bật/tắt endpoint trên server.

## Danh sách case đã chuẩn hóa

| TC | Kiểm tra | Thực hiện / mong đợi |
| --- | --- | --- |
| TC01 | HTTP status health | Postman: 200 |
| TC02 | Định dạng health | Postman: JSON object hợp lệ |
| TC03 | Trạng thái backend | Postman: `status = Healthy` |
| TC04 | Tên ứng dụng | Postman: `application = Vietnam Discovery API` |
| TC05 | Timestamp | Postman: ISO 8601 có timezone, parse được |
| TC06 | Thời gian phản hồi | Postman: HTTP 200, thời gian < `maxResponseTimeMs` (2000 ms) |
| TC07 | Route không tồn tại | GET `/api/v1/not-found`: 404 + ProblemDetails có traceId, kể cả Accept text/html |
| TC08 | Exception | GET `/api/v1/test-error`: 500, chỉ chạy khi bật diagnostics |
| TC09 | Response lỗi 500 | ProblemDetails: type/title/status/instance/traceId và detail chung |
| TC10 | Không lộ thông tin lỗi | Không có marker `POSTMAN_TEST_EXCEPTION`, tên exception, stack trace hoặc đường dẫn source |
| TC11 | Recovery | Sau TC08, cùng baseUrl/server: health 200 và Healthy |
| TC12 | Content-Type health | `application/json`, chấp nhận tham số charset |
| TC13 | Git repository | CLI: `git status` hoạt động |
| TC14 | Ignore build/secrets | CLI: bin/obj/.env bị ignore và không nằm trong tracked files |
| TC15 | User-secrets | Kiểm tra cấu hình ngoài Git, không có giá trị secret thật trong source/commit |
| TC16 | Build | CLI: `dotnet build` thành công |
| TC17 | README | Có prerequisites, clone/restore/build/run/test và hướng dẫn secrets |
| TC18 | Commit | CLI: commit tồn tại, kiểm tra file trong commit và working tree |
| TC19 | Collection artifact | Có JSON, import/chạy được; lưu hoặc export lại được theo workspace đang dùng |
| TC20 | Chạy từ source | Clone mới, restore/build/run theo README, health thành công |
| TC21 | HTTP method sai | POST health: 405, Allow GET, ProblemDetails |
| TC22 | Swagger Development | OpenAPI 200, có health và không có controller validation nội bộ của integration tests |
| TC23 | Diagnostics tắt mặc định | `/api/v1/test-error` trả 404 khi không bật flag |

TC01–TC04 nằm trong một request; TC05/TC06/TC12 là các request health riêng cho dễ theo dõi. TC21–TC23 bổ sung cho 20 TC ban đầu. Thời gian TC06 là kiểm tra nhanh tại localhost, không phải kiểm thử tải.

## Checklist ngoài Postman

Chạy trong thư mục solution, ghi kết quả thực tế vào báo cáo; không đánh PASS cho nhóm này chỉ vì collection chạy xanh.

```powershell
# TC13, TC18
git status
git log -1 --oneline
git show --stat HEAD

# TC14: lệnh đầu phải in cả 3 đường dẫn mẫu; lệnh sau không được có kết quả
git check-ignore backend/VietnamDiscovery.Api/bin/probe.txt backend/VietnamDiscovery.Api/obj/probe.txt .env
git ls-files | Select-String '(^|/)(bin|obj)/|(^|/)\.env($|\.)'

# TC16 và regression tests
dotnet build VietnamDiscovery.sln -c Release
dotnet test VietnamDiscovery.sln -c Release --no-build

# TC15: kiểm tra định danh user-secrets; không in giá trị secrets thật vào log/báo cáo
Select-String -Path backend/VietnamDiscovery.Api/VietnamDiscovery.Api.csproj -Pattern UserSecretsId
git diff --cached
```

TC15 còn cần đọc các file cấu hình đang theo dõi để chắc rằng chỉ có giá trị mẫu. `UserSecretsId` là định danh, có thể commit; nội dung kho user-secrets phải nằm ngoài repository. TC17/TC20 thực hiện theo README gốc trên bản clone mới. TC19 lưu lại hai file JSON trong `exports/` và kết quả chạy; nếu phiên bản Native Git không có nút Export JSON, dùng bản tương thích cung cấp sẵn hoặc workspace hỗ trợ export. Không đưa token thật vào environment JSON chia sẻ.

## Khi không kết nối được

| Hiện tượng | Cách xử lý |
| --- | --- |
| `ECONNREFUSED` / Could not send request | Backend phải đang chạy; kiểm tra host/port ở log, cập nhật `baseUrl` |
| Biến `{{baseUrl}}` chưa có giá trị | Chọn đúng environment; kiểm tra giá trị local đang dùng |
| TC08 trả 404 | Chưa chạy Development với flag diagnostics; khởi động lại đúng lệnh |
| TC23 trả 500 | Server đang bật diagnostics nhưng environment Postman báo false; tắt flag hoặc chạy chế độ đầy đủ |
| TC11 thiếu kết quả TC08 | Chạy nhóm 03 rồi nhóm 04 trên cùng server, hoặc Run collection đúng thứ tự |
| Swagger trả 404 | Kiểm tra backend có chạy Development hay không |
| Build báo file exe đang bị khóa | Dừng phiên API cũ trước khi build; cấu hình Release tránh trùng phiên Debug |
| Đang dùng HTTPS và lỗi certificate | Dùng HTTP cho bài test local này; hoặc thiết lập chứng chỉ dev hợp lệ bằng `dotnet dev-certs https --trust` |

## Kết quả kiểm tra bộ collection

Đã chạy bản JSON bằng Newman 6.2.3 trên API localhost thật:

- Mặc định: 8 request, 13 assertion, 0 lỗi; TC08–TC11 skip đúng điều kiện.
- Diagnostics bật: 9 request, 16 assertion, 0 lỗi; 500 → health 200 trên cùng server.
- Native YAML và JSON có cùng URL/method/script, script đã kiểm tra cú pháp.
- Build Release 0 warning/0 error; 18 integration tests đạt, gồm endpoint bị chặn ở Production ngay cả khi bật flag.

Đây là kết quả kiểm tra tại thời điểm tạo file; kết quả trong Postman của bạn phụ thuộc server và environment đang chọn.

## Tài liệu Postman

- [Import dữ liệu](https://learning.postman.com/v11/docs/getting-started/importing-and-exporting/importing-data)
- [Post-response scripts](https://learning.postman.com/docs/tests-and-scripts/write-scripts/test-scripts/)
- [Desktop Agent](https://learning.postman.com/v11/docs/getting-started/basics/about-postman-agent)
- [Bỏ qua request có điều kiện](https://learning.postman.com/latest-v-12/docs/tests-and-scripts/write-scripts/postman-sandbox-reference/pm-execution)
