<script setup lang="ts">
import { computed, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Collection } from '@element-plus/icons-vue'
import ContactFormDialog from '../components/ContactFormDialog.vue'
import ContactTable from '../components/ContactTable.vue'
import ContactToolbar from '../components/ContactToolbar.vue'
import { useContacts } from '../composables/useContacts'
import * as contactsApi from '../api/contacts'
import { getErrorMessage } from '../api/client'
import type { Contact, ContactPayload } from '../types/contact'

const {
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
} = useContacts()

const formVisible = ref(false)
const editingContact = ref<Contact | null>(null)
const submitting = ref(false)

const favoriteOnPage = computed(() => items.value.filter((contact) => contact.isFavorite).length)

function openCreate(): void {
  editingContact.value = null
  formVisible.value = true
}

function openEdit(contact: Contact): void {
  editingContact.value = contact
  formVisible.value = true
}

async function handleSubmit(payload: ContactPayload): Promise<void> {
  submitting.value = true
  try {
    if (editingContact.value) {
      await contactsApi.updateContact(editingContact.value.id, payload)
      ElMessage.success('联系人已更新')
    } else {
      await contactsApi.createContact(payload)
      ElMessage.success('联系人已添加')
    }

    formVisible.value = false
    await load()
  } catch (error) {
    ElMessage.error(getErrorMessage(error))
  } finally {
    submitting.value = false
  }
}

async function handleDelete(contact: Contact): Promise<void> {
  try {
    await ElMessageBox.confirm(`确定删除「${contact.name}」吗？删除后不可恢复。`, '删除联系人', {
      confirmButtonText: '删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
  } catch {
    return
  }

  try {
    await remove(contact)
    ElMessage.success('已删除')
  } catch (error) {
    ElMessage.error(getErrorMessage(error))
  }
}

async function handleToggleFavorite(contact: Contact): Promise<void> {
  try {
    await toggleFavorite(contact)
  } catch (error) {
    ElMessage.error(getErrorMessage(error))
  }
}

async function handleImport(file: File): Promise<void> {
  try {
    const result = await importFile(file)
    const parts = [`导入 ${result.imported} 条`]
    if (result.skipped > 0) {
      parts.push(`跳过 ${result.skipped} 条`)
    }
    ElMessage.success(parts.join('，'))

    if (result.errors.length > 0) {
      ElMessage.warning(result.errors.slice(0, 3).join('；'))
    }
  } catch (error) {
    ElMessage.error(getErrorMessage(error))
  }
}

async function handleExport(): Promise<void> {
  try {
    await exportFile()
    ElMessage.success('Excel 已导出')
  } catch (error) {
    ElMessage.error(getErrorMessage(error))
  }
}
</script>

<template>
  <div class="app-shell">
    <header class="hero">
      <div class="hero__content">
        <div class="hero__badge">
          <el-icon><Collection /></el-icon>
          Extreme Programming
        </div>
        <h1>我的通讯录</h1>
        <p>收藏重要联系人，管理多种联系方式，并一键完成 Excel 导入导出。</p>
      </div>
      <div class="hero__stats">
        <div class="stat">
          <span class="stat__value">{{ total }}</span>
          <span class="stat__label">联系人</span>
        </div>
        <div class="stat">
          <span class="stat__value">{{ favoriteOnPage }}</span>
          <span class="stat__label">当前页收藏</span>
        </div>
      </div>
    </header>

    <main class="workspace">
      <ContactToolbar
        v-model:search="search"
        v-model:favoriteOnly="favoriteOnly"
        :loading="loading"
        @search="applySearch"
        @favorite-change="applyFavoriteFilter"
        @add="openCreate"
        @import="handleImport"
        @export="handleExport"
        @refresh="load"
      />

      <ContactTable
        :contacts="items"
        :loading="loading"
        @edit="openEdit"
        @delete="handleDelete"
        @toggle-favorite="handleToggleFavorite"
      />

      <div class="workspace__footer">
        <span class="workspace__hint">共 {{ total }} 位联系人，点击联系方式标签可复制</span>
        <el-pagination
          background
          layout="sizes, prev, pager, next"
          :total="total"
          :current-page="page"
          :page-size="pageSize"
          :page-sizes="[10, 20, 50]"
          @current-change="changePage"
          @size-change="changePageSize"
        />
      </div>
    </main>

    <ContactFormDialog
      v-model="formVisible"
      :contact="editingContact"
      :submitting="submitting"
      @submit="handleSubmit"
    />
  </div>
</template>
