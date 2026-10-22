import { screen } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { server } from '../../../../tests/mocks/server'
import { renderWithProviders } from '../../../../tests/setup/render'
import { UsersPage } from './UsersPage'

describe('UsersPage', () => {
  it('hiển thị dữ liệu từ API', async () => {
    renderWithProviders(<UsersPage />)

    expect(await screen.findByRole('heading', { name: 'Module users: users' })).toBeInTheDocument()
  })

  it('hiển thị lỗi khi API lỗi', async () => {
    server.use(http.get('*/api/users/ping', () => new HttpResponse(null, { status: 500 })))

    renderWithProviders(<UsersPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Không kết nối được backend.')
  })
})
