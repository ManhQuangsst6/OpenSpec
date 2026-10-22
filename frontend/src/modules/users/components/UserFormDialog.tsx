import { useEffect, useRef, useState, type FormEvent } from 'react'
import { ApiError, useCreateUser, useUpdateUser, type User } from '@/modules/users/api/usersApi'

interface Props {
  /** Người dùng cần sửa; null khi tạo mới. */
  user: User | null
  onClose: () => void
}

const FULL_NAME_MAX = 200

export function UserFormDialog({ user, onClose }: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const [email, setEmail] = useState(user?.email ?? '')
  const [fullName, setFullName] = useState(user?.fullName ?? '')
  const [errors, setErrors] = useState<{ email?: string; fullName?: string; form?: string }>({})

  const createUser = useCreateUser()
  const updateUser = useUpdateUser()
  const isSaving = createUser.isPending || updateUser.isPending

  useEffect(() => {
    // jsdom chưa hỗ trợ showModal
    const dialog = dialogRef.current
    if (dialog && !dialog.open) {
      if (typeof dialog.showModal === 'function') dialog.showModal()
      else dialog.setAttribute('open', '')
    }
  }, [])

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()

    const clientErrors: typeof errors = {}
    if (!/^[^\s@]+@[^\s@]+$/.test(email.trim())) clientErrors.email = 'Email không hợp lệ.'
    if (!fullName.trim()) clientErrors.fullName = 'Vui lòng nhập họ tên.'
    else if (fullName.trim().length > FULL_NAME_MAX) clientErrors.fullName = `Họ tên tối đa ${FULL_NAME_MAX} ký tự.`
    setErrors(clientErrors)
    if (Object.keys(clientErrors).length > 0) return

    try {
      const body = { email: email.trim(), fullName: fullName.trim() }
      if (user) await updateUser.mutateAsync({ id: user.id, ...body })
      else await createUser.mutateAsync(body)
      onClose()
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) setErrors({ email: 'Email đã được sử dụng.' })
      else if (error instanceof ApiError && error.status === 400)
        setErrors({ email: error.fieldErrors.email?.[0], fullName: error.fieldErrors.fullName?.[0], form: error.message })
      else setErrors({ form: error instanceof Error ? error.message : 'Có lỗi xảy ra.' })
    }
  }

  const title = user ? 'Sửa người dùng' : 'Thêm người dùng'

  return (
    <dialog ref={dialogRef} aria-labelledby="user-form-title" onCancel={onClose}>
      <form onSubmit={handleSubmit} noValidate>
        <h2 id="user-form-title">{title}</h2>

        <label>
          Email
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            aria-invalid={!!errors.email}
            aria-describedby={errors.email ? 'user-email-error' : undefined}
          />
        </label>
        {errors.email && <p id="user-email-error" role="alert">{errors.email}</p>}

        <label>
          Họ tên
          <input
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            aria-invalid={!!errors.fullName}
            aria-describedby={errors.fullName ? 'user-name-error' : undefined}
          />
        </label>
        {errors.fullName && <p id="user-name-error" role="alert">{errors.fullName}</p>}

        {errors.form && !errors.email && !errors.fullName && <p role="alert">{errors.form}</p>}

        <div>
          <button type="button" onClick={onClose} disabled={isSaving}>
            Huỷ
          </button>
          <button type="submit" disabled={isSaving}>
            {isSaving ? 'Đang lưu...' : 'Lưu'}
          </button>
        </div>
      </form>
    </dialog>
  )
}
