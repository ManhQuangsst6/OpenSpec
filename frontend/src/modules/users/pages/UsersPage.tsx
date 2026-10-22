import { useEffect, useState } from 'react'
import { useDeleteUser, useUsers, type User } from '@/modules/users/api/usersApi'
import { UserFormDialog } from '@/modules/users/components/UserFormDialog'

const PAGE_SIZE = 20
const SEARCH_DEBOUNCE_MS = 300

const dateFormat = new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' })

export function UsersPage() {
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  // undefined: đóng form; null: tạo mới; User: sửa
  const [editing, setEditing] = useState<User | null | undefined>(undefined)

  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchInput.trim())
      setPage(1)
    }, SEARCH_DEBOUNCE_MS)
    return () => clearTimeout(timer)
  }, [searchInput])

  const { data, isPending, isError, error } = useUsers({ page, pageSize: PAGE_SIZE, search })
  const deleteUser = useDeleteUser()

  function handleDelete(user: User) {
    if (!window.confirm(`Xoá người dùng ${user.email}?`)) return
    deleteUser.mutate(user.id)
  }

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE)) : 1

  return (
    <main>
      <h1>Người dùng</h1>

      <div>
        <input
          type="search"
          aria-label="Tìm theo email hoặc họ tên"
          placeholder="Tìm theo email hoặc họ tên"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
        />
        <button type="button" onClick={() => setEditing(null)}>
          Thêm người dùng
        </button>
      </div>

      {deleteUser.isError && <p role="alert">Xoá thất bại: {deleteUser.error.message}</p>}

      {isPending ? (
        <p>Đang tải...</p>
      ) : isError ? (
        <p role="alert">Không tải được danh sách người dùng: {error.message}</p>
      ) : (
        <>
          <p>Tổng số: {data.totalCount}</p>
          <table>
            <thead>
              <tr>
                <th>Email</th>
                <th>Họ tên</th>
                <th>Ngày tạo</th>
                <th>
                  <span className="sr-only">Thao tác</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {data.items.length === 0 ? (
                <tr>
                  <td colSpan={4}>Không có người dùng nào.</td>
                </tr>
              ) : (
                data.items.map((user) => (
                  <tr key={user.id}>
                    <td>{user.email}</td>
                    <td>{user.fullName}</td>
                    <td>{dateFormat.format(new Date(user.createdAt))}</td>
                    <td>
                      <button type="button" onClick={() => setEditing(user)} aria-label={`Sửa ${user.email}`}>
                        Sửa
                      </button>
                      <button
                        type="button"
                        onClick={() => handleDelete(user)}
                        disabled={deleteUser.isPending}
                        aria-label={`Xoá ${user.email}`}
                      >
                        Xoá
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>

          <nav aria-label="Phân trang">
            <button type="button" onClick={() => setPage((p) => p - 1)} disabled={page <= 1}>
              Trước
            </button>
            <span>
              Trang {page}/{totalPages}
            </span>
            <button type="button" onClick={() => setPage((p) => p + 1)} disabled={page >= totalPages}>
              Sau
            </button>
          </nav>
        </>
      )}

      {editing !== undefined && (
        <UserFormDialog key={editing?.id ?? 'new'} user={editing} onClose={() => setEditing(undefined)} />
      )}
    </main>
  )
}
