export type ContactMethodType = 'Phone' | 'Email' | 'WeChat' | 'QQ' | 'Address' | 'Social' | 'Other'

export interface ContactMethod {
  id: string
  type: ContactMethodType
  label: string
  value: string
  isPrimary: boolean
}

export interface Contact {
  id: string
  name: string
  company: string | null
  notes: string | null
  isFavorite: boolean
  methods: ContactMethod[]
  createdAt: string
  updatedAt: string
}

export interface ContactMethodInput {
  type: ContactMethodType
  label?: string
  value: string
  isPrimary: boolean
}

export interface ContactPayload {
  name: string
  company?: string | null
  notes?: string | null
  isFavorite: boolean
  methods: ContactMethodInput[]
}

export interface ContactQuery {
  search?: string
  favorite?: boolean
  page?: number
  pageSize?: number
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface ImportResult {
  imported: number
  skipped: number
  errors: string[]
}

export interface MethodTypeMeta {
  value: ContactMethodType
  label: string
  tagType: 'primary' | 'success' | 'info' | 'warning' | 'danger'
  icon: string
}

export const METHOD_TYPES: MethodTypeMeta[] = [
  { value: 'Phone', label: '电话', tagType: 'success', icon: 'Phone' },
  { value: 'Email', label: '邮箱', tagType: 'primary', icon: 'Message' },
  { value: 'WeChat', label: '微信', tagType: 'success', icon: 'ChatDotRound' },
  { value: 'QQ', label: 'QQ', tagType: 'info', icon: 'ChatLineRound' },
  { value: 'Address', label: '地址', tagType: 'warning', icon: 'Location' },
  { value: 'Social', label: '社交', tagType: 'danger', icon: 'Share' },
  { value: 'Other', label: '其他', tagType: 'info', icon: 'More' }
]

export function methodMeta(type: ContactMethodType): MethodTypeMeta {
  return METHOD_TYPES.find((item) => item.value === type) ?? METHOD_TYPES[6]
}
