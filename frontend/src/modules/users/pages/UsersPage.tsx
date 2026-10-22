import { useUsersPing } from '@/modules/users/api/usersApi'

export function UsersPage() {
  const { data, isPending, isError } = useUsersPing()

  if (isPending) return <p>Đang tải...</p>
  if (isError) return <p role="alert">Không kết nối được backend.</p>

  return <h1>Module users: {data}</h1>
}
