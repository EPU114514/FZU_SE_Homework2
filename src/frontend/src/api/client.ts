import axios from 'axios'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  timeout: 20000
})

interface ProblemPayload {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const payload = error.response?.data as ProblemPayload | undefined
    if (payload?.errors) {
      const first = Object.values(payload.errors).flat().find(Boolean)
      if (first) {
        return first
      }
    }

    if (payload?.detail) {
      return payload.detail
    }

    if (payload?.title) {
      return payload.title
    }

    if (error.code === 'ECONNABORTED') {
      return '请求超时，请确认后端服务已启动'
    }

    return error.message
  }

  return error instanceof Error ? error.message : '未知错误'
}
