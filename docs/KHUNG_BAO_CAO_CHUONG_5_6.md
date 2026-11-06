# Khung nội dung báo cáo chương 5–6

Tài liệu hỗ trợ để nhóm đưa vào template chính thức. Cần điền tên, phân công thực tế, ảnh và link GitHub. Không giữ các chỗ trống khi nộp.

## Chương 5. Xây dựng và kiểm thử hệ thống

### 5.1. Phạm vi xây dựng

Nhóm chọn luồng bán hàng xuyên suốt làm phạm vi demo: FR10, FR13, FR14, FR16, FR17, FR19, FR24. Theo workbook có 24 FR mức Cao, phạm vi 7 FR tương ứng khoảng 29,2% nhóm quan trọng. Các chức năng hỗ trợ và giới hạn được liệt kê trong PHAM_VI_VA_TRUY_VET.md. Đây là demo học phần, chưa khẳng định mọi yêu cầu của hệ thống hoàn thành.

**Bổ sung vào mục 1.7:** họ tên, nhiệm vụ cụ thể, phần đã thực hiện/review/kiểm thử, commit hoặc bằng chứng tương ứng. Thành viên ghi đúng công việc thực tế, không dùng bảng phân công đề xuất như biên bản hoàn thành.

### 5.2. Kết quả xây dựng module

| Nhóm chức năng | Kết quả có thể minh chứng | Ảnh cần chèn từ app thật |
|---|---|---|
| Tài khoản/quyền | Đăng nhập 4 vai trò, API kiểm tra quyền, logout thu hồi phiên | Màn hình đăng nhập và một lỗi từ chối quyền |
| Bàn/đơn | Tạo/gọi thêm, snapshot, gửi dòng mới, SQL lưu bền vững | Đơn và batch món mới |
| Bếp/kho | Start transaction, tiêu hao một lần, thiếu kho giữ Chờ | Hàng đợi và lịch sử Consume |
| Thanh toán | Thu một lần, tiền thừa, trạng thái đơn độc lập | Đơn đã trả/hóa đơn nội bộ |
| Kiểm soát/báo cáo | Duyệt hủy, hao hụt, dashboard đọc Payment | Yêu cầu duyệt, kho, doanh thu |

Công nghệ: ASP.NET/C#, HTML/CSS/JS, SQL Server LocalDB; thư viện và phiên bản cụ thể trong csproj/global.json. Mô tả đúng việc dùng Minimal API và Store.cs, không nói đã có kiến trúc layer hoàn chỉnh khi code đang gộp.

### 5.3. Mô hình và kế hoạch kiểm thử

Áp dụng kiểm thử tích hợp qua HTTP với backend và SQL thật trên database riêng. Bộ ca dùng cả thành công, sai dữ liệu, lặp thao tác, cạnh tranh và rollback. Đối chiếu kết quả với workbook FR/BR. Người phụ trách test điền tên thực tế. Chưa có unit test tách service/domain hoặc benchmark hiệu năng độc lập.

Môi trường của lần chạy ghi trong `evidence/test-environment.json`. Dữ liệu fixture được tạo mới trong DB test, không trộn dữ liệu buổi demo. Thao tác có CSRF và cookie đăng nhập tương ứng từng vai trò. Bằng chứng thực thi ở `evidence/integration-results.json`; mã ca ở `tests/integration.py`.

### 5.4. Kết quả kiểm thử

Phiên chạy hiện tại: **22 PASS, 0 FAIL**. Khi nhóm sửa code, phải chạy lại và dùng file kết quả mới nhất, không chép số này cho một phiên bản chưa test. Chèn bảng KET_QUA_KIEM_THU.md, tóm tắt TC10/TC20 (kho cạnh tranh), TC14 (thu trùng), TC15 (hủy/hao hụt), TC19 (cookie sau logout bị chặn). Thời gian ca test không phải kết quả đo p95 của NFR02.

Các lỗi còn lại chưa có bằng chứng loại trừ: điều kiện ngày biên báo cáo, thay đổi công thức thực tế, thiết bị di động/browser đa dạng, hiệu năng khi tải lớn, backup/restore. Không khẳng định “đạt 100% chất lượng”.

### 5.5. Minh chứng Git

Chèn link repository, ảnh git log và commit/PR của thành viên. Commit khởi tạo ghi nhận mốc tạo bộ nền. Mỗi người bổ sung/review/kiểm thử và ghi lại công việc thực tế của mình.

## Chương 6. Kết luận

### 6.1. Đánh giá chất lượng

Demo đã thể hiện được luồng đơn hàng, bếp, nguyên liệu và thu tiền với dữ liệu SQL, cùng các ràng buộc quan trọng được kiểm tra bằng 22 ca tích hợp. Việc đánh giá hiện giới hạn ở phạm vi ca đã chạy trên máy thử, chưa đủ kết luận hiệu năng, độ ổn định dài hạn hoặc vận hành thực tế.

### 6.2. Ưu điểm

- Luồng xuyên suốt và phân vai có thể trình diễn trực tiếp.
- Tổng tiền tính ở backend, giá chụp bảo vệ đơn cũ.
- Transaction và UNIQUE bảo vệ tiêu hao/thanh toán trong các tình huống đã kiểm tra.
- Có lịch sử và dữ liệu kiểm thử tái tạo được bằng script.

### 6.3. Nhược điểm và tồn tại

- Một phần UI quản trị/danh mục/công thức/tài khoản chưa hoàn thiện.
- Dịch vụ và truy cập SQL còn gộp; session chỉ hỗ trợ một server.
- Chưa có hoàn tiền, thuế/phí, ngân hàng hoặc hóa đơn điện tử thuế.
- Chưa đo tải, kiểm chứng đa thiết bị đầy đủ, hoặc backup/restore.
- Công thức và dữ liệu seed cần đối chiếu quán thực tế.

### 6.4. Hướng phát triển

Ưu tiên hoàn thiện quản trị người dùng và công thức, tách lớp service/repository, bổ sung unit/integration test biên ngày và trạng thái, rồi kiểm thử tải và thiết bị. Thiết kế quy trình trả trước nhưng thiếu kho/khách không chờ, hoàn tiền có kiểm soát trước khi thử vận hành thực tế. Đồng bộ lại UML/DFD/ERD với code và schema cuối cùng.
