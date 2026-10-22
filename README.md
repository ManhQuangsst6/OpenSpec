# OpenSpec

Modular monolith: React + TypeScript + Vite (frontend) và ASP.NET Core Web API .NET 10 (backend), SQL Server, REST + OpenAPI.

```
backend/                     ASP.NET Core .NET 10
  OpenSpec.slnx
  Directory.Build.props      Cấu hình chung (net10.0, nullable, warnings)
  src/
    Host/OpenSpec.Api/       Composition root: Program.cs, middleware, OpenAPI, đăng ký module
    Shared/
      OpenSpec.Shared.Kernel/          Base entity, Result, value objects
      OpenSpec.Shared.Infrastructure/  EF Core, messaging, OpenAPI helpers
      OpenSpec.Shared.Contracts/       Integration events dùng chung
    Modules/<Module>/
      *.Domain          Entities, domain logic (không phụ thuộc gì)
      *.Application     Use case: Commands / Queries / DTOs
      *.Infrastructure  DbContext riêng (schema SQL riêng), migrations
      *.Api             Minimal API endpoints của module
      *.Contracts       API public cho module khác gọi (chỉ phụ thuộc vào đây)
  tests/
    Modules/<Module>/*.UnitTests     Unit tests (xUnit)
    OpenSpec.Api.IntegrationTests    WebApplicationFactory + Testcontainers SQL Server
    OpenSpec.ArchitectureTests       Kiểm tra ranh giới module (NetArchTest)

frontend/                    React + TS + Vite
  src/
    app/          providers, router, App.tsx
    api/generated Client sinh từ OpenAPI (openapi-typescript)
    modules/<module>/  components, pages, hooks, api  (khớp module backend)
    shared/       components, hooks, lib, types dùng chung
  tests/          setup Vitest + MSW mocks; component test đặt cạnh component (*.test.tsx)

api/openapi/      openapi.json export từ backend → nguồn sinh client frontend
database/         scripts, seed data (schema quản lý bằng EF migrations theo module)
docs/             architecture, ADR
deploy/docker/    Dockerfile, docker-compose (SQL Server)
```

## Quy tắc module
- Mỗi module sở hữu schema SQL riêng (`users`, `catalog`, `orders`).
- Module chỉ tham chiếu `*.Contracts` của module khác — được kiểm tra bởi ArchitectureTests.

## Chạy
```
docker compose -f deploy/docker/docker-compose.yml up -d   # SQL Server
dotnet run --project backend/src/Host/OpenSpec.Api
cd frontend && npm install && npm run dev
```

## Tài liệu: openspec vs docs
- `openspec/specs/<module>/spec.md` – hành vi hệ thống ("hệ thống phải làm gì"); capability trùng tên module backend/frontend.
- `openspec/changes/` – đề xuất thay đổi đang thực hiện (`/opsx:propose`, `/opsx:apply`, `/opsx:archive`).
- `docs/adr`, `docs/architecture` – quyết định kỹ thuật ("vì sao làm vậy").
