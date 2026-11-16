<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { Delete, Plus } from '@element-plus/icons-vue'
import type { Contact, ContactMethodInput, ContactMethodType, ContactPayload } from '../types/contact'
import { METHOD_TYPES } from '../types/contact'

const props = defineProps<{
  modelValue: boolean
  contact: Contact | null
  submitting: boolean
}>()

const emit = defineEmits<{
  (event: 'update:modelValue', value: boolean): void
  (event: 'submit', payload: ContactPayload): void
}>()

interface ContactForm {
  name: string
  company: string
  notes: string
  isFavorite: boolean
  methods: ContactMethodInput[]
}

const formRef = ref<FormInstance>()
const form = reactive<ContactForm>({
  name: '',
  company: '',
  notes: '',
  isFavorite: false,
  methods: []
})

const title = computed(() => (props.contact ? '编辑联系人' : '新增联系人'))
const visible = computed({
  get: () => props.modelValue,
  set: (value: boolean) => emit('update:modelValue', value)
})

const rules: FormRules = {
  name: [{ required: true, message: '请输入联系人姓名', trigger: 'blur' }]
}

watch(
  () => props.modelValue,
  (open) => {
    if (!open) {
      return
    }

    const contact = props.contact
    form.name = contact?.name ?? ''
    form.company = contact?.company ?? ''
    form.notes = contact?.notes ?? ''
    form.isFavorite = contact?.isFavorite ?? false
    form.methods = contact
      ? contact.methods.map((method) => ({
          type: method.type,
          label: method.label,
          value: method.value,
          isPrimary: method.isPrimary
        }))
      : [{ type: 'Phone', label: '手机', value: '', isPrimary: true }]
    formRef.value?.clearValidate()
  },
  { immediate: true }
)

function addMethod(): void {
  form.methods.push({ type: 'Phone', label: '', value: '', isPrimary: false })
}

function removeMethod(index: number): void {
  const [removed] = form.methods.splice(index, 1)
  if (removed?.isPrimary) {
    const replacement = form.methods.find((method) => method.type === removed.type)
    if (replacement) {
      replacement.isPrimary = true
    }
  }
}

function makePrimary(index: number): void {
  const selected = form.methods[index]
  if (!selected) {
    return
  }

  form.methods.forEach((method, methodIndex) => {
    if (method.type === selected.type) {
      method.isPrimary = methodIndex === index
    }
  })
}

function onTypeChange(index: number): void {
  const method = form.methods[index]
  if (!method) {
    return
  }

  const sameType = form.methods.filter((item) => item.type === method.type)
  if (sameType.length > 0 && !sameType.some((item) => item.isPrimary)) {
    method.isPrimary = true
  }
}

function onPrimaryChange(index: number, value: string | number | boolean): void {
  if (Boolean(value)) {
    makePrimary(index)
  }
}

async function submit(): Promise<void> {
  const valid = (await formRef.value?.validate().catch(() => false)) ?? false
  if (!valid) {
    return
  }

  if (form.methods.length === 0) {
    ElMessage.warning('请至少添加一种联系方式')
    return
  }

  if (form.methods.some((method) => !method.value.trim())) {
    ElMessage.warning('请填写联系方式的号码或内容，或删除空行')
    return
  }

  const methods = form.methods.map((method) => ({
    type: method.type as ContactMethodType,
    label: method.label?.trim() || undefined,
    value: method.value.trim(),
    isPrimary: method.isPrimary
  }))

  for (const type of new Set(methods.map((method) => method.type))) {
    const sameType = methods.filter((method) => method.type === type)
    if (!sameType.some((method) => method.isPrimary)) {
      sameType[0].isPrimary = true
    }
  }

  emit('submit', {
    name: form.name.trim(),
    company: form.company.trim() || null,
    notes: form.notes.trim() || null,
    isFavorite: form.isFavorite,
    methods
  })
}
</script>

<template>
  <el-dialog
    v-model="visible"
    :title="title"
    width="780px"
    class="contact-dialog"
    destroy-on-close
  >
    <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
      <div class="form-grid">
        <el-form-item label="姓名" prop="name">
          <el-input v-model="form.name" maxlength="100" placeholder="例如：张三" />
        </el-form-item>
        <el-form-item label="公司 / 单位">
          <el-input v-model="form.company" maxlength="200" placeholder="例如：福州大学" />
        </el-form-item>
      </div>

      <el-form-item label="备注">
        <el-input
          v-model="form.notes"
          type="textarea"
          :rows="2"
          maxlength="2000"
          show-word-limit
          placeholder="补充说明..."
        />
      </el-form-item>

      <el-form-item>
        <el-checkbox v-model="form.isFavorite">收藏该联系人</el-checkbox>
      </el-form-item>

      <div class="methods-editor">
        <div class="methods-editor__head">
          <span>联系方式</span>
          <el-button type="primary" link :icon="Plus" @click="addMethod">添加联系方式</el-button>
        </div>

        <div v-for="(method, index) in form.methods" :key="index" class="method-row">
          <el-select v-model="method.type" class="method-row__type" @change="onTypeChange(index)">
            <el-option
              v-for="type in METHOD_TYPES"
              :key="type.value"
              :label="type.label"
              :value="type.value"
            />
          </el-select>
          <el-input v-model="method.label" class="method-row__label" placeholder="标签" maxlength="50" />
          <el-input
            v-model="method.value"
            class="method-row__value"
            placeholder="号码 / 账号 / 地址"
            maxlength="500"
          />
          <el-switch
            :model-value="method.isPrimary"
            inline-prompt
            active-text="主"
            inactive-text="副"
            @change="(value) => onPrimaryChange(index, value)"
          />
          <el-button link type="danger" :icon="Delete" @click="removeMethod(index)" />
        </div>

        <el-empty v-if="form.methods.length === 0" :image-size="56" description="还没有联系方式" />
      </div>
    </el-form>

    <template #footer>
      <el-button @click="visible = false">取消</el-button>
      <el-button type="primary" :loading="submitting" @click="submit">保存</el-button>
    </template>
  </el-dialog>
</template>
