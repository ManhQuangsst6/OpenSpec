# Proposal

## Why

Module Users hiện chỉ có entity `User` (chỉ có email) và endpoint `/api/users/ping`; chưa có cách nào để quản trị viên xem hoặc quản lý người dùng. Đây là tính năng đầu tiên chạy xuyên suốt từ DB → API → UI, dùng làm mẫu chuẩn cho các module khác.

## What Changes

- Bổ sung thông tin người dùng: email (duy nhất, chuẩn hoá chữ thường) và họ tên; lưu thời điểm tạo.
- API REST cho người dùng: liệt kê có phân trang + tìm kiếm theo email/họ tên, xem chi tiết, tạo, cập nhật, xoá.
- Kiểm tra dữ liệu: email đúng định dạng và không trùng; họ tên bắt buộc, tối đa 200 ký tự. Lỗi trả về dạng ProblemDetails.
- Tạo bảng `users.Users` qua EF Core migration đầu tiên của module Users.
- Cập nhật `api/openapi/openapi.json` và client sinh ra ở frontend.
- Trang Users trên frontend: bảng danh sách có tìm kiếm/phân trang, form tạo/sửa, xác nhận xoá. Thay thế màn hình "ping" hiện tại.
- Endpoint `/api/users/ping` được giữ nguyên.

## Capabilities

### New Capabilities
- `users`: Quản lý hồ sơ người dùng — tạo, xem, tìm kiếm, cập nhật và xoá người dùng, kèm quy tắc hợp lệ của email và họ tên.

### Modified Capabilities
<!-- Không có: openspec/specs chưa có capability nào. -->

## Impact

- **Module bị ảnh hưởng:** chỉ Users (Domain, Application, Infrastructure, Api). Contracts của Users chưa cần thêm gì vì chưa module nào khác dùng tới.
- **Ngoài phạm vi:** đăng nhập/xác thực và phân quyền (API chưa được bảo vệ), mật khẩu, vai trò, xoá mềm/khôi phục, import hàng loạt, thông báo sự kiện sang module khác (Orders...).
- **DB:** thêm migration đầu tiên trong `OpenSpec.Modules.Users.Infrastructure/Persistence/Migrations`; cần áp dụng migration ở môi trường dev (`.\SQLEXPRESS` qua user-secrets) và trong integration test.
- **API:** thêm các endpoint dưới `/api/users`; client frontend sinh lại bằng `npm run gen:api`.
- **Frontend:** `src/modules/users` (api, pages, components) và handler MSW dùng trong test.
