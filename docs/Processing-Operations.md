# Gửi hàng tái chế và tiêu hủy

## Quy tắc

- Nhãn B (`ConditionRating = 2`) chỉ đi `Recycling`, tới `RecyclingOrganization`.
- Nhãn C (`ConditionRating = 3`) chỉ đi `Disposal`, tới `DisposalOrganization`.
- Nhãn A dành cho từ thiện; không xuất qua processing operations.
- Mỗi yêu cầu chọn nguyên batch cùng một kho và một hướng xử lý. Backend lấy số lượng/khối lượng từ tồn kho. Client có thể gửi snapshot `requestedQuantity` / `requestedWeight`; nếu tồn kho đã đổi, yêu cầu bị từ chối để người dùng tải lại.
- Batch có khối lượng nhưng chưa ghi số món (`Quantity = 0`) vẫn được xử lý.
- Yêu cầu chờ tổ chức, chờ quản lý hoặc đã duyệt khóa batch với các yêu cầu processing khác. Khi từ chối hoặc hủy, batch được chọn lại.
- Khi quản lý duyệt, giữ toàn bộ số lượng/khối lượng. Khi xuất, trừ tồn, giải phóng phần giữ và cập nhật khối lượng ở vị trí, khu, kho trong cùng transaction.
- Hàng B/C không được xuất trực tiếp bằng API xuất tồn kho chung.

## Trạng thái và API

| API sau `/api/processing-operations` | Quyền | Chuyển trạng thái |
| --- | --- | --- |
| `GET /catalog?warehouseId=...&operationType=Recycling` | Manager | Kho, tổ chức, batch B/C và trạng thái khóa |
| `POST /` | Manager | → `PendingOrganizationApproval` |
| `POST /{id}/organization/approve` | Tổ chức được chỉ định | → `PendingManagerApproval` |
| `POST /{id}/organization/reject` | Tổ chức được chỉ định | → `RejectedByOrganization` |
| `POST /{id}/manager/approve` | Manager | → `Approved`, giữ tồn |
| `POST /{id}/manager/reject` | Manager | → `RejectedByManager` |
| `POST /{id}/cancel` | Manager | Trước khi xuất → `Cancelled`, trả tồn đã giữ nếu có |
| `POST /{id}/issue` | WarehouseStaff cùng kho | `Approved` → `ReadyForGhn`, tạo một phiếu OUT |
| `POST /{id}/ghn` | WarehouseStaff cùng kho | `ReadyForGhn` → `GhnBooked`, tạo vận đơn GHN đến kho lấy hàng |
| `POST /{id}/ghn/refresh` | Manager, WarehouseStaff cùng kho, tổ chức được chỉ định | Cập nhật trạng thái GHN và lịch sử; `delivered` → `Delivered` |
| `POST /{id}/organization/receive` | Tổ chức được chỉ định | Xác nhận thực nhận sau xuất kho từ `Issued`, `ReadyForGhn`, `GhnBooked`, `InTransit`, `Delivered`, `DeliveryException` → `OrganizationReceived`; không cần chờ GHN cập nhật. Ghi thời điểm và người xác nhận, không đổi trạng thái vận đơn GHN. |
| `POST /{id}/organization/complete` | Tổ chức tiêu hủy được chỉ định | `OrganizationReceived` → `Completed` |
| `GET /` | Manager, Organization, WarehouseStaff | Manager xem toàn bộ; tổ chức xem của mình; nhân viên kho xem yêu cầu chờ xuất/đã xuất của kho mình |
| `GET /{id}` | Manager, Organization, WarehouseStaff | Kiểm tra quyền sở hữu / cùng kho |

`reject` và `cancel` cần `{ "rejectionReason": "..." }`.
`issue` nhận `{ "notes": "..." }`. Kho tạo vận đơn ở bước riêng `/ghn` sau khi lập phiếu xuất; mã vận đơn được lấy từ GHN.
`complete` dành cho tiêu hủy, cần `{ "completionNotes": "Kết quả xử lý thực tế..." }`. Tái chế dùng luồng hẹn ngày, gửi trả và nhận về ở bên dưới.

Ví dụ tạo yêu cầu:

```json
{
  "operationType": "Recycling",
  "warehouseId": "<warehouse-guid>",
  "organizationId": "<organization-guid>",
  "requestNotes": "Bàn giao nguyên batch nhãn B",
  "inputs": [{ "inventoryId": "<inventory-guid>" }]
}
```

Các thao tác ghi dùng transaction Serializable. RowVersion trên operation, inventory, vị trí, khu và kho chặn ghi đè từ dữ liệu cũ. Unique index trên `InventoryTransactions.ReferenceId` với `ReferenceType = ProcessingOperation` chặn phiếu xuất trùng. Khi xung đột, API trả 409 để tải lại trạng thái; quyền sai trả 403, ID không tồn tại trả 404.

## Database và kiểm tra

Cần áp dụng migration trước khi chạy backend mới:

```powershell
dotnet ef database update --project src/DAL --startup-project src/Capstone-API
dotnet build src/Capstone-API/Capstone-API.csproj --configuration Release
dotnet run --project tests/ProcessingOperations.Checks --configuration Release
```

Các migration mới:

- `20260910115309_HardenProcessingOperationConcurrency`
- `20260910121152_ProtectWarehouseCapacityConcurrency`
- `20260910122303_AddProcessingGhnShipments`
- `20260910125534_AddRecyclingReturnWorkflow`
- `20260910140601_ProtectClassifiedBatchEdits`
- `20260910141311_AddClassificationBatchCarryover`

Chương trình kiểm tra tạo database SQL Server LocalDB riêng với tên ngẫu nhiên, không đọc connection string của ứng dụng và tự xóa database kiểm tra khi kết thúc. Nó kiểm tra nhãn, tổ chức, quyền, thứ tự duyệt, từ chối/hủy mở khóa batch, cập nhật tồn và công suất, cạnh tranh tạo/xuất, ghi đè dữ liệu cũ, nhận hàng và hoàn tất.

## Web

- Manager: `/manager/processing-operations` — tạo, lọc, duyệt/từ chối, hủy, theo dõi.
- Organization: `/organization/processing-operations` — phản hồi, nhận hàng, hoàn tất.
- WarehouseStaff: `/warehouse/processing-operations` — danh sách của kho, xuất và theo dõi bàn giao.
- Chi tiết: thêm `/{id}`; notification mở đúng route này.

Web dùng `AdminLayout` / `OpsLayout`, theme sáng/tối, toast và pagination hiện có. Kiểm tra trình duyệt bằng dữ liệu API giả lập tách biệt với kiểm tra service trên SQL Server LocalDB; không tạo yêu cầu vận hành thật để kiểm tra giao diện.

## GHN chiều kho → tổ chức xử lý

Web dùng cùng API dữ liệu tỉnh/quận/phường và cùng cấu hình `Ghn:Endpoint`, `Ghn:Token` (hoặc `GHN:Key`), `Ghn:ShopId` như flow từ thiện. Người gửi là kho; người nhận, điện thoại và địa chỉ lấy từ hồ sơ tổ chức.

Form tạo đơn có địa chỉ hai bên, bên trả phí, dịch vụ, yêu cầu xem hàng và kích thước kiện thực tế. Khối lượng gửi lấy từ `IssuedWeight`, đổi kg sang gram. Với tổng từ 20 kg trở lên, dùng dịch vụ hàng nặng; chia riêng mỗi batch thành kiện tối đa 30 kg. Kho cần đóng kiện đúng thông tin đã xác nhận. COD bằng 0.

`client_order_code = PROC-{operationId:N}` cố định qua mọi lần thử. Theo [tài liệu tạo đơn GHN](https://developer.ghn.vn/en/docs/order/create), gửi lại mã đã dùng trả vận đơn cũ; vì vậy có thể thử lại an toàn khi timeout hoặc lưu database thất bại. Không tự tạo mã khác khi lỗi. API kiểm tra cả HTTP status và trường `code` trong JSON.

Refresh thủ công cập nhật trạng thái, không tạo thêm event khi trạng thái không đổi và không kéo yêu cầu đã nhận/hoàn tất quay lại trạng thái giao hàng. Vận đơn hủy, hoàn, thất bại được hiển thị để xử lý; không tự cộng tồn hoặc đặt lại vận đơn. Chưa tích hợp webhook GHN cho processing operations.

Kiểm thử GHN dùng HttpMessageHandler giả lập, không gọi GHN thật: kiểm tra quyền, chưa xuất không được đặt đơn, dữ liệu địa chỉ, lỗi HTTP 200 nhưng `code=400`, timeout sau khi hãng đã nhận đơn, thử lại cùng mã, đổi kg/gram, lịch sử và thứ tự nhận hàng.

## Đồ tái chế trả về và phân loại lại

Sau `OrganizationReceived`, tổ chức tái chế dùng popup để xác nhận ngày trả dự kiến (ngày Việt Nam, không trước hôm nay), rồi khai báo các batch thực tế gửi về. Có thể đổi lịch trước khi gửi.

| API sau `/api/processing-operations/{id}` | Quyền | Kết quả |
| --- | --- | --- |
| `POST /organization/return-schedule` | Tổ chức tái chế được chỉ định | `ReturnScheduled`; `{ expectedReturnDate, notes }` |
| `POST /organization/return-dispatch` | Tổ chức tái chế được chỉ định | `ReturnInTransit`; `{ carrierName, trackingCode, completionNotes, batches: [{ description, quantity, weight }] }` |
| `GET /return-receipt-options` | WarehouseStaff cùng kho | Ca hôm nay và vị trí khu `Recycled` |
| `POST /return-receive` | WarehouseStaff cùng kho | `ReturnReceived`; `{ shiftId, batches: [{ outputId, storageLocationId, quantity, weight, notes }] }` |

Chiều về ghi nhận đơn vị vận chuyển và mã vận đơn/biên bản do tổ chức cung cấp; không tự đặt GHN chiều về. Tracking GHN chiều đi không ghi đè các trạng thái nhận/trả hàng.

Manager cấu hình khu loại **Đồ đã tái chế (`Recycled`)**, dãy và vị trí trong quản lý kho, theo sức chứa thực tế. Không tự lấy sức chứa từ các khu đang dùng. Cần có ca làm việc hôm nay của kho để ghi nhận nhập hàng. Popup báo rõ nếu thiếu cấu hình.

Manager cũng có thể chọn khu lưu kho riêng cho **Từ thiện (A)**, **Chờ tái chế (B)**, **Cách ly/tiêu hủy (C)** hoặc lưu kho đa mục đích. Các khu này vẫn có `AreaType=Storage`, kèm `ProcessingDirection` tương ứng. Vị trí mới kế thừa hướng xử lý của khu; API chặn hướng xử lý không khớp và chặn đổi mục đích khu đang có hàng. Migration `20260910151113_AddWarehouseAreaProcessingDirection` bổ sung trường này và nhận diện khu cũ khi tất cả vị trí hoạt động cùng hướng xử lý.

Kho xác nhận toàn bộ chuyến trong một transaction; mỗi batch có số lượng/khối lượng thực nhận và vị trí riêng. Chênh lệch với số gửi phải có lý do. Hệ thống kiểm tra sức chứa vị trí/dãy/khu, chống nhập trùng bằng trạng thái, rowversion và unique output → intake. Không tạo inventory ở bước này.

Mỗi output trở thành một `IntakeBatch` mã `RC-...`, trạng thái `AwaitingClassificationAssignment`, có liên kết `ProcessingOperationOutputId`. Manager phân công trên `/manager/classification-dispatch`; đội phân loại nhận, kiểm đếm, phân loại lại, ghép batch, đặt khu đã phân loại và bàn giao kho theo flow cũ. Nhãn mới quyết định hướng Charity/Recycling/Disposal. Tồn kho chỉ xuất hiện sau bước kho nhận batch đã phân loại và trở thành `Available` sau putaway.

Chi tiết yêu cầu hiển thị số gửi/thực nhận, mã batch mới, vị trí và đội/trạng thái phân loại. Trạng thái đợt xử lý giữ `ReturnReceived` sau bàn giao trở lại kho; tiến độ phân loại tiếp tục theo từng intake batch. Nguồn được truy qua inventory → classified batch → classified items → intake → output → operation → inputs, không gán yêu cầu quyên góp giả cho đồ trả về.

Kiểm tra LocalDB bao gồm hẹn ngày, quyền sở hữu, thứ tự gửi/nhận, sức chứa, chênh lệch, nhập trùng, manager phân công, nhân viên tiếp nhận và phân loại lại, truy xuất nguồn và luồng nhập kho sau phân loại. Kiểm tra browser dùng API giả lập cho popup ngày trả, gửi hàng, nhận hàng và liên kết phân công.

## Tiếp tục phân loại từ ca trước

Ngày nhập lô không giới hạn ngày phân loại. Lô chưa hoàn tất của ca đã hết giờ có thể chuyển sang team đang làm việc hôm nay bằng `POST /api/classification-operations/batches/{id}/resume` với `{ teamId }`. Người thao tác phải thuộc cả team cũ và team tiếp nhận, cùng kho. API giữ nguyên trạng thái lô, kiểm đếm, vật phẩm, vị trí và thông tin phân công ban đầu; bảng `ClassificationBatchTransfers` ghi lại team cũ/mới, người chuyển và thời điểm.

Web hiển thị các ca hôm nay qua `GET /api/classification-operations/my-current-teams`, kể cả team chưa có lô mới. Bắt đầu ca, mở lô cũ và xác nhận popup “Tiếp tục lô từ ca trước”, rồi mở lô để xử lý tiếp. Không mở lại ca cũ, không chuyển lô đã hoàn tất hoặc lô đang thuộc ca chưa hết giờ. Nếu chưa có team hôm nay, manager cần xếp ca trước.
