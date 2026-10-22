import { http, HttpResponse } from 'msw'
import type { components } from '@/api/generated/schema'

type Product = components['schemas']['ProductDto']
type ProductInput = components['schemas']['SaveProductRequest']

/** Dữ liệu sản phẩm giả trong bộ nhớ; gọi resetProducts() trước mỗi test. */
export const productsDb: Product[] = []

/** Các query string API đã nhận (để test kiểm tra tham số gửi lên). */
export const productQueries: URLSearchParams[] = []

export function resetProducts(products: Product[] = []) {
  productsDb.splice(0, productsDb.length, ...products)
  productQueries.length = 0
}

export function makeProduct(overrides: Partial<Product> = {}): Product {
  const id = overrides.id ?? crypto.randomUUID()
  return {
    id,
    sku: `SKU-${id.slice(0, 6).toUpperCase()}`,
    name: 'Sản phẩm',
    description: null,
    price: 100_000,
    stock: 10,
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

const conflict = () =>
  HttpResponse.json({ status: 409, title: 'SKU đã được sử dụng.', code: 'Product.SkuTaken' }, { status: 409 })
const notFound = () => HttpResponse.json({ status: 404, title: 'Không tìm thấy sản phẩm.' }, { status: 404 })

export const catalogHandlers = [
  http.get('*/api/catalog/products', ({ request }) => {
    const url = new URL(request.url)
    productQueries.push(url.searchParams)
    const page = Number(url.searchParams.get('page') ?? 1)
    const pageSize = Number(url.searchParams.get('pageSize') ?? 20)
    const search = url.searchParams.get('search')?.toLowerCase()
    const isActive = url.searchParams.get('isActive')
    const sortBy = (url.searchParams.get('sortBy') ?? 'createdAt') as keyof Product
    const dir = url.searchParams.get('sortDir') === 'asc' ? 1 : -1

    const matches = productsDb
      .filter((p) => !search || p.sku.toLowerCase().includes(search) || p.name.toLowerCase().includes(search))
      .filter((p) => isActive === null || String(p.isActive) === isActive)
      .sort((a, b) => ((a[sortBy] ?? '') > (b[sortBy] ?? '') ? dir : (a[sortBy] ?? '') < (b[sortBy] ?? '') ? -dir : 0))

    return HttpResponse.json({
      items: matches.slice((page - 1) * pageSize, page * pageSize),
      page,
      pageSize,
      totalCount: matches.length,
    })
  }),

  http.post('*/api/catalog/products', async ({ request }) => {
    const body = (await request.json()) as ProductInput
    const sku = body.sku.toUpperCase()
    if (productsDb.some((p) => p.sku === sku)) return conflict()
    const product = makeProduct({ ...body, sku, createdAt: new Date().toISOString() })
    productsDb.push(product)
    return HttpResponse.json(product, { status: 201 })
  }),

  http.put('*/api/catalog/products/:id', async ({ params, request }) => {
    const product = productsDb.find((p) => p.id === params.id)
    if (!product) return notFound()
    const body = (await request.json()) as ProductInput
    const sku = body.sku.toUpperCase()
    if (productsDb.some((p) => p.sku === sku && p.id !== product.id)) return conflict()
    Object.assign(product, { ...body, sku })
    return HttpResponse.json(product)
  }),

  http.delete('*/api/catalog/products/:id', ({ params }) => {
    const index = productsDb.findIndex((p) => p.id === params.id)
    if (index < 0) return notFound()
    productsDb.splice(index, 1)
    return new HttpResponse(null, { status: 204 })
  }),
]
