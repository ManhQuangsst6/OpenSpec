import { http, HttpResponse } from 'msw'

export const handlers = [
  http.get('*/api/users/ping', () => HttpResponse.json('users')),
]
