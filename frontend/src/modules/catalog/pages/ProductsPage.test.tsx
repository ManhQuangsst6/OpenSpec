import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { makeProduct, productQueries, productsDb, resetProducts } from '../../../../tests/mocks/catalogHandlers'
import { server } from '../../../../tests/mocks/server'
import { renderWithProviders } from '../../../../tests/setup/render'
import { ProductsPage } from './ProductsPage'

const ao = makeProduct({ sku: 'AO-THUN-01', name: 'Áo thun', price: 150_000, stock: 10, createdAt: '2026-01-03T00:00:00Z' })
const quan = makeProduct({ sku: 'QUAN-01', name: 'Quần jean', price: 90_000, stock: 0, isActive: false, createdAt: '2026-01-02T00:00:00Z' })
const mu = makeProduct({ sku: 'MU-01', name: 'Mũ lưỡi trai', price: 250_000, stock: 3, createdAt: '2026-01-01T00:00:00Z' })

// Ký tự khoảng trắng trong định dạng tiền tệ vi-VN có thể là NBSP
const normalize = (text: string | null) => (text ?? '').replace(/\s/g, ' ')
const skuColumn = () =>
  screen
    .getAllByRole('row')
    .slice(1)
    .map((row) => within(row).getAllByRole('cell')[0].textContent)

// antd gắn aria-labelledby="test-id" khi chạy test nên không truy vấn dialog theo tên được
async function findDialog(title: string) {
  const dialog = await screen.findByRole('dialog')
  expect(dialog).toHaveTextContent(title)
  return dialog
}

describe('ProductsPage', () => {
  beforeEach(() => resetProducts([ao, quan, mu].map((p) => ({ ...p }))))

  it('hiển thị sản phẩm với giá VND, tồn kho, trạng thái và tổng số', async () => {
    renderWithProviders(<ProductsPage />)

    const row = (await screen.findByRole('cell', { name: 'AO-THUN-01' })).closest('tr')!
    expect(normalize(row.textContent)).toContain('150.000 ₫')
    expect(within(row).getByText('Đang bán')).toBeInTheDocument()

    const quanRow = screen.getByRole('cell', { name: 'QUAN-01' }).closest('tr')!
    expect(within(quanRow).getByText('Hết hàng')).toBeInTheDocument()
    expect(within(quanRow).getByText('Ngừng bán')).toBeInTheDocument()

    expect(await screen.findByText('Tổng số: 3')).toBeInTheDocument()
    expect(skuColumn()).toEqual(['AO-THUN-01', 'QUAN-01', 'MU-01']) // mặc định: mới nhất trước
  })

  it('sắp xếp theo giá khi bấm tiêu đề cột và ghi lên URL', async () => {
    const user = userEvent.setup()
    const { router } = renderWithProviders(<ProductsPage />)
    await screen.findByRole('cell', { name: 'AO-THUN-01' })

    await user.click(screen.getByRole('columnheader', { name: /^Giá/ }))

    await waitFor(() => expect(skuColumn()).toEqual(['QUAN-01', 'AO-THUN-01', 'MU-01']))
    expect(router.state.location.search).toBe('?sortBy=price&sortDir=asc')
    expect(productQueries.at(-1)?.get('sortBy')).toBe('price')

    await user.click(screen.getByRole('columnheader', { name: /^Giá/ }))

    await waitFor(() => expect(skuColumn()).toEqual(['MU-01', 'AO-THUN-01', 'QUAN-01']))
    expect(router.state.location.search).toBe('?sortBy=price&sortDir=desc')
  })

  it('khôi phục trang, bộ lọc, từ khoá và sắp xếp từ URL', async () => {
    resetProducts(
      Array.from({ length: 25 }, (_, i) =>
        makeProduct({ sku: `AO-${String(i).padStart(2, '0')}`, name: `Áo thun ${i}`, isActive: false, price: i * 1000 }),
      ),
    )

    renderWithProviders(<ProductsPage />, {
      route: '/?page=2&pageSize=10&status=inactive&search=thun&sortBy=price&sortDir=asc',
    })

    expect(await screen.findByRole('cell', { name: 'AO-10' })).toBeInTheDocument()
    expect(skuColumn()[0]).toBe('AO-10')
    expect(screen.getByRole('textbox', { name: 'Tìm theo SKU hoặc tên' })).toHaveValue('thun')
    expect(screen.getByRole('radio', { name: 'Ngừng bán' })).toBeChecked()
    expect(screen.getByTitle('2')).toHaveClass('ant-pagination-item-active')

    const query = productQueries.at(-1)!
    expect(Object.fromEntries(query)).toEqual({
      page: '2',
      pageSize: '10',
      search: 'thun',
      isActive: 'false',
      sortBy: 'price',
      sortDir: 'asc',
    })
  })

  it('bỏ qua giá trị lạ trên URL và dùng mặc định', async () => {
    renderWithProviders(<ProductsPage />, { route: '/?page=abc&pageSize=7&sortBy=password&status=x' })

    await screen.findByRole('cell', { name: 'AO-THUN-01' })
    expect(Object.fromEntries(productQueries.at(-1)!)).toEqual({
      page: '1',
      pageSize: '20',
      sortBy: 'createdAt',
      sortDir: 'desc',
    })
  })

  it('lọc theo trạng thái và tìm kiếm', async () => {
    const user = userEvent.setup()
    const { router } = renderWithProviders(<ProductsPage />)
    await screen.findByRole('cell', { name: 'AO-THUN-01' })

    await user.click(screen.getByText('Ngừng bán', { selector: '.ant-segmented-item-label' }))
    await waitFor(() => expect(skuColumn()).toEqual(['QUAN-01']))
    expect(router.state.location.search).toBe('?status=inactive')

    await user.click(screen.getByText('Tất cả', { selector: '.ant-segmented-item-label' }))
    await user.type(screen.getByRole('textbox', { name: 'Tìm theo SKU hoặc tên' }), 'mũ')
    await waitFor(() => expect(skuColumn()).toEqual(['MU-01']))
    expect(new URLSearchParams(router.state.location.search).get('search')).toBe('mũ')
  })

  it('tạo sản phẩm mới', async () => {
    const user = userEvent.setup()
    renderWithProviders(<ProductsPage />)
    await screen.findByRole('cell', { name: 'AO-THUN-01' })

    await user.click(screen.getByRole('button', { name: /Thêm sản phẩm/ }))
    const dialog = await findDialog('Thêm sản phẩm')
    await user.type(within(dialog).getByLabelText('SKU'), 'giay-01')
    await user.type(within(dialog).getByLabelText('Tên sản phẩm'), 'Giày thể thao')
    const price = within(dialog).getByLabelText('Giá (₫)')
    await user.clear(price)
    await user.type(price, '500000')
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))

    expect(await screen.findByRole('cell', { name: 'GIAY-01' })).toBeInTheDocument()
    expect(productsDb.find((p) => p.sku === 'GIAY-01')).toMatchObject({ name: 'Giày thể thao', price: 500000, isActive: true })
  })

  it('giữ form mở và báo lỗi khi SKU đã được sử dụng', async () => {
    const user = userEvent.setup()
    renderWithProviders(<ProductsPage />)
    await screen.findByRole('cell', { name: 'AO-THUN-01' })

    await user.click(screen.getByRole('button', { name: /Thêm sản phẩm/ }))
    const dialog = await findDialog('Thêm sản phẩm')
    await user.type(within(dialog).getByLabelText('SKU'), 'ao-thun-01')
    await user.type(within(dialog).getByLabelText('Tên sản phẩm'), 'Trùng')
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))

    expect(await within(dialog).findByText('SKU đã được sử dụng.')).toBeInTheDocument()
    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(productsDb).toHaveLength(3)
  })

  it('hiển thị lỗi theo trường từ API', async () => {
    server.use(
      http.post('*/api/catalog/products', () =>
        HttpResponse.json(
          { status: 400, title: 'Dữ liệu sản phẩm không hợp lệ.', errors: { name: ['Tên bị máy chủ từ chối.'] } },
          { status: 400 },
        ),
      ),
    )
    const user = userEvent.setup()
    renderWithProviders(<ProductsPage />)
    await screen.findByRole('cell', { name: 'AO-THUN-01' })

    await user.click(screen.getByRole('button', { name: /Thêm sản phẩm/ }))
    const dialog = await findDialog('Thêm sản phẩm')
    await user.type(within(dialog).getByLabelText('SKU'), 'NEW-01')
    await user.type(within(dialog).getByLabelText('Tên sản phẩm'), 'Tên')
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))

    expect(await within(dialog).findByText('Tên bị máy chủ từ chối.')).toBeInTheDocument()
  })

  it('sửa sản phẩm', async () => {
    const user = userEvent.setup()
    renderWithProviders(<ProductsPage />)

    await user.click(await screen.findByRole('button', { name: 'Sửa QUAN-01' }))
    const dialog = await findDialog('Sửa sản phẩm')
    expect(within(dialog).getByLabelText('SKU')).toHaveValue('QUAN-01')
    await user.click(within(dialog).getByRole('switch'))
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))

    await waitFor(() => expect(productsDb.find((p) => p.sku === 'QUAN-01')?.isActive).toBe(true))
  })

  it('xoá sản phẩm sau khi xác nhận, không xoá khi huỷ', async () => {
    const user = userEvent.setup()
    renderWithProviders(<ProductsPage />)

    await user.click(await screen.findByRole('button', { name: 'Xoá MU-01' }))
    let confirm = await screen.findByRole('tooltip', { name: /Xoá sản phẩm MU-01\?/ })
    await user.click(within(confirm).getByRole('button', { name: 'Huỷ' }))
    expect(productsDb).toHaveLength(3)

    await user.click(screen.getByRole('button', { name: 'Xoá MU-01' }))
    confirm = await screen.findByRole('tooltip', { name: /Xoá sản phẩm MU-01\?/ })
    await user.click(within(confirm).getByRole('button', { name: 'Xoá' }))

    await waitFor(() => expect(screen.queryByRole('cell', { name: 'MU-01' })).not.toBeInTheDocument())
    expect(productsDb).toHaveLength(2)
  })

  it('hiển thị lỗi khi API lỗi', async () => {
    server.use(http.get('*/api/catalog/products', () => new HttpResponse(null, { status: 500 })))

    renderWithProviders(<ProductsPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Không tải được danh sách sản phẩm')
  })
})
