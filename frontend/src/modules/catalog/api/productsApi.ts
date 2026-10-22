import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/api/client'
import type { components } from '@/api/generated/schema'
import { toApiError } from '@/shared/api/apiError'
import type { ProductListParams } from '@/modules/catalog/hooks/useProductListParams'

export type Product = components['schemas']['ProductDto']
export type ProductInput = components['schemas']['SaveProductRequest']
export { ApiError } from '@/shared/api/apiError'

const productsKey = ['catalog', 'products'] as const

export function useProducts(params: ProductListParams) {
  return useQuery({
    queryKey: [...productsKey, 'list', params],
    queryFn: async ({ signal }) => {
      const { data, error, response } = await apiClient.GET('/api/catalog/products', {
        params: {
          query: {
            page: params.page,
            pageSize: params.pageSize,
            search: params.search || undefined,
            isActive: params.status === 'all' ? undefined : params.status === 'active',
            sortBy: params.sortBy,
            sortDir: params.sortDir,
          },
        },
        signal,
      })
      if (error || !data) throw toApiError(response, error)
      return data
    },
    placeholderData: keepPreviousData,
  })
}

export function useCreateProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (body: ProductInput) => {
      const { data, error, response } = await apiClient.POST('/api/catalog/products', { body })
      if (error || !data) throw toApiError(response, error)
      return data
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: productsKey }),
  })
}

export function useUpdateProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, ...body }: ProductInput & { id: string }) => {
      const { data, error, response } = await apiClient.PUT('/api/catalog/products/{id}', {
        params: { path: { id } },
        body,
      })
      if (error || !data) throw toApiError(response, error)
      return data
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: productsKey }),
  })
}

export function useDeleteProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      const { error, response } = await apiClient.DELETE('/api/catalog/products/{id}', { params: { path: { id } } })
      if (!response.ok) throw toApiError(response, error)
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: productsKey }),
  })
}
