import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router'

export const SORT_FIELDS = ['createdAt', 'sku', 'name', 'price', 'stock'] as const
export const PAGE_SIZES = [10, 20, 50, 100] as const

export type SortField = (typeof SORT_FIELDS)[number]
export type SortDir = 'asc' | 'desc'
export type StatusFilter = 'all' | 'active' | 'inactive'

export interface ProductListParams {
  page: number
  pageSize: number
  search: string
  status: StatusFilter
  sortBy: SortField
  sortDir: SortDir
}

export const DEFAULT_PARAMS: ProductListParams = {
  page: 1,
  pageSize: 20,
  search: '',
  status: 'all',
  sortBy: 'createdAt',
  sortDir: 'desc',
}

function parse(params: URLSearchParams): ProductListParams {
  const page = Number(params.get('page'))
  const pageSize = Number(params.get('pageSize'))
  const status = params.get('status')
  const sortBy = params.get('sortBy')
  const sortDir = params.get('sortDir')

  // Giá trị lạ trên URL rơi về mặc định thay vì gây lỗi API
  return {
    page: Number.isInteger(page) && page >= 1 ? page : DEFAULT_PARAMS.page,
    pageSize: (PAGE_SIZES as readonly number[]).includes(pageSize) ? pageSize : DEFAULT_PARAMS.pageSize,
    search: params.get('search') ?? DEFAULT_PARAMS.search,
    status: status === 'active' || status === 'inactive' ? status : DEFAULT_PARAMS.status,
    sortBy: (SORT_FIELDS as readonly string[]).includes(sortBy ?? '') ? (sortBy as SortField) : DEFAULT_PARAMS.sortBy,
    sortDir: sortDir === 'asc' || sortDir === 'desc' ? sortDir : DEFAULT_PARAMS.sortDir,
  }
}

/** Trạng thái danh sách sản phẩm lưu trên URL (F5/chia sẻ link không mất). Chỉ ghi các giá trị khác mặc định. */
export function useProductListParams() {
  const [searchParams, setSearchParams] = useSearchParams()
  const params = useMemo(() => parse(searchParams), [searchParams])

  const update = useCallback(
    (changes: Partial<ProductListParams>) => {
      setSearchParams(
        (current) => {
          const next = { ...parse(current), ...changes }
          const result = new URLSearchParams()
          for (const key of Object.keys(DEFAULT_PARAMS) as (keyof ProductListParams)[]) {
            // sortDir luôn đi cùng sortBy khác mặc định để link không phụ thuộc chiều mặc định
            const keep = key === 'sortDir' && next.sortBy !== DEFAULT_PARAMS.sortBy
            if (keep || next[key] !== DEFAULT_PARAMS[key]) result.set(key, String(next[key]))
          }
          return result
        },
        { replace: true },
      )
    },
    [setSearchParams],
  )

  return [params, update] as const
}
