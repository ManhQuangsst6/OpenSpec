type Problem = { title?: string | null; errors?: Record<string, string[]> }

/** Lỗi từ API: giữ status và lỗi theo trường (ValidationProblem) để form hiển thị. */
export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, problem?: Problem) {
    super(problem?.title ?? 'Có lỗi xảy ra, vui lòng thử lại.')
    this.status = status
    this.fieldErrors = problem?.errors ?? {}
  }
}

/** Tạo ApiError từ kết quả lỗi của openapi-fetch. */
export const toApiError = (response: Response, problem: unknown) =>
  new ApiError(response.status, (problem ?? undefined) as Problem | undefined)
