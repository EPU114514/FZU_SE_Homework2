import { computed, onMounted, ref } from 'vue'
import * as contactsApi from '../api/contacts'
import type { Contact, ContactQuery } from '../types/contact'
import { downloadBlob } from '../utils/download'

export function useContacts() {
  const items = ref<Contact[]>([])
  const total = ref(0)
  const page = ref(1)
  const pageSize = ref(10)
  const search = ref('')
  const favoriteOnly = ref(false)
  const loading = ref(false)

  const query = computed<ContactQuery>(() => ({
    search: search.value.trim() || undefined,
    favorite: favoriteOnly.value ? true : undefined,
    page: page.value,
    pageSize: pageSize.value
  }))

  async function load(): Promise<void> {
    loading.value = true
    try {
      const result = await contactsApi.getContacts(query.value)
      items.value = result.items
      total.value = result.totalCount

      if (result.items.length === 0 && page.value > 1) {
        page.value -= 1
        await load()
      }
    } finally {
      loading.value = false
    }
  }

  function applySearch(value: string): void {
    search.value = value
    page.value = 1
    void load()
  }

  function applyFavoriteFilter(value: boolean): void {
    favoriteOnly.value = value
    page.value = 1
    void load()
  }

  function changePage(value: number): void {
    page.value = value
    void load()
  }

  function changePageSize(value: number): void {
    pageSize.value = value
    page.value = 1
    void load()
  }

  async function toggleFavorite(contact: Contact): Promise<void> {
    const updated = await contactsApi.setContactFavorite(contact.id, !contact.isFavorite)
    const index = items.value.findIndex((item) => item.id === contact.id)
    if (index >= 0) {
      items.value[index] = updated
    }

    if (favoriteOnly.value && !updated.isFavorite) {
      items.value = items.value.filter((item) => item.id !== contact.id)
      total.value = Math.max(0, total.value - 1)
    }
  }

  async function remove(contact: Contact): Promise<void> {
    await contactsApi.deleteContact(contact.id)
    if (items.value.length === 1 && page.value > 1) {
      page.value -= 1
    }
    await load()
  }

  async function importFile(file: File) {
    const result = await contactsApi.importContacts(file)
    page.value = 1
    await load()
    return result
  }

  async function exportFile(): Promise<void> {
    const blob = await contactsApi.exportContacts({
      search: query.value.search,
      favorite: query.value.favorite
    })
    downloadBlob(blob, `contacts-${new Date().toISOString().slice(0, 10)}.xlsx`)
  }

  onMounted(() => {
    void load()
  })

  return {
    items,
    total,
    page,
    pageSize,
    search,
    favoriteOnly,
    loading,
    load,
    applySearch,
    applyFavoriteFilter,
    changePage,
    changePageSize,
    toggleFavorite,
    remove,
    importFile,
    exportFile
  }
}
