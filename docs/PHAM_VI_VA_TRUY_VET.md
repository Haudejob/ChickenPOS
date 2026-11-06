# Phạm vi demo và truy vết yêu cầu

Workbook có 26 FR, trong đó 24 FR mức Cao, FR20 và FR25 mức Trung bình. Theo cách tính này, 20% nhóm quan trọng là 4,8, cần chọn **ít nhất 5 FR Cao**. Bản demo chọn **7 FR trọng tâm** dưới đây: 7/24 ≈ 29,2% nhóm Cao; kể cả dùng mẫu số toàn bộ 26 FR thì 7/26 ≈ 26,9%. Đây là cách nhóm xác định phạm vi xây dựng/kiểm thử, cần giảng viên chấp nhận tiêu chí ưu tiên, không phải tỷ lệ hoàn tất toàn bộ sản phẩm.

| FR trọng tâm | Chức năng chạy thật | Điểm triển khai | Ca kiểm thử thực thi |
|---|---|---|---|
| FR10 | Đăng nhập/đăng xuất, vai trò, thu hồi phiên phía server | Program.cs, SessionRegistry.cs, Store.Login/Active | TC01, TC19 (có thử replay cookie cũ) |
| FR13 | Tạo đơn quầy/bàn, giá chụp, gọi thêm trước trả tiền | Store.Create/Add, DiningSession, OrderDetail | TC06, TC08, TC12, TC18, TC22 |
| FR14 | Gửi chỉ dòng mới, batch/SentAt, chống gửi lặp | Store.Send | TC07–TC08 |
| FR16 | Chờ → Chế biến → Sẵn sàng, liên kết kiểm tra kho | Store.Start/Progress | TC09–TC11, TC13, TC20 |
| FR17 | Giao món, hoàn tất chỉ khi đã trả và giao/hủy hết | Store.Progress/CloseIfComplete | TC13, TC18 |
| FR19 | Tiền mặt/chuyển khoản thủ công, trả toàn bộ một lần | Store.Pay, Payment UNIQUE | TC13–TC14, TC18, TC21 |
| FR24 | Định lượng × số lượng, đủ toàn bộ, transaction, chống tiêu hao trùng | Store.Start, UNIQUE Consume | TC09–TC11, TC20 |

## Phần hỗ trợ có thật nhưng không tính gộp thành “đủ toàn bộ FR”

- FR01–FR05: có danh sách/tìm món, quản lý thêm/sửa/ngừng bán và kiểm tra dữ liệu cơ bản. Chưa kiểm chứng đầy đủ mọi tiêu chí, cạnh tranh chỉnh sửa và tìm tiếng Việt có/không dấu.
- FR06–FR09: giỏ, số lượng, tổng tiền backend và ghi SQL transaction. Giỏ phía trình duyệt chưa xác nhận không giữ qua reload. Không dùng tỷ lệ này để nhận tất cả ngoại lệ đã test.
- FR11: kiểm tra vai trò API có, nhưng chưa có giao diện tạo/khóa/gán tài khoản.
- FR12: tự mở/đóng phiên theo đơn và điều kiện hoàn tất, chưa có màn hình CRUD danh mục bàn riêng.
- FR18: có yêu cầu/duyệt hủy và hao hụt, TC15–TC16. Chưa hoàn tiền, đúng giới hạn TK06.
- FR20: có xem/in hóa đơn nội bộ sau thanh toán; chưa có bộ lọc tra cứu hóa đơn đầy đủ hoặc kết nối máy in chuyên dụng đã kiểm chứng.
- FR21–FR22: có nguyên liệu và công thức seed, FK/ràng buộc; chưa có UI CRUD nguyên liệu/phiên bản công thức. Dữ liệu minh họa 4 nguyên liệu/6 món, chưa là công thức quán thực tế.
- FR23: có nhập/điều chỉnh, lý do, khóa chống lặp và chặn âm, TC17. Chưa quản lý chứng từ nhà cung cấp.
- FR25: có cảnh báo tồn ≤ ngưỡng. Chưa có test riêng ngưỡng bằng 0 và UI sửa ngưỡng.
- FR26: dashboard dữ liệu thật, API lọc ngày (giờ Việt Nam), Payment không cộng trùng, TC14. UI hiện chỉ hiển thị hôm nay; chưa test đầy đủ mọi ranh giới ngày/tháng.

## Chênh lệch triển khai với thiết kế buổi 3

1. Công nghệ giữ đúng HTML/CSS/JS, ASP.NET/C#, SQL Server. Dùng Minimal API thay lớp MVC Controller; dịch vụ và truy cập SQL còn gộp trong Store.cs để kịp demo. Chưa phải tách layer hoàn chỉnh theo bản thiết kế.
2. Giữ 13 bảng logic; tên vật lý dùng Roles/Users/Orders để rõ và tránh từ khóa SQL. Recipe dùng khóa ghép theo workbook, không theo RecipeId trong SVG cũ.
3. Session đăng nhập có thu hồi phía server bằng bộ nhớ một tiến trình, không thêm bảng thứ 14. Chưa hỗ trợ nhiều server và restart yêu cầu đăng nhập lại.
4. UI là giao diện code thật mới, sử dụng màu và nhóm nghiệp vụ ChickenPOS. Sáu mockup là tham chiếu; không dùng ảnh mockup làm bằng chứng sản phẩm thực thi.
5. Dữ liệu tiền, món và tồn seed chỉ minh họa. Không gán là khảo sát hoặc dữ liệu vận hành thật.

## Phạm vi của kết quả test

22 ca HTTP/integration kiểm tra backend chạy với SQL Server LocalDB. Có dữ liệu sai, quyền, chống CSRF, idempotency, cạnh tranh và rollback. Chưa có bộ đo hiệu năng NFR02, thử tải 20 phiên, kiểm thử bảo mật chuyên sâu, kiểm tra toàn bộ viewport/browser hoặc người dùng thực địa. Kết quả PASS chỉ áp dụng assertion của từng ca trong phiên bản code đã chạy, không bảo đảm hệ thống không còn lỗi.
