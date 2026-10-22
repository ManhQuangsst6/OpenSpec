import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { makeUser, resetUsers, usersDb } from '../../../../tests/mocks/handlers'
import { server } from '../../../../tests/mocks/server'
import { renderWithProviders } from '../../../../tests/setup/render'
import { UsersPage } from './UsersPage'

const alice = makeUser({ email: 'alice@example.com', fullName: 'Alice Nguyễn' })
const bob = makeUser({ email: 'bob@example.com', fullName: 'Bob Trần' })

describe('UsersPage', () => {
  beforeEach(() => resetUsers([alice, bob].map((u) => ({ ...u }))))
  afterEach(() => vi.restoreAllMocks())

  it('hiển thị danh sách người dùng và tổng số', async () => {
    renderWithProviders(<UsersPage />)

    expect(await screen.findByRole('cell', { name: 'alice@example.com' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'Bob Trần' })).toBeInTheDocument()
    expect(screen.getByText('Tổng số: 2')).toBeInTheDocument()
  })

  it('lọc theo từ khoá tìm kiếm', async () => {
    const user = userEvent.setup()
    renderWithProviders(<UsersPage />)
    await screen.findByRole('cell', { name: 'alice@example.com' })

    await user.type(screen.getByRole('searchbox', { name: 'Tìm theo email hoặc họ tên' }), 'bob')

    await waitFor(() => expect(screen.queryByRole('cell', { name: 'alice@example.com' })).not.toBeInTheDocument())
    expect(screen.getByRole('cell', { name: 'bob@example.com' })).toBeInTheDocument()
    expect(screen.getByText('Tổng số: 1')).toBeInTheDocument()
  })

  it('tạo người dùng mới và hiển thị trong danh sách', async () => {
    const user = userEvent.setup()
    renderWithProviders(<UsersPage />)
    await screen.findByRole('cell', { name: 'alice@example.com' })

    await user.click(screen.getByRole('button', { name: 'Thêm người dùng' }))
    const dialog = screen.getByRole('dialog', { name: 'Thêm người dùng' })
    await user.type(within(dialog).getByLabelText('Email'), 'carol@example.com')
    await user.type(within(dialog).getByLabelText('Họ tên'), 'Carol Lê')
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))

    expect(await screen.findByRole('cell', { name: 'carol@example.com' })).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('giữ form mở và báo lỗi khi email đã được sử dụng', async () => {
    const user = userEvent.setup()
    renderWithProviders(<UsersPage />)
    await screen.findByRole('cell', { name: 'alice@example.com' })

    await user.click(screen.getByRole('button', { name: 'Thêm người dùng' }))
    const dialog = screen.getByRole('dialog', { name: 'Thêm người dùng' })
    await user.type(within(dialog).getByLabelText('Email'), 'ALICE@example.com')
    await user.type(within(dialog).getByLabelText('Họ tên'), 'Alice khác')
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))

    expect(await within(dialog).findByRole('alert')).toHaveTextContent('Email đã được sử dụng.')
    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(usersDb).toHaveLength(2)
  })

  it('kiểm tra dữ liệu phía client trước khi gửi', async () => {
    const user = userEvent.setup()
    renderWithProviders(<UsersPage />)
    await screen.findByRole('cell', { name: 'alice@example.com' })

    await user.click(screen.getByRole('button', { name: 'Thêm người dùng' }))
    const dialog = screen.getByRole('dialog', { name: 'Thêm người dùng' })
    await user.type(within(dialog).getByLabelText('Email'), 'sai-email')
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))

    expect(within(dialog).getByText('Email không hợp lệ.')).toBeInTheDocument()
    expect(within(dialog).getByText('Vui lòng nhập họ tên.')).toBeInTheDocument()
  })

  it('sửa người dùng', async () => {
    const user = userEvent.setup()
    renderWithProviders(<UsersPage />)

    await user.click(await screen.findByRole('button', { name: 'Sửa bob@example.com' }))
    const dialog = screen.getByRole('dialog', { name: 'Sửa người dùng' })
    const name = within(dialog).getByLabelText('Họ tên')
    await user.clear(name)
    await user.type(name, 'Bob Mới')
    await user.click(within(dialog).getByRole('button', { name: 'Lưu' }))

    expect(await screen.findByRole('cell', { name: 'Bob Mới' })).toBeInTheDocument()
  })

  it('xoá người dùng sau khi xác nhận', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    const user = userEvent.setup()
    renderWithProviders(<UsersPage />)

    await user.click(await screen.findByRole('button', { name: 'Xoá alice@example.com' }))

    await waitFor(() => expect(screen.queryByRole('cell', { name: 'alice@example.com' })).not.toBeInTheDocument())
    expect(window.confirm).toHaveBeenCalledWith('Xoá người dùng alice@example.com?')
  })

  it('không xoá khi huỷ xác nhận', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    const user = userEvent.setup()
    renderWithProviders(<UsersPage />)

    await user.click(await screen.findByRole('button', { name: 'Xoá alice@example.com' }))

    expect(screen.getByRole('cell', { name: 'alice@example.com' })).toBeInTheDocument()
    expect(usersDb).toHaveLength(2)
  })

  it('hiển thị lỗi khi API lỗi', async () => {
    server.use(http.get('*/api/users', () => new HttpResponse(null, { status: 500 })))

    renderWithProviders(<UsersPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Không tải được danh sách người dùng')
  })
})
