# Proposal

## Why

Module Catalog mới chỉ có endpoint ping. Hệ thống cần danh mục sản phẩm để quản trị viên quản lý hàng hoá, và là nền cho module Orders sau này (đơn hàng tham chiếu sản phẩm, giá, tồn kho).

## What Changes

- Sản phẩm gồm: mã SKU (duy nhất), tên, mô tả (tuỳ chọn), giá bán (VND), số lượng tồn kho, trạng thái đang bán/ngừng bán, thời điểm tạo và cập nhật.
- API REST `/api/catalog/products`: liệt kê có phân trang, tìm kiếm theo SKU/tên, lọc theo trạng thái, **sắp xếp theo cột**; xem chi tiết; tạo; cập nhật; xoá.
- Kiểm tra dữ liệu: SKU đúng định dạng và không trùng (409); tên bắt buộc; giá và tồn kho không âm.
- Bảng `catalog.Products` qua migration đầu tiên của module Catalog.
- Trang **Sản phẩm** trên frontend (Ant Design): bảng có phân trang + sắp xếp theo cột + lọc trạng thái + tìm kiếm, form tạo/sửa, xác nhận xoá. Trạng thái trang (trang, sắp xếp, bộ lọc) lưu trên URL để F5 hoặc chia sẻ link không mất.
- Menu điều hướng giữa trang Người dùng và Sản phẩm.
- `PagedResult<T>` chuyển sang `Shared.Kernel` để Users và Catalog dùng chung (không đổi hành vi API của Users).

## Capabilities

### New Capabilities
- `catalog`: Quản lý danh mục sản phẩm — tạo, xem, tìm kiếm/lọc/sắp xếp có phân trang, cập nhật, xoá sản phẩm và quy tắc hợp lệ của SKU, giá, tồn kho.

### Modified Capabilities
<!-- Không có: hành vi của users không đổi. -->

## Impact

- **Module bị ảnh hưởng:** Catalog (Domain, Application, Infrastructure, Api). Users chỉ đổi chỗ đặt `PagedResult<T>` (refactor nội bộ). Shared.Kernel thêm `PagedResult<T>`.
- **Ngoài phạm vi:** ảnh sản phẩm, danh mục/nhóm sản phẩm, biến thể, nhiều mức giá/khuyến mãi, lịch sử nhập–xuất kho, đặt hàng, xác thực/phân quyền, Contracts cho Orders (làm khi Orders cần).
- **DB:** migration `InitialCatalog` trong `OpenSpec.Modules.Catalog.Infrastructure/Persistence/Migrations`.
- **API/Frontend:** thêm path mới vào `api/openapi/openapi.json`, sinh lại client; thêm route `/products`.
