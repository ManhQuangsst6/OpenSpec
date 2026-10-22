import createClient from 'openapi-fetch'
import type { paths } from '@/api/generated/schema'

/** Client REST có kiểu, sinh từ api/openapi/openapi.json (`npm run gen:api`). */
export const apiClient = createClient<paths>({
  baseUrl: window.location.origin,
  // Gọi fetch lúc request (không giữ tham chiếu lúc khởi tạo) để MSW chặn được trong test
  fetch: (request) => globalThis.fetch(request),
})
