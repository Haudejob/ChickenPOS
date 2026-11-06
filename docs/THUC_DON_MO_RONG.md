# Thực đơn mở rộng

Ứng dụng tự áp dụng cập nhật `menu-four-dishes-20261107` khi khởi động. Sau khi pull, dừng server bằng `scripts/Stop-Demo.ps1`, rồi chạy `scripts/Start-Demo.ps1` để build và cập nhật database. Không dùng `-NoBuild` cho lần cập nhật đầu.

## Giá và định lượng mỗi phần

| Món | Giá | Nguyên liệu tiêu hao |
|---|---:|---|
| Gà sốt phô mai | 49.000đ | Thịt gà 0,250 kg; bột chiên 0,040 kg; dầu chiên 0,020 lít; sốt phô mai 0,050 kg |
| Gà sốt tỏi cay | 49.000đ | Thịt gà 0,250 kg; bột chiên 0,040 kg; dầu chiên 0,020 lít; sốt tỏi cay 0,050 kg |
| Khoai tây lắc phô mai | 35.000đ | Khoai tây 0,200 kg; dầu chiên 0,015 lít; bột phô mai 0,015 kg |
| Trà chanh | 22.000đ | Trà khô 0,005 kg; chanh tươi 0,040 kg; đường 0,025 kg; đá viên 0,150 kg |

Định lượng là quy ước ban đầu của project, cần cân chỉnh theo khẩu phần thực tế. Dầu là lượng tiêu hao phân bổ mỗi phần, không phải toàn bộ dầu trong chảo. Sốt tỏi cay và sốt phô mai được quản lý như nguyên liệu pha sẵn; không trừ thêm thành phần của sốt. Bột phô mai và sốt phô mai là hai nguyên liệu khác nhau.

## Nguyên liệu bổ sung

| Nguyên liệu | Đơn vị | Tồn đầu khởi tạo | Ngưỡng cảnh báo |
|---|---|---:|---:|
| Bột chiên | kg | 3 | 0,5 |
| Dầu chiên | lít | 5 | 1 |
| Sốt phô mai | kg | 1,5 | 0,3 |
| Sốt tỏi cay | kg | 1,5 | 0,3 |
| Bột phô mai | kg | 0,5 | 0,1 |
| Trà khô | kg | 0,3 | 0,05 |
| Chanh tươi | kg | 2 | 0,4 |
| Đường | kg | 2 | 0,4 |
| Đá viên | kg | 10 | 2 |

Đây là số lượng khởi tạo cho dữ liệu project, không phải kết quả kiểm kê thực tế. Thịt gà và khoai tây dùng lại tồn hiện có. Công thức sáu món cũ được giữ nguyên.

Mỗi nguyên liệu mới có giao dịch `Opening` tương ứng. Tồn hiện có không bị đặt lại. Món và nguyên liệu được tìm theo tên (nguyên liệu kèm đơn vị), không dựa vào ID cố định. Đường dẫn ảnh mới được lưu trên món. Mốc cập nhật được ghi cùng giao dịch với món, công thức và tồn đầu; khởi động lại không thêm trùng hoặc cộng tồn lần nữa. Đơn hàng cũ giữ nguyên tên và giá đã chụp.

## Kiểm tra

`scripts/Run-Tests.ps1` chạy trên database test riêng. TC23 kiểm tra đủ bốn món, giá, file ảnh, chưa trừ kho lúc tạo đơn, trừ chính xác công thức khi bắt đầu chế biến và không trừ lặp khi bấm bắt đầu lần hai. Đã kiểm tra thêm khởi động lại không thay đổi danh mục, tồn kho hoặc lịch sử kho trên database hiện có.
