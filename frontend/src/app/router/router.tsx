import { createBrowserRouter } from 'react-router'
import { AppLayout } from '@/app/layout/AppLayout'
import { ProductsPage } from '@/modules/catalog/pages/ProductsPage'
import { UsersPage } from '@/modules/users/pages/UsersPage'

export const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      { path: '/', element: <UsersPage /> },
      { path: '/products', element: <ProductsPage /> },
    ],
  },
])
