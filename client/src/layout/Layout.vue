<script setup>
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useSidebar } from '@/composables/useSidebar'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'

const { t } = useI18n()
const { locale, locales, setLocale } = useLocale()
const { isOpen, isCollapsed, toggle, close, toggleCollapse } = useSidebar()
const themeStore = useThemeStore()
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const navItems = computed(() => [
  {
    labelKey: 'nav.orders',
    icon: 'ph:receipt-bold',
    to: route.name === 'order' && route.params.tableId
      ? { name: 'order', params: { tableId: route.params.tableId }, query: { code: route.query.code } }
      : { name: 'order' },
  },
])

const isActive = (to) => route.name === to.name

const guestId = (() => {
  const key = 'guest_id'
  let id = localStorage.getItem(key)
  if (!id) {
    id = 'Guest-' + Math.random().toString(36).slice(2, 6).toUpperCase()
    localStorage.setItem(key, id)
  }
  return id
})()

const fullName = computed(() => auth.user?.fullName || auth.user?.email || guestId)

const avatarLabel = computed(() => {
  if (auth.isAuthenticated) {
    const name = auth.user?.fullName || auth.user?.email || ''
    return name[0]?.toUpperCase() || 'U'
  }
  const parts = fullName.value.split('-')
  return parts[0]?.[0]?.toUpperCase() || 'G'
})

const profileMenu = ref()

const profileItems = computed(() => {
  if (auth.isAuthenticated) {
    return [
      {
        label: t('auth.logout'),
        command: async () => {
          await auth.logout()
          router.push({ name: 'login' })
        },
      },
    ]
  }
  return [
    {
      label: t('auth.login'),
      command: () => router.push({ name: 'login' }),
    },
    {
      label: t('auth.register'),
      command: () => router.push({ name: 'register' }),
    },
  ]
})

const toggleProfileMenu = (event) => profileMenu.value?.toggle(event)
</script>

<template>
  <div class="app-shell tw:min-h-screen" :class="{ 'app-dark': themeStore.isDark }">
    <prime-toast position="top-right" />
    <div class="app-background tw:fixed tw:inset-0 tw:z-0" />

    <!-- Mobile overlay -->
    <transition
      enter-active-class="tw:transition-opacity tw:duration-300"
      enter-from-class="tw:opacity-0"
      enter-to-class="tw:opacity-100"
      leave-active-class="tw:transition-opacity tw:duration-300"
      leave-from-class="tw:opacity-100"
      leave-to-class="tw:opacity-0"
    >
      <div
        v-if="isOpen"
        class="tw:fixed tw:inset-0 tw:z-30 tw:bg-black/50 tw:lg:hidden"
        @click="close"
      />
    </transition>

    <!-- Sidebar -->
    <aside
      class="app-sidebar tw:fixed tw:inset-y-0 tw:left-0 tw:z-40 tw:flex tw:flex-col tw:border-r tw:backdrop-blur tw:transition-all tw:duration-300 tw:overflow-x-hidden"
      :class="[
        isCollapsed ? 'tw:w-16' : 'tw:w-64',
        isOpen ? 'tw:translate-x-0' : 'tw:-translate-x-full tw:lg:translate-x-0'
      ]"
    >
      <!-- Brand -->
      <div
        class="tw:shrink-0 tw:flex tw:border-b tw:transition-all tw:duration-300 tw:h-[5.25rem]"
        :class="isCollapsed ? 'tw:items-center tw:justify-center tw:px-2' : 'tw:flex-col tw:justify-center tw:px-6'"
        style="border-color: var(--app-border)"
      >
        <template v-if="!isCollapsed">
          <p class="tw:text-[10px] tw:font-semibold tw:uppercase tw:tracking-[0.4em] tw:text-primary-400">
            {{ t('layout.brand') }}
          </p>
          <h1 class="tw:mt-1 tw:text-base tw:font-semibold">{{ t('layout.menuPanel') }}</h1>
        </template>
        <iconify v-else icon="ph:coffee-bold" class="tw:text-xl tw:text-primary-400" />
      </div>

      <!-- Nav -->
      <nav
        class="tw:flex-1 tw:overflow-y-auto tw:py-4 tw:transition-all tw:duration-300"
        :class="isCollapsed ? 'tw:px-2' : 'tw:px-3'"
      >
        <div class="tw:space-y-0.5">
          <router-link
            v-for="item in navItems"
            :key="item.to.name"
            :to="item.to"
            class="tw:flex tw:items-center tw:rounded-lg tw:py-2.5 tw:text-sm tw:font-medium tw:transition-all tw:duration-150 tw:no-underline"
            :class="[
              isCollapsed ? 'tw:justify-center tw:px-0' : 'tw:gap-3 tw:px-3',
              isActive(item.to)
                ? 'tw:bg-primary-500/10 tw:text-primary-400'
                : 'tw:text-muted tw:hover:bg-white/5 tw:hover:text-white'
            ]"
            :title="isCollapsed ? t(item.labelKey) : undefined"
            @click="close"
          >
            <iconify :icon="item.icon" class="tw:shrink-0 tw:text-base" />
            <span v-if="!isCollapsed">{{ t(item.labelKey) }}</span>
          </router-link>
        </div>
      </nav>

      <!-- Language + Theme toggle -->
      <div
        class="tw:shrink-0 tw:border-t tw:px-3 tw:py-2 tw:flex tw:gap-1"
        style="border-color: var(--app-border)"
        :class="isCollapsed ? 'tw:justify-center' : ''"
      >
        <template v-if="!isCollapsed">
          <button
            v-for="l in locales"
            :key="l.code"
            type="button"
            class="tw:flex-1 tw:rounded-lg tw:py-1 tw:text-xs tw:font-semibold tw:transition-all tw:duration-150"
            :class="locale === l.code
              ? 'tw:bg-primary-500/15 tw:text-primary-400'
              : 'app-text-subtle tw:hover:bg-white/5 tw:hover:text-white'"
            @click="setLocale(l.code)"
          >
            {{ l.label }}
          </button>
          <button
            type="button"
            class="tw:rounded-lg tw:px-2 tw:py-1 tw:text-xs tw:transition-all tw:duration-150 app-text-subtle tw:hover:bg-white/5 tw:hover:text-white"
            :title="themeStore.isDark ? 'Switch to light' : 'Switch to dark'"
            @click="themeStore.toggleTheme()"
          >
            <iconify :icon="themeStore.isDark ? 'ph:sun-bold' : 'ph:moon-bold'" class="tw:text-sm" />
          </button>
        </template>
        <button
          v-else
          type="button"
          class="tw:rounded-lg tw:p-1.5 tw:text-xs tw:transition-all tw:duration-150 app-text-subtle tw:hover:bg-white/5 tw:hover:text-white"
          :title="themeStore.isDark ? 'Switch to light' : 'Switch to dark'"
          @click="themeStore.toggleTheme()"
        >
          <iconify :icon="themeStore.isDark ? 'ph:sun-bold' : 'ph:moon-bold'" class="tw:text-sm" />
        </button>
      </div>

      <!-- User profile footer -->
      <div class="tw:shrink-0 tw:border-t tw:p-3" style="border-color: var(--app-border)">
        <button
          type="button"
          class="tw:flex tw:w-full tw:items-center tw:rounded-xl tw:py-2.5 tw:transition-all tw:duration-150 tw:hover:bg-white/5"
          :class="isCollapsed ? 'tw:justify-center tw:px-0' : 'tw:gap-3 tw:px-3'"
          :title="isCollapsed ? fullName : undefined"
          @click="toggleProfileMenu"
        >
          <prime-avatar
            :label="avatarLabel"
            shape="circle"
            size="normal"
            class="tw:shrink-0 tw:bg-primary-500/20 tw:text-primary-300"
          />
          <template v-if="!isCollapsed">
            <div class="tw:min-w-0 tw:flex-1 tw:text-left">
              <p class="tw:truncate tw:text-sm tw:font-semibold">{{ fullName }}</p>
              <p class="tw:text-xs app-text-subtle">{{ auth.isAuthenticated ? (auth.user?.email || '') : t('layout.guestLabel') }}</p>
            </div>
            <iconify icon="ph:dots-three-bold" class="tw:shrink-0 tw:text-base app-text-subtle" />
          </template>
        </button>
        <prime-menu ref="profileMenu" :model="profileItems" popup />
      </div>
    </aside>

    <!-- Main content -->
    <div
      class="tw:flex tw:min-h-screen tw:flex-col tw:transition-all tw:duration-300"
      :class="isCollapsed ? 'tw:lg:ml-16' : 'tw:lg:ml-64'"
    >
      <!-- Header -->
      <header
        class="app-header tw:flex tw:shrink-0 tw:items-center tw:justify-between tw:border-b tw:px-4 tw:h-[5.25rem] tw:backdrop-blur tw:sticky tw:top-0 tw:z-20 lg:tw:px-8"
      >
        <div class="tw:flex tw:items-center tw:gap-3">
          <!-- Mobile: toggle sidebar -->
          <button
            type="button"
            class="tw:flex tw:h-9 tw:w-9 tw:items-center tw:justify-center tw:rounded-lg tw:transition-colors tw:hover:bg-white/5 tw:lg:hidden"
            @click="toggle"
          >
            <iconify icon="ph:list-bold" class="tw:text-lg" />
          </button>
          <!-- Desktop: collapse sidebar -->
          <button
            type="button"
            class="tw:hidden tw:lg:flex tw:h-9 tw:w-9 tw:items-center tw:justify-center tw:rounded-lg tw:transition-colors tw:hover:bg-white/5"
            @click="toggleCollapse"
          >
            <iconify icon="ph:sidebar-simple-bold" class="tw:text-lg" />
          </button>
          <div>
            <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-subtle">
              {{ t('layout.workspace') }}
            </p>
            <h2 class="tw:text-lg tw:font-semibold">{{ t('layout.orderGreeting') }}</h2>
          </div>
        </div>
      </header>

      <main class="tw:flex-1 tw:px-4 tw:py-4 tw:sm:px-8 tw:sm:py-8 tw:z-10">
        <div class="tw:max-w-10xl tw:px-5 tw:mx-auto tw:w-full">
          <router-view />
        </div>
      </main>
    </div>
  </div>
</template>
