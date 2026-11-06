# Làm việc nhóm qua GitHub

Repository: https://github.com/Haudejob/ChickenPOS

Nhánh chính: `main`. Chủ repo là người tích hợp bản dùng để báo cáo. Collaborators sẽ được thêm sau khi thành viên gửi username; người được mời phải chấp nhận lời mời bằng đúng tài khoản GitHub đó.

## 1. Thành viên nhận code lần đầu

Có thể dùng GitHub Desktop: đăng nhập tài khoản của mình → File → Clone repository → chọn repo `Haudejob/ChickenPOS` hoặc nhập URL → chọn thư mục riêng trên máy.

Hoặc dùng Git:

```powershell
git clone https://github.com/Haudejob/ChickenPOS.git
cd ChickenPOS
powershell -ExecutionPolicy Bypass -File .\scripts\Setup.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\Start-Demo.ps1 -NoBuild
```

Cần SQL Server Express LocalDB; xem README. `.tools` và `artifacts` không nằm trên GitHub, nên lần đầu phải chạy Setup, không chỉ clone rồi chạy NoBuild. Để máy chính demo ổn định, kiểm tra máy khác từ tối nay.

## 2. Mỗi lần bắt đầu công việc

Trước khi chuyển nhánh, xử lý các thay đổi đang làm: commit vào nhánh của mình hoặc dùng stash của GitHub Desktop. Không chọn Discard changes nếu còn cần nội dung.

```powershell
git switch main
git pull --ff-only origin main
git switch -c feature/ten-cong-viec
```

Tên nhánh ví dụ: `feature/pos-ui`, `feature/order-review`, `feature/kitchen-stock`, `feature/payment-report`, `test/integration`, `docs/chapter-5-6`. Nếu đã có nhánh làm việc, chuyển lại nhánh đó thay vì tạo trùng.

## 3. Gửi thay đổi để người khác nhận

Chỉ stage các tệp liên quan đến công việc của mình. Trong GitHub Desktop: xem Diff → tick tệp → viết Summary → Commit → Push origin → Create Pull Request.

Ví dụ bằng Git (thay đường dẫn bằng tệp mình đã sửa):

```powershell
git add src/ChickenPos.Web/wwwroot/app.css
git commit -m "Improve POS layout on smaller screens"
git push -u origin feature/ten-cong-viec
```

Tạo Pull Request trên GitHub, base `main`, mô tả thay đổi và cách kiểm tra. Người tích hợp review, chạy test nếu liên quan nghiệp vụ, rồi merge. Không dùng force push lên `main`, không dùng tài khoản của người khác để tạo lịch sử đóng góp.

## 4. Máy của Hậu nhận bản đã hợp nhất

Khi `main` đã có thay đổi mới và working tree sạch:

```powershell
git switch main
git pull --ff-only origin main
powershell -ExecutionPolicy Bypass -File .\scripts\Stop-Demo.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\Start-Demo.ps1
```

Code không tự sửa trên máy ngay khi người khác đang gõ. Pull nhận các commit đã merge. Sau thay đổi backend cần build/khởi động lại, rồi tải lại trang. Nếu chỉ đổi HTML/CSS/JS, trình duyệt vẫn cần tải lại để xem bản mới. Nếu `pull --ff-only` báo lịch sử đã phân nhánh hoặc có thay đổi đè nhau, dừng và review xung đột; không reset/xóa công việc để vượt lỗi.

## 5. Quy tắc phối hợp

- Mỗi người dùng tên/email Git của chính mình. Commit đầu tiên của repo được ghi `Codex AI-assisted` vì code nền do AI hỗ trợ khởi tạo.
- `Store.cs` hiện chứa nhiều nghiệp vụ chung: TV3/TV4/TV5 thống nhất vùng sửa trước, tránh cùng chỉnh một đoạn. Tên người phụ trách trong bảng phân công đã được chủ nhóm điền.
- Không commit `.tools`, `artifacts`, `.env`, mật khẩu riêng, file MDF/LDF hoặc thư mục `.vs`. `.gitignore` đã có sẵn.
- Chỉnh schema không tự cập nhật cấu trúc DB cũ: hiện schema tạo lần đầu, chưa có migration framework. Mọi thay đổi bảng phải có kế hoạch nâng cấp dữ liệu, không xóa DB demo để che lỗi.
- Trước báo cáo, chốt một commit trên `main`; không merge thêm ngay giữa lúc demo.

## Chung code và chung dữ liệu là hai việc khác nhau

Clone/pull chỉ đồng bộ source. Mỗi máy hiện dùng LocalDB riêng, nên đơn hàng/tồn kho không tự đồng bộ. Buổi báo cáo nên dùng một máy demo chính. Nếu cần cả nhóm thao tác cùng dữ liệu qua trình duyệt, phải triển khai server/database chung và cấu hình truy cập riêng; repo GitHub không làm việc đó thay cho ứng dụng.
