<script setup>
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const { t }  = useI18n()
const router = useRouter()
const auth   = useAuthStore()

const fullName    = computed(() => auth.user?.fullName || 'Staff')
const avatarImage = computed(() => auth.user?.avatarUrl || auth.user?.avatar || null)
const avatarLabel = computed(() => {
  const parts = fullName.value.split(' ').filter(Boolean)
  if (!parts.length) return 'ST'
  return parts.slice(0, 2).map(p => p[0]?.toUpperCase()).join('') || 'ST'
})
const roleLabel = computed(() => {
  if (Array.isArray(auth.user?.roles) && auth.user.roles.length > 0) return auth.user.roles[0]
  return auth.user?.role || 'Staff'
})
const roleDotColor = computed(() => {
  const r = roleLabel.value.toLowerCase()
  if (r === 'admin') return 'tw:bg-primary-400'
  if (r === 'manager') return 'tw:bg-sky-400'
  return 'tw:bg-amber-400'
})

const profileMenu  = ref()
const profileItems = computed(() => [
  { label: t('auth.profile'),  command: () => router.push({ name: 'profile', query: { tab: 'overview' } }) },
  { label: t('auth.settings'), command: () => router.push({ name: 'profile', query: { tab: 'settings' } }) },
  { separator: true },
  { label: t('auth.helpSupport'), command: () => window.open('mailto:support@cafe.com', '_blank') },
  { separator: true },
  { label: t('auth.logout'), command: () => auth.logout() },
])
</script>

<template>
  <div class="tw:relative">
    <prime-button
      variant="text"
      class="tw:relative tw:flex tw:h-9! tw:w-9! tw:p-0! tw:items-center tw:justify-center tw:rounded-full tw:transition-colors tw:hover:ring-2 tw:hover:ring-white/20"
      :title="fullName"
      @click="profileMenu.toggle($event)"
    >
      <prime-avatar
        v-if="avatarImage"
        :image="avatarImage"
        shape="circle"
        class="tw:w-8! tw:h-8! tw:text-xs!"
      />
      <prime-avatar
        v-else
        :label="avatarLabel"
        shape="circle"
        class="tw:w-8! tw:h-8! tw:text-xs! tw:bg-primary-500/20! tw:text-primary-300!"
      />
      <span
        class="tw:absolute tw:bottom-0 tw:right-0 tw:w-2.5 tw:h-2.5 tw:rounded-full tw:border-2 tw:border-[var(--app-header-bg,#0f172a)]"
        :class="roleDotColor"
      />
    </prime-button>
    <prime-menu ref="profileMenu" :model="profileItems" popup>
      <template #start>
        <div class="tw:flex tw:items-center tw:gap-3 tw:px-3 tw:py-3 tw:border-b tw:border-white/10">
          <div class="tw:relative tw:shrink-0">
            <prime-avatar
              v-if="avatarImage"
              :image="avatarImage"
              shape="circle"
              class="tw:w-10! tw:h-10!"
            />
            <prime-avatar
              v-else
              :label="avatarLabel"
              shape="circle"
              class="tw:w-10! tw:h-10! tw:text-sm! tw:bg-primary-500/20! tw:text-primary-300!"
            />
            <span
              class="tw:absolute tw:bottom-0 tw:right-0 tw:w-2 tw:h-2 tw:rounded-full"
              :class="roleDotColor"
            />
          </div>
          <div class="tw:min-w-0">
            <p class="tw:text-sm tw:font-semibold tw:truncate">{{ fullName }}</p>
            <p class="tw:text-xs tw:truncate tw:mt-0.5 tw:text-muted">{{ roleLabel }}</p>
          </div>
        </div>
      </template>
    </prime-menu>
  </div>
</template>
