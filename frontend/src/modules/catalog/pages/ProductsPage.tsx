import { DeleteOutlined, EditOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons'
import {
  Alert,
  App,
  Button,
  Card,
  Flex,
  Input,
  Popconfirm,
  Segmented,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd'
import type { TableProps } from 'antd'
import type { SortOrder } from 'antd/es/table/interface'
import { useEffect, useState } from 'react'
import { useDeleteProduct, useProducts, type Product } from '@/modules/catalog/api/productsApi'
import { ProductFormDialog } from '@/modules/catalog/components/ProductFormDialog'
import {
  DEFAULT_PARAMS,
  PAGE_SIZES,
  SORT_FIELDS,
  useProductListParams,
  type SortField,
  type StatusFilter,
} from '@/modules/catalog/hooks/useProductListParams'

const SEARCH_DEBOUNCE_MS = 300

const formatVnd =(value: number) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value)

const statusOptions: { label: string; value: StatusFilter }[] = [
  { label: 'Tất cả', value: 'all' },
  { label: 'Đang bán', value: 'active' },
  { label: 'Ngừng bán', value: 'inactive' },
]

export function ProductsPage() {
  const { message } = App.useApp()
  const [params, setParams] = useProductListParams()
  const [searchInput, setSearchInput] = useState(params.search)
  // undefined: đóng form; null: tạo mới; Product: sửa
  const [editing, setEditing] = useState<Product | null | undefined>(undefined)

  // Đồng bộ ô tìm kiếm → URL (debounce); khi tìm mới thì về trang 1
  useEffect(() => {
    const term = searchInput.trim()
    if (term === params.search) return
    const timer = setTimeout(() => setParams({ search: term, page: 1 }), SEARCH_DEBOUNCE_MS)
    return () => clearTimeout(timer)
  }, [searchInput, params.search, setParams])

  const { data, isPending, isFetching, isError, error } = useProducts(params)
  const deleteProduct = useDeleteProduct()

  async function handleDelete(product: Product) {
    try {
      await deleteProduct.mutateAsync(product.id)
      message.success(`Đã xoá ${product.sku}.`)
    } catch (err) {
      message.error(`Xoá thất bại: ${err instanceof Error ? err.message : ''}`)
    }
  }

  // Cột chỉ hiển thị mũi tên sắp xếp khi đang được sắp theo cột đó (mặc định createdAt giảm dần)
  const sortOrderOf = (field: SortField): SortOrder =>
    params.sortBy === field ? (params.sortDir === 'asc' ? 'ascend' : 'descend') : null

  const columns: TableProps<Product>['columns'] = [
    { title: 'SKU', dataIndex: 'sku', key: 'sku', sorter: true, sortOrder: sortOrderOf('sku'), width: 160 },
    {
      title: 'Tên sản phẩm',
      dataIndex: 'name',
      key: 'name',
      sorter: true,
      sortOrder: sortOrderOf('name'),
      render: (name: string, product) => (
        <Space orientation="vertical" size={0}>
          <Typography.Text>{name}</Typography.Text>
          {product.description && (
            <Typography.Text type="secondary" ellipsis style={{ maxWidth: 360, fontSize: 12 }}>
              {product.description}
            </Typography.Text>
          )}
        </Space>
      ),
    },
    {
      title: 'Giá',
      dataIndex: 'price',
      key: 'price',
      sorter: true,
      sortOrder: sortOrderOf('price'),
      align: 'right',
      width: 150,
      render: (price: number) => formatVnd(price),
    },
    {
      title: 'Tồn kho',
      dataIndex: 'stock',
      key: 'stock',
      sorter: true,
      sortOrder: sortOrderOf('stock'),
      align: 'right',
      width: 110,
      render: (stock: number) =>
        stock === 0 ? <Typography.Text type="danger">Hết hàng</Typography.Text> : stock.toLocaleString('vi-VN'),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'isActive',
      key: 'isActive',
      width: 120,
      render: (isActive: boolean) =>
        isActive ? <Tag color="green">Đang bán</Tag> : <Tag color="default">Ngừng bán</Tag>,
    },
    {
      title: 'Thao tác',
      key: 'actions',
      width: 110,
      align: 'right',
      render: (_, product) => (
        <Space>
          <Tooltip title="Sửa">
            <Button
              type="text"
              icon={<EditOutlined />}
              aria-label={`Sửa ${product.sku}`}
              onClick={() => setEditing(product)}
            />
          </Tooltip>
          <Popconfirm
            title="Xoá sản phẩm"
            description={`Xoá sản phẩm ${product.sku}?`}
            okText="Xoá"
            cancelText="Huỷ"
            okButtonProps={{ danger: true }}
            onConfirm={() => handleDelete(product)}
          >
            <Tooltip title="Xoá">
              <Button type="text" danger icon={<DeleteOutlined />} aria-label={`Xoá ${product.sku}`} />
            </Tooltip>
          </Popconfirm>
        </Space>
      ),
    },
  ]

  const handleTableChange: TableProps<Product>['onChange'] = (pagination, _filters, sorter, extra) => {
    if (extra.action === 'sort') {
      const s = Array.isArray(sorter) ? sorter[0] : sorter
      const field = String(s?.columnKey ?? '')
      const valid = s?.order && (SORT_FIELDS as readonly string[]).includes(field)
      // Bỏ sắp xếp (lần bấm thứ 3) → quay về mặc định: mới nhất trước
      setParams({
        sortBy: valid ? (field as SortField) : DEFAULT_PARAMS.sortBy,
        sortDir: valid ? (s.order === 'ascend' ? 'asc' : 'desc') : DEFAULT_PARAMS.sortDir,
        page: 1,
      })
      return
    }
    const pageSize = pagination.pageSize ?? params.pageSize
    setParams({ page: pageSize === params.pageSize ? (pagination.current ?? 1) : 1, pageSize })
  }

  return (
    <Card>
      <Flex justify="space-between" align="center" gap={16} wrap style={{ marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Sản phẩm
        </Typography.Title>
        <Space wrap>
          <Segmented<StatusFilter>
            aria-label="Lọc theo trạng thái"
            options={statusOptions}
            value={params.status}
            onChange={(status) => setParams({ status, page: 1 })}
          />
          <Input
            allowClear
            prefix={<SearchOutlined />}
            aria-label="Tìm theo SKU hoặc tên"
            placeholder="Tìm theo SKU hoặc tên"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            style={{ width: 240 }}
          />
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setEditing(null)}>
            Thêm sản phẩm
          </Button>
        </Space>
      </Flex>

      {isError ? (
        <Alert type="error" showIcon title={`Không tải được danh sách sản phẩm: ${error.message}`} />
      ) : (
        <Table<Product>
          rowKey="id"
          columns={columns}
          dataSource={data?.items}
          loading={isPending || isFetching}
          onChange={handleTableChange}
          locale={{ emptyText: 'Không có sản phẩm nào.' }}
          scroll={{ x: 800 }}
          pagination={{
            current: params.page,
            pageSize: params.pageSize,
            total: data?.totalCount ?? 0,
            showSizeChanger: true,
            pageSizeOptions: [...PAGE_SIZES],
            showTotal: (total) => `Tổng số: ${total}`,
          }}
        />
      )}

      {editing !== undefined && (
        <ProductFormDialog key={editing?.id ?? 'new'} product={editing} onClose={() => setEditing(undefined)} />
      )}
    </Card>
  )
}
