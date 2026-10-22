import { DeleteOutlined, EditOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons'
import { Alert, App, Button, Card, Flex, Input, Popconfirm, Space, Table, Tooltip, Typography } from 'antd'
import type { TableProps } from 'antd'
import { useEffect, useState } from 'react'
import { useDeleteUser, useUsers, type User } from '@/modules/users/api/usersApi'
import { UserFormDialog } from '@/modules/users/components/UserFormDialog'

const DEFAULT_PAGE_SIZE = 20
const SEARCH_DEBOUNCE_MS = 300

const dateFormat = new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' })

export function UsersPage() {
  const { message } = App.useApp()
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE)
  // undefined: đóng form; null: tạo mới; User: sửa
  const [editing, setEditing] = useState<User | null | undefined>(undefined)

  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchInput.trim())
      setPage(1)
    }, SEARCH_DEBOUNCE_MS)
    return () => clearTimeout(timer)
  }, [searchInput])

  const { data, isPending, isFetching, isError, error } = useUsers({ page, pageSize, search })
  const deleteUser = useDeleteUser()

  async function handleDelete(user: User) {
    try {
      await deleteUser.mutateAsync(user.id)
      message.success(`Đã xoá ${user.email}.`)
    } catch (err) {
      message.error(`Xoá thất bại: ${err instanceof Error ? err.message : ''}`)
    }
  }

  const columns: TableProps<User>['columns'] = [
    { title: 'Email', dataIndex: 'email', key: 'email' },
    { title: 'Họ tên', dataIndex: 'fullName', key: 'fullName' },
    {
      title: 'Ngày tạo',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 180,
      render: (value: string) => dateFormat.format(new Date(value)),
    },
    {
      title: 'Thao tác',
      key: 'actions',
      width: 120,
      align: 'right',
      render: (_, user) => (
        <Space>
          <Tooltip title="Sửa">
            <Button type="text" icon={<EditOutlined />} aria-label={`Sửa ${user.email}`} onClick={() => setEditing(user)} />
          </Tooltip>
          <Popconfirm
            title="Xoá người dùng"
            description={`Xoá người dùng ${user.email}?`}
            okText="Xoá"
            cancelText="Huỷ"
            okButtonProps={{ danger: true }}
            onConfirm={() => handleDelete(user)}
          >
            <Tooltip title="Xoá">
              <Button type="text" danger icon={<DeleteOutlined />} aria-label={`Xoá ${user.email}`} />
            </Tooltip>
          </Popconfirm>
        </Space>
      ),
    },
  ]

  return (
    <Card>
      <Flex justify="space-between" align="center" gap={16} wrap style={{ marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Người dùng
        </Typography.Title>
        <Space wrap>
          <Input
            allowClear
            prefix={<SearchOutlined />}
            aria-label="Tìm theo email hoặc họ tên"
            placeholder="Tìm theo email hoặc họ tên"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            style={{ width: 280 }}
          />
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setEditing(null)}>
            Thêm người dùng
          </Button>
        </Space>
      </Flex>

      {isError ? (
        <Alert type="error" showIcon title={`Không tải được danh sách người dùng: ${error.message}`} />
      ) : (
        <Table<User>
          rowKey="id"
          columns={columns}
          dataSource={data?.items}
          loading={isPending || isFetching}
          locale={{ emptyText: 'Không có người dùng nào.' }}
          pagination={{
            current: page,
            pageSize,
            total: data?.totalCount ?? 0,
            showSizeChanger: true,
            pageSizeOptions: [10, 20, 50, 100],
            showTotal: (total) => `Tổng số: ${total}`,
            onChange: (nextPage, nextSize) => {
              setPage(nextSize === pageSize ? nextPage : 1)
              setPageSize(nextSize)
            },
          }}
        />
      )}

      {editing !== undefined && (
        <UserFormDialog key={editing?.id ?? 'new'} user={editing} onClose={() => setEditing(undefined)} />
      )}
    </Card>
  )
}
