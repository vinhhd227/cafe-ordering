<script setup>
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore, PRIMARY_COLORS, FONTS } from '@/stores/theme'

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
    <div :class="[bgGlass, borderGlass, 'tw:rounded-2xl tw:overflow-hidden']">
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
          class="tw:w-20! tw:h-20! tw:text-2xl! tw:bg-primary-500/20! tw:text-primary-300!"
        />
        <div class="tw:text-center">
          <p class="tw:font-semibold tw:text-xl tw:leading-tight">{{ fullName }}</p>
          <p class="tw:text-muted tw:mt-0.5">@{{ username }}</p>
          <span class="tw:inline-block tw:mt-2 tw:text-[11px] tw:font-medium tw:px-2.5 tw:py-0.5 tw:rounded-full tw:bg-primary-500/10 tw:text-primary-400">
            {{ roleLabel }}
          </span>
        </div>
      </div>

      <prime-divider class="tw:w-[70%]! tw:mx-auto! tw:my-0!" />
      <div>
        <button
          class="tw:flex tw:items-center tw:gap-5 tw:w-full tw:px-4 tw:py-3.5 tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-left"
          @click="router.push({ name: 'profile' })"
        >
          <div class="tw:w-8 tw:h-8 tw:rounded-full tw:bg-primary-500/10 tw:flex tw:items-center tw:justify-center tw:shrink-0">
            <iconify icon="ph:user-bold" class="tw:text-primary-400" />
          </div>
          <span class="tw:flex-1 tw:text-lg tw:font-medium">{{ t('mobileProfile.editProfile') }}</span>
          <iconify icon="ph:caret-right-bold" class="tw:text-muted tw:text-sm" />
        </button>
      </div>
    </div>

    <!-- Appearance -->
    <div class="tw:space-y-2">
      <p class="tw:text-[11px] tw:font-semibold tw:uppercase tw:tracking-widest tw:text-muted tw:px-1">
        {{ t('mobileProfile.sectionAppearance') }}
      </p>
      <div :class="[bgGlass, borderGlass, 'tw:rounded-2xl']">
        <div class="tw:flex tw:items-center tw:gap-5 tw:px-4 tw:py-3.5">
          <div class="tw:w-8 tw:h-8 tw:rounded-full tw:bg-primary-500/10 tw:flex tw:items-center tw:justify-center tw:shrink-0">
            <iconify icon="ph:moon-bold" class="tw:text-primary-400" />
          </div>
          <span class="tw:flex-1 tw:text-lg tw:font-medium">{{ t('mobileProfile.darkMode') }}</span>
          <prime-toggle-switch :model-value="themeStore.isDark" @update:model-value="themeStore.toggleTheme()" />
        </div>
        <prime-divider class="tw:w-[70%]! tw:mx-auto! tw:my-0!" />
        <div class="tw:flex tw:items-center tw:gap-5 tw:px-4 tw:py-3.5">
          <div class="tw:w-8 tw:h-8 tw:rounded-full tw:bg-primary-500/10 tw:flex tw:items-center tw:justify-center tw:shrink-0">
            <iconify icon="ph:globe-bold" class="tw:text-primary-400" />
          </div>
          <span class="tw:flex-1 tw:text-lg tw:font-medium">{{ t('mobileProfile.language') }}</span>
          <div class="tw:flex tw:gap-1">
            <button
              v-for="l in locales"
              :key="l.code"
              class="tw:px-2.5 tw:py-1 tw:rounded-lg tw:text-xs tw:font-semibold tw:border-0 tw:cursor-pointer tw:transition-colors"
              :class="locale === l.code
                ? 'tw:bg-primary-500 tw:text-white'
                : 'tw:bg-black/5 tw:dark:bg-white/10 tw:text-muted'"
              @click="setLocale(l.code)"
            >
              {{ l.label }}
            </button>
          </div>
        </div>
        <prime-divider class="tw:w-[70%]! tw:mx-auto! tw:my-0!" />
        <div class="tw:flex tw:items-center tw:gap-5 tw:px-4 tw:py-3.5">
          <div class="tw:w-8 tw:h-8 tw:rounded-full tw:bg-primary-500/10 tw:flex tw:items-center tw:justify-center tw:shrink-0">
            <iconify icon="ph:paint-brush-bold" class="tw:text-primary-400" />
          </div>
          <span class="tw:flex-1 tw:text-lg tw:font-medium">{{ t('mobileProfile.accentColor') }}</span>
          <div class="tw:flex tw:gap-1.5">
            <button
              v-for="color in PRIMARY_COLORS"
              :key="color.name"
              :style="{ backgroundColor: color.hex }"
              class="tw:w-5 tw:h-5 tw:rounded-full tw:border-2 tw:cursor-pointer tw:transition-all tw:outline-none tw:p-0 tw:shrink-0"
              :class="themeStore.primaryColor === color.name
                ? 'tw:border-white tw:scale-125 tw:shadow-md'
                : 'tw:border-transparent tw:opacity-50 tw:hover:opacity-80'"
              @click="themeStore.applyPrimaryColor(color.name)"
            />
          </div>
        </div>
        <prime-divider class="tw:w-[70%]! tw:mx-auto! tw:my-0!" />
        <div class="tw:flex tw:items-center tw:gap-5 tw:px-4 tw:py-3.5">
          <div class="tw:w-8 tw:h-8 tw:rounded-full tw:bg-primary-500/10 tw:flex tw:items-center tw:justify-center tw:shrink-0">
            <iconify icon="ph:text-aa-bold" class="tw:text-primary-400" />
          </div>
          <span class="tw:flex-1 tw:text-lg tw:font-medium">{{ t('mobileProfile.font') }}</span>
          <prime-select
            :model-value="themeStore.font"
            :options="FONTS"
            option-label="label"
            option-value="name"
            size="small"
            class="tw:min-w-40"
            @update:model-value="themeStore.applyFont"
          >
            <template #value="{ value }">
              <span :style="{ fontFamily: FONTS.find(f => f.name === value)?.family }">{{ value }}</span>
            </template>
            <template #option="{ option }">
              <span :style="{ fontFamily: option.family }">{{ option.label }}</span>
            </template>
          </prime-select>
        </div>
      </div>
    </div>

    <!-- Account -->
    <div class="tw:space-y-2">
      <p class="tw:text-[11px] tw:font-semibold tw:uppercase tw:tracking-widest tw:text-muted tw:px-1">
        {{ t('mobileProfile.sectionAccount') }}
      </p>
      <div :class="[bgGlass, borderGlass, 'tw:rounded-2xl']">
        <button
          class="tw:flex tw:items-center tw:gap-5 tw:w-full tw:px-4 tw:py-3.5 tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-left tw:text-red-500"
          @click="auth.logout()"
        >
          <div class="tw:w-8 tw:h-8 tw:rounded-full tw:bg-red-500/10 tw:flex tw:items-center tw:justify-center tw:shrink-0">
            <iconify icon="ph:sign-out-bold" class="tw:text-red-500" />
          </div>
          <span class="tw:flex-1 tw:text-lg tw:font-semibold">{{ t('auth.logout') }}</span>
        </button>
      </div>
    </div>

  </div>
</template>
