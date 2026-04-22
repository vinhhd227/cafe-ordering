<script setup>
import { marked } from 'marked'
import DOMPurify from 'dompurify'
import { getRecipe, createRecipe, updateRecipe } from '@/services/recipe.service'

const { t }  = useI18n()
const router = useRouter()
const route  = useRoute()
const toast  = useToast()

// ── Form (declared early — toolbar functions reference form.content) ──
const form = reactive({
  name:     '',
  type:     'Drink',
  category: 'Other',
  content:  '',
  yield:    '',
  notes:    '',
})

const contentPreview = computed(() =>
  DOMPurify.sanitize(marked.parse(form.content || ''))
)

// ── Markdown toolbar ───────────────────────────────────────────────
const getTextarea = () => document.getElementById('recipe-content')

// ── Undo/Redo history stack ────────────────────────────────────────
const undoStack = ref([])
const redoStack = ref([])
let _isUndoRedo       = false
let _fromToolbar      = false
let _typingTimer      = null
let _snapBeforeTyping = null

const pushSnapshot = () => {
  undoStack.value.push(form.content)
  if (undoStack.value.length > 200) undoStack.value.shift()
  redoStack.value = []
  _fromToolbar = true
}

watch(() => form.content, (_, oldVal) => {
  if (_isUndoRedo) return
  if (_fromToolbar) { _fromToolbar = false; return }
  if (_typingTimer === null) _snapBeforeTyping = oldVal
  clearTimeout(_typingTimer)
  _typingTimer = setTimeout(() => {
    _typingTimer = null
    undoStack.value.push(_snapBeforeTyping)
    if (undoStack.value.length > 200) undoStack.value.shift()
    redoStack.value = []
    _snapBeforeTyping = null
  }, 500)
})

const undoContent = () => {
  if (!undoStack.value.length) return
  _isUndoRedo = true
  redoStack.value.push(form.content)
  form.content = undoStack.value.pop()
  nextTick(() => { _isUndoRedo = false; getTextarea()?.focus() })
}

const redoContent = () => {
  if (!redoStack.value.length) return
  _isUndoRedo = true
  undoStack.value.push(form.content)
  form.content = redoStack.value.pop()
  nextTick(() => { _isUndoRedo = false; getTextarea()?.focus() })
}

const insertWrap = (prefix, suffix, placeholder = 'text') => {
  const el = getTextarea()
  if (!el) return
  pushSnapshot()
  const start = el.selectionStart
  const end   = el.selectionEnd
  const sel   = form.content.slice(start, end) || placeholder
  form.content = form.content.slice(0, start) + prefix + sel + suffix + form.content.slice(end)
  nextTick(() => {
    el.focus()
    el.setSelectionRange(start + prefix.length, start + prefix.length + sel.length)
  })
}

const insertLinePrefix = (prefix) => {
  const el = getTextarea()
  if (!el) return
  pushSnapshot()
  const start     = el.selectionStart
  const lineStart = form.content.lastIndexOf('\n', start - 1) + 1
  form.content    = form.content.slice(0, lineStart) + prefix + form.content.slice(lineStart)
  nextTick(() => { el.focus(); el.setSelectionRange(start + prefix.length, start + prefix.length) })
}

const insertBlock = (text) => {
  const el = getTextarea()
  if (!el) return
  pushSnapshot()
  const pos = el.selectionStart
  const pad = pos > 0 && form.content[pos - 1] !== '\n' ? '\n' : ''
  form.content = form.content.slice(0, pos) + pad + text + form.content.slice(pos)
  const newPos = pos + pad.length + text.length
  nextTick(() => { el.focus(); el.setSelectionRange(newPos, newPos) })
}

const headingOptions = [
  { label: 'Paragraph', value: '' },
  { label: 'Heading 1', value: '# ' },
  { label: 'Heading 2', value: '## ' },
  { label: 'Heading 3', value: '### ' },
  { label: 'Heading 4', value: '#### ' },
]
const selectedHeading = ref('')
const applyHeading = (prefix) => {
  if (!prefix) return
  insertLinePrefix(prefix)
  nextTick(() => { selectedHeading.value = '' })
}

const mdTools = [
  { icon: 'ph:text-b-bold',            title: 'Bold',          action: () => insertWrap('**', '**') },
  { icon: 'ph:text-italic-bold',        title: 'Italic',        action: () => insertWrap('*', '*') },
  { icon: 'ph:text-strikethrough-bold', title: 'Strikethrough', action: () => insertWrap('~~', '~~') },
  { type: 'sep' },
  { icon: 'ph:list-bullets-bold',       title: 'Bullet list',   action: () => insertLinePrefix('- ') },
  { icon: 'ph:list-numbers-bold',       title: 'Numbered list', action: () => insertLinePrefix('1. ') },
  { icon: 'ph:list-checks-bold',        title: 'Checklist',     action: () => insertLinePrefix('- [ ] ') },
  { type: 'sep' },
  { icon: 'ph:quotes-bold',             title: 'Blockquote',    action: () => insertLinePrefix('> ') },
  { icon: 'ph:code-block-bold',         title: 'Code block',    action: () => insertBlock('```\n\n```') },
  { type: 'sep' },
  { icon: 'ph:table-bold',              title: 'Table',         action: () => insertBlock('| Column 1 | Column 2 | Column 3 |\n| --- | --- | --- |\n| Cell | Cell | Cell |\n') },
  { icon: 'ph:minus-bold',              title: 'Divider',       action: () => insertBlock('---\n') },
  { type: 'sep' },
  { icon: 'ph:link-bold',               title: 'Link',          action: () => insertWrap('[', '](url)', 'link text') },
  { icon: 'ph:image-bold',              title: 'Image',         action: () => insertWrap('![', '](url)', 'alt text') },
]

const isNew = computed(() => route.name === 'recipeNew')
const id    = computed(() => route.params.id ? parseInt(route.params.id) : null)

// ── Form ───────────────────────────────────────────────────────────
const loading = ref(false)
const saving  = ref(false)
const errorMessage = ref('')
const originalForm = ref(null)
const isDirty = computed(() => JSON.stringify(form) !== JSON.stringify(originalForm.value))

const typeOptions = [
  { label: t('recipes.type.BaseIngredient'), value: 'BaseIngredient' },
  { label: t('recipes.type.Drink'),          value: 'Drink' },
]
const categoryOptions = [
  { label: t('recipes.category.BaseIngredient'), value: 'BaseIngredient' },
  { label: t('recipes.category.FruitTea'),       value: 'FruitTea' },
  { label: t('recipes.category.Coffee'),         value: 'Coffee' },
  { label: t('recipes.category.Smoothie'),       value: 'Smoothie' },
  { label: t('recipes.category.MilkTea'),        value: 'MilkTea' },
  { label: t('recipes.category.Other'),          value: 'Other' },
]

// ── Load ───────────────────────────────────────────────────────────
const load = async () => {
  if (isNew.value) { originalForm.value = { ...form }; return }
  loading.value = true
  try {
    const res = await getRecipe(id.value)
    const r = res.data
    form.name     = r.name
    form.type     = r.type
    form.category = r.category
    form.content  = r.content
    form.yield    = r.yield  ?? ''
    form.notes    = r.notes  ?? ''
    originalForm.value = { ...form }
  } catch {
    errorMessage.value = t('recipes.error.loadDetailFailed')
  } finally {
    loading.value = false
    nextTick(() => {
      clearTimeout(_typingTimer)
      _typingTimer = null
      _snapBeforeTyping = null
      undoStack.value = []
      redoStack.value = []
    })
  }
}

// ── Validate ───────────────────────────────────────────────────────
const errors = reactive({ name: '', content: '' })
const validate = () => {
  errors.name    = form.name.trim()    ? '' : t('recipes.validation.nameRequired')
  errors.content = form.content.trim() ? '' : t('recipes.validation.contentRequired')
  return !errors.name && !errors.content
}

// ── Save ───────────────────────────────────────────────────────────
const handleSave = async () => {
  if (!validate()) return
  saving.value = true
  try {
    const payload = {
      name:     form.name.trim(),
      type:     form.type,
      category: form.category,
      content:  form.content.trim(),
      yield:    form.yield.trim()  || null,
      notes:    form.notes.trim()  || null,
    }
    if (isNew.value) {
      const res = await createRecipe(payload)
      toast.add({ severity: 'success', summary: t('common.saved'), life: 2500 })
      router.replace({ name: 'recipeDetail', params: { id: res.data.id } })
    } else {
      await updateRecipe(id.value, payload)
      originalForm.value = { ...form }
      toast.add({ severity: 'success', summary: t('common.saved'), life: 2500 })
      router.push({ name: 'recipeDetail', params: { id: id.value } })
    }
  } catch {
    toast.add({ severity: 'error', summary: t('recipes.error.saveFailed'), life: 3000 })
  } finally {
    saving.value = false
  }
}

const handleCancel = () => {
  if (isNew.value) router.push({ name: 'recipes' })
  else router.push({ name: 'recipeDetail', params: { id: id.value } })
}

onMounted(load)
onBeforeUnmount(() => clearTimeout(_typingTimer))
</script>

<template>
  <section class="tw:space-y-6">

    <!-- Header -->
    <div class="tw:flex tw:flex-wrap tw:items-start tw:justify-between tw:gap-4">
      <div>
        <button
          class="tw:flex tw:items-center tw:gap-1.5 tw:text-xs tw:text-muted tw:mb-3 tw:cursor-pointer tw:bg-transparent tw:border-0 tw:p-0 tw:hover:text-primary-500 tw:transition-colors"
          @click="handleCancel"
        >
          <iconify icon="ph:arrow-left-bold" />
          {{ isNew ? t('recipes.detail.backToList') : t('recipes.edit.backToDetail') }}
        </button>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">{{ t('nav.groups.operations') }}</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">
          {{ isNew ? t('recipes.detail.newRecipe') : t('recipes.edit.title') }}
        </h1>
        <p v-if="isDirty && !isNew" class="tw:mt-1 tw:text-xs tw:text-amber-400 tw:flex tw:items-center tw:gap-1">
          <iconify icon="ph:warning-bold" />
          {{ t('recipes.detail.unsavedChanges') }}
        </p>
      </div>

      <div class="tw:flex tw:gap-2">
        <prime-button severity="secondary" outlined size="small" @click="handleCancel">
          <iconify icon="ph:x-bold" />
          <span>{{ t('common.cancel') }}</span>
        </prime-button>
        <prime-button
          severity="primary" size="small"
          :loading="saving"
          :disabled="!isDirty && !isNew"
          @click="handleSave"
        >
          <iconify icon="ph:floppy-disk-bold" />
          <span>{{ isNew ? t('recipes.addRecipe') : t('recipes.detail.saveChanges') }}</span>
        </prime-button>
      </div>
    </div>

    <!-- Error -->
    <prime-alert v-if="errorMessage" severity="error" variant="accent" closable @close="errorMessage = ''">
      {{ errorMessage }}
    </prime-alert>

    <!-- Skeleton -->
    <div v-if="loading" class="tw:space-y-4">
      <prime-skeleton height="2.5rem" />
      <prime-skeleton height="2.5rem" />
      <prime-skeleton height="14rem" />
    </div>

    <!-- Form -->
    <div v-else :class="appCard" class="tw:rounded-2xl tw:p-6 tw:space-y-5">

      <!-- Name -->
      <div class="tw:space-y-1.5">
        <label for="recipe-name" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('recipes.form.name') }}
        </label>
        <prime-input-text
          id="recipe-name"
          v-model="form.name"
          :placeholder="t('recipes.form.namePlaceholder')"
          class="app-input tw:w-full"
          :class="{ 'p-invalid': errors.name }"
        />
        <p v-if="errors.name" class="tw:text-xs tw:text-red-400">{{ errors.name }}</p>
      </div>

      <!-- Type + Category -->
      <div class="tw:grid tw:grid-cols-2 tw:gap-4">
        <div class="tw:space-y-1.5">
          <label for="recipe-type" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('recipes.form.type') }}
          </label>
          <prime-select
            id="recipe-type"
            v-model="form.type"
            :options="typeOptions"
            option-label="label"
            option-value="value"
            class="app-input tw:w-full"
          />
        </div>
        <div class="tw:space-y-1.5">
          <label for="recipe-category" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('recipes.form.category') }}
          </label>
          <prime-select
            id="recipe-category"
            v-model="form.category"
            :options="categoryOptions"
            option-label="label"
            option-value="value"
            class="app-input tw:w-full"
          />
        </div>
      </div>

      <!-- Yield -->
      <div class="tw:space-y-1.5">
        <label for="recipe-yield" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('recipes.form.yield') }}
        </label>
        <prime-input-text
          id="recipe-yield"
          v-model="form.yield"
          :placeholder="t('recipes.form.yieldPlaceholder')"
          class="app-input tw:w-full"
        />
        <p class="tw:text-[11px] app-text-subtle">{{ t('recipes.form.yieldHint') }}</p>
      </div>

      <!-- Content -->
      <div>
        <label for="recipe-content" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('recipes.form.content') }}
        </label>
        <!-- Toolbar -->
        <div class="tw:flex tw:items-center tw:gap-0.5 tw:flex-wrap tw:rounded-t-xl tw:border tw:border-b-0 tw:border-white/10 tw:bg-white/3 tw:px-2 tw:py-1.5">
          <button type="button" v-tooltip.top="'Undo'" :disabled="!undoStack.length" class="tw:flex tw:items-center tw:justify-center tw:w-7 tw:h-7 tw:rounded tw:text-sm tw:transition-colors tw:text-muted tw:hover:text-white tw:hover:bg-white/10 tw:disabled:opacity-30 tw:disabled:cursor-not-allowed tw:disabled:pointer-events-none" @click="undoContent">
            <iconify icon="ph:arrow-counter-clockwise-bold" />
          </button>
          <button type="button" v-tooltip.top="'Redo'" :disabled="!redoStack.length" class="tw:flex tw:items-center tw:justify-center tw:w-7 tw:h-7 tw:rounded tw:text-sm tw:transition-colors tw:text-muted tw:hover:text-white tw:hover:bg-white/10 tw:disabled:opacity-30 tw:disabled:cursor-not-allowed tw:disabled:pointer-events-none" @click="redoContent">
            <iconify icon="ph:arrow-clockwise-bold" />
          </button>
          <div class="tw:w-px tw:h-4 tw:bg-white/10 tw:mx-1" />
          <prime-select
            v-model="selectedHeading"
            :options="headingOptions"
            option-label="label"
            option-value="value"
            placeholder="Paragraph"
            class="tw:h-7 tw:text-xs tw:w-28"
            :pt="{
              root: { class: 'tw:h-7! tw:min-w-0! tw:border-white/10! tw:bg-transparent!' },
              label: { class: 'tw:text-xs! tw:py-0! tw:px-2! tw:flex! tw:items-center! tw:leading-none!' },
              dropdown: { class: 'tw:w-5! tw:px-0!' },
            }"
            @change="applyHeading(selectedHeading)"
          />
          <div class="tw:w-px tw:h-4 tw:bg-white/10 tw:mx-1" />
          <template v-for="tool in mdTools" :key="tool.icon ?? tool.type">
            <div v-if="tool.type === 'sep'" class="tw:w-px tw:h-4 tw:bg-white/10 tw:mx-1" />
            <button
              v-else
              type="button"
              v-tooltip.top="tool.title"
              class="tw:flex tw:items-center tw:justify-center tw:w-7 tw:h-7 tw:rounded tw:text-sm tw:transition-colors tw:text-muted tw:hover:text-white tw:hover:bg-white/10"
              @click="tool.action()"
            >
              <iconify :icon="tool.icon" />
            </button>
          </template>
        </div>
        <!-- Editor + Preview side by side -->
        <div class="tw:grid tw:lg:grid-cols-2 tw:border tw:border-white/10 tw:rounded-b-xl tw:overflow-hidden">
          <prime-textarea
            id="recipe-content"
            v-model="form.content"
            :placeholder="t('recipes.form.contentPlaceholder')"
            class="app-input tw:w-full tw:font-mono tw:text-sm tw:rounded-none! tw:border-0! tw:border-r! tw:border-white/10! tw:resize-none!"
            :rows="16"
            :class="{ 'p-invalid': errors.content }"
          />
          <div
            class="tw:p-4 tw:prose tw:prose-invert tw:prose-sm tw:max-w-none tw:min-h-48 tw:overflow-y-auto tw:border-t tw:border-white/10 tw:lg:border-t-0"
            v-html="contentPreview || `<span class='tw:opacity-30 tw:text-sm'>${t('recipes.form.previewEmpty')}</span>`"
          />
        </div>
        <p v-if="errors.content" class="tw:text-xs tw:text-red-400">{{ errors.content }}</p>
      </div>

      <!-- Notes -->
      <div class="tw:space-y-1.5">
        <label for="recipe-notes" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('recipes.form.notes') }}
        </label>
        <prime-textarea
          id="recipe-notes"
          v-model="form.notes"
          :placeholder="t('recipes.form.notesPlaceholder')"
          class="app-input tw:w-full tw:text-sm"
          :rows="3"
          auto-resize
        />
      </div>

    </div>
  </section>
</template>
