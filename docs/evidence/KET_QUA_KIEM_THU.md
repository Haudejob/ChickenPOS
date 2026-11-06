# Kết quả kiểm thử tích hợp ChickenPOS

Thực thi UTC: 2026-11-06T18:46:51.674274+00:00

Kiểm thử HTTP trên ứng dụng ASP.NET và SQL Server LocalDB riêng. Không chạy trên DB demo buổi báo cáo.

| Ca | FR/BR | Nội dung | Thực tế | Thời gian (ms) |
|---|---|---|---|---|
| TC01 | FR10 | Đăng nhập sai bị từ chối | PASS | 244 |
| TC02 | FR11/BR17 | Bếp không được quản lý món | PASS | 28 |
| TC03 | NFR06 | POST thiếu CSRF bị từ chối | PASS | 30 |
| TC04 | FR02 | Giá âm bị chặn | PASS | 35 |
| TC05 | FR06/BR11 | Số lượng 0 bị chặn | PASS | 49 |
| TC06 | FR08/FR09/FR13 | Tạo đơn, tính 97.000đ, lưu DB và chưa trừ kho | PASS | 268 |
| TC07 | FR14 | Gửi lặp không tạo công việc bếp trùng | PASS | 84 |
| TC08 | FR13/FR14 | Gọi thêm chỉ gửi dòng mới, gọi lặp không thêm trùng | PASS | 118 |
| TC09 | FR16/FR24 | Bấm bắt đầu hai lần chỉ trừ kho một lần | PASS | 199 |
| TC10 | FR24/BR13 | Hai yêu cầu đồng thời cho cùng dòng chỉ có một Consume | PASS | 1485 |
| TC11 | FR24/BR19 | Thiếu thành phần thứ hai: không trừ phần thứ nhất, giữ Chờ | PASS | 422 |
| TC12 | FR03/BR10 | Đổi giá thực đơn không đổi đơn giá đã chụp | PASS | 86 |
| TC13 | FR16/FR17/FR19 | Quầy trả trước, tiền thừa đúng, chế biến tiếp và hoàn tất | PASS | 536 |
| TC14 | FR19/FR26/BR12 | Hai thu ngân đồng thời chỉ một Payment và doanh thu cộng một lần | PASS | 512 |
| TC15 | FR18/BR20 | Hủy sau chế biến cần duyệt, ghi hao hụt và không xuất/hoàn lần hai | PASS | 516 |
| TC16 | FR18/TK06 | Đơn đã thanh toán không được hủy | PASS | 13 |
| TC17 | FR23/NFR08 | Điều chỉnh có khóa chống lặp, không làm tồn âm | PASS | 127 |
| TC18 | FR12/FR17/FR19 | Bàn giao món trước thu tiền, thu xong đóng phiên và chặn gọi thêm | PASS | 767 |
| TC19 | FR10 | Đăng xuất khiến phiên cũ không đọc được API | PASS | 1243 |
| TC20 | FR24/BR19 | Tồn 1kg, hai dòng cần 0,75kg: chỉ một dòng bắt đầu, không tồn âm | PASS | 1109 |
| TC21 | FR19/BR17 | Bán hàng không được gọi API thu tiền | PASS | 8 |
| TC22 | FR12/BR18 | Một bàn không có hai phiên mở | PASS | 126 |
| TC23 | Menu mở rộng | Bốn món mới: đúng ảnh, giá, công thức và chỉ trừ nguyên liệu một lần | PASS | 2011 |

Tổng: 23 PASS, 0 FAIL. Thời gian là thời gian ca thử, không phải kết quả benchmark NFR02.
