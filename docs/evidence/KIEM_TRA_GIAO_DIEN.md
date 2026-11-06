# Kiểm tra giao diện thủ công ngày 06/11/2026

Đã chạy bằng trình duyệt trên `http://127.0.0.1:5080`, backend ASP.NET và SQL Server LocalDB của bản demo. Đây là kiểm tra luồng giao diện trên một môi trường, không phải xác nhận tương thích mọi thiết bị.

1. Đăng nhập `banhang`, chọn 1 Gà giòn cay + 1 Coca-Cola và Bàn 08: tổng 58.000đ.
2. Lưu đơn `CP-8F647608FA`, gửi hai dòng sang bếp.
3. Đăng nhập `bep`, bắt đầu từng món, chuyển Sẵn sàng.
4. Đăng nhập `thungan`, xác nhận giao cả hai món, thanh toán tiền mặt 100.000đ: tiền thừa 42.000đ. UI hiển thị Hoàn tất, Đã thanh toán và In hóa đơn.
5. Đăng nhập `quanly`: dashboard hiển thị doanh thu 58.000đ, một đơn đã trả và hai món bán. Kho gà từ 10kg còn 9,75kg, nước ngọt từ 20 còn 19 chai. Hai Consume có tên nhân viên bếp. Đây là dữ liệu demo phát sinh thực, không phải số liệu quán thật.
6. Đơn thử trước đó `CP-F5F8DE2489` vẫn có trong SQL sau khi server khởi động lại; trạng thái lúc kiểm tra là Đã hủy. Không reset/xóa dữ liệu demo để chụp ảnh.

Ảnh minh chứng:

- `01-dang-nhap.png`: trang đăng nhập thật.
- `02-ban-hang.png`: giỏ 58.000đ, bàn 08, trước xác nhận lưu.
- `03-bep.png`: một dòng chờ và một dòng đang chế biến.
- `04-thanh-toan.png`: hộp thoại tiền mặt 58.000đ, tiền khách đưa 100.000đ.
- `05-dashboard.png`: số liệu sau Payment thành công.
- `06-kho.png`: tồn sau tiêu hao.

Ảnh chỉ thể hiện trạng thái tại lúc chụp; số liệu sẽ thay đổi nếu nhóm tiếp tục thao tác trên cùng DB. Không có benchmark, kiểm thử máy in chuyên dụng hay chứng nhận bảo mật từ phiên kiểm tra giao diện này.
