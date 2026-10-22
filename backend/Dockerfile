# syntax=docker/dockerfile:1
# Build context: thư mục backend/
#   docker build --target api      backend   → image API
#   docker build --target migrator backend   → image chạy EF migrations

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Host/OpenSpec.Api/OpenSpec.Api.csproj
RUN dotnet publish src/Host/OpenSpec.Api/OpenSpec.Api.csproj -c Release --no-restore -o /out/api /p:UseAppHost=false

# EF migration bundles: mỗi module một file chạy độc lập, không cần SDK lúc deploy
FROM build AS bundles
ARG EF_VERSION=10.0.13
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef --version $EF_VERSION
RUN for module in Users Catalog; do \
      project=src/Modules/$module/OpenSpec.Modules.$module.Infrastructure; \
      dotnet ef migrations bundle --project $project --startup-project $project \
        --context ${module}DbContext --configuration Release \
        --self-contained -r linux-x64 -o /out/bundles/$(echo $module | tr '[:upper:]' '[:lower:]')-efbundle || exit 1; \
    done

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS migrator
WORKDIR /app
COPY --from=bundles /out/bundles/ ./
COPY --chmod=755 deploy/migrate.sh ./migrate.sh
USER $APP_UID
# Cần biến môi trường ConnectionStrings__Default
ENTRYPOINT ["./migrate.sh"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /out/api ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "OpenSpec.Api.dll"]
