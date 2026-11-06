# ChickenPOS · Bản demo báo cáo sản phẩm

Ứng dụng web quản lý quán gà rán cho học phần Công nghệ phần mềm. Backend ASP.NET/C#, giao diện HTML/CSS/JavaScript, dữ liệu SQL Server LocalDB. Được khởi tạo với hỗ trợ AI, cần cả nhóm review và hiểu phần mình trình bày.

**Đọc trước:** [Việc cần làm cho báo cáo 07/11/2026](docs/HUONG_DAN_BAO_CAO_NGAY_MAI.md), [kịch bản demo](docs/KICH_BAN_DEMO.md), [phạm vi và truy vết](docs/PHAM_VI_VA_TRUY_VET.md), [kết quả test thực tế](docs/evidence/KET_QUA_KIEM_THU.md).

## Chạy ngay trên máy đang làm việc

Mở PowerShell tại thư mục project:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Start-Demo.ps1 -NoBuild
```

Mở **http://127.0.0.1:5080**. `-NoBuild` dùng bản đã biên dịch, phù hợp buổi demo không có mạng. Sau khi sửa backend, dừng server bằng `Stop-Demo.ps1`, rồi chạy `Start-Demo.ps1` không kèm `-NoBuild`.

## Tài khoản demo cục bộ

| Tài khoản | Vai trò | Phạm vi chính |
|---|---|---|
| `quanly` | Quản lý | Thực đơn, kho, duyệt hủy, dashboard |
| `banhang` | Bán hàng | Đơn, gọi thêm, gửi bếp, giao món |
| `thungan` | Thu ngân | Tạo đơn, thu tiền, hóa đơn, giao món |
| `bep` | Bếp | Hàng đợi, bắt đầu, sẵn sàng |

Mật khẩu mẫu chung: **`Chicken@2026!`**. Chỉ dùng tài khoản này cho demo cục bộ. Mật khẩu trong DB lưu dạng hash. Có thể đặt biến môi trường `DemoPassword` **trước lần khởi tạo DB mới** để thay mật khẩu seed; đổi biến sau khi DB đã có tài khoản không đổi hash đang lưu.

## Thiết lập trên máy thành viên khác

1. Cần Windows x64 và **SQL Server Express LocalDB**. Khởi động được instance `MSSQLLocalDB`. Máy dùng SQL Server khác có thể đặt `ConnectionStrings__ChickenPos`; tên DB cần tiền tố `ChickenPOS_` để ứng dụng tự tạo an toàn trong phạm vi project.
2. Giải nén/clone source. Chạy `scripts/Setup.ps1` bằng PowerShell. Script tải SDK .NET 10 chính thức vào `.tools` nếu máy chưa có SDK phù hợp, khôi phục gói và build. Cần mạng ở lần thiết lập đầu.
3. Chạy `scripts/Start-Demo.ps1 -NoBuild`. Cơ sở dữ liệu demo `ChickenPOS_Demo_20261106` tự tạo và seed lần đầu; không reset/xóa dữ liệu khi chạy lại.
4. Muốn chạy bộ test cần Python 3.10+ và `scripts/Run-Tests.ps1`. Script tạo DB `ChickenPOS_Test_<timestamp>` mới, chạy 22 ca trên cổng 5081 rồi dừng server test, giữ DB để đối chiếu. DB test không trộn vào DB demo.

Các script chỉ quản lý server do chúng khởi động. Đóng trình duyệt không xóa đơn, thanh toán hay tồn kho. `.tools`, `artifacts`, MDF/LDF và bí mật không đưa vào Git/ZIP source. SDK và SQL Server không được đóng gói trong ZIP source; kiểm tra máy thuyết trình từ tối nay.

## Luồng có thể demo

- Đăng nhập bốn vai trò, kiểm tra quyền tại backend, logout thu hồi phiên.
- Thực đơn, tìm món, giỏ hàng, tạo đơn quầy/bàn, một phiên mở/bàn.
- Gọi thêm trước thanh toán, lưu giá chụp, chỉ gửi dòng mới sang bếp.
- Bếp bắt đầu: kiểm tra mọi nguyên liệu, trừ/ghi Consume/đổi trạng thái trong một transaction.
- Chế biến, sẵn sàng, giao món. Quầy trả trước, bàn trả sau, tiền mặt hoặc chuyển khoản thủ công.
- Chống thu trùng, hóa đơn nội bộ, đóng phiên khi đủ điều kiện.
- Điều chỉnh kho có lý do và lịch sử, cảnh báo tồn thấp, hủy sau bắt đầu cần quản lý duyệt, không xuất/hoàn lần hai.
- Dashboard từ Payment thành công, không phải số liệu mockup.

## Giới hạn cần nói đúng

Đây là bản demo học phần, không phải hệ thống production. Chưa có quản lý tài khoản qua UI, quy trình hoàn tiền/đổi món, thuế/phí/voucher, hóa đơn điện tử thuế, tích hợp ngân hàng, sao lưu/khôi phục, hay chứng nhận bảo mật. Chưa có bộ test hiệu năng p95/20 phiên hoặc kiểm thử đầy đủ mọi trình duyệt/thiết bị. Kho và công thức hiện dùng dữ liệu seed; chưa đủ toàn bộ chức năng CRUD danh mục/phiên bản công thức. Session thu hồi trong bộ nhớ của một server, restart server yêu cầu đăng nhập lại; nhiều server cần session store chia sẻ. Giao diện được xây mới theo tông ChickenPOS, chưa khẳng định khớp pixel với sáu mockup.

## Cấu trúc và làm việc nhóm

```text
src/ChickenPos.Web/Program.cs         API, xác thực, quyền, CSRF
src/ChickenPos.Web/SessionRegistry.cs Thu hồi phiên trên một server
src/ChickenPos.Web/Data/Store.cs      Dịch vụ nghiệp vụ và SQL transaction
src/ChickenPos.Web/Data/schema.sql    13 bảng logic, FK, UNIQUE, CHECK
src/ChickenPos.Web/wwwroot/           Giao diện thật đang chạy
scripts/                            Thiết lập, chạy demo, test, dừng server
tests/integration.py                Ca kiểm thử HTTP + SQL
docs/                               Hướng dẫn và bằng chứng báo cáo
```

Để kịp demo, dịch vụ và truy cập SQL còn cùng trong `Store.cs`, trong khi thiết kế buổi 3 tách lớp. Đây là đơn giản hóa triển khai cần nêu rõ; bước sau nên tách service/repository. Các controller logic hiện dùng ASP.NET Minimal API, không có lớp MVC Controller riêng.

Chưa có remote GitHub khi khởi tạo. Một người tạo repository nhóm rồi cấu hình remote/push; người khác clone, tạo nhánh và PR. Xem phân công trong hướng dẫn. Không sửa trực tiếp cùng một file qua thư mục đồng bộ khi chưa thống nhất. Không tạo commit giả cho người chưa làm. Mỗi thành viên cần review, sửa hoặc bổ sung thật và commit phần đó.

## Nguồn thiết kế

Workbook `QL_QuanGaRan_ThietKe_Buoi3.xlsx` trong thư mục buổi 3 của người dùng (SHA-256 `4123c7ba7a9de949b211fa122834ef8d67b968380ab1bba2b1e3059735591097`), template học phần và các quyết định TK01–TK09. Dữ liệu món/tồn đầu kỳ trong app là **dữ liệu seed demo**, không phải số liệu khảo sát hay vận hành quán thật.
