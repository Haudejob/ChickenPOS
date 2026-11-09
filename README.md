# ChickenPOS

ChickenPOS là ứng dụng web quản lý cửa hàng gà rán. Ứng dụng hỗ trợ bán tại quầy và tại bàn, chuyển món sang bếp, thu tiền, quản lý thực đơn và nguyên liệu, theo dõi doanh thu.

Xem [Hướng dẫn quy trình sử dụng ChickenPOS](docs/HUONG_DAN_SU_DUNG.md) để thao tác theo từng vai trò.

## Cài đặt và khởi động

Cần Windows 64-bit, PowerShell và SQL Server Express LocalDB với instance `MSSQLLocalDB`. Mở PowerShell tại thư mục chứa README này. Ở lần đầu, chạy lệnh sau để chuẩn bị .NET SDK 10 và biên dịch ứng dụng. Bước này cần Internet để tải công cụ và thư viện.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Setup.ps1
```

Khởi động ứng dụng, sau đó mở [ChickenPOS trên máy này](http://127.0.0.1:5080).

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Start-Demo.ps1 -NoBuild
```

Ứng dụng tự tạo database và dữ liệu ban đầu ở lần chạy đầu tiên. Đơn hàng, thanh toán, tồn kho và ảnh món tải lên được lưu trong database của máy đang chạy. Mỗi máy có dữ liệu riêng.

Để dừng ứng dụng, chạy lệnh sau. Sau khi cập nhật code backend, dừng ứng dụng rồi chạy lại `Start-Demo.ps1` **không kèm `-NoBuild`** để biên dịch bản mới.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Stop-Demo.ps1
```

## Tài khoản cục bộ

| Tên đăng nhập | Vai trò | Công việc chính |
| --- | --- | --- |
| `quanly` | Quản lý | Thực đơn, kho, duyệt hủy, tổng quan và doanh thu |
| `banhang` | Bán hàng | Tạo đơn, gọi thêm, gửi bếp và xác nhận giao món |
| `thungan` | Thu ngân | Tạo đơn, thu tiền, xác nhận giao món và in hóa đơn nội bộ |
| `bep` | Bếp | Nhận món, bắt đầu chế biến và đánh dấu sẵn sàng |

Mật khẩu khởi tạo mặc định cho bốn tài khoản cục bộ là `Chicken@2026!`. Có thể đặt biến môi trường `DemoPassword` trước lần tạo database đầu tiên để chọn mật khẩu khác.

## Phát triển và kiểm thử

- `src/ChickenPos.Web/` chứa ứng dụng ASP.NET Core và mã truy cập SQL Server.
- `src/ChickenPos.Web/wwwroot/` chứa giao diện và ảnh thực đơn đi kèm.
- `scripts/` chứa các lệnh thiết lập, khởi động, dừng và kiểm thử.
- `tests/integration.py` chứa các ca kiểm thử tích hợp.

Để chạy kiểm thử, cài Python 3.10 trở lên rồi chạy lệnh bên dưới. Bộ kiểm thử chạy trên cổng 5081 với database riêng.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Run-Tests.ps1
```

Xem [hướng dẫn làm việc nhóm qua GitHub](docs/LAM_VIEC_NHOM_GITHUB.md) để tạo nhánh, gửi Pull Request và cập nhật code.
