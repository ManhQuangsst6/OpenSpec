# Design

## Context

Module Users mới có `User.Create(email)` (kiểm tra email bằng `Contains('@')`), `UsersDbContext` schema `users` chưa có `DbSet`/migration, và endpoint ping dạng Minimal API trong `UsersModule`. Project có sẵn `Result<T>`/`Error` trong Shared.Kernel, `AddProblemDetails()` ở Host, OpenAPI tạo bằng `Microsoft.AspNetCore.OpenApi`. Frontend dùng `openapi-fetch` + TanStack Query, test bằng MSW. Integration test có `SqlServerFixture` (Testcontainers) nhưng `ApiFactory` chưa nối chuỗi kết nối vào.

## Goals / Non-Goals

**Goals:**
- Một lát cắt dọc chuẩn (Domain → Application → Infrastructure → Api → UI) làm mẫu cho module khác.
- Lỗi nghiệp vụ ánh xạ nhất quán sang HTTP status + ProblemDetails.

**Non-Goals:**
- Không thêm MediatR, FluentValidation hay repository generic; không dùng optimistic concurrency.
- Không áp dụng migration tự động khi khởi động ở môi trường production.

## Decisions

### Domain
- `User` thêm `FullName` (bắt buộc, ≤ 200), `CreatedAt` (`DateTimeOffset`, truyền vào từ `TimeProvider` để test được). Thêm `Update(email, fullName)` trả `Result`.
- Kiểm tra email bằng `System.Net.Mail.MailAddress.TryCreate` + yêu cầu địa chỉ khớp nguyên chuỗi, ≤ 256 ký tự. Thay `Contains('@')` vì quá lỏng.
- Mã lỗi: `User.InvalidEmail`, `User.InvalidFullName` (→ 400), `User.EmailTaken` (→ 409), `User.NotFound` (→ 404).

### Application
- Lớp `UserService` (không dùng mediator — module còn nhỏ) với các phương thức `ListAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, trả `Result<T>` với DTO.
- Application truy cập dữ liệu qua interface `IUserRepository` (EmailExists/Find/Add/Remove/List phân trang + SaveChanges), cài đặt ở Infrastructure bằng `UsersDbContext`. Giữ Application không phụ thuộc EF Core. Phương án bị loại: cho Application dùng thẳng `DbContext` — gọn hơn nhưng khó unit test.
- DTO request/response (`UserDto`, `CreateUserRequest`, `UpdateUserRequest`, `PagedResult<T>`) đặt trong Application; chưa đưa vào Contracts vì chưa module khác dùng.

### Persistence (schema DB)
- Bảng `users.Users`: `Id uniqueidentifier PK`, `Email nvarchar(256) NOT NULL`, `FullName nvarchar(200) NOT NULL`, `CreatedAt datetimeoffset NOT NULL`.
- Unique index `IX_Users_Email` — chốt chặn cuối cho race condition; `DbUpdateException` do vi phạm unique (SQL error 2601/2627) được ánh xạ sang `User.EmailTaken`.
- Index `IX_Users_CreatedAt` phục vụ sắp xếp.
- Tìm kiếm dùng `LIKE '%term%'` (collation mặc định không phân biệt hoa thường; email đã lưu chữ thường). Đủ cho quy mô hiện tại.
- Migration `InitialUsers` trong `Persistence/Migrations`; thêm `IDesignTimeDbContextFactory` để `dotnet ef` chạy được mà không cần Host.

### REST endpoints (`/api/users`, tag Users)
| Method | Path | Kết quả |
|---|---|---|
| GET | `/api/users?page=&pageSize=&search=` | 200 `PagedResult<UserDto>`, 400 |
| GET | `/api/users/{id:guid}` | 200 `UserDto`, 404 |
| POST | `/api/users` | 201 + Location, 400, 409 |
| PUT | `/api/users/{id:guid}` | 200 `UserDto`, 400, 404, 409 |
| DELETE | `/api/users/{id:guid}` | 204, 404 |

- Dùng `TypedResults` + `Results<...>` để OpenAPI sinh đủ status code. Lỗi validation trả `ValidationProblem` với key `email`/`fullName`; các lỗi khác trả `Problem` có `title` là thông điệp và `type`/extension `code` là mã lỗi.
- Endpoint ping giữ nguyên.

### Contract giữa module
- Không có. Users.Contracts không đổi; quy tắc chỉ tham chiếu Contracts vẫn được `OpenSpec.ArchitectureTests` kiểm tra.

### Frontend
- `modules/users/api/usersApi.ts`: hook `useUsers(params)`, `useCreateUser`, `useUpdateUser`, `useDeleteUser` (invalidate query `['users']`).
- Dùng **Ant Design 6** (locale `vi_VN`) làm thư viện UI cho toàn dự án; `AppProviders` bọc `ConfigProvider` + `App`, `AppLayout` có header/menu. Phương án ban đầu (HTML thuần, không thư viện) bị loại vì giao diện quá sơ sài; antd chọn thay Mantine/MUI/shadcn vì có sẵn Table phân trang, Modal Form, Popconfirm cho trang quản trị.
- `UsersPage`: `Table` (phân trang server, chọn kích thước trang), ô tìm kiếm debounce 300ms, nút "Thêm người dùng"; `UserFormDialog` là `Modal` + `Form` (kiểm tra phía client, lỗi từ API gắn vào trường); xoá qua `Popconfirm`; thông báo kết quả bằng `message`.
- Lỗi 409 hiển thị ngay trong form ("Email đã được sử dụng"); lỗi 400 map theo trường.

### Test
- Unit: `User` (validate, chuẩn hoá, update) và `UserService` với repository giả.
- Integration: `ApiFactory` nhận connection string từ `SqlServerFixture`, áp dụng migration lúc khởi tạo; test CRUD, trùng email, 404, phân trang/tìm kiếm.
- Component: `UsersPage` với MSW (danh sách, tạo trùng email, xoá có xác nhận).

## Risks / Trade-offs

- [Kiểm tra trùng email trước khi lưu có race condition] → unique index + ánh xạ lỗi DB sang 409.
- [`LIKE '%term%'` không dùng được index, chậm khi dữ liệu lớn] → chấp nhận hiện tại; chuyển full-text khi cần.
- [Integration test cần Docker, máy dev hiện chưa cài] → test vẫn chạy trên CI (GitHub runner có Docker); ở local chạy được unit + component test.
- [API chưa có xác thực, ai cũng gọi được CRUD] → ghi rõ ngoài phạm vi; làm ở change riêng trước khi deploy công khai.

## Migration Plan

1. Dev: `dotnet ef database update` cho `UsersDbContext` (chuỗi kết nối từ user-secrets, `.\SQLEXPRESS`).
2. Rollback: `dotnet ef database update 0 --context UsersDbContext` rồi xoá migration — bảng mới, không ảnh hưởng dữ liệu cũ.
