# Giới hạn tải và gợi ý phân công Receiving

## Hành vi

- Mặc định mỗi receiving team trong một ca: **8 đơn / 80 kg dự kiến**. Đây là cấu hình ban đầu, không phải kết luận về sức chở an toàn của mọi phương tiện.
- Manager chỉnh mặc định theo kho hoặc ghi đè cho từng team. Team kế thừa mặc định khi hai trường ghi đè là `null`.
- Giới hạn áp dụng cho team đi lấy và team trực kho. Không tự nhân giới hạn theo số nhân viên.
- Tải tính trên các phân công còn hiệu lực của team trong ca, gồm đơn đã nhận. Đơn hủy/hẹn lại bị loại khỏi tải. Tổng kg dùng `EstimateWeight`, kg đã cân được hiển thị riêng.
- API phân công đơn, chuyển team, lập tuyến cũ, tự động phân công cũ và xác nhận bản gợi ý đều kiểm tra giới hạn.
- Không cho hạ giới hạn dưới tải đã phân công của team/ca còn hoạt động. Có thể chuyển bớt đơn hoặc dùng ghi đè trước.
- Cân thực tế vẫn ghi nhận được khi cao hơn mức kế hoạch; Manager thấy cảnh báo nếu kg thực nhận vượt giới hạn. Không ép giảm số cân hoặc chặn hàng đã lấy.
- Các team/ca đã kết thúc không nhận phân công mới; team đã bắt đầu không bị thay đổi phân công.

## Gợi ý

Manager chọn **Điều phối tiếp nhận → ngày/kho → Gợi ý chia đơn trong ca**.

1. Chỉ xét đơn chưa phân công, cùng kho, cùng ngày, giờ hẹn nằm trong ca. Chọn đúng loại team đi lấy/trực kho, có 1–2 thành viên và chưa bắt đầu.
2. Tính tải đang có của từng team. Xử lý đơn nặng trước để giảm phần sức chứa còn lại không dùng được.
3. Loại team sẽ vượt số đơn hoặc kg sau khi nhận thêm.
4. Ưu tiên điểm tải dự kiến thấp nhất: `0.5 × số đơn / giới hạn đơn + 0.5 × kg dự kiến / giới hạn kg`.
5. Nếu điểm tải bằng nhau, ưu tiên team đã có cùng địa chỉ rồi cùng khu vực. Khu vực được suy ra từ địa chỉ; đây chưa phải bộ tối ưu khoảng cách/thời gian lái xe.
6. Liệt kê đơn không xếp được với lý do. Đơn vẫn chờ để Manager bổ sung team, đổi giới hạn hoặc xử lý lịch hẹn.

Gợi ý không ghi dữ liệu và không phát thông báo. Manager xem tải sau phân công, đổi team, bỏ đơn rồi xác nhận. Hệ thống giữ nguyên các đơn đã phân công; không âm thầm chuyển chúng sang team khác. Khi các ca có lịch hẹn/sức chứa khác nhau, kết quả không nhất thiết bằng nhau tuyệt đối.

Xác nhận kiểm tra lại toàn bộ điều kiện theo dữ liệu mới nhất. Toàn bộ đợt thành công hoặc rollback; một đơn lỗi không để lại các đơn đã lưu dở. Các thao tác ghi phân công/cấu hình dùng transaction Serializable và SQL Server application lock để chống vượt tải do gửi đồng thời.

## API (Manager)

| Method | Đường dẫn dưới `/api/receiving-operations` | Mục đích |
|---|---|---|
| GET | `capacity?warehouseId=...&date=YYYY-MM-DD` | Mặc định kho và tải team trong ngày; không truyền ngày thì dùng hôm nay tại Việt Nam |
| PUT | `warehouses/{id}/receiving-limits` | `{ maxRequests, maxWeightKg }` |
| PUT | `teams/{id}/receiving-limits` | Ghi đè giới hạn team |
| DELETE | `teams/{id}/receiving-limits` | Trở lại mặc định kho |
| GET | `plan-preview/{shiftId}` | Bản gợi ý, tải ban đầu, đơn chưa xếp được |
| POST | `apply-plan` | `{ shiftId, assignments: [{ requestId, teamId }] }` |

`dispatch-board` bổ sung `loads` và `estimateWeight` cho đơn. `manager/shifts` bổ sung `load` cho team và `estimateWeight` cho đơn đã phân công. Các endpoint cũ vẫn giữ cấu trúc phản hồi; tự động phân công nay chỉ thêm đơn trong ca được chọn, không tự cân lại cả ngày.

## Triển khai

Migration: `20260918034352_AddReceivingTeamCapacity`.

- Thêm mặc định 8/80 vào Warehouse, các cột ghi đè nullable vào OperationalTeam.
- Cần chạy migration trên database đích trước khi dùng backend mới; không có thay đổi dữ liệu phân công hiện có.
- Frontend và mobile mới cần backend này. APK 1.6.0 build 23 đã xuất trước đó chưa chứa phần mới; cần build APK mới nếu dùng trên điện thoại.
- Đã chạy migration trên production `ReThreadsDb` ngày 18/09/2026. Xác minh đủ 4 cột và 2 giá trị mặc định; không còn migration chờ. Cả 2 kho có mặc định 8 đơn / 80 kg, 27 team kế thừa mặc định; số kho/team không đổi. Việc cập nhật database không đồng nghĩa đã triển khai backend/frontend hoặc build APK mới.

Tạo script chỉ cho migration mới để kiểm tra trước khi triển khai:

```powershell
dotnet ef migrations script 20260915143640_AddOrganizationRegistration AddReceivingTeamCapacity --idempotent --project src/DAL --startup-project src/Capstone-API --output receiving-capacity.sql
```

## Kiểm tra

```powershell
dotnet run --project tests/ProcessingOperations.Checks
```

Bộ kiểm tra tạo LocalDB riêng rồi xóa, không dùng connection string production:

- Mặc định/ghi đè/reset; giới hạn không hợp lệ và hạ dưới tải hiện tại.
- Preview không ghi DB/thông báo; đơn dư có lý do.
- 6 đơn `35,35,5,5,5,5 kg` → 2 team, mỗi team 3 đơn / 45 kg.
- Chặn vượt tải khi phân công tay/chuyển team/endpoint lập tuyến cũ.
- Hai yêu cầu đồng thời chỉ một yêu cầu chiếm được suất cuối.
- Preview cũ không tạo phân công trùng; đợt lỗi rollback hoàn toàn.
- Team đã bắt đầu và ca đã đóng không nhận phân công mới.
- Nhận thực tế 50 kg vẫn được ghi đúng dù giới hạn lập kế hoạch là 1 kg.

Web: production build; kiểm tra bằng Edge/Playwright với API giả lập ở 1280 px và 390 px, gồm xem trước, đổi team vượt tải, hủy, xác nhận và sửa mặc định. Mobile: TypeScript, workflow tests và test component preview/chặn vượt tải/xác nhận một lần. Chưa xuất APK mới hoặc kiểm tra trực quan màn này trên thiết bị Android.
