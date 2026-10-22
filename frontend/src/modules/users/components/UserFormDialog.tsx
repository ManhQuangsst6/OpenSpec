import { App, Form, Input, Modal } from 'antd'
import { ApiError, useCreateUser, useUpdateUser, type User, type UserInput } from '@/modules/users/api/usersApi'

interface Props {
  /** Người dùng cần sửa; null khi tạo mới. */
  user: User | null
  onClose: () => void
}

const FULL_NAME_MAX = 200

export function UserFormDialog({ user, onClose }: Props) {
  const [form] = Form.useForm<UserInput>()
  const { message } = App.useApp()
  const createUser = useCreateUser()
  const updateUser = useUpdateUser()
  const isSaving = createUser.isPending || updateUser.isPending

  async function handleFinish(values: UserInput) {
    const body = { email: values.email.trim(), fullName: values.fullName.trim() }
    try {
      if (user) await updateUser.mutateAsync({ id: user.id, ...body })
      else await createUser.mutateAsync(body)
      message.success(user ? 'Đã cập nhật người dùng.' : 'Đã thêm người dùng.')
      onClose()
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        form.setFields([{ name: 'email', errors: ['Email đã được sử dụng.'] }])
      } else if (error instanceof ApiError && error.status === 400) {
        form.setFields(
          (['email', 'fullName'] as const)
            .filter((field) => error.fieldErrors[field])
            .map((field) => ({ name: field, errors: error.fieldErrors[field] })),
        )
      } else {
        message.error(error instanceof Error ? error.message : 'Có lỗi xảy ra.')
      }
    }
  }

  return (
    <Modal
      open
      title={user ? 'Sửa người dùng' : 'Thêm người dùng'}
      okText="Lưu"
      cancelText="Huỷ"
      onOk={() => form.submit()}
      onCancel={onClose}
      confirmLoading={isSaving}
      destroyOnHidden
    >
      <Form
        form={form}
        layout="vertical"
        initialValues={{ email: user?.email ?? '', fullName: user?.fullName ?? '' }}
        onFinish={handleFinish}
        requiredMark={false}
      >
        <Form.Item
          name="email"
          label="Email"
          rules={[
            { required: true, whitespace: true, message: 'Vui lòng nhập email.' },
            { type: 'email', message: 'Email không hợp lệ.' },
          ]}
          normalize={(value: string) => value.trimStart()}
        >
          <Input placeholder="ten@congty.com" autoFocus />
        </Form.Item>
        <Form.Item
          name="fullName"
          label="Họ tên"
          rules={[
            { required: true, whitespace: true, message: 'Vui lòng nhập họ tên.' },
            { max: FULL_NAME_MAX, message: `Họ tên tối đa ${FULL_NAME_MAX} ký tự.` },
          ]}
        >
          <Input placeholder="Nguyễn Văn A" />
        </Form.Item>
      </Form>
    </Modal>
  )
}
