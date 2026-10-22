import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import { App as AntApp, ConfigProvider } from 'antd'
import viVN from 'antd/locale/vi_VN'
import type { ReactElement } from 'react'
import { createMemoryRouter, RouterProvider } from 'react-router'

/**
 * Render với QueryClient, antd và router trong bộ nhớ.
 * `route` là URL ban đầu; đọc URL hiện tại qua `router.state.location`.
 */
export function renderWithProviders(ui: ReactElement, { route = '/' }: { route?: string } = {}) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  const router = createMemoryRouter([{ path: '*', element: ui }], { initialEntries: [route] })
  const result = render(
    <QueryClientProvider client={queryClient}>
      <ConfigProvider locale={viVN}>
        <AntApp>
          <RouterProvider router={router} />
        </AntApp>
      </ConfigProvider>
    </QueryClientProvider>,
  )
  return { ...result, router }
}
