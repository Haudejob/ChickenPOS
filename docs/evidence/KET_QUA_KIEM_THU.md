# Kết quả kiểm thử tích hợp ChickenPOS

Thực thi UTC: 2026-11-09T11:06:42.356545+00:00

Kiểm thử HTTP trên ứng dụng ASP.NET và SQL Server LocalDB riêng. Không chạy trên DB demo buổi báo cáo.

| Ca | FR/BR | Nội dung | Thực tế | Thời gian (ms) |
|---|---|---|---|---|
| TC01 | FR10 | Đăng nhập sai bị từ chối | PASS | 103 |
| TC02 | FR11/BR17 | Bếp không được quản lý món | PASS | 6 |
| TC03 | NFR06 | POST thiếu CSRF bị từ chối | PASS | 17 |
| TC04 | FR02 | Giá âm bị chặn | PASS | 11 |
| TC05 | FR06/BR11 | Số lượng 0 bị chặn | PASS | 14 |
| TC06 | FR08/FR09/FR13 | Tạo đơn, tính 97.000đ, lưu DB và chưa trừ kho | PASS | 132 |
| TC07 | FR14 | Gửi lặp không tạo công việc bếp trùng | PASS | 115 |
| TC08 | FR13/FR14 | Gọi thêm chỉ gửi dòng mới, gọi lặp không thêm trùng | PASS | 34 |
| TC09 | FR16/FR24 | Bấm bắt đầu hai lần chỉ trừ kho một lần | PASS | 94 |
| TC10 | FR24/BR13 | Hai yêu cầu đồng thời cho cùng dòng chỉ có một Consume | FAIL | 201 |
| TC11 | FR24/BR19 | Thiếu thành phần thứ hai: không trừ phần thứ nhất, giữ Chờ | PASS | 189 |
| TC12 | FR03/BR10 | Đổi giá thực đơn không đổi đơn giá đã chụp | PASS | 35 |
| TC13 | FR16/FR17/FR19 | Quầy trả trước, tiền thừa đúng, chế biến tiếp và hoàn tất | PASS | 258 |
| TC14 | FR19/FR26/BR12 | Hai thu ngân đồng thời chỉ một Payment và doanh thu cộng một lần | PASS | 235 |
| TC15 | FR18/BR20 | Hủy sau chế biến cần duyệt, ghi hao hụt và không xuất/hoàn lần hai | PASS | 175 |
| TC16 | FR18/TK06 | Đơn đã thanh toán không được hủy | PASS | 75 |
| TC17 | FR23/NFR08 | Điều chỉnh có khóa chống lặp, không làm tồn âm | PASS | 34 |
| TC18 | FR12/FR17/FR19 | Bàn hoàn tất đóng phiên cũ, mở phiên mới và chặn gọi thêm vào đơn cũ | PASS | 233 |
| TC19 | FR10 | Đăng xuất khiến phiên cũ không đọc được API | PASS | 457 |
| TC20 | FR24/BR19 | Tồn 1kg, hai dòng cần 0,75kg: chỉ một dòng bắt đầu, không tồn âm | PASS | 356 |
| TC21 | FR19/BR17 | Bán hàng không được gọi API thu tiền | PASS | 3 |
| TC22 | FR12/BR18 | Một bàn không có hai phiên mở | PASS | 62 |
| TC23 | Menu mở rộng | Các món và combo mới đúng ảnh, giá, công thức và chỉ trừ kho một lần | PASS | 1843 |
| TC24 | FR18 | Đơn chỉ có món Served bị từ chối hủy | PASS | 303 |
| TC25 | FR18/FR19 | Chỉ hủy món Cooking, giữ món Served và hao hụt riêng | PASS | 357 |
| TC26 | FR18/FR19 | Hủy món Ready chưa giao, giữ nguyên món Served | PASS | 398 |
| TC27 | FR18 | Manager tự duyệt hủy và ghi Waste đúng một lần | PASS | 222 |
| TC28 | FR02/FR03 | Quản lý thêm, thay ảnh món và giữ ảnh khi chỉ sửa thông tin | PASS | 141 |
| TC29 | Thực đơn | Xóa món chưa dùng, giữ món có lịch sử đơn và kiểm tra quyền | PASS | 106 |

Tổng: 28 PASS, 1 FAIL. Thời gian là thời gian ca thử, không phải kết quả benchmark NFR02.

TC10: 
