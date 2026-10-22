import { App, Form, Input, InputNumber, Modal, Switch } from 'antd'
import {
  ApiError,
  useCreateProduct,
  useUpdateProduct,
  type Product,
  type ProductInput,
} from '@/modules/catalog/api/productsApi'

interface Props {
  /** Sản phẩm cần sửa; null khi tạo mới. */
  product: Product | null
  onClose: () => void
}

const FIELDS = ['sku', 'name', 'description', 'price', 'stock'] as const
const MAX_PRICE = 1_000_000_000

const vndFormatter = (value: number | string | undefined) =>
  value === undefined || value === '' ? '' : `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, '.')
const vndParser = (value: string | undefined) => Number((value ?? '').replace(/\./g, '').replace(',', '.'))

export function ProductFormDialog({ product, onClose }: Props) {
  const [form] = Form.useForm<ProductInput>()
  const { message } = App.useApp()
  const createProduct = useCreateProduct()
  const updateProduct = useUpdateProduct()
  const isSaving = createProduct.isPending || updateProduct.isPending

  async function handleFinish(values: ProductInput) {
    const body: ProductInput = {
      ...values,
      sku: values.sku.trim(),
      name: values.name.trim(),
      description: values.description?.trim() || null,
    }
    try {
      if (product) await updateProduct.mutateAsync({ id: product.id, ...body })
      else await createProduct.mutateAsync(body)
      message.success(product ? 'Đã cập nhật sản phẩm.' : 'Đã thêm sản phẩm.')
      onClose()
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        form.setFields([{ name: 'sku', errors: ['SKU đã được sử dụng.'] }])
      } else if (error instanceof ApiError && error.status === 400) {
        form.setFields(
          FIELDS.filter((field) => error.fieldErrors[field]).map((field) => ({
            name: field,
            errors: error.fieldErrors[field],
          })),
        )
      } else {
        message.error(error instanceof Error ? error.message : 'Có lỗi xảy ra.')
      }
    }
  }

  return (
    <Modal
      open
      title={product ? 'Sửa sản phẩm' : 'Thêm sản phẩm'}
      okText="Lưu"
      cancelText="Huỷ"
      onOk={() => form.submit()}
      onCancel={onClose}
      confirmLoading={isSaving}
      destroyOnHidden
    >
      <Form<ProductInput>
        form={form}
        layout="vertical"
        requiredMark={false}
        onFinish={handleFinish}
        initialValues={{
          sku: product?.sku ?? '',
          name: product?.name ?? '',
          description: product?.description ?? '',
          price: product?.price ?? 0,
          stock: product?.stock ?? 0,
          isActive: product?.isActive ?? true,
        }}
      >
        <Form.Item
          name="sku"
          label="SKU"
          normalize={(value: string) => value.toUpperCase()}
          rules={[
            { required: true, whitespace: true, message: 'Vui lòng nhập SKU.' },
            {
              pattern: /^\s*[A-Za-z0-9-]{3,32}\s*$/,
              message: 'SKU gồm 3–32 ký tự chữ, số hoặc dấu gạch ngang.',
            },
          ]}
        >
          <Input placeholder="AO-THUN-01" autoFocus />
        </Form.Item>
        <Form.Item
          name="name"
          label="Tên sản phẩm"
          rules={[
            { required: true, whitespace: true, message: 'Vui lòng nhập tên sản phẩm.' },
            { max: 200, message: 'Tên tối đa 200 ký tự.' },
          ]}
        >
          <Input placeholder="Áo thun cotton" />
        </Form.Item>
        <Form.Item name="description" label="Mô tả" rules={[{ max: 2000, message: 'Mô tả tối đa 2000 ký tự.' }]}>
          <Input.TextArea rows={3} showCount maxLength={2000} />
        </Form.Item>
        <div style={{ display: 'flex', gap: 16 }}>
          <Form.Item
            name="price"
            label="Giá (₫)"
            style={{ flex: 1 }}
            rules={[{ required: true, type: 'number', min: 0, max: MAX_PRICE, message: 'Giá từ 0 đến 1.000.000.000.' }]}
          >
            <InputNumber<number>
              style={{ width: '100%' }}
              min={0}
              max={MAX_PRICE}
              step={1000}
              formatter={vndFormatter}
              parser={vndParser}
            />
          </Form.Item>
          <Form.Item
            name="stock"
            label="Tồn kho"
            style={{ flex: 1 }}
            rules={[{ required: true, type: 'integer', min: 0, message: 'Tồn kho là số nguyên không âm.' }]}
          >
            <InputNumber<number> style={{ width: '100%' }} min={0} precision={0} />
          </Form.Item>
        </div>
        <Form.Item name="isActive" label="Đang bán" valuePropName="checked">
          <Switch />
        </Form.Item>
      </Form>
    </Modal>
  )
}
