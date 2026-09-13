# FB 11/09 — Backend Design (feat/feedback-1109)

Phạm vi: 17 mục feedback ngày 11/09 + bug "Receiving Staff không mở được dãy kho".
Repo này là backend .NET 10 (src/Capstone-API, src/BLL, src/DAL, SQL Server `UsedClothingDb`).
Mọi thay đổi schema gộp thành MỘT EF migration. Label UI tiếng Việt nằm ở frontend/mobile, ngoài repo này.

## A. Bug: Receiving Staff mở được dãy kho nhận đồ

Root cause: `DomainCrudControllers.cs` — `AreaGroupController` (api/area-groups) và `WarehouseAreaController`
(api/warehouse-areas) đều `[Authorize(Roles="Manager,WarehouseStaff")]` → ReceivingStaff bị 403.
CrudService không scope theo `Users.WarehouseId` nên không thể mở thẳng CRUD cho role này.

Fix:
- Endpoint mới `GET api/receiving-operations/my-warehouse-layout` — `[Authorize(Roles="ReceivingStaff")]`.
- Service: lấy `Users.WarehouseId` của staff → trả khu `AreaType == "Receiving"` của kho đó
  (dãy + vị trí + `BatchCount`, capacity/available), shape tái dùng `ReceivingStagingGroupDto`.
- Nếu kho chưa có khu Receiving → tự tạo mặc định theo `EnsureDefaultLayoutAsync` (WarehouseOperationsService).
- 404 khi staff chưa được gán kho.

## B. Estimate thực tế + giới hạn batch

Model changes:
- `DonationRequest`: + `EstimatedItemCount` (int, 1–2000), `EstimatedVolumeLiters` (decimal, 0.1–10000);
  `EstimateWeight` validate 0.5–500 kg, bắt buộc khi tạo/sửa (StaffPickup + DropOff).
- `Warehouse`: + `MaxBatchWeightKg` (default 200), `MaxBatchItemCount` (default 500),
  `MaxBatchVolumeLiters` (default 1500). Manager chỉnh qua CRUD kho hiện có.

Enforcement:
- Tạo/sửa DR: estimate ≤ max batch của kho đích.
- `ConfirmPickupAsync` / `ConfirmWarehouseDropOffAsync`: tổng dồn của IntakeBatch + estimate mới
  không vượt max của kho (kg, món, lít) → lỗi message tiếng Anh theo convention.
- `PlanShiftAsync` / `AutoBalanceShiftAsync`: khi gom requests vào batch, không vượt max kg của kho
  (món/lít cũng check khi ước tính; request vượt max đơn lẻ → skip + ghi chú).
- `SendToClassificationAsync`: chặn khi tổng khối lượng ≤ 0.

New endpoint: `GET api/donor-requests/eligible-warehouses?latitude&longitude&pickupDate&estimateWeight`
— trả các kho: trong `ServiceRadiusKm`, `TotalCapacityKg - CurrentWeight ≥ estimate`, có ca Scheduled/InProgress
phủ pickupDate. Donor form gọi khi nhập địa chỉ. `CreateDonorRequestDto` thêm `WarehouseId` tùy chọn
(không gửi → auto-nearest như hiện tại); server validate donor chỉ chọn được kho trong eligible list.

## C. Quota xe per team / ca

Model changes:
- `OperationalTeam`: + `VehicleType` (string, "Motorbike"|"Car"; bắt buộc với TeamType ReceivingPickup),
  `MaxOrdersPerShift` (int, 1–200), `MaxKgPerShift` (decimal, 1–5000). Null = không giới hạn.

Enforcement:
- `PlanShiftAsync`, `AssignRequestAsync`: đếm assignments Pending của team trong ca
  (+ kg theo EstimateWeight) → vượt quota → lỗi/skip.
- `AutoBalanceShiftAsync`: bỏ chia count-only; chia theo kg chiếm dụng (best-fit) + quota đơn/kg;
  team nào hết quota bị loại khỏi vòng.

Exposure:
- `ReceivingBatchDto` + `WarehouseDutyContextDto`: + `VehicleType, MaxOrdersPerShift, UsedOrders,
  MaxKgPerShift, UsedKg` (used tính từ assignments Pending/Processed trong ca của team đó).

## D. Map + thứ tự lấy hàng + không cân lại

Model changes:
- `DonationRequest`: + `PickupLatitude`, `PickupLongitude` (double?, nullable).

Flow:
- `CreateAsync` / `UpdateAsync` (DonorRequestService): geocode `PickupAddress` qua Geoapify
  (HttpClient đã config) — fail/log lỗi thì để null, KHÔNG chặn tạo DR. Donor gửi
  `PickupLatitude/PickupLongitude` sẵn → ưu tiên dùng.
- `ReceivingRequestDto`: + `RouteOrder, AreaKey, PickupLatitude, PickupLongitude`.
  (danh sách đã sort theo RouteOrder phía service) → mobile vẽ map + số thứ tự + donor contact.
- `ConfirmPickupDto`: bỏ `ActualWeight` (param `decimal? ActualWeight = null` vẫn nhận cho tương thích
  mobile cũ — bỏ trống thì server lấy `EstimateWeight`). `ActualWeight` entity được set = EstimateWeight
  để báo cáo/điểm thưởng hiện hành không vỡ. FB "chỉ lấy hàng không cân lại" = không bắt buộc nhập cân.

## E. Tái chế về + bàn giao classification + màn hình phân loại

DTO exposure:
- Batch list/detail DTOs (classification + warehouse): + `IsRecycledReturn` (INT có
  `ProcessingOperationOutputId`), `SourceOperationCode`, `SourceOrganizationName`.
- Classification batch list tách nhóm: `NormalBatches` / `RecycledReturnBatches` — client hiển thị
  section "Đồ tái chế về" riêng.
- Batch tái chế (RC-) chỉ được xếp khu `Recycled` (giữ ràng buộc hiện có, thêm message rõ hơn khi sai khu).
- Batch list DTO thêm `ImageUrls` (nếu thiếu) — mobile hiển thị hình ngoài detail.

Handover ràng buộc:
- `SendToClassificationAsync` (receiving): tổng ActualWeight > 0 và tổng estimated items > 0.
- `ConfirmReceiptAsync` (classification): DTO bắt buộc `ReceivedItemCount`, `ReceivedTotalWeight`
  → ghi `CountedItemCount/CountedTotalWeight`; lệch với handover → bắt buộc `CountingNotes`
  (điền lý do chênh lệch) mới cho xác nhận.

Layout:
- `GET classified-area-layout`: trả sections theo khu (Unclassified / Classified / Recycled),
  mỗi section có areas + capacity/used/available + locations. Client chia màn hình theo section.

## F. Form đăng ký tổ chức + Manager duyệt

Model changes:
- `User`: + `OrganizationName`, `TaxCode`, `CertificateImageUrl` (string?, nvarchar max 500).
- `UserStatus` thêm giá trị `"PendingApproval"` (string status, không enum).

Endpoints:
- `POST api/auth/register-organization` — anonymous, multipart/form-data:
  `OrganizationType` (CharityOrganization|RecyclingOrganization|DisposalOrganization), `OrganizationName`,
  `TaxCode`, `Address` (địa chỉ vật lý), `UserName`, `Email`, `PhoneNumber`, `Password`,
  `CertificateFile` (IFormFile, ≤5MB, pdf/jpg/jpeg/png). Lưu file vào `wwwroot/uploads/certificates/`,
  trả `CertificateImageUrl` qua static files. Tạo User với `UserStatus = "PendingApproval"`,
  `EmailConfirmed = false`. Email xác minh vẫn chạy như flow donor, nhưng login chặn tới khi Active.
- ManagerAccounts: `GET api/manager-accounts/pending-organizations`,
  `POST {id}/approve` (set Active + EmailConfirmed + notification),
  `POST {id}/reject` (body reason → set Inactive + notification).

## G. Charity yêu cầu theo thuộc tính (thay catalog chọn batch)

Model changes:
- `DistributionRequest`: + `RequestedClothingTypeId`, `RequestedGenderId`, `RequestedSizeId`,
  `RequestedTargetUserId` (Guid? → Categories), `RequestedWeightKg` (decimal, > 0),
  `RequestedQuantity` (int, > 0) — charity request không có Items (không chọn Inventory).

Flow:
- `GET api/distribution-operations/request-criteria`: danh mục ClothingType/Gender/Size/TargetUser
  từ `Categories` (Type + ParentId) — cho form chọn.
- `CreateAsync` (charity): nhận criteria + kg/món mong muốn, không cần InventoryIds.
  Status như flow cũ ("PendingManagerApproval"), notification Manager giữ nguyên.
- `ApproveAsync` (manager): auto-match inventories (`Status=Available`, `ProcessingDirection=Charity`,
  khớp attributes, sắp theo created date) cho tới đủ kg/món → tạo `DistributionItems` + reserve.
  Không đủ tồn → reject với lý do "Không đủ hàng khớp yêu cầu".
- Warehouse issue / GHN / confirm-receipt flow giữ nguyên. Flow Manager→Recycling/Disposal giữ nguyên
  (vẫn chọn inventory cụ thể).

## H. AI quota

Model changes:
- `AiPromptConfiguration`: + `DailyRequestLimit` (int?), `TotalRequestLimit` (int?) — null = không giới hạn.
- Bảng mới `AiUsageLogs`: `Id`, `UserId`, `Feature` (string 100), `UsageDate` (date),
  `CreateAt`; index `(Feature, UsageDate, UserId)`.

Flow:
- `POST analyze-images` (ClassificationOperationsController): trước khi gọi Gemini —
  trong transaction, đếm usage ngày hiện tại (giờ VN qua `VietnamTime.Now.Date`) + tổng;
  vượt Daily hoặc Total limit → InvalidOperationException message rõ ("AI quota exceeded for today /
  total, please try later"). Pass → insert AiUsageLog rồi gọi.

## I. Rename label "Chuyên viên xuất nhập kho"

- Đổi seed `Description` của role WarehouseStaff trong `AppDbContext` (migration data update).
  `RoleName` giữ nguyên "WarehouseStaff" để không phá token/Authorize.
- Label nút "khu vực lưu trữ", tên hiển thị role: frontend/mobile (ngoài repo).

## Kiểm chứng

1. `dotnet build` toàn solution.
2. EF migration áp lên DB local; kiểm tra cột/bảng mới.
3. Smoke test bằng tài khoản demo: manager.demo / receiving.demo(01-05) / classification.demo(01-05) /
   warehouse.demo(01-05) / charity.demo(01-05) — mật khẩu ReThreads@2026:
   login từng role, gọi endpoint chính của phase tương ứng.
4. Check-style scripts (mẫu tests/ProcessingOperations.Checks) cho quota/limit/approve.
5. Regression: receiving pickup flow (plan → staff confirm), classification flow, warehouse issue flow.
