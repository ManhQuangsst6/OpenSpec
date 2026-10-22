import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/api/client'
import type { components } from '@/api/generated/schema'

export type User = components['schemas']['UserDto']
export type UserInput = components['schemas']['CreateUserRequest']
export type UsersPage = components['schemas']['PagedResultOfUserDto']

export interface UsersQuery {
  page: number
  pageSize: number
  search?: string
}

type Problem = { status?: number | null; title?: string | null; errors?: Record<string, string[]> }

/** Lỗi từ API: giữ status và lỗi theo trường (ValidationProblem) để form hiển thị. */
export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, problem?: Problem) {
    super(problem?.title ?? 'Có lỗi xảy ra, vui lòng thử lại.')
    this.status = status
    this.fieldErrors = problem?.errors ?? {}
  }
}

function toApiError(response: Response, problem: unknown) {
  return new ApiError(response.status, (problem ?? undefined) as Problem | undefined)
}

const usersKey = ['users'] as const

export function useUsers(query: UsersQuery) {
  return useQuery({
    queryKey: [...usersKey, 'list', query],
    queryFn: async ({ signal }) => {
      const { data, error, response } = await apiClient.GET('/api/users', {
        params: { query: { page: query.page, pageSize: query.pageSize, search: query.search || undefined } },
        signal,
      })
      if (error || !data) throw toApiError(response, error)
      return data
    },
    placeholderData: keepPreviousData,
  })
}

export function useCreateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (body: UserInput) => {
      const { data, error, response } = await apiClient.POST('/api/users', { body })
      if (error || !data) throw toApiError(response, error)
      return data
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: usersKey }),
  })
}

export function useUpdateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, ...body }: UserInput & { id: string }) => {
      const { data, error, response } = await apiClient.PUT('/api/users/{id}', { params: { path: { id } }, body })
      if (error || !data) throw toApiError(response, error)
      return data
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: usersKey }),
  })
}

export function useDeleteUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      const { error, response } = await apiClient.DELETE('/api/users/{id}', { params: { path: { id } } })
      if (!response.ok) throw toApiError(response, error)
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: usersKey }),
  })
}
