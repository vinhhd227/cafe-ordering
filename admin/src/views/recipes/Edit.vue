<script setup>
import { getRecipe, createRecipe, updateRecipe } from '@/services/recipe.service'

const { t }  = useI18n()
const router = useRouter()
const route  = useRoute()
const toast  = useToast()

const isNew = computed(() => route.name === 'recipeNew')
const id    = computed(() => route.params.id ? parseInt(route.params.id) : null)

// ── Form ───────────────────────────────────────────────────────────
const loading = ref(false)
const saving  = ref(false)
const errorMessage = ref('')

const form = reactive({
  name:     '',
  type:     'Drink',
  category: 'Other',
  content:  '',
  yield:    '',
  notes:    '',
})
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
</script>

<template>
  <section class="tw:space-y-6">

    <!-- Header -->
    <div class="tw:flex tw:flex-wrap tw:items-start tw:justify-between tw:gap-4">
      <div>
        <button
          class="tw:flex tw:items-center tw:gap-1.5 tw:text-xs app-text-muted tw:mb-3 tw:cursor-pointer tw:bg-transparent tw:border-0 tw:p-0 tw:hover:text-primary-500 tw:transition-colors"
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
    <div v-else :class="appCard" class="tw:rounded-2xl tw:p-6 tw:space-y-5 tw:max-w-3xl">

      <!-- Name -->
      <div class="tw:space-y-1.5">
        <label for="recipe-name" class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
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
          <label for="recipe-type" class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
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
          <label for="recipe-category" class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
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
        <label for="recipe-yield" class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
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
      <div class="tw:space-y-1.5">
        <label for="recipe-content" class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
          {{ t('recipes.form.content') }}
        </label>
        <prime-textarea
          id="recipe-content"
          v-model="form.content"
          :placeholder="t('recipes.form.contentPlaceholder')"
          class="app-input tw:w-full tw:font-mono tw:text-sm"
          :rows="12"
          auto-resize
          :class="{ 'p-invalid': errors.content }"
        />
        <p v-if="errors.content" class="tw:text-xs tw:text-red-400">{{ errors.content }}</p>
      </div>

      <!-- Notes -->
      <div class="tw:space-y-1.5">
        <label for="recipe-notes" class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
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
