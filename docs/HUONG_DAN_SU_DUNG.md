# Hướng dẫn quy trình sử dụng ChickenPOS

Mở [ChickenPOS](http://127.0.0.1:5080) và đăng nhập bằng tài khoản của vai trò bạn cần. Nếu ứng dụng chưa chạy, xem [hướng dẫn cài đặt và khởi động](../README.md#cài-đặt-và-khởi-động).

## Bán hàng và tạo đơn

1. Đăng nhập bằng tài khoản **Bán hàng** hoặc **Thu ngân** và mở **Bán hàng**.
2. Tìm món theo tên hoặc nhóm, bấm vào món để thêm vào giỏ và chỉnh số lượng.
3. Chọn **Tại bàn** rồi chọn bàn trống, hoặc chọn **Tại quầy** cho đơn trả trước.
4. Tạo đơn. Trong **Đơn hàng**, chọn đơn để xem chi tiết, gọi thêm món khi đơn còn mở và chưa thanh toán, hoặc gửi các món mới sang bếp.

| Hình thức | Thứ tự thao tác |
| --- | --- |
| Tại bàn | Tạo đơn → gửi bếp → chế biến → giao món → thanh toán |
| Tại quầy | Tạo đơn → thanh toán → gửi bếp → chế biến → giao món |

## Chế biến và giao món

1. Tài khoản **Bếp** mở **Màn hình bếp**, chọn món đang chờ và bấm **Bắt đầu chế biến**. ChickenPOS kiểm tra tồn kho và trừ nguyên liệu theo công thức ở bước này.
2. Khi chế biến xong, bếp bấm **Món đã sẵn sàng**.
3. Sau khi giao món cho khách, nhân viên **Bán hàng** hoặc **Thu ngân** bấm **Xác nhận giao** trong chi tiết đơn hoặc **Xác nhận giao món** trên màn hình bếp.

Nếu bếp báo thiếu nguyên liệu, quản lý kiểm tra mục **Kho nguyên liệu**. Nếu hệ thống báo món chưa có công thức, cần bổ sung công thức trước khi bếp có thể bắt đầu chế biến. Hộp thoại **Thêm món** hiện dùng để nhập thông tin bán hàng và ảnh.

## Thu tiền và in hóa đơn

1. Đăng nhập bằng tài khoản **Thu ngân**, vào **Đơn hàng**, mở đơn và bấm **Thanh toán**.
2. Chọn **Tiền mặt** và nhập số tiền khách đưa để hệ thống tính tiền thừa. Với **Chuyển khoản**, kiểm tra tiền đã nhận rồi đánh dấu xác nhận.
3. Xác nhận thanh toán. Khi cần, mở lại đơn và chọn **In hóa đơn** để in hóa đơn nội bộ.

## Quản lý thực đơn, kho và doanh thu

| Mục | Cách thao tác |
| --- | --- |
| **Thực đơn** | Chọn **Thêm món** hoặc **Sửa**, nhập tên, giá, nhóm và chọn ảnh nếu cần. Hỗ trợ JPG, PNG và WebP tối đa 10 MB. |
| **Ngừng bán hoặc xóa món** | Bỏ chọn **Đang kinh doanh** để ẩn món khỏi màn bán hàng. Dùng **Xóa** với món chưa từng xuất hiện trong đơn hàng. |
| **Kho nguyên liệu** | Xem tồn và cảnh báo dưới ngưỡng. Khi nhập hàng hoặc kiểm kê, chọn **Nhập / điều chỉnh kho**, nhập lượng thay đổi và lý do. |
| **Duyệt hủy** | Nhân viên gửi lý do để tạo yêu cầu **Chờ duyệt**; hóa đơn chưa bị hủy. Quản lý kiểm tra đơn, người yêu cầu và lý do, sau đó chọn **Duyệt hủy** hoặc **Từ chối** và nhập lý do quyết định. Đơn đã thanh toán không thể gửi hoặc duyệt hủy. |
| **Tổng quan** và **Doanh thu** | Xem hoạt động trong ngày, cảnh báo kho và báo cáo doanh thu theo ngày. Chọn **Cập nhật** để tải lại số liệu. |
