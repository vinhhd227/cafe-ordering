<script setup>
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const themeStore = useThemeStore()
const { locale, locales, setLocale } = useLocale()

const fullName = computed(() => auth.user?.fullName || 'Staff')
const username = computed(() => auth.user?.username || '')
const roleLabel = computed(() => auth.user?.roles?.[0] || 'Staff')
const avatarLabel = computed(() => {
  const parts = fullName.value.split(' ').filter(Boolean)
  if (!parts.length) return 'ST'
  return parts.slice(0, 2).map(p => p[0]?.toUpperCase()).join('') || 'ST'
})
const avatarImage = computed(() => auth.user?.avatarUrl || null)
</script>

<template>
  <div class="tw:space-y-5 tw:pb-6">

    <!-- User card -->
    <div class="app-panel tw:rounded-2xl tw:border tw:overflow-hidden">
      <div class="tw:flex tw:flex-col tw:items-center tw:gap-3 tw:pt-7 tw:pb-5 tw:px-4">
        <prime-avatar
          v-if="avatarImage"
          :image="avatarImage"
          shape="circle"
          class="tw:w-20! tw:h-20!"
        />
        <prime-avatar
          v-else
          :label="avatarLabel"
          shape="circle"
          class="tw:w-20! tw:h-20! tw:text-2xl! tw:bg-emerald-500/20! tw:text-emerald-300!"
        />
        <div class="tw:text-center">
          <p class="tw:font-semibold tw:text-lg tw:leading-tight">{{ fullName }}</p>
          <p class="tw:text-sm tw:text-muted tw:mt-0.5">@{{ username }}</p>
          <span class="tw:inline-block tw:mt-2 tw:text-[11px] tw:font-medium tw:px-2.5 tw:py-0.5 tw:rounded-full tw:bg-emerald-500/10 tw:text-emerald-400">
            {{ roleLabel }}
          </span>
        </div>
      </div>

      <prime-divider class="tw:w-[70%]! tw:mx-auto! tw:my-0!" />
      <div>
        <button
          class="tw:flex tw:items-center tw:gap-3 tw:w-full tw:px-4 tw:py-3.5 tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-left"
          @click="router.push({ name: 'profile' })"
        >
          <div class="tw:w-8 tw:h-8 tw:rounded-full tw:bg-emerald-500/10 tw:flex tw:items-center tw:justify-center tw:shrink-0">
            <iconify icon="ph:user-bold" class="tw:text-emerald-400" />
          </div>
          <span class="tw:flex-1 tw:text-sm tw:font-medium">{{ t('mobileProfile.editProfile') }}</span>
          <iconify icon="ph:caret-right-bold" class="tw:text-muted tw:text-sm" />
        </button>
      </div>
    </div>

    <!-- Appearance -->
    <div class="tw:space-y-2">
      <p class="tw:text-[11px] tw:font-semibold tw:uppercase tw:tracking-widest tw:text-muted tw:px-1">
        {{ t('mobileProfile.sectionAppearance') }}
      </p>
      <div class="app-panel tw:rounded-2xl tw:border">
        <div class="tw:flex tw:items-center tw:gap-3 tw:px-4 tw:py-3.5">
          <div class="tw:w-8 tw:h-8 tw:rounded-full tw:bg-slate-500/10 tw:flex tw:items-center tw:justify-center tw:shrink-0">
            <iconify icon="ph:moon-bold" class="tw:text-slate-400" />
          </div>
          <span class="tw:flex-1 tw:text-sm tw:font-medium">{{ t('mobileProfile.darkMode') }}</span>
          <prime-toggle-switch :model-value="themeStore.isDark" @update:model-value="themeStore.toggleTheme()" />
        </div>
        <prime-divider class="tw:w-[70%]! tw:mx-auto! tw:my-0!" />
        <div class="tw:flex tw:items-center tw:gap-3 tw:px-4 tw:py-3.5">
          <div class="tw:w-8 tw:h-8 tw:rounded-full tw:bg-blue-500/10 tw:flex tw:items-center tw:justify-center tw:shrink-0">
            <iconify icon="ph:globe-bold" class="tw:text-blue-400" />
          </div>
          <span class="tw:flex-1 tw:text-sm tw:font-medium">{{ t('mobileProfile.language') }}</span>
          <div class="tw:flex tw:gap-1">
            <button
              v-for="l in locales"
              :key="l.code"
              class="tw:px-2.5 tw:py-1 tw:rounded-lg tw:text-xs tw:font-semibold tw:border-0 tw:cursor-pointer tw:transition-colors"
              :class="locale === l.code
                ? 'tw:bg-emerald-500 tw:text-white'
                : 'tw:bg-black/5 tw:dark:bg-white/10 tw:text-muted'"
              @click="setLocale(l.code)"
            >
              {{ l.label }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Account -->
    <div class="tw:space-y-2">
      <p class="tw:text-[11px] tw:font-semibold tw:uppercase tw:tracking-widest tw:text-muted tw:px-1">
        {{ t('mobileProfile.sectionAccount') }}
      </p>
      <div class="app-panel tw:rounded-2xl tw:border">
        <button
          class="tw:flex tw:items-center tw:gap-3 tw:w-full tw:px-4 tw:py-3.5 tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-left tw:text-red-500"
          @click="auth.logout()"
        >
          <div class="tw:w-8 tw:h-8 tw:rounded-full tw:bg-red-500/10 tw:flex tw:items-center tw:justify-center tw:shrink-0">
            <iconify icon="ph:sign-out-bold" class="tw:text-red-500" />
          </div>
          <span class="tw:flex-1 tw:text-sm tw:font-medium">{{ t('auth.logout') }}</span>
        </button>
      </div>
    </div>

  </div>
</template>
