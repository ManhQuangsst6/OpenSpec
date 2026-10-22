import { http, HttpResponse } from 'msw'
import type { components } from '@/api/generated/schema'

type User = components['schemas']['UserDto']
type UserInput = components['schemas']['CreateUserRequest']

/** Dữ liệu users giả trong bộ nhớ; gọi resetUsers() trước mỗi test cần trạng thái sạch. */
export const usersDb: User[] = []

export function resetUsers(users: User[] = []) {
  usersDb.splice(0, usersDb.length, ...users)
}

export function makeUser(overrides: Partial<User> = {}): User {
  const id = overrides.id ?? crypto.randomUUID()
  return {
    id,
    email: `${id.slice(0, 8)}@example.com`,
    fullName: 'Người dùng',
    createdAt: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

const conflict = () =>
  HttpResponse.json({ status: 409, title: 'Email đã được sử dụng.', code: 'User.EmailTaken' }, { status: 409 })
const notFound = () => HttpResponse.json({ status: 404, title: 'Không tìm thấy người dùng.' }, { status: 404 })

export const handlers = [
  http.get('*/api/users/ping', () => HttpResponse.json('users')),

  http.get('*/api/users', ({ request }) => {
    const url = new URL(request.url)
    const page = Number(url.searchParams.get('page') ?? 1)
    const pageSize = Number(url.searchParams.get('pageSize') ?? 20)
    const search = url.searchParams.get('search')?.toLowerCase()
    const matches = search
      ? usersDb.filter((u) => u.email.includes(search) || u.fullName.toLowerCase().includes(search))
      : usersDb
    return HttpResponse.json({
      items: matches.slice((page - 1) * pageSize, page * pageSize),
      page,
      pageSize,
      totalCount: matches.length,
    })
  }),

  http.post('*/api/users', async ({ request }) => {
    const body = (await request.json()) as UserInput
    const email = body.email.toLowerCase()
    if (usersDb.some((u) => u.email === email)) return conflict()
    const user = makeUser({ email, fullName: body.fullName })
    usersDb.unshift(user)
    return HttpResponse.json(user, { status: 201 })
  }),

  http.put('*/api/users/:id', async ({ params, request }) => {
    const user = usersDb.find((u) => u.id === params.id)
    if (!user) return notFound()
    const body = (await request.json()) as UserInput
    const email = body.email.toLowerCase()
    if (usersDb.some((u) => u.email === email && u.id !== user.id)) return conflict()
    Object.assign(user, { email, fullName: body.fullName })
    return HttpResponse.json(user)
  }),

  http.delete('*/api/users/:id', ({ params }) => {
    const index = usersDb.findIndex((u) => u.id === params.id)
    if (index < 0) return notFound()
    usersDb.splice(index, 1)
    return new HttpResponse(null, { status: 204 })
  }),
]
