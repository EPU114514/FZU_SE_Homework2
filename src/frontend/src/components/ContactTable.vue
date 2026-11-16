<script setup lang="ts">
import { ElMessage } from 'element-plus'
import {
  ChatDotRound,
  ChatLineRound,
  Delete,
  Edit,
  Location,
  Message,
  More,
  Phone,
  Share,
  Star,
  StarFilled
} from '@element-plus/icons-vue'
import type { Component } from 'vue'
import type { Contact, ContactMethodType } from '../types/contact'
import { methodMeta } from '../types/contact'

defineProps<{
  contacts: Contact[]
  loading: boolean
}>()

const emit = defineEmits<{
  (event: 'edit', contact: Contact): void
  (event: 'delete', contact: Contact): void
  (event: 'toggle-favorite', contact: Contact): void
}>()

const iconMap: Record<string, Component> = {
  Phone,
  Message,
  ChatDotRound,
  ChatLineRound,
  Location,
  Share,
  More
}

function methodIcon(type: ContactMethodType): Component {
  return iconMap[methodMeta(type).icon] ?? More
}

function formatDate(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return '—'
  }

  return date.toLocaleString('zh-CN', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

function rowClassName({ row }: { row: Contact }): string {
  return row.isFavorite ? 'favorite-row' : ''
}

async function copyValue(value: string): Promise<void> {
  try {
    await navigator.clipboard.writeText(value)
    ElMessage.success('已复制')
  } catch {
    const textarea = document.createElement('textarea')
    textarea.value = value
    document.body.appendChild(textarea)
    textarea.select()
    document.execCommand('copy')
    textarea.remove()
    ElMessage.success('已复制')
  }
}
</script>

<template>
  <div class="table-card">
    <el-table
      :data="contacts"
      v-loading="loading"
      :row-class-name="rowClassName"
      empty-text="暂无联系人"
    >
      <el-table-column width="56" align="center">
        <template #default="{ row }">
          <el-tooltip :content="row.isFavorite ? '取消收藏' : '设为收藏'" placement="top">
            <el-button
              link
              class="star-button"
              :class="{ 'is-active': row.isFavorite }"
              @click="emit('toggle-favorite', row)"
            >
              <el-icon :size="20">
                <StarFilled v-if="row.isFavorite" />
                <Star v-else />
              </el-icon>
            </el-button>
          </el-tooltip>
        </template>
      </el-table-column>

      <el-table-column label="联系人" min-width="180">
        <template #default="{ row }">
          <div class="contact-name">{{ row.name }}</div>
          <div class="contact-company">{{ row.company || '未填写公司' }}</div>
        </template>
      </el-table-column>

      <el-table-column label="联系方式" min-width="330">
        <template #default="{ row }">
          <div class="method-list">
            <el-tag
              v-for="method in row.methods.slice(0, 4)"
              :key="method.id"
              :type="methodMeta(method.type).tagType"
              :effect="method.isPrimary ? 'dark' : 'light'"
              class="method-tag"
              @click="copyValue(method.value)"
            >
              <el-icon><component :is="methodIcon(method.type)" /></el-icon>
              <span>{{ methodMeta(method.type).label }} · {{ method.value }}</span>
              <span v-if="method.label" class="method-tag__label">{{ method.label }}</span>
            </el-tag>
            <el-tag v-if="row.methods.length > 4" type="info" effect="plain">
              +{{ row.methods.length - 4 }}
            </el-tag>
            <span v-if="row.methods.length === 0" class="muted">暂无联系方式</span>
          </div>
        </template>
      </el-table-column>

      <el-table-column label="备注" min-width="150" show-overflow-tooltip>
        <template #default="{ row }">
          <span class="muted">{{ row.notes || '—' }}</span>
        </template>
      </el-table-column>

      <el-table-column label="更新时间" width="130">
        <template #default="{ row }">
          <span class="muted">{{ formatDate(row.updatedAt) }}</span>
        </template>
      </el-table-column>

      <el-table-column label="操作" width="160" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" :icon="Edit" @click="emit('edit', row)">编辑</el-button>
          <el-button link type="danger" :icon="Delete" @click="emit('delete', row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>
