import { apiClient } from './client'
import type {
  Contact,
  ContactPayload,
  ContactQuery,
  ImportResult,
  PagedResult
} from '../types/contact'

export async function getContacts(query: ContactQuery): Promise<PagedResult<Contact>> {
  const { data } = await apiClient.get<PagedResult<Contact>>('/contacts', { params: query })
  return data
}

export async function getContact(id: string): Promise<Contact> {
  const { data } = await apiClient.get<Contact>(`/contacts/${id}`)
  return data
}

export async function createContact(payload: ContactPayload): Promise<Contact> {
  const { data } = await apiClient.post<Contact>('/contacts', payload)
  return data
}

export async function updateContact(id: string, payload: ContactPayload): Promise<Contact> {
  const { data } = await apiClient.put<Contact>(`/contacts/${id}`, payload)
  return data
}

export async function setContactFavorite(id: string, isFavorite: boolean): Promise<Contact> {
  const { data } = await apiClient.patch<Contact>(`/contacts/${id}/favorite`, { isFavorite })
  return data
}

export async function deleteContact(id: string): Promise<void> {
  await apiClient.delete(`/contacts/${id}`)
}

export async function exportContacts(query: ContactQuery): Promise<Blob> {
  const { data } = await apiClient.get<Blob>('/contacts/export', {
    params: query,
    responseType: 'blob'
  })
  return data
}

export async function importContacts(file: File, skipDuplicates = true): Promise<ImportResult> {
  const form = new FormData()
  form.append('file', file)

  const { data } = await apiClient.post<ImportResult>('/contacts/import', form, {
    params: { skipDuplicates }
  })
  return data
}
