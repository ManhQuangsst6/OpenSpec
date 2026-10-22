import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/api/client'
import type { components } from '@/api/generated/schema'
import { toApiError } from '@/shared/api/apiError'

export type User = components['schemas']['UserDto']
export type UserInput = components['schemas']['CreateUserRequest']
export type UsersPage = components['schemas']['PagedResultOfUserDto']

export { ApiError } from '@/shared/api/apiError'

export interface UsersQuery {
  page: number
  pageSize: number
  search?: string
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
