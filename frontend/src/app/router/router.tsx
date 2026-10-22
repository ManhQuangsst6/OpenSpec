import { createBrowserRouter } from 'react-router'
import { UsersPage } from '@/modules/users/pages/UsersPage'

export const router = createBrowserRouter([
  { path: '/', element: <UsersPage /> },
])
