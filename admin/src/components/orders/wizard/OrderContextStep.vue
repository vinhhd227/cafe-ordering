<script setup>
const { t } = useI18n()

const props = defineProps({
  orderType: { type: String, required: true },
  // DineIn
  tables:             { type: Array, default: () => [] },
  selectedTableId:    { type: [Number, null], default: null },
  sessionId:          { type: [String, null], default: null },
  sessionLoading:     { type: Boolean, default: false },
  sessionError:       { type: String, default: '' },
  sessionHadExisting: { type: Boolean, default: false },
  guestCount:         { type: [Number, null], default: null },
  // Takeaway / Delivery
  customerName:    { type: String, default: '' },
  customerPhone:   { type: String, default: '' },
  deliveryAddress: { type: String, default: '' },
  deliveryNote:    { type: String, default: '' },
})

const emit = defineEmits([
  'update:selectedTableId',
  'update:guestCount',
  'update:customerName',
  'update:customerPhone',
  'update:deliveryAddress',
  'update:deliveryNote',
  'tableSelect',
])

const deliveryAddressError = ref('')

const validateDelivery = () => {
  if (props.orderType === 'DELIVERY' && !props.deliveryAddress.trim()) {
    deliveryAddressError.value = t('orders.create.delivery.addressRequired')
    return false
  }
  deliveryAddressError.value = ''
  return true
}

defineExpose({ validateDelivery })

// Group active tables by zone
const tableGroups = computed(() => {
  const groups = new Map()
  for (const table of props.tables) {
    if (!table.isActive) continue
    const key = table.zoneName ?? ''
    if (!groups.has(key)) groups.set(key, [])
    groups.get(key).push(table)
  }
  return [...groups.entries()]
    .sort(([a], [b]) => {
      if (!a && !b) return 0
      if (!a) return 1
      if (!b) return -1
      return a.localeCompare(b)
    })
    .map(([zoneName, tables]) => ({ zoneName, tables }))
})

const statusDot = (status) => {
  if (status === 'Available') return 'tw:bg-emerald-400'
  if (status === 'Occupied')  return 'tw:bg-amber-400'
  return 'tw:bg-slate-400'
}

const selectTable = (id) => {
  emit('update:selectedTableId', id)
  emit('tableSelect', id)
}
</script>

<template>
  <!-- DineIn: table grid + guest count -->
  <div v-if="orderType === 'DINE_IN'" class="tw:space-y-4">

    <!-- Table grid grouped by zone -->
    <div class="tw:space-y-3">
      <p class="tw:text-sm tw:font-medium">{{ t('orders.create.selectTable') }}</p>

      <div v-if="tableGroups.length === 0" class="tw:text-sm tw:text-muted tw:text-center tw:py-4">
        {{ t('orders.create.noTables') }}
      </div>

      <div v-for="group in tableGroups" :key="group.zoneName" class="tw:space-y-1.5">
        <!-- Zone label -->
        <p v-if="group.zoneName" class="tw:text-xs tw:text-muted tw:uppercase tw:tracking-widest tw:font-medium">
          {{ group.zoneName }}
        </p>

        <!-- Table cards scroll row -->
        <div class="tw:flex tw:gap-1.5 tw:overflow-x-auto tw:pb-1 tw:snap-x tw:snap-mandatory" style="scrollbar-width: none;">
          <button
            v-for="table in group.tables"
            :key="table.id"
            class="tw:relative tw:flex tw:flex-col tw:items-center tw:justify-center tw:gap-0.5 tw:rounded-lg tw:border tw:p-1.5 tw:text-center tw:transition-all tw:cursor-pointer tw:aspect-square tw:snap-start tw:shrink-0"
            style="width: calc(25% - 0.28rem)"
            :class="selectedTableId === table.id
              ? 'tw:border-primary-500 tw:bg-primary-500/15 tw:text-primary-300'
              : 'tw:hover:border-primary-500/40'"
            :style="selectedTableId === table.id ? 'width: calc(25% - 0.28rem)' : 'width: calc(25% - 0.28rem); border-color: var(--app-border); background: var(--app-bg-subtle)'"
            :disabled="sessionLoading"
            @click="selectTable(table.id)"
          >
            <!-- Status dot -->
            <span
              class="tw:absolute tw:top-1.5 tw:right-1.5 tw:h-2 tw:w-2 tw:rounded-full tw:shrink-0"
              :class="statusDot(table.status)"
            />
            <!-- Table code -->
            <iconify icon="ph:armchair-bold" class="tw:text-xl tw:opacity-60" />
            <span class="tw:text-xs tw:font-semibold tw:leading-tight tw:line-clamp-2 tw:w-full tw:text-center">
              {{ table.code }}
            </span>
          </button>
        </div>
      </div>
    </div>

    <!-- Guest count -->
    <div class="tw:space-y-1.5">
      <label for="wizard-guests" class="tw:text-sm tw:font-medium">{{ t('orders.create.guestCountPlaceholder') }}</label>
      <prime-input-number
        id="wizard-guests"
        :modelValue="guestCount"
        :min="1" :max="99" :useGrouping="false"
        :placeholder="t('orders.create.guestCountPlaceholder')"
        inputClass="tw:w-full"
        class="tw:w-full"
        @update:modelValue="$emit('update:guestCount', $event)"
      />
    </div>

    <!-- Session status -->
    <div v-if="sessionLoading" class="tw:flex tw:items-center tw:gap-2 tw:text-sm tw:text-muted">
      <iconify icon="prime:spinner" class="tw:animate-spin" />
      <span>{{ t('orders.create.connecting') }}</span>
    </div>
    <prime-message v-else-if="sessionError" severity="error" :closable="false">{{ sessionError }}</prime-message>
    <prime-tag v-else-if="sessionId && sessionHadExisting" severity="info">
      <iconify icon="prime:info-circle" class="tw:mr-1" />
      {{ t('orders.create.existingSession') }}
    </prime-tag>
    <prime-tag v-else-if="sessionId" severity="success">
      <iconify icon="prime:check" class="tw:mr-1" />
      {{ t('orders.create.sessionReady') }}
    </prime-tag>
  </div>

  <!-- Takeaway: name + phone -->
  <div v-else-if="orderType === 'TAKEAWAY'" class="tw:space-y-5">
    <div class="tw:space-y-1.5">
      <label for="wizard-name" class="tw:text-sm tw:font-medium">{{ t('orders.create.customer.name') }}</label>
      <prime-input-text
        id="wizard-name"
        :modelValue="customerName"
        :placeholder="t('orders.create.customer.namePlaceholder')"
        class="app-input tw:w-full"
        @update:modelValue="$emit('update:customerName', $event)"
      />
    </div>
    <div class="tw:space-y-1.5">
      <label for="wizard-phone" class="tw:text-sm tw:font-medium">{{ t('orders.create.customer.phone') }}</label>
      <prime-input-text
        id="wizard-phone"
        :modelValue="customerPhone"
        :placeholder="t('orders.create.customer.phonePlaceholder')"
        class="app-input tw:w-full"
        @update:modelValue="$emit('update:customerPhone', $event)"
      />
    </div>
  </div>

  <!-- Delivery: name + phone + address + note -->
  <div v-else-if="orderType === 'DELIVERY'" class="tw:space-y-5">
    <div class="tw:grid tw:grid-cols-1 tw:sm:grid-cols-2 tw:gap-4">
      <div class="tw:space-y-1.5">
        <label for="wizard-del-name" class="tw:text-sm tw:font-medium">{{ t('orders.create.customer.name') }}</label>
        <prime-input-text
          id="wizard-del-name"
          :modelValue="customerName"
          :placeholder="t('orders.create.customer.namePlaceholder')"
          class="app-input tw:w-full"
          @update:modelValue="$emit('update:customerName', $event)"
        />
      </div>
      <div class="tw:space-y-1.5">
        <label for="wizard-del-phone" class="tw:text-sm tw:font-medium">{{ t('orders.create.customer.phone') }}</label>
        <prime-input-text
          id="wizard-del-phone"
          :modelValue="customerPhone"
          :placeholder="t('orders.create.customer.phonePlaceholder')"
          class="app-input tw:w-full"
          @update:modelValue="$emit('update:customerPhone', $event)"
        />
      </div>
    </div>

    <div class="tw:space-y-1.5">
      <label for="wizard-addr" class="tw:text-sm tw:font-medium tw:flex tw:items-center tw:gap-1">
        {{ t('orders.create.delivery.address') }}
        <span class="tw:text-red-400">*</span>
      </label>
      <prime-input-text
        id="wizard-addr"
        :modelValue="deliveryAddress"
        :placeholder="t('orders.create.delivery.addressPlaceholder')"
        :invalid="!!deliveryAddressError"
        class="app-input tw:w-full"
        @update:modelValue="$emit('update:deliveryAddress', $event); deliveryAddressError = ''"
      />
      <p v-if="deliveryAddressError" class="tw:text-xs tw:text-red-400">{{ deliveryAddressError }}</p>
    </div>

    <div class="tw:space-y-1.5">
      <label for="wizard-del-note" class="tw:text-sm tw:font-medium">{{ t('orders.create.delivery.note') }}</label>
      <prime-textarea
        id="wizard-del-note"
        :modelValue="deliveryNote"
        :placeholder="t('orders.create.delivery.notePlaceholder')"
        rows="2"
        auto-resize
        class="app-input tw:w-full"
        @update:modelValue="$emit('update:deliveryNote', $event)"
      />
    </div>
  </div>
</template>
