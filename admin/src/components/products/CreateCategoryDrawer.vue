<script setup>
import { ref, watch } from 'vue'
import { createCategory } from '@/services/category.service'
import { uploadImage } from '@/services/upload.service'

const props = defineProps({
  visible: { type: Boolean, required: true },
})

const emit = defineEmits(['update:visible', 'created'])

const { t } = useI18n()
const toast = useToast()

const createName = ref('')
const createDescription = ref('')
const createImageUrl = ref('')
const createLoading = ref(false)
const imageUploading = ref(false)
const fileInputRef = ref(null)

const close = () => emit('update:visible', false)

watch(() => props.visible, (val) => {
  if (val) {
    createName.value = ''
    createDescription.value = ''
    createImageUrl.value = ''
  }
})

const handleImageSelect = async (e) => {
  const file = e.target.files?.[0]
  if (!file) return
  imageUploading.value = true
  try {
    const res = await uploadImage(file)
    createImageUrl.value = res?.data?.url ?? ''
  } catch {
    // ignore — category can be created without image
  } finally {
    imageUploading.value = false
    e.target.value = ''
  }
}

const submitCreate = async () => {
  if (!createName.value.trim() || createLoading.value) return
  createLoading.value = true
  try {
    const res = await createCategory({
      name: createName.value.trim(),
      description: createDescription.value.trim() || null,
      imageUrl: createImageUrl.value || null,
    })
    emit('created', res?.data)
    close()
  } catch (err) {
    toast.add({
      severity: 'error',
      summary: err?.response?.data?.message || t('categories.create.error'),
      life: 3000,
    })
  } finally {
    createLoading.value = false
  }
}
</script>

<template>
  <prime-drawer
    :visible="visible"
    position="bottom"
    :style="{ height: 'auto' }"
    :show-close-icon="false"
    :pt="{
      root: { class: 'tw:rounded-t-2xl' },
      header: { class: 'tw:pt-3 tw:pb-0 tw:px-5' },
      content: { class: 'tw:px-5 tw:pb-8' },
    }"
    @update:visible="$emit('update:visible', $event)"
  >
    <template #header>
      <div class="tw:flex tw:flex-col tw:w-full">
        <div class="tw:flex tw:justify-center tw:pb-3">
          <div class="tw:w-10 tw:h-1 tw:rounded-full tw:bg-slate-300 tw:dark:bg-white/20" />
        </div>
        <div class="tw:flex tw:items-center tw:justify-between">
          <h3 class="tw:text-base tw:font-bold tw:text-slate-800 tw:dark:text-white">
            {{ t('products.mobile.createCategory') }}
          </h3>
          <button
            type="button"
            class="tw:w-8 tw:h-8 tw:flex tw:items-center tw:justify-center tw:rounded-full tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-slate-400 tw:active:bg-slate-100 tw:dark:active:bg-white/10"
            @click="close"
          >
            <iconify icon="ph:x-bold" class="tw:text-base" />
          </button>
        </div>
      </div>
    </template>

    <!-- Image picker -->
    <div class="tw:flex tw:justify-center tw:mt-5 tw:mb-7">
      <div class="tw:relative tw:w-36 tw:h-36">
        <div class="tw:w-full tw:h-full tw:rounded-2xl tw:bg-slate-100 tw:dark:bg-white/10 tw:overflow-hidden tw:flex tw:items-center tw:justify-center">
          <img v-if="createImageUrl" :src="createImageUrl" class="tw:w-full tw:h-full tw:object-cover" />
          <iconify v-else icon="ph:image-square-bold" class="tw:text-5xl tw:text-slate-300 tw:dark:text-white/20" />
        </div>
        <button
          type="button"
          class="tw:absolute tw:-bottom-2 tw:-right-2 tw:w-9 tw:h-9 tw:rounded-full tw:bg-primary-500 tw:flex tw:items-center tw:justify-center tw:border-2 tw:border-white tw:dark:border-neutral-900 tw:cursor-pointer tw:shadow-md"
          @click="fileInputRef?.click()"
        >
          <iconify
            :icon="imageUploading ? 'ph:spinner-bold' : 'ph:camera-bold'"
            class="tw:text-sm tw:text-white"
            :class="{ 'tw:animate-spin': imageUploading }"
          />
        </button>
        <input ref="fileInputRef" type="file" accept="image/*" class="tw:hidden" @change="handleImageSelect" />
      </div>
    </div>

    <!-- Name input -->
    <div class="tw:mb-5">
      <label class="tw:block tw:text-sm tw:font-semibold tw:text-primary-600 tw:dark:text-primary-400 tw:mb-2">
        {{ t('products.mobile.categoryNameLabel') }}<span class="tw:text-red-500">*</span>
      </label>
      <div class="tw:relative">
        <input
          v-model="createName"
          type="text"
          :placeholder="t('products.mobile.categoryNamePlaceholder')"
          class="tw:w-full tw:bg-transparent tw:border-0 tw:border-b-2 tw:border-primary-500 tw:py-2 tw:pr-6 tw:text-sm tw:outline-none tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-400 tw:dark:placeholder-white/30"
          @keyup.enter="submitCreate"
        />
        <button
          v-if="createName"
          type="button"
          class="tw:absolute tw:right-0 tw:top-1/2 tw:-translate-y-1/2 tw:text-slate-400 tw:border-0 tw:bg-transparent tw:cursor-pointer tw:p-0.5"
          @click="createName = ''"
        >
          <iconify icon="ph:x-circle-fill" class="tw:text-base" />
        </button>
      </div>
    </div>

    <!-- Description input -->
    <div class="tw:mb-7">
      <label class="tw:block tw:text-sm tw:font-semibold tw:text-slate-500 tw:dark:text-slate-400 tw:mb-2">
        {{ t('products.mobile.categoryDescriptionLabel') }}
      </label>
      <div class="tw:relative">
        <input
          v-model="createDescription"
          type="text"
          :placeholder="t('products.mobile.categoryDescriptionPlaceholder')"
          class="tw:w-full tw:bg-transparent tw:border-0 tw:border-b tw:border-slate-300 tw:dark:border-white/20 tw:py-2 tw:pr-6 tw:text-sm tw:outline-none tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-400 tw:dark:placeholder-white/30"
        />
        <button
          v-if="createDescription"
          type="button"
          class="tw:absolute tw:right-0 tw:top-1/2 tw:-translate-y-1/2 tw:text-slate-400 tw:border-0 tw:bg-transparent tw:cursor-pointer tw:p-0.5"
          @click="createDescription = ''"
        >
          <iconify icon="ph:x-circle-fill" class="tw:text-base" />
        </button>
      </div>
    </div>

    <!-- Submit button -->
    <button
      type="button"
      class="tw:w-full tw:rounded-2xl tw:py-4 tw:text-sm tw:font-semibold tw:transition-colors tw:border-0 tw:cursor-pointer"
      :class="createName.trim()
        ? 'tw:bg-primary-500 tw:text-white'
        : 'tw:bg-slate-100 tw:dark:bg-white/5 tw:text-slate-400 tw:dark:text-white/30 tw:cursor-not-allowed'"
      :disabled="!createName.trim() || createLoading"
      @click="submitCreate"
    >
      <iconify v-if="createLoading" icon="ph:spinner-bold" class="tw:animate-spin tw:text-base" />
      <span v-else>{{ t('products.mobile.createSubmit') }}</span>
    </button>
  </prime-drawer>
</template>
