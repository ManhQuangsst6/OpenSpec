# Design

## Context

Đi theo mẫu đã có ở module Users (change `users-management`): entity giàu hành vi trả `Result`, `*Service` ở Application + interface repository cài ở Infrastructure, Minimal API `TypedResults`, migration riêng theo schema, UI Ant Design 6. Catalog hiện chỉ có `CatalogDbContext` (schema `catalog`) và endpoint ping.

## Goals / Non-Goals

**Goals:**
- Lát cắt dọc cho sản phẩm, nhất quán với Users để module sau copy được.
- Phân trang + sắp xếp phía server an toàn (không ghép chuỗi SQL từ input).

**Non-Goals:**
- Không làm sắp xếp nhiều cột; không cung cấp Contracts cho Orders ở change này.

## Decisions

### Domain
- `Product`: `Sku` (3–32, `^[A-Z0-9-]+$` sau khi trim + upper), `Name` (≤ 200), `Description` (≤ 2000, rỗng → null), `Price` (`decimal`, 0..1.000.000.000, tối đa 2 chữ số thập phân), `Stock` (`int` ≥ 0), `IsActive`, `CreatedAt`, `UpdatedAt` (từ `TimeProvider`).
- Domain gom **mọi** lỗi trường trong một lần kiểm tra (`Result<Product>` với lỗi `Product.Validation` chứa danh sách lỗi theo trường) để API trả đủ lỗi cùng lúc — khác Users (dừng ở lỗi đầu) vì form sản phẩm nhiều trường hơn. Thêm `ValidationError : Error` vào Shared.Kernel mang `IReadOnlyDictionary<string,string[]>`.
- Lỗi khác: `Product.SkuTaken` (409), `Product.NotFound` (404), `Product.InvalidQuery` (400).

### Application
- `ProductService` (List/Get/Create/Update/Delete) + `IProductRepository`, giống Users.
- `ProductListQuery(page, pageSize, search, isActive?, sortBy, sortDir)`; `sortBy` ∈ {`sku`,`name`,`price`,`stock`,`createdAt`}, `sortDir` ∈ {`asc`,`desc`} — kiểm tra bằng whitelist, giá trị khác → `Product.InvalidQuery`.
- `PagedResult<T>` chuyển sang `OpenSpec.Shared.Kernel.Results` (Users đổi `using`, shape JSON giữ nguyên).

### Persistence (schema DB)
- Bảng `catalog.Products`: `Id uniqueidentifier PK`, `Sku nvarchar(32)`, `Name nvarchar(200)`, `Description nvarchar(2000) NULL`, `Price decimal(18,2)`, `Stock int`, `IsActive bit`, `CreatedAt`, `UpdatedAt datetimeoffset`.
- Unique index `IX_Products_Sku`; index `IX_Products_CreatedAt`. Check constraint `Price >= 0`, `Stock >= 0` làm chốt chặn cuối.
- Sắp xếp: ánh xạ whitelist sang biểu thức LINQ (`switch` trả `IOrderedQueryable`), luôn `ThenBy(Id)` để thứ tự ổn định giữa các trang. Phương án bị loại: `System.Linq.Dynamic` — thêm dependency và rủi ro nếu whitelist hở.
- Tìm kiếm `LIKE` có escape như Users. Vi phạm unique (2601/2627) → `Product.SkuTaken`.

### REST endpoints (`/api/catalog/products`, tag Catalog)
| Method | Path | Kết quả |
|---|---|---|
| GET | `?page&pageSize&search&isActive&sortBy&sortDir` | 200 `PagedResult<ProductDto>`, 400 |
| GET | `/{id:guid}` | 200, 404 |
| POST | `/` | 201 + Location, 400, 409 |
| PUT | `/{id:guid}` | 200, 400, 404, 409 |
| DELETE | `/{id:guid}` | 204, 404 |

`sortBy`/`sortDir` khai báo là chuỗi trong OpenAPI (giá trị hợp lệ ghi ở mô tả). Validation trả `ValidationProblem` với key trường camelCase.

### Contract giữa module
- Không có; Catalog.Contracts chưa thay đổi.

### Frontend
- Thêm route `/products`; `AppLayout` dùng `Menu` điều hướng theo `useLocation`/`useNavigate`.
- `useProductListParams()` đọc/ghi trạng thái danh sách bằng `useSearchParams` (page, pageSize, search, status, sortBy, sortDir); giá trị lạ trên URL rơi về mặc định.
- `ProductsPage`: `Table` với `sorter` server-side (`sortOrder` điều khiển từ URL), `Segmented` lọc trạng thái (Tất cả/Đang bán/Ngừng bán), ô tìm kiếm debounce, `Tag` trạng thái, giá format `Intl.NumberFormat('vi-VN', {style:'currency', currency:'VND'})`, tồn kho = 0 hiện màu cảnh báo.
- `ProductFormDialog`: `Modal` + `Form` (`InputNumber` cho giá/tồn kho, `Switch` trạng thái, `TextArea` mô tả); lỗi 400 theo trường và 409 gắn vào `sku`.

### Test
- Unit: `Product` (chuẩn hoá SKU, từng quy tắc, gom nhiều lỗi), `ProductService` với repo giả (SkuTaken, NotFound, query không hợp lệ).
- Integration: `ApiFactory` migrate thêm `CatalogDbContext`; CRUD, trùng SKU, 404, validation nhiều trường, phân trang, tìm/lọc, sắp xếp theo giá + ổn định giữa trang, sortBy lạ → 400.
- Component: `ProductsPage` với MSW — hiển thị/định dạng, sắp xếp bằng tiêu đề cột cập nhật URL, khôi phục từ URL, lọc trạng thái, tạo trùng SKU, xoá có xác nhận.

## Risks / Trade-offs

- [Chuyển `PagedResult<T>` có thể đổi tên schema OpenAPI của Users] → kiểm tra `PagedResultOfUserDto` vẫn giữ tên sau khi sinh lại; tên schema dựa trên tên type nên không đổi.
- [Domain gom nhiều lỗi khác cách Users] → chấp nhận; có thể đồng bộ Users sau.
- [`decimal(18,2)` với VND] → đủ dư; VND hiển thị không phần lẻ nhưng vẫn cho phép 2 chữ số để dùng chung cho tiền tệ khác sau này.

## Migration Plan

1. `dotnet ef database update` cho `CatalogDbContext` vào `.\SQLEXPRESS` (dbcreator đã có).
2. Rollback: `dotnet ef database update 0 --context CatalogDbContext` — bảng mới, không ảnh hưởng dữ liệu khác.
