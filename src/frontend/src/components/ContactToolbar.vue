<script setup lang="ts">
import { ref } from 'vue'
import { Download, Plus, Refresh, Search, Upload } from '@element-plus/icons-vue'

defineProps<{
  search: string
  favoriteOnly: boolean
  loading: boolean
}>()

const emit = defineEmits<{
  (event: 'update:search', value: string): void
  (event: 'update:favoriteOnly', value: boolean): void
  (event: 'search', value: string): void
  (event: 'favorite-change', value: boolean): void
  (event: 'add'): void
  (event: 'import', file: File): void
  (event: 'export'): void
  (event: 'refresh'): void
}>()

const fileInput = ref<HTMLInputElement | null>(null)

function onSearchInput(value: string): void {
  emit('update:search', value)
}

function onFavoriteToggle(value: string | number | boolean): void {
  const enabled = Boolean(value)
  emit('update:favoriteOnly', enabled)
  emit('favorite-change', enabled)
}

function onFileChange(event: Event): void {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (file) {
    emit('import', file)
  }
  input.value = ''
}
</script>

<template>
  <section class="toolbar">
    <div class="toolbar__search">
      <el-input
        :model-value="search"
        size="large"
        clearable
        placeholder="搜索姓名、公司或联系方式..."
        @update:model-value="onSearchInput"
        @keyup.enter="emit('search', search)"
        @clear="emit('search', '')"
      >
        <template #prefix>
          <el-icon><Search /></el-icon>
        </template>
      </el-input>
    </div>

    <div class="toolbar__filters">
      <el-switch
        :model-value="favoriteOnly"
        active-text="仅看收藏"
        @change="onFavoriteToggle"
      />
      <el-tooltip content="刷新列表" placement="top">
        <el-button :icon="Refresh" :loading="loading" circle @click="emit('refresh')" />
      </el-tooltip>
    </div>

    <div class="toolbar__actions">
      <el-button type="primary" :icon="Plus" @click="emit('add')">新增联系人</el-button>
      <el-button :icon="Upload" @click="fileInput?.click()">导入 Excel</el-button>
      <el-button :icon="Download" @click="emit('export')">导出 Excel</el-button>
      <input ref="fileInput" type="file" accept=".xlsx" hidden @change="onFileChange" />
    </div>
  </section>
</template>
