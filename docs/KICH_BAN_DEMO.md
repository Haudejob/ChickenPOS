# Kịch bản demo trực tiếp 8–10 phút

URL: `http://127.0.0.1:5080`. Mật khẩu mẫu: `Chicken@2026!`. Toàn bộ tiền và nguyên liệu là dữ liệu demo, không giao dịch ngân hàng thật.

## 0. Chuẩn bị trước khi vào lớp

- Chạy app và kiểm tra SQL Server, không phụ thuộc mạng khi demo bản đã build.
- Xem bàn nào trống; ví dụ dưới dùng Bàn 02 nếu Bàn 01 đã có đơn từ lúc thử.
- Kiểm tra gà còn ≥ 1kg và khoai ≥ 0,2kg. Nếu cần nhập thêm, dùng `quanly` → Kho, có lý do “Chuẩn bị demo”. Không sửa số tồn trực tiếp trong SQL.
- Dùng logout/login để chuyển vai trò, hoặc các profile/cửa sổ riêng có cookie độc lập. Hai tab cùng trình duyệt chia sẻ cookie nên không giữ được hai vai trò khác nhau chỉ bằng việc mở thêm tab.

## 1. Bán hàng tạo đơn tại bàn (2 phút)

1. Đăng nhập `banhang`.
2. Chọn 2 Gà giòn cay + 1 Coca-Cola, chọn bàn trống, bấm Tạo đơn và lưu.
3. Mở chi tiết: tổng là 97.000đ với giá seed ban đầu. Chưa trừ kho ở bước tạo đơn.
4. Bấm Gửi món mới → bếp. Hai dòng vào trạng thái Chờ chế biến.
5. Gọi thêm 1 Khoai tây chiên, xác nhận, gửi lại. Lần này chỉ có 1 dòng mới gửi, tổng là 126.000đ.

Nói: “Một phiên bàn có một đơn tổng hợp. Dòng đã gửi giữ nguyên; gọi thêm tạo dòng mới.”

## 2. Bếp chế biến và giao món (2 phút)

1. Đăng xuất, vào `bep`.
2. Hàng đợi hiển thị mã đơn và các đợt gửi. Bắt đầu từng dòng, rồi bấm Món đã sẵn sàng.
3. Giải thích: 2 phần gà × 0,25kg = 0,50kg. Trừ kho và chuyển trạng thái cùng giao dịch.
4. Vào `banhang`, mục bếp hoặc chi tiết đơn, Xác nhận giao từng dòng Ready.
5. Đơn bàn chưa trả tiền vẫn chưa hoàn tất dù đã giao hết món.

## 3. Thu ngân thu tiền (1–2 phút)

1. Vào `thungan`, mở đúng mã đơn vừa demo.
2. Chọn tiền mặt, nhập 150.000đ. Tổng 126.000đ, tiền thừa 24.000đ nếu giá seed không đổi.
3. Xác nhận, mở lại đơn: Payment đã có, không có nút thu lại; khi món đã giao hết, đơn hoàn tất và bàn được giải phóng.
4. Mở In hóa đơn nếu cần, thoát hộp thoại in bằng Cancel. Đây là hóa đơn nội bộ, không phải hóa đơn thuế điện tử.

Nói: “Đơn quầy trả trước; đơn bàn dùng luồng trả sau. Thanh toán không tự làm món sẵn sàng.”

## 4. Quản lý xem kho và dashboard (1 phút)

1. Vào `quanly` → Kho: xem các Consume gắn dòng món, lượng tiêu hao và người thực hiện.
2. Dashboard: doanh thu và số đơn lấy từ Payment thật của DB demo. Nếu đã thử trước đó, chỉ số là tổng lũy kế hôm nay, không nhất thiết chỉ có đúng một đơn.
3. Mở lịch sử đơn để chỉ ra tên người và thời điểm của các thao tác.

## 5. Trình bày kiểm thử và giới hạn (2 phút)

- Mở `docs/evidence/KET_QUA_KIEM_THU.md`, 22 ca đã chạy trên DB test riêng.
- Chọn TC10/TC20: hai yêu cầu cùng bắt đầu không xuất trùng hoặc làm âm tồn.
- Chọn TC14: hai thu ngân cùng thanh toán chỉ một Payment và cộng doanh thu một lần.
- Chọn TC15: hủy sau bắt đầu cần quản lý, ghi hao hụt, không xuất/hoàn kho lần hai.
- Nêu rõ chưa test tải p95, chưa có hoàn tiền, account CRUD UI, ngân hàng hoặc hóa đơn thuế.

Nếu muốn demo lỗi thiếu kho trực tiếp: tạo một đơn mới, dùng quản lý điều chỉnh kho cần thiết về 0 với lý do demo, vào bếp bấm Bắt đầu, quan sát lỗi và Chờ giữ nguyên, sau đó nhập lại kho. Chỉ làm khi còn đủ thời gian; không phá lượng tồn đang cần cho luồng chính.
