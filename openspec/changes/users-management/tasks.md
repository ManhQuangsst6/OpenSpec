# Tasks

## 1. Domain

- [x] 1.1 [backend/users] Mở rộng `User`: thêm `FullName`, `CreatedAt`, `Update(email, fullName)`, kiểm tra email bằng `MailAddress` (≤ 256) và họ tên (bắt buộc, ≤ 200), mã lỗi `User.InvalidEmail`/`User.InvalidFullName`/`User.EmailTaken`/`User.NotFound`; verify bằng unit test mới trong `UserTests` (chuẩn hoá, email/họ tên không hợp lệ, update) qua `dotnet test`

## 2. Persistence

- [x] 2.1 [backend/users] Thêm `DbSet<User>` và cấu hình entity (độ dài cột, unique index `Email`, index `CreatedAt`) cùng `IDesignTimeDbContextFactory`; verify `dotnet build` thành công
- [ ] 2.2 [backend/users] Tạo migration `InitialUsers` trong `Persistence/Migrations` và áp dụng vào `.\SQLEXPRESS`; verify bảng `users.Users` và index tồn tại sau `dotnet ef database update`
- [x] 2.3 [backend/users] Cài đặt `IUserRepository` ở Infrastructure (phân trang, tìm kiếm `LIKE`, ánh xạ lỗi unique 2601/2627 sang `User.EmailTaken`) và đăng ký DI; verify qua integration test ở nhóm 4

## 3. Application

- [x] 3.1 [backend/users] Thêm DTO (`UserDto`, `CreateUserRequest`, `UpdateUserRequest`, `PagedResult<T>`) và `UserService` (List/Get/Create/Update/Delete, kiểm tra trùng email, kiểm tra tham số phân trang, `TimeProvider`); verify bằng unit test `UserServiceTests` với repository giả (trùng email → EmailTaken, không tìm thấy → NotFound, page/pageSize sai → lỗi)

## 4. API

- [ ] 4.1 [backend/users] Map 5 endpoint trong `UsersModule` bằng `TypedResults`, ánh xạ mã lỗi sang 400/404/409 ProblemDetails; verify trên Scalar (`/scalar`) với SQLEXPRESS
- [x] 4.2 [backend/users] Nối `SqlServerFixture` vào `ApiFactory` (override `ConnectionStrings:Default`, chạy migration) và viết integration test `UsersEndpointsTests`: CRUD, 201 + Location, trùng email 409, 404, phân trang và tìm kiếm; verify `dotnet test` xanh trên CI
- [x] 4.3 [backend/users] Xuất `/openapi/v1.json` vào `api/openapi/openapi.json` và chạy `npm run gen:api`; verify `schema.ts` có các path `/api/users` mới và `npm run typecheck` qua

## 5. Frontend

- [x] 5.1 [frontend/users] Viết hook `useUsers`, `useCreateUser`, `useUpdateUser`, `useDeleteUser` trong `usersApi.ts`; verify `npm run typecheck`
- [x] 5.2 [frontend/users] Làm lại `UsersPage`: bảng, tìm kiếm debounce, phân trang, `UserFormDialog` tạo/sửa (hiện lỗi theo trường và lỗi 409), xoá có `confirm`; cập nhật handler MSW; verify component test `UsersPage.test.tsx` (danh sách, tạo trùng email, xoá có/không xác nhận) qua `npm run test:run`

## 6. Kiểm tra tích hợp

- [ ] 6.1 Chạy API + `npm run dev` với SQLEXPRESS, thao tác tạo/sửa/tìm/xoá trên UI; verify toàn bộ `dotnet test`, `npm run lint`, `npm run test:run`, `openspec validate users-management --strict` đều qua và CI xanh

## Workflow follow-up

- Archive change sau khi review xong (`/opsx:archive`).
