# Chuẩn bị báo cáo sản phẩm ngày 07/11/2026

## Báo cáo phần nào?

Theo template giảng viên, **chương 5 là trọng tâm phù hợp cho buổi show sản phẩm**: chọn xây dựng và kiểm thử ít nhất 20% yêu cầu quan trọng, chỉ rõ ai làm gì trong bảng phân công mục 1.7, trình bày nhóm chức năng đã xây, kế hoạch/kết quả kiểm thử và minh chứng Git. Chương 6 là kết luận ngắn về chất lượng, ưu/nhược điểm và hướng phát triển. Lời nhắn “show sản phẩm” chưa tự chứng minh giảng viên yêu cầu nộp toàn bộ chương 5–6 ngay trong buổi này; nhóm nên chuẩn bị nội dung đó để có thể báo cáo.

Không dành phần lớn thời gian trình bày lại DFD/ERD hoặc sáu mockup. Mở ứng dụng thật, tạo dữ liệu, chuyển vai trò, xử lý nghiệp vụ và cho thấy kết quả được lưu.

## Những việc phải hoàn thành tối nay, theo thứ tự

1. **Chốt máy demo và chạy app:** dùng `Start-Demo.ps1 -NoBuild`, thử cả bốn tài khoản. Máy hiện tại đã có bộ SDK cục bộ và LocalDB. Máy khác phải chạy Setup và kiểm tra trước, không đợi đến lớp mới tải phụ thuộc.
2. **Phân công sáu người:** điền tên thật vào bảng dưới, mỗi người đọc code/phạm vi của mình và xác nhận có thể giải thích. AI đã hỗ trợ khởi tạo; không nhận mọi thành viên đã viết phần đó khi chưa thực hiện.
3. **Chạy lại test:** dùng `Run-Tests.ps1`, đọc file kết quả. Nếu có sửa nghiệp vụ sau đó, chạy lại các ca liên quan hoặc cả bộ test. Không sửa PASS/FAIL bằng tay.
4. **Tập luồng demo 8–10 phút:** theo `KICH_BAN_DEMO.md`. Dùng bàn còn trống. Không nhầm DB test (cổng 5081) với demo (5080).
5. **Lưu bằng chứng:** Git log thật, ảnh ứng dụng chạy, file test JSON/Markdown. Quay một video dự phòng ngắn của cùng luồng nếu nhóm có công cụ ghi màn hình.
6. **Hoàn thiện báo cáo:** cập nhật bảng ai-làm-gì, chọn 7 FR trọng tâm, kế hoạch/kết quả test và phần giới hạn trong chương 6. Bản khung ở `KHUNG_BAO_CAO_CHUONG_5_6.md`.
7. **Đóng gói và diễn tập:** source ZIP hoặc GitHub, máy đã build sẵn, bản slide mới chỉ cần cô đọng phần sản phẩm. Dữ liệu LocalDB không đi theo ZIP; máy khác khởi tạo seed riêng, cần tập trước.

## Phân công đề xuất cho 6 thành viên

Đây là **nhiệm vụ cần review/bổ sung thực tế từ thời điểm nhận project**, không phải xác nhận sáu người đã viết các module AI vừa khởi tạo.

| Người (điền tên) | Việc tối nay | Minh chứng thật cần có | Vai trò lúc demo |
|---|---|---|---|
| TV1 | Tích hợp, chạy máy demo, Git/README, rà soát kiến trúc | Commit tích hợp hoặc review thực tế, app khởi động được | Dẫn bài và tổng kết |
| TV2 | Review giao diện POS, tìm món, giỏ, quản lý thực đơn | Sửa/cải thiện UI, ảnh before/after nếu có | Tạo đơn và gọi thêm |
| TV3 | Review đơn/bàn/phiên, snapshot, gửi dòng mới | Kiểm tra và bổ sung ca biên/commit thật | Giải thích đơn và phiên |
| TV4 | Review bếp, công thức, transaction, chống trừ trùng | Test thiếu kho/cạnh tranh, giải thích SQL | Thao tác bếp và kho |
| TV5 | Review thanh toán, hóa đơn, dashboard | Kiểm tra tiền thừa, thu trùng, tính doanh thu | Thu tiền và xem báo cáo |
| TV6 | QA, truy vết FR, biên bản test, khung chương 5–6 | Chạy test, kiểm tra phạm vi, biên bản review thật | Trình bày test và hạn chế |

Nếu giảng viên vẫn chỉ cho hai người thuyết trình: chọn TV1+TV4 hoặc hai người hiểu luồng tốt nhất. Bốn người còn lại vẫn phải có nhiệm vụ và hiểu phần mình phụ trách.

## Tổ chức Git cho cả nhóm

1. Một người tạo repo GitHub cho nhóm. Đẩy source đã kiểm tra, không đẩy `.tools`, `artifacts`, thông tin bí mật, MDF/LDF.
2. Mỗi người clone riêng, tạo nhánh ví dụ `feature/pos-ui`, `feature/order-review`, `test/stock-race`, `docs/chapter-5-6`.
3. Chốt đầu mối từng file: `app.js`/`app.css` TV2; `Store.cs` TV3/TV4/TV5 phối hợp trước khi sửa, tránh làm đồng thời cùng vùng.
4. Người tích hợp review và merge, chạy lại test trước demo.
5. Chụp `git log` thực tế. Không tạo nhiều commit bằng tên sáu người để giả lập quá trình làm việc đã diễn ra. Ghi rõ việc sử dụng AI nếu giảng viên có quy định.

## Cấu trúc nói trong buổi báo cáo

- 1 phút: đã chọn 7 yêu cầu trọng tâm nào, vì sao đủ mức tối thiểu.
- 6 phút: demo bàn → gọi thêm → bếp → kho → giao món → thu tiền → doanh thu.
- 2 phút: một lỗi thiếu kho hoặc thao tác lặp và file kết quả 22 ca test.
- 1 phút: phân công, Git, ưu/nhược điểm và việc tiếp theo.

Ưu tiên cho thấy luồng chạy trọn vẹn. Nếu thời gian ngắn, bỏ bớt phần quản lý thực đơn và dùng bảng kết quả test thay vì thực hiện tất cả ca lỗi trực tiếp.
