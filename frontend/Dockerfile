# syntax=docker/dockerfile:1
# Build context: thư mục frontend/. Nginx phục vụ SPA và chuyển /api/* sang API_URL.

FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.29-alpine AS web
# URL nội bộ của API (không có dấu / cuối), ví dụ http://api:8080
ENV API_URL=http://api:8080
COPY deploy/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
