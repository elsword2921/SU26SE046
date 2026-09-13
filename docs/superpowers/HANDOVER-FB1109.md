# CHUYỆN VIỆC — Tiếp tục FB 11/09 trên nhánh `feat/feedback-1109`

Ngày bàn giao: 13/09/2026
Người làm trước: AI session (đã hoàn thành phần 1: bug + phases A→E)
Người nhận: ________________

## 1. Môi trường làm việc (quan trọng)

- **Repo**: `D:/Long/SU26SE046`, nhánh hiện tại `feat/feedback-1109` (đã commit 3 commits sạch, `git log --oneline` để xem).
- **Spec thiết kế đầy đủ**: `docs/superpowers/specs/2026-09-13-feedback-1109-design.md` — đọc TRƯỚC khi làm.
- **Build**: `cd src && dotnet build Capstone-API/Capstone-API.csproj` — cần **.NET SDK 10** (đã cài 10.0.401, `dotnet --list-sdks`). Máy mặc định từng trỏ SDK 8 — nếu lỗi NETSDK1045 thì mở shell mới.
- **DB local test**: SQL Server instance `PUCK\SQLEXPRESS01`, kết nối bằng **port**: `Server=localhost,1433;Database=UsedClothingDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;` (KHÔNG dùng `localhost` không port — 6 instance SQL trên máy, chỉ SQLEXPRESS01 chạy và có DB).
  - Chạy service SQL nếu máy mới khởi động: `powershell -Command "Get-Service 'MSSQL$SQLEXPRESS01'"`. Nếu Stopped cần admin: SQL Server Configuration Manager hoặc Services.msc → Start "SQL Server (SQLEXPRESS01)".
  - DB đã migrate tới `20260913141136_Feedback1109_FeedbackLimits` và seed 21 tài khoản demo (xem mục 4).
  - Tạo migration mới: `cd src && dotnet-ef migrations add <Tên> --project DAL/DAL.csproj --startup-project Capstone-API/Capstone-API.csproj` với env `ConnectionStrings__DefaultConnection` như trên, rồi `dotnet-ef database update ...`.
- **Chạy API local**: `cd src/Capstone-API && dotnet run --urls http://localhost:5000` với biến môi trường `ConnectionStrings__DefaultConnection` như trên + `ASPNETCORE_ENVIRONMENT=Development`. Swagger: `http://localhost:5000/swagger`.
- **Azure SQL của nhóm** (`rethreads-sql.database.windows.net/ReThreadsDb`): firewall đang CHẶN IP máy này. Muốn test với DB nhóm phải chủ nhóm mở rule cho IP hiện tại (kiểm tra IP: `curl https://api.ipify.org`). Production App Service **không còn tồn tại** (DNS NXDOMAIN) — đừng mất thời tìm.

## 2. Đã hoàn thành (phần 1) — đừng làm lại

Tất cả đã build sạch 0 error và commit trên nhánh:

| Phase | Nội dung | Files chính |
|---|---|---|
| A | **Bug 403 dãy kho**: endpoint mới `GET /api/receiving-operations/my-warehouse-layout` (ReceivingStaff) trả khu Receiving + dãy + vị trí + batchCount của kho được gán. Đã test live: 200 với token receiving.demo01, 401 không token, 403 manager. Giữ nguyên `api/area-groups` chỉ Manager/WarehouseStaff (đúng bảo mật). | `ReceivingOperationsService.cs` (GetMyWarehouseLayoutAsync ~dòng 1187), controller, interface |
| B | **Estimate + giới hạn batch**: `DonationRequest.EstimatedItemCount/EstimatedVolumeLiters/PickupLatitude/PickupLongitude`; validate 0.5–500kg, 1–2000 món, 0.1–10000 lít tại create/update; `Warehouse.MaxBatchWeightKg(200)/MaxBatchItemCount(500)/MaxBatchVolumeLiters(1500)`; enforce khi confirm pickup/drop-off (`EnsureBatchCapacityAsync`); endpoint mới `GET /api/donor-requests/eligible-warehouses?latitude&longitude&pickupDate&estimateWeight`. | `DonorRequestService.cs`, `ReceivingOperationsService.cs`, `DonorRequestController.cs`, DTOs |
| C | **Quota xe theo ca**: `OperationalTeam.VehicleType/MaxOrdersPerShift/MaxKgPerShift`; bắt buộc khai VehicleType (Motorbike/Car) khi tạo pickup team; enforce trong PlanShift (skip khi vượt), AutoBalance (best-fit theo kg + quota, thay chia đều count), AssignRequest (lỗi rõ ràng); expose `VehicleType, MaxOrdersPerShift, UsedOrders, MaxKgPerShift, UsedKg` trong `ReceivingBatchDto` + `WarehouseDutyContextDto`. | `ReceivingOperationsService.cs` (CreateTeamAsync ~560, helpers ~701, PlanShift ~730, AutoBalance ~775, AssignRequest ~1121, MapBatch ~1853) |
| D | **Map + không cân lại**: `ConfirmPickupDto.ActualWeight` giờ `decimal?` (null = dùng EstimateWeight — không bắt nhập cân); `ReceivingRequestDto` thêm `RouteOrder, AreaKey, PickupLatitude, PickupLongitude`; lat/lng lưu khi donor tạo DR. | `ReceivingOperationsService.cs`, `DonorRequestService.cs`, DTOs |
| E | **Recycle info + bàn giao**: `ClassificationBatchSummaryDto`/`DetailDto` thêm `IsRecycledReturn, SourceOperationCode, SourceOrganizationName, ImageUrls/BatchImages` (batch RC- hiển thị riêng); `GroupedClassifiedBatchDto.IsRecycledReturn`; `CountBatchAsync` cho phép lệch kg/món so với bàn giao NHƯNG bắt buộc ghi chú lý do chênh lệch. | `ClassificationOperationsService.cs`, DTOs |
| Migration gộp | `20260913141136_Feedback1109_FeedbackLimits` — đã áp DB local. Gồm: các cột mới ở trên + `Users.OrganizationName/TaxCode/CertificateImageUrl` + `DistributionRequests.Requested*` + `AiPromptConfigurations.DailyRequestLimit/TotalRequestLimit` + bảng `AiUsageLogs` + seed Description role WarehouseStaff = "Chuyên viên xuất nhập kho" (phase I xong trong migration). | `src/DAL/Migrations/20260913141136_*`, models |
| I | **Rename label**: seed Description role WarehouseStaff đổi thành "Chuyên viên xuất nhập kho" (trong migration trên). RoleName API giữ "WarehouseStaff" — label UI tiếng Việt là việc của frontend/mobile. | Migration |

Schema cho phases F/G/H **đã có sẵn trong migration gộp** — không cần tạo migration mới, chỉ cần viết service/controller.

## 3. Việc còn lại (phần 2)

Thứ tự khuyến nghị: H (nhỏ) → F → G → Verification. Hoặc theo deadline riêng.

### Phase H — AI quota (nhỏ nhất, làm trước)
- Bảng `AiUsageLogs` + cột `DailyRequestLimit/TotalRequestLimit` trên `AiPromptConfigurations` **đã migrate**.
- Việc: trong `POST api/classification-operations/analyze-images` (controller `ClassificationOperationsController.cs:33`) hoặc trong `GeminiClassificationService.AnalyzeAsync` (`src/Capstone-API/Services/OpenAiClassificationService.cs`): trước khi gọi Gemini — đọc config limit theo Feature "classification"; đếm `AiUsageLogs` của user hiện tại theo ngày VN (`BLL.Common.VietnamTime.Now.Date`) và tổng; vượt 1 trong 2 → throw `InvalidOperationException("AI quota ... vượt giới hạn, thử lại sau.")`; pass → insert log row `AiUsageLog { UserId, Feature, UsageDate, CreateAt, IsActive=true }` rồi gọi.
- DTO quản lý: thêm 2 field vào `SaveAiPromptConfigurationDto` + `AiPromptConfigurationDto` (`src/BLL/DTOs/AiPromptConfigurationDtos.cs`) để Manager cấu hình qua `AiPromptConfigurationsController` có sẵn.
- Test: set DailyRequestLimit=2, gọi analyze-images 3 lần → lần 3 lỗi.

### Phase F — Form đăng ký tổ chức + Manager duyệt
- Cột `Users.OrganizationName/TaxCode/CertificateImageUrl` + status string "PendingApproval" **đã migrate**.
- Việc 1 — `POST api/auth/register-organization` (AuthController, anonymous, `[FromForm]` vì có file):
  - DTO mới `RegisterOrganizationRequest`: OrganizationType ("CharityOrganization"|"RecyclingOrganization"|"DisposalOrganization"), OrganizationName, TaxCode, Address (địa chỉ vật lý), UserName, Email, PhoneNumber, Password, `IFormFile CertificateFile`.
  - Validate: file ≤5MB, phần mở rộng pdf/jpg/jpeg/png; các trường text bắt buộc; UserName/Email/Phone unique (xem pattern `AuthService.RegisterAsync` dòng ~70).
  - Lưu file vào `wwwroot/uploads/certificates/{guid}{ext}`; thêm static files serving trong `Program.cs` nếu chưa có (`app.UseStaticFiles()` + optional `FileServer` cho /uploads).
  - Tạo `User` với role theo OrganizationType, `UserStatus="PendingApproval"`, `EmailConfirmed=false`, `IsActive=false` (theo pattern donor register).
  - KHÔNG gửi email xác minh ở bước này (duyệt xong Manager mới kích hoạt) — hoặc gửi như donor nhưng login bị chặn tới khi Active.
- Việc 2 — `AuthService.LoginAsync` (dòng ~52): đã chặn `UserStatus != "Active"` → tự động chặn PendingApproval. Kiểm tra exception message thân thiện.
- Việc 3 — ManagerAccounts (`ManagerAccountService.cs` + `ManagerAccountsController.cs`): thêm
  - `GET api/manager-accounts/pending-organizations` — list users role org + status PendingApproval (kèm OrganizationName, TaxCode, CertificateImageUrl, Address).
  - `POST api/manager-accounts/{id}/approve` — set `UserStatus="Active"`, `EmailConfirmed=true`, `IsActive=true` + notification "Tài khoản tổ chức đã được duyệt".
  - `POST api/manager-accounts/{id}/reject` — body `{ reason }` → set `UserStatus="Inactive"` (hoặc xóa mềm IsActive=false) + notification kèm lý do.
  - Follow pattern `AllowedRoles`/`WarehouseRoles` arrays ở đầu service.
- Test: register org → login bị chặn → manager approve → login được (password do bạn đặt khi register).

### Phase G — Charity yêu cầu theo thuộc tính (thay catalog)
- Cột `DistributionRequests.RequestedClothingTypeId/GenderId/SizeId/TargetUserId/RequestedWeightKg/RequestedQuantity` **đã migrate**.
- Việc 1 — `GET api/distribution-operations/request-criteria` (CharityOrganization): trả 4 danh mục ClothingType/Gender/Size/TargetUser từ `Categories` (pattern `ClassificationOperationsService.GetCatalogAsync` — `OfType(type)`; hằng số type nằm ở class `ClassificationOperationsService`, tìm `const string ClothingType`).
- Việc 2 — charity `CreateAsync` (`DistributionOperationsService.cs:43`): DTO mới `CreateCharityDistributionRequestDto { WarehouseId, RecipientName, RecipientPhone, ToAddress, Notes, RequestedClothingTypeId, RequestedGenderId?, RequestedSizeId?, RequestedTargetUserId?, RequestedWeightKg, RequestedQuantity? }` — KHÔNG chọn InventoryIds. Lưu criteria + status "PendingManagerApproval" + notify Manager (giữ nguyên pattern). Validate: RequestedWeightKg > 0; ClothingTypeId bắt buộc thuộc type ClothingType.
  - **Quyết định cần chốt với chủ dự án**: có giữ endpoint catalog cũ cho charity không, hay thay hẳn? Spec hiện tại: **thay** (charity không còn `/catalog` + create với Items). Flow Manager→Recycling/Disposal (`CreateManagerRequestAsync`) GIỮ NGUYÊN vì đó là xuất kho xử lý, không phải charity.
- Việc 3 — `ApproveAsync` (manager, ~dòng 190): nếu request có `RequestedClothingTypeId` → auto-match: query `Inventories` status Available + ProcessingDirection "Charity" + khớp ClothingTypeId (+GenderId/SizeId/TargetUserId nếu request có) + `TotalWeight - ReservedWeight > 0` + kho = request.WarehouseId + không nằm trong DistributionItems của request khác active (pattern lockedIds ở `CreateAsync:50`), orderBy CreateAt, lấy dần tới đủ RequestedWeightKg → tạo `DistributionItem` rows (RequestedWeight theo amount lấy được) + reserve. Không đủ kg → throw lỗi rõ ràng khi approve. Sau đó flow warehouse issue/GHN giữ nguyên.
- Test: seed vài inventory Charity (xem `tests/ProcessingOperations.Checks` hoặc scripts/seed-*.sql làm mẫu) → charity request theo loại → approve → kiểm tra DistributionItems khớp thuộc tính + đủ kg.

### Verification (bắt buộc trước khi báo "xong")
1. `dotnet build` toàn solution — 0 error.
2. Chạy API local (mục 1) + smoke test bằng đúng các tài khoản dưới; từng endpoint chính của phase mình vừa làm.
3. Regression nhanh receiving flow: manager tạo shift + pickup team (với VehicleType) → plan → staff `my-batches` thấy RouteOrder + lat/lng + quota → confirm không cần ActualWeight → receive-at-warehouse → send-to-classification. Có thể làm qua Swagger.
4. Viết check script theo mẫu `tests/ProcessingOperations.Checks` cho quota/limit/approve nếu thêm thời gian.
5. Commit theo phase, message tiếng Anh conventional commits.

## 4. Tài khoản demo (DB local đã seed, password tất cả: `ReThreads@2026`)

```
manager.demo
receiving.demo01 .. receiving.demo05
classification.demo01 .. classification.demo05
warehouse.demo01 .. warehouse.demo05
charity.demo01 .. charity.demo05
```
(Cũ hơn trong seed EF: manager.demo / receiving.staff / classification.staff / warehouse.staff — hash không khớp password mới, dùng *.demo là chuẩn.)
Login: `POST /api/auth/login` `{"userName":"receiving.demo01","password":"ReThreads@2026"}` → `token`.
Seed script: `scripts/seed-demo-accounts-local.sql` (chạy lại được, idempotent).

Lưu ý mật khẩu: hash BCrypt cost 11 của `ReThreads@2026` = `$2a$11$jgbRJYcef1ZZzJTzu.AYMu55wiPtjfsyzI4pbpOC7PczUf/hdZzsm` (nêm seed thêm user mới thì dùng hash này).

## 5. Cạm bẫy đã biết (đừng giẫm lại)

- **Edit tool**: file ReceivingOperationsService.cs rất dài (2000+ dòng) và từng bị hỏng do edit chèn sai chỗ — luôn `read` vùng định sửa trước, sửa xong `read` lại + build ngay.
- **API đang chạy khóa DLL**: phải stop API (`dotnet build` sẽ lỗi MSB3027 "file locked by Capstone-API") trước khi build/migrate.
- `ConfirmPickupDto` positional record — mobile cũ gửi ActualWeight vẫn OK (optional), nhưng message mapper các chỗ dùng `dto.ActualWeight` đã đổi sang effectiveWeight — grep `dto.ActualWeight` sau khi sửa để chắc không còn sót.
- Status string `DonationRequestStatus` enum: KHÔNG thêm giá trị enum mới bừa — status DR là enum, UserStatus là string (chỉ "PendingApproval" là string mới trên Users).
- `CanTakeRequest`/`DescribeQuota`/`GetTeamQuotaUsageAsync` là private trong ReceivingOperationsService — nếu cần ở service khác, copy theo pattern, đừng đổi visibility bừa.
- AI quota tính theo ngày **giờ Việt Nam** (`VietnamTime.Now.Date`), không dùng `DateTime.Today` (máy đặt timezone khác sẽ lệch).
- Đừng push thẳng main — đang ở nhánh `feat/feedback-1109`, tiếp tục commit vào đây.

## 6. FB gốc (đối chiếu khi nghi ngờ)

17 mục + bug ngày 11/09 nằm ở cuộc hội thoại gốc; bản dịch thiết kế từng mục trong spec `docs/superpowers/specs/2026-09-13-feedback-1109-design.md` mục A→I. Mục nào chưa rõ → hỏi chủ dự án, đừng tự đoán.
