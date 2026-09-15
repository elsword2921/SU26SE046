# BÁO CÁO HOÀN THÀNH — XỬ LÝ PHẢN HỒI NGÀY 11/09/2026

**Ngày hoàn thành:** 15/09/2026
**Phạm vi:** Backend (BE) + Frontend web (FE). Riêng **mobile app do bạn khác phụ trách** — BE đã chuẩn bị sẵn toàn bộ dữ liệu mobile cần (thứ tự đơn, tọa độ bản đồ, thông tin liên hệ, hạn mức xe), mobile chỉ cần hiển thị.

---

## 1. Tổng quan

Phản hồi ngày 11/09 gồm **17 mục cải tiến + 1 lỗi**. Tất cả đã được xử lý xong, build sạch **0 lỗi**, và được **kiểm thử trực tiếp trên hệ thống chạy thật** (API chạy local + cơ sở dữ liệu SQL Server thật) trước khi bàn giao.

| Hệ thống | Repository | Nhánh đã đẩy | Trạng thái |
|---|---|---|---|
| Backend | elsword2921/SU26SE046 | `feat/feedback-1109` + đã gộp vào `main` | ✅ Xong |
| Frontend web | longvufpt123/Used_clothing_fe | `LongVu` + đã gộp vào `main` | ✅ Xong |

---

## 2. Kết quả từng mục phản hồi

### Mục 1 — Nhập địa chỉ lấy hàng, hiện các kho có thể tiếp nhận

**Trước:** Donor nhập địa chỉ nhưng không biết kho nào nhận được đơn của mình; nếu chọn nhầm kho thì bị từ chối ở bước sau.

**Sau:**
- BE có API riêng: khi donor nhập đủ **địa chỉ + ngày lấy + khối lượng ước tính**, hệ thống tự tính khoảng cách đến từng kho, kiểm tra kho còn sức chứa, và có ca làm việc phù hợp ngày đó. Kho nào nhận được thì hiện ra — kèm **khoảng cách (km)** và **số kg còn nhận được**.
- FE web: ngay dưới ô địa chỉ lấy hàng hiện hộp xanh **"Các kho có thể tiếp nhận đơn của bạn"** — cập nhật tự động khi donor thay đổi địa chỉ/ngày/trọng lượng.

**Kết quả kiểm thử thực tế:** nhập tọa độ Quận 5 TP.HCM → hệ thống trả về kho Thủ Đức, cách 15.85 km, còn nhận 60.000 kg.

**File chính (BE):** `DonorRequestService.cs` (hàm GetEligibleWarehousesAsync), `DonorRequestController.cs`
**File chính (FE):** `Products.tsx`, `Products.css`

---

### Mục 2 — Donor ước tính khối lượng, số lượng, kích thước thực tế hơn

**Trước:** Donor chỉ chọn mức chung chung "5–10 kg"; không có số lượng món hay thể tích → không tính được kích thước lô hàng thật.

**Sau:**
- Donor khai thêm **số lượng món (1–2000)** và **thể tích lít (0.1–10000)**; khối lượng nhập chính xác 0.5–500 kg thay vì chọn mức.
- BE tự động kiểm tra giới hạn khi tạo/sửa đơn — nhập sai sẽ được thông báo ngay bằng tiếng Việt.

**File chính (BE):** `DonorRequestService.cs` (hàm ValidateEstimate), `CreateDonorRequestDto.cs`, `UpdateDonorRequestDto.cs`
**File chính (FE):** `Products.tsx` (thêm 2 ô nhập), `MyOrders.tsx`

---

### Mục 3 — Hạn mức tối đa đơn + kg mỗi team theo loại phương tiện

**Trước:** Không giới hạn số đơn mỗi team nhận trong ca → dễ giao quá tải cho xe máy.

**Sau:**
- Khi tạo team đi lấy hàng, Manager **bắt buộc chọn loại xe (xe máy / ô tô)** và có thể đặt **giới hạn số đơn** và **giới hạn kg mỗi ca**.
- Hệ thống tự động từ chối gán đơn khi team chạm hạn mức; chế độ chia đơn tự động cũng tôn trọng hạn mức này.

**File chính (BE):** `ReceivingOperationsService.cs` (tạo team, phân ca, chia đơn, gán đơn)
**File chính (FE):** `DispatchOperations.tsx` (form tạo team thêm ô loại xe + hạn mức)

---

### Mục 4 — Xem địa chỉ lấy hàng trên bản đồ (Receiving Staff)

**Trước:** Staff chỉ thấy địa chỉ dạng chữ, phải tự suy đoán vị trí.

**Sau:** Mỗi đơn giữ lại **vĩ độ/kinh độ** do donor chấm trên bản đồ lúc tạo đơn; nhân viên thấy ngay vị trí chính xác trên bản đồ số.

**File chính (BE):** lưu tọa độ khi tạo đơn (`DonorRequestService.cs`)
**File chính (FE):** `RouteMap.tsx`

---

### Mục 5 — Thứ tự đơn lấy trước, thông tin Donor, chi tiết từng điểm trên bản đồ

*(Mục này nói về mobile app — phần hiển thị do bạn khác làm.)*

**Phần BE chuẩn bị (đã xong):** API trả về **số thứ tự từng đơn trong tuyến (routeOrder)**, **tọa độ từng điểm**, **tên + số điện thoại donor** để mobile vẽ map đánh số và bấm gọi ngay.
**FE web (đã làm thêm):** bản đồ tuyến đường của staff hiện thứ tự theo điều phối của hệ thống thay vì tự xếp gần nhất như trước.

**File chính (BE):** `ReceivingOperationsService.cs`, `ReceivingOperationsDtos.cs` (thêm RouteOrder, PickupLatitude/PickupLongitude)

---

### Mục 6 — Chỉ lấy hàng, không cân lại

**Trước:** Staff buộc phải nhập cân nặng thực tế ở từng điểm lấy → mất thời gian, không có cân khi lấy hàng.

**Sau:** Ô cân nặng **để trống được** — hệ thống dùng khối lượng donor đã ước tính. Ai muốn cân vẫn nhập được như thường.

**File chính (BE):** `ReceivingOperationsService.cs` (ConfirmPickup)
**File chính (FE):** `ProcessRequest.tsx` (bỏ bắt buộc nhập cân)

---

### Mục 7 — Thể hiện khả năng tiếp còn lại của xe

**Trước:** Không ai biết team còn nhận được bao nhiêu đơn/kg nữa.

**Sau:** Dashboard của Receiving Staff hiện rõ trên thẻ team: **"Xe máy · 2/5 đơn đã nhận · 8/50 kg"**. Dữ liệu cập nhật theo từng đơn được gán.

**Kết quả kiểm thử thực tế:** tạo team xe máy hạn mức 5 đơn/50 kg, gán 2 đơn 8 kg → dashboard hiển thị đúng `2/5 đơn · 8/50 kg`.

**File chính (FE):** `Dashboard.tsx` (màn hình Receiving)
**File chính (BE):** DTO ReceivingBatch trả thêm các trường hạn mức

---

### Mục 8 — Giới hạn khối lượng / số lượng / kích thước từng lô (batch)

**Trước:** Không giới hạn → một lô có thể to quá sức chứa của kho.

**Sau:** Mỗi kho có **trọng lượng tối đa/lô (200 kg), số món tối đa/lô (500), thể tích tối đa/lô (1500 lít)** — Manager chỉnh được. Hệ thống chặn khi nhận đồ vào kho hoặc gom đơn vào lô vượt giới hạn.

**File chính (BE):** `ReceivingOperationsService.cs` (EnsureBatchCapacityAsync), `Warehouse` model

---

### Mục 9 — Tách riêng thông tin batch tái chế về

**Trước:** Đồ tái chế gửi về trộn lẫn với đồ thường, không phân biệt được.

**Sau:**
- Màn hình phân loại chia 2 khu: **"Đồ tái chế về"** hiển thị riêng, kèm **mã vận hành nguồn** và **tên tổ chức tái chế** đã gửi về.
- Lô tái chế (mã RC-) chỉ được xếp vào khu tái chế — xếp nhầm bị chặn kèm thông báo rõ.

**File chính (FE):** `Dashboard.tsx` (màn hình Classification — section riêng + thẻ nhận diện)
**File chính (BE):** `ClassificationOperationsService.cs`, DTOs

---

### Mục 10 — Ràng buộc số lượng + kg khi bàn giao cho Classification Staff

**Trước:** Staff phân loại nhận bao nhiêu không rõ, lệch bao nhiêu cũng được.

**Sau:** Khi bàn giao, hệ thống ghi rõ tổng số món + tổng kg. Staff phân loại **phải đếm lại và xác nhận số thật**; nếu lệch với bàn giao thì **bắt buộc ghi lý do chênh lệch** mới cho xác nhận.

**File chính (BE):** `ClassificationOperationsService.cs` (ConfirmReceiptAsync, CountBatchAsync)

---

### Mục 11 — Kiểm soát số lần dùng AI theo ngày và tổng

**Trước:** Không giới hạn — nhân viên có thể gọi AI phân loại ảnh vô hạn, tốn chi phí.

**Sau:**
- Manager cấu hình 2 con số: **giới hạn số lần/ngày** và **giới hạn tổng** cho mỗi nhân viên (để trống = không giới hạn).
- Vượt hạn mức → thông báo rõ "AI quota exceeded (2/2 requests today), please try again tomorrow". Việc tính "một ngày" theo **giờ Việt Nam**, không lệch múi giờ máy chủ.

**Kết quả kiểm thử thực tế:** đặt giới hạn 2 lần/ngày → lần gọi thứ 3 bị chặn đúng thông báo.

**File chính (BE):** `OpenAiClassificationService.cs` (EnforceQuotaAsync), bảng `AiUsageLogs`
**File chính (FE):** `AiPromptManagement.tsx` (thêm 2 ô cấu hình)

---

### Mục 12 — Màn hình phân loại chia rõ từng khu vực thao tác

*(Mục mobile — phần hiển thị do bạn khác làm.)*

**Phần BE chuẩn bị (đã xong):** API `classified-area-layout` trả về sơ đồ chia theo **3 khu: Chưa phân loại / Đã phân loại / Tái chế**, mỗi khu có khu vực + sức chứa + vị trí kệ. FE web đã dùng API này sẵn.

---

### Mục 13 — Đổi tên Warehouse Staff thành "Chuyên viên xuất nhập kho"

**Trước:** Hiển thị "Nhân viên kho".

**Sau:** Mọi chỗ hiển thị trên web (quản lý tài khoản, hồ sơ cá nhân, nhãn chat, trang quản trị) đều đổi thành **"Chuyên viên xuất nhập kho"**. Mã role trong hệ thống giữ nguyên để không ảnh hưởng đăng nhập/phan quyền.

**File chính (BE):** cập nhật mô tả role trong migration
**File chính (FE):** `Users.tsx`, `Profile.tsx`, `DonationChatRealtime.tsx`, trang Users của quản trị

---

### Mục 14 — Các nút "...kho" đổi thành "khu vực lưu trữ"

**Trước:** Menu kho hiển thị "Khu vực kho".

**Sau:** Đổi thành **"Khu vực lưu trữ"** trên menu kho của web.

**File chính (FE):** `WarehouseShell.tsx` (menu kho)

---

### Mục 15 — Hình quần áo hiển thị ngoài chỗ xem chi tiết batch

**Trước:** Muốn xem hình phải bấm vào từng lô.

**Sau:** Danh sách lô hàng phân loại hiện **ảnh đại diện ngay trên thẻ lô** (kèm số ảnh còn lại, ví dụ "+3"), không cần bấm vào chi tiết.

**File chính (FE):** `Dashboard.tsx` (màn Classification) + BE trả kèm danh sách ảnh trong DTO lô.

---

### Mục 16 — Form đăng ký riêng cho tổ chức + Manager phê duyệt

**Trước:** Tổ chức không có cách đăng ký riêng; không có giấy tờ chứng nhận.

**Sau:**
- Trang đăng nhập có nút **"Đăng ký tổ chức"** riêng: chọn loại tổ chức (từ thiện / tái chế / xử lý), điền tên, **mã số thuế**, **địa chỉ vật lý**, tải lên **giấy chứng nhận** (PDF/ảnh, tối đa 5MB).
- Tài khoản tạo ở trạng thái **chờ duyệt — đăng nhập bị chặn**.
- Manager thấy danh sách **"Tổ chức chờ duyệt"** (xem được giấy chứng nhận), bấm **Duyệt** (tài khoản kích hoạt + thông báo) hoặc **Từ chối** (kèm lý do gửi về tổ chức).

**Kết quả kiểm thử thực tế:** đăng ký tổ chức mới → đăng nhập bị chặn → Manager duyệt → đăng nhập thành công. Nhánh từ chối cũng kiểm thử đúng (kèm lý do).

**File chính (BE):** `AuthService.cs`, `ManagerAccountService.cs`, `AuthController.cs`, `ManagerAccountsController.cs`
**File chính (FE):** `Login.tsx` (form tổ chức), `Users.tsx` (tab chờ duyệt)

---

### Mục 17 — Tổ chức từ thiện chỉ yêu cầu loại quần áo, không chọn trong kho

**Trước:** Tổ chức từ thiện phải tự duyệt từng lô hàng trong kho để chọn — phức tạp và dễ chọn sai.

**Sau:**
- Tổ chức chỉ cần khai **mong muốn gì**: loại quần áo (bắt buộc), giới tính / kích cỡ / đối tượng (tùy chọn), **số kg mong muốn**.
- Khi Manager bấm duyệt, **hệ thống tự chọn các lô hàng phù hợp nhất** (lô cũ nhất trước) cho đến khi đủ số kg yêu cầu, và giữ chỗ luôn.
- Nếu không đủ hàng khớp yêu cầu → Manager nhận thông báo rõ ràng ("Chỉ còn 0/5 kg") để từ chối hoặc chờ hàng về.

**Kết quả kiểm thử thực tế:** tổ chức yêu cầu 20 kg áo → hệ thống tự ghép 2 lô (9 kg + 11 kg) và giữ chỗ đúng 20 kg. Yêu cầu 5 kg khi kho hết → báo lỗi rõ.

**File chính (BE):** `DistributionOperationsService.cs` (CreateAsync, ApproveAsync — tự động ghép lô)
**File chính (FE):** `DistributionPortal.tsx` (form yêu cầu theo tiêu chí, thay form chọn lô cũ)

---

### Lỗi — Receiving Staff không mở được dãy kho nhận đồ

**Trước:** Staff tiếp nhận bấm xem khu nhận đồ → **bị chặn 403** (API chỉ cho Manager/Warehouse Staff).

**Sau:** Có API riêng cho Receiving Staff — trả đúng khu nhận đồ + dãy + vị trí của **kho mà staff đó được phân**, tự tạo sơ đồ mặc định nếu kho chưa có. Kiểm thử: staff xem được 200, Manager vẫn dùng API quản trị như cũ (bảo mật giữ nguyên).

**File chính (BE):** `ReceivingOperationsService.cs` (GetMyWarehouseLayoutAsync), `ReceivingOperationsController.cs`

---

## 3. Lỗi phát sinh thêm — phát hiện & sửa trong lúc kiểm thử

Ba lỗi chỉ bộc lộ khi chạy thử hệ thống thật, đều đã sửa:

1. **Giấy chứng nhận tải lên nhưng không xem được (404):** folder chứa file chưa tồn tại lúc hệ thống khởi động nên tính năng trả file tĩnh tự tắt vô hình. → Đã fix để luôn hoạt động. *(BE — Program.cs)*
2. **Duyệt yêu cầu từ thiện bị lỗi "vừa thay đổi, tải lại":** lỗi kỹ thuật của EF khi tạo dòng chi tiết mới trong giao dịch duyệt. → Đã fix, duyệt mượt. *(BE — DistributionOperationsService.cs)*
3. **Danh sách lô của Receiving Staff treo quá 30 giây (timeout):** khi có lô đang hoạt động, truy vấn dữ liệu phình to kiểu bùng nổ tổ hợp. → Fix **1 dòng cấu hình**, thời gian phản hồi từ timeout xuống **~0.3 giây**. *(BE — Program.cs)*

---

## 4. Kiểm chứng tổng thể

| Hạng mục | Kết quả |
|---|---|
| Build toàn bộ Backend | ✅ 0 lỗi |
| Build toàn bộ Frontend web (kiểm tra type + đóng gói production) | ✅ 0 lỗi |
| Đăng ký tổ chức → chặn đăng nhập → Manager duyệt → đăng nhập được | ✅ Đã chạy thử |
| Manager từ chối kèm lý do | ✅ Đã chạy thử |
| Tạo team xe máy hạn mức → gán đơn → hiển thị 2/5 đơn, 8/50 kg | ✅ Đã chạy thử |
| Bản đồ: thứ tự đơn 1-2 + tọa độ từng điểm | ✅ Đã chạy thử |
| Kho khả dụng theo địa chỉ (15.85 km, 60000 kg) | ✅ Đã chạy thử |
| Hạn mức AI: lần thứ 3 bị chặn đúng | ✅ Đã chạy thử |
| Yêu cầu từ thiện 20 kg → tự ghép 2 lô đúng 20 kg | ✅ Đã chạy thử |
| Quy trình xuất kho + giao vận GHN sau khi tự ghép lô | ✅ Chạy hồi quy, không vỡ |

Dữ liệu dùng để kiểm thử đã được dọn khỏi cơ sở dữ liệu sau khi xong.

---

## 5. Ghi chú vận hành

- **Cấu hình Geoapify:** để tính tọa độ kho tự động (mục 1), Backend cần khai báo `Geoapify:ApiKey`. Máy chủ nhóm chưa cấu hình — đã xác nhận hoạt động ngay khi có key. Kho hiện tại đã có tọa độ sẵn trong DB.
- **Mobile app:** BE đã trả đủ `routeOrder`, tọa độ, liên hệ donor, hạn mức xe, 3 khu phân loại — mobile chỉ việc hiển thị.
- Mọi thay đổi đã commit trên GitHub (Backend: nhánh `feat/feedback-1109` + `main`; Frontend: nhánh `LongVu` + `main`), toàn bộ lịch sử commit ghi tên **longvufpt123**.
