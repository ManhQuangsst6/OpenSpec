# Tasks

## 1. Shared

- [x] 1.1 [backend/shared] Chuyển `PagedResult<T>` sang `Shared.Kernel/Results`, thêm `ValidationError` (lỗi theo trường); cập nhật Users dùng bản chung; verify `dotnet test` Users + ArchitectureTests vẫn xanh và schema OpenAPI `PagedResultOfUserDto` không đổi tên

## 2. Domain

- [x] 2.1 [backend/catalog] Tạo entity `Product` + `ProductErrors` với chuẩn hoá SKU và kiểm tra gom nhiều lỗi theo trường; verify unit test `ProductTests` (chuẩn hoá, từng quy tắc biên, nhiều lỗi cùng lúc, update giữ CreatedAt và đổi UpdatedAt)

## 3. Persistence

- [x] 3.1 [backend/catalog] Cấu hình EF (`DbSet`, độ dài, `decimal(18,2)`, unique SKU, index CreatedAt, check constraint) + `IDesignTimeDbContextFactory`; tạo migration `InitialCatalog` và áp dụng vào SQLEXPRESS; verify bảng `catalog.Products`, index và constraint tồn tại
- [ ] 3.2 [backend/catalog] Cài `ProductRepository` (tìm kiếm escape LIKE, lọc trạng thái, sắp xếp whitelist + `ThenBy(Id)`, ánh xạ lỗi unique → `SkuTaken`) và đăng ký DI; verify qua integration test ở nhóm 5

## 4. Application

- [x] 4.1 [backend/catalog] Thêm DTO, `ProductListQuery`, `ProductService`; verify unit test `ProductServiceTests` (SkuTaken khi tạo/cập nhật, NotFound, page/pageSize/sortBy/sortDir không hợp lệ → InvalidQuery)

## 5. API

- [x] 5.1 [backend/catalog] Map 5 endpoint `/api/catalog/products` với `TypedResults` + `ProducesProblem`, ánh xạ lỗi sang 400/404/409; verify gọi thật vào SQLEXPRESS (tạo, trùng SKU, sửa, sắp xếp, xoá)
- [ ] 5.2 [backend/catalog] `ApiFactory` migrate thêm `CatalogDbContext`; viết `ProductsEndpointsTests` (CRUD, 201 + Location, 409, 404, validation nhiều trường, phân trang, tìm/lọc, sắp xếp giá + ổn định, sortBy lạ → 400); verify CI xanh
- [x] 5.3 [backend/catalog] Xuất OpenAPI vào `api/openapi/openapi.json` và `npm run gen:api`; verify có path `/api/catalog/products` và `npm run typecheck` qua

## 6. Frontend

- [ ] 6.1 [frontend/catalog] Route `/products`, menu điều hướng trong `AppLayout`; verify chuyển trang Người dùng ↔ Sản phẩm trên trình duyệt và `npm run typecheck`
- [x] 6.2 [frontend/catalog] Hook API (`useProducts`, create/update/delete) và `useProductListParams` đồng bộ URL; verify `npm run typecheck`
- [x] 6.3 [frontend/catalog] `ProductsPage` + `ProductFormDialog` (bảng sắp xếp server, lọc trạng thái, tìm kiếm, định dạng VND, form, xoá có xác nhận) và handler MSW; verify component test `ProductsPage.test.tsx` (hiển thị/định dạng, sắp xếp cập nhật URL, khôi phục từ URL, lọc, tạo trùng SKU, xoá) qua `npm run test:run`

## 7. Kiểm tra tích hợp

- [ ] 7.1 Chạy API + frontend với SQLEXPRESS, thử đủ thao tác trên trang Sản phẩm; verify `dotnet test` (CI), `npm run lint`, `npm run test:run`, `openspec validate catalog-products --strict` đều qua

## Workflow follow-up

- Mở PR `feature/catalog-products` (base: `feature/users-management` cho tới khi PR #1 được merge).
- Archive change sau khi review xong (`/opsx:archive`).
