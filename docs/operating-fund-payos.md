# Quỹ vận hành ReThreads qua PayOS

## Luồng sử dụng

- Donor: menu **Quỹ ReThreads** (`/fund`). Tổ chức: **Quỹ ReThreads** (`/organization/fund`).
- Manager: **Quỹ vận hành** (`/manager/fund`). Xem tổng thu, chi, số dư, lịch sử giao dịch và đối soát lại giao dịch khi cần.
- Mobile: **Thêm → Quỹ ReThreads**, dành cho Donor, ba loại tổ chức và Manager.
- Chọn số tiền nguyên VND, xác nhận rồi mở trang PayOS. Sau thanh toán mobile, quay lại ứng dụng và chọn **Kiểm tra thanh toán**. Web quay về `/fund?contribution=...` tự yêu cầu máy chủ đối soát.
- Chỉ callback có chữ ký hợp lệ và phản hồi API PayOS được xác thực mới ghi nhận tiền. Query string quay về không phải bằng chứng thanh toán.
- Manager công bố khoản đã chi: nội dung, mục đích, ngày chi, số tiền, chứng từ bắt buộc. Web nhận JPG/PNG/PDF; mobile chọn JPG/PNG. Tối đa 5 MB. Cả hai tải được chứng từ JPG/PNG/PDF.
- Khoản chi và chứng từ chỉ cho tài khoản Donor, tổ chức và Manager đang hoạt động, đã xác nhận email, đăng nhập. Lịch sử đóng góp cá nhân chỉ chủ tài khoản hoặc Manager xem được.
- Hủy khoản ghi nhầm giữ nguyên chứng từ, nội dung, thời điểm và lý do hủy. Không có thao tác xóa lịch sử tài chính.

## Cấu hình máy chủ

Mặc định không có cấu hình thì thanh toán bị tắt. Không có chế độ mô phỏng thanh toán trong API production. Test dùng gateway giả trên database LocalDB riêng.

```text
PayOS__Enabled=false
PayOS__ClientId=<client-id của kênh thanh toán>
PayOS__ApiKey=<api-key>
PayOS__ChecksumKey=<checksum-key>
PayOS__ReturnUrl=https://<frontend-domain>/fund
```

Chỉ cấu hình khóa trên backend (environment/secret store), không đưa vào web/mobile hoặc Git. ReturnUrl bắt buộc HTTPS, không chứa query/fragment. Sau khi cấu hình đủ và kiểm tra kênh, đặt `PayOS__Enabled=true`.

Đăng ký webhook trên kênh PayOS:

```text
https://<api-domain>/api/operating-fund/payos/webhook
```

Endpoint nhận mẫu đăng ký có chữ ký hợp lệ và trả 200 nếu mã giao dịch không thuộc hệ thống. Webhook thật được kiểm tra chữ ký, loại tiền, mã link, rồi truy vấn API PayOS đối chiếu tổng tiền. Thanh toán thiếu/dư không tự cộng vào sổ; cần đối soát trên PayOS. Không đánh dấu Paid bằng tay.

Tài liệu PayOS hiện công bố endpoint production. Không gọi dữ liệu giả là PayOS Sandbox. Cần tài khoản/kênh đã xác thực để kiểm tra giao dịch thật:

- https://payos.vn/docs/api/
- https://payos.vn/docs/tich-hop-webhook/kiem-tra-du-lieu-voi-signature/

## Database và triển khai

Migration `20260922105104_AddOperatingFundPayOs` thêm `FundContribution` và `FundExpense`, không sửa dữ liệu nghiệp vụ cũ. Chưa áp dụng production trong lần triển khai mã nguồn này.

```powershell
dotnet ef migrations script 20260918034352_AddReceivingTeamCapacity AddOperatingFundPayOs --idempotent --project src/DAL --startup-project src/Capstone-API --output operating-fund.sql
```

Kiểm tra tên migration trước đó bằng danh sách file migration trước khi tạo script; có thể dùng `dotnet ef migrations script --idempotent` để tạo script toàn bộ lịch sử. Backup database và áp dụng migration trước khi phát hành API/web/mobile. Không rollback bằng cách drop bảng sau khi đã có dữ liệu tiền.

## Đảm bảo dữ liệu

- OrderCode tăng bằng SQL identity, có unique index. RequestKey chống tạo trùng khi retry; PaymentLinkId cũng unique.
- Tiền VND chỉ nhận số nguyên; đóng góp 1.000–500.000.000 VND. Giới hạn 5 link chờ chưa hết hạn mỗi tài khoản.
- Ghi nhận Paid, khoản chi và hủy khoản chi được tuần tự hóa bằng SQL transaction/application lock. Chi vượt số dư bị từ chối kể cả hai Manager ghi đồng thời.
- API thu–chi dùng `no-store`; chứng từ nằm trong database riêng, tải qua endpoint có xác thực, không qua URL ảnh công khai.
- Số dư là tổng đóng góp Paid trừ khoản chi còn hiệu lực, không phải số dư ngân hàng. Phí phát sinh phải ghi bằng khoản chi riêng. Đây là sổ theo dõi, không tích hợp PayOS chi hộ hay hoàn tiền ngân hàng.
- Không cộng điểm quần áo khi đóng góp tiền.
- Mobile lưu chứng từ tạm để mở/lưu bằng hệ điều hành rồi xóa bản cache của ứng dụng.

## Kiểm tra

```powershell
dotnet run --project tests/OperatingFund.Checks
```

Test luôn tạo và xóa database LocalDB ngẫu nhiên; không đọc connection string app. Bao gồm HMAC, chữ ký sai, mã link, số tiền, callback trùng/đồng thời, lịch sử riêng, quyền Manager, chi vượt số dư đồng thời, chứng từ và lịch sử hủy ghi nhầm. Cần Windows SQL LocalDB. Gateway được mô phỏng, không phát sinh thanh toán thật.
