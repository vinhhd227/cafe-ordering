<script setup>
defineProps({
  groups: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false },
})
const emit = defineEmits(['create', 'edit', 'delete'])

const { t } = useI18n()
</script>

<template>
  <div class="tw:flex tw:flex-col tw:min-h-full">

    <!-- Loading skeletons -->
    <template v-if="loading">
      <div
        v-for="n in 3"
        :key="n"
        class="tw:mx-4 tw:mt-3 tw:rounded-xl tw:bg-white tw:dark:bg-neutral-900 tw:p-4 tw:space-y-3"
      >
        <prime-skeleton width="60%" height="1rem" />
        <prime-skeleton width="100%" height="0.75rem" />
        <prime-skeleton width="80%" height="0.75rem" />
      </div>
    </template>

    <!-- Empty state -->
    <div
      v-else-if="groups.length === 0"
      class="tw:flex tw:flex-col tw:items-center tw:justify-center tw:gap-5 tw:py-24 tw:px-8 tw:text-center"
    >
      <div class="tw:w-20 tw:h-20 tw:rounded-full tw:bg-primary-50 tw:dark:bg-primary-900/20 tw:flex tw:items-center tw:justify-center">
        <iconify icon="ph:plus-square-bold" class="tw:text-4xl tw:text-primary-400" />
      </div>
      <div>
        <p class="tw:font-semibold tw:text-slate-700 tw:dark:text-white">{{ t('products.mobile.addons.empty') }}</p>
      </div>
    </div>

    <!-- Group list -->
    <template v-else>
      <div class="tw:pb-32 tw:px-4 tw:pt-3 tw:space-y-2.5">
        <div
          v-for="group in groups"
          :key="group.id"
          class="tw:rounded-xl tw:overflow-hidden tw:shadow-sm tw:bg-white tw:dark:bg-neutral-900"
        >
          <!-- Main info -->
          <div class="tw:px-4 tw:py-3.5">
            <p class="tw:text-xl tw:font-semibold tw:text-slate-800 tw:dark:text-white tw:leading-tight">{{ group.name }}</p>
            <p
              v-if="group.valueNames && group.valueNames.length > 0"
              class="tw:text tw:text-slate-400 tw:dark:text-slate-500 tw:mt-0.5 tw:line-clamp-1"
            >{{ group.valueNames.join('; ') }}</p>
            <p
              v-else
              class="tw:text-xs tw:text-slate-300 tw:dark:text-slate-600 tw:mt-0.5 tw:italic"
            >{{ t('products.mobile.addons.noOptions') }}</p>
          </div>

          <!-- Bottom row: stats + action buttons -->
          <div class="tw:border-t tw:border-slate-50 tw:dark:border-white/5 tw:flex tw:items-stretch">
            <!-- Stats -->
            <div class="tw:flex-1 tw:flex tw:divide-x tw:divide-slate-100 tw:dark:divide-white/5">
              <div class="tw:flex-1 tw:flex tw:items-center tw:gap-1 tw:px-3 tw:py-2">
                <span class="tw:text tw:text-blue-500 tw:dark:text-primary-400 tw:font-medium">
                  {{ t('products.mobile.addons.valueCount', { n: group.valueCount }) }}
                </span>
              </div>
              <div class="tw:flex-1 tw:flex tw:items-center tw:gap-1 tw:px-3 tw:py-2">
                <span class="tw:text tw:text-blue-500 tw:dark:text-primary-400 tw:font-medium">
                  {{ t('products.mobile.addons.linkedProductCount', { n: group.linkedProductCount ?? 0 }) }}
                </span>
              </div>
            </div>

            <!-- Edit / Delete buttons -->
            <div class="tw:flex tw:divide-x tw:divide-slate-100 tw:dark:divide-white/5 tw:border-l tw:border-slate-100 tw:dark:border-white/5">
              <button
                type="button"
                class="tw:w-11 tw:flex tw:items-center tw:justify-center tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-primary-500 tw:dark:text-primary-400 tw:active:bg-blue-50 tw:dark:active:bg-blue-900/20"
                @click="emit('edit', group)"
              >
                <iconify icon="ph:pencil-simple-bold" class="tw:text-base" />
              </button>
              <button
                type="button"
                class="tw:w-11 tw:flex tw:items-center tw:justify-center tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-red-400 tw:active:bg-red-50 tw:dark:active:bg-red-900/20"
                @click="emit('delete', group)"
              >
                <iconify icon="ph:trash-bold" class="tw:text-base" />
              </button>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Fixed bottom FAB -->
    <div v-if="!loading" class="tw:fixed tw:bottom-6 tw:left-4 tw:right-4 tw:z-20">
      <prime-button rounded class="tw:w-full tw:py-3.5!" @click="emit('create')">
        <iconify icon="ph:plus-bold" class="tw:text-base" />
        <span class="tw:font-semibold tw:text-xl">{{ t('products.mobile.addons.createGroup') }}</span>
      </prime-button>
    </div>

  </div>
</template>
