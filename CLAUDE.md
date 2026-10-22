# CLAUDE.md

## Cấu trúc
- `openspec/` – đặc tả (specs) và change proposals. Dùng lệnh `/opsx:*`. Capability trùng tên module.
- `backend/` – ASP.NET Core .NET 10, modular monolith. Host: `src/Host/OpenSpec.Api`.
  Module: `src/Modules/<Module>/` gồm Domain, Application, Infrastructure, Api, Contracts.
- `frontend/` – React + TypeScript + Vite. Code theo module ở `src/modules/<module>`.
- `api/openapi/openapi.json` – contract REST, nguồn sinh client `frontend/src/api/generated`.
- `docs/adr` – quyết định kiến trúc. `deploy/docker` – SQL Server cho dev.

## Quy tắc
- Module chỉ tham chiếu `*.Contracts` của module khác (kiểm tra bởi `OpenSpec.ArchitectureTests`).
- Mỗi module có DbContext + schema SQL riêng; migration nằm trong `*.Infrastructure/Persistence/Migrations`.
- Version NuGet quản lý tập trung trong `backend/Directory.Packages.props` (không ghi Version trong .csproj).
- Không sửa tay `frontend/src/api/generated`; chạy `npm run gen:api`.

## Lệnh
- Backend: `dotnet build backend/OpenSpec.slnx`, `dotnet test backend/OpenSpec.slnx`
- Frontend (trong `frontend/`): `npm run dev`, `npm run test:run`, `npm run lint`, `npm run typecheck`
- OpenAPI: chạy API (Development), lưu `http://localhost:5056/openapi/v1.json` vào `api/openapi/openapi.json`, rồi `npm run gen:api`
- API docs (dev): `http://localhost:5056/scalar`
- DB: `docker compose -f deploy/docker/docker-compose.yml up -d`

## Test
- Unit: `backend/tests/Modules/<Module>/*.UnitTests` (xUnit)
- Integration API: `backend/tests/OpenSpec.Api.IntegrationTests` (WebApplicationFactory + Testcontainers)
- Component: `*.test.tsx` cạnh component (Vitest + Testing Library + MSW)
