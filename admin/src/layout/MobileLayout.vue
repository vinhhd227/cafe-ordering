<script setup>
import { computed, ref } from 'vue'
import NotificationBell from '@/components/NotificationBell.vue'
import { useAuthStore } from '@/stores/auth'
import { navGroups } from '@/layout/nav'

const { t } = useI18n()
const route = useRoute()
const auth = useAuthStore()

const moreVisible = ref(false)

const canAccess = (item) => {
  const user = auth.user
  if (!user) return false
  const isAdmin = user.roles?.includes('Admin')
  if (item.adminOnly) return isAdmin
  if (!item.requiredClaim) return true
  return isAdmin || user.permissions?.includes(item.requiredClaim)
}

// Flatten children into individual items for More drawer grid
const visibleNavGroups = computed(() =>
  navGroups
    .map(group => ({
      ...group,
      items: group.items
        .filter(canAccess)
        .flatMap(item =>
          item.children
            ? item.children.map(child => ({ ...child }))
            : [item]
        ),
    }))
    .filter(g => g.items.length > 0)
)

const bottomTabs = [
  { labelKey: 'nav.dashboard',       icon: 'ph:squares-four-bold',  to: { name: 'dashboard' } },
  { more: true, labelKey: 'nav.more', icon: 'ph:grid-nine-bold' },
  { cta: true, labelKey: 'nav.newOrder', icon: 'ph:plus-bold',      to: { name: 'ordersCreate' } },
  { bell: true, labelKey: 'nav.notifications' },
  { labelKey: 'nav.profile',         icon: 'ph:user-circle-bold',   to: { name: 'mobileProfile' } },
]

const isTabActive = (tab) => {
  if (!tab.to) return false
  const name = typeof route.name === 'string' ? route.name : ''
  if (tab.to.name === 'ordersList') return name === 'ordersList'
  return name === tab.to.name
}

const isNavItemActive = (item) => {
  const name = typeof route.name === 'string' ? route.name : ''
  return name === item.to?.name
}

const ctaTab = computed(() => bottomTabs.find(t => t.cta))

const fullName = computed(() => auth.user?.fullName || 'Staff')
const avatarLabel = computed(() => {
  const parts = fullName.value.split(' ').filter(Boolean)
  if (!parts.length) return 'ST'
  return parts.slice(0, 2).map(p => p[0]?.toUpperCase()).join('') || 'ST'
})
const avatarImage = computed(() => auth.user?.avatarUrl || auth.user?.avatar || null)
const roleLabel = computed(() => auth.user?.roles?.[0] || 'Staff')
</script>

<template>
  <div class="app-shell tw:flex tw:flex-col tw:min-h-screen">
    <prime-toast position="top-right" />
    <div class="app-background tw:fixed tw:inset-0 tw:z-0" />

    <!-- Main content -->
    <main
      :class="route.meta.hideBottomNav
        ? 'tw:h-dvh tw:z-10 tw:overflow-hidden tw:flex tw:flex-col'
        : 'tw:flex-1 tw:z-10 tw:px-4 tw:py-5 tw:pb-28'"
    >
      <router-view />
    </main>

    <!-- SVG clip path definition: bezier notch, scales with element via objectBoundingBox -->
    <svg width="0" height="0" style="position:absolute;overflow:hidden;">
      <defs>
        <clipPath id="mobile-nav-notch" clipPathUnits="objectBoundingBox">
          <path d="M 0,0 L 0.35,0 C 0.406,0 0.462,0.58 0.5,0.58 C 0.538,0.58 0.594,0 0.65,0 L 1,0 L 1,1 L 0,1 Z" />
        </clipPath>
      </defs>
    </svg>

    <!-- Bottom nav wrapper (FAB lives here, outside the clipped nav) -->
    <div v-if="!route.meta.hideBottomNav" class="tw:fixed tw:bottom-0 tw:inset-x-0 tw:z-20">

      <!-- FAB: outside the clipped nav so it's not cut -->
      <router-link
        v-if="ctaTab"
        :to="ctaTab.to"
        class="tw:absolute tw:left-1/2 tw:-translate-x-1/2 tw:z-10 tw:no-underline"
        style="bottom: calc(40px + env(safe-area-inset-bottom, 0px));"
      >
        <div
          class="tw:w-14 tw:h-14 tw:rounded-full tw:bg-emerald-500 tw:flex tw:items-center tw:justify-center tw:shadow-xl tw:shadow-emerald-500/40 tw:transition-transform tw:active:scale-95"
        >
          <iconify :icon="ctaTab.icon" class="tw:text-2xl tw:text-white" />
        </div>
      </router-link>

      <!-- Nav bar with bezier notch clip -->
      <nav
        class="app-header mobile-bottom-nav tw:backdrop-blur tw:flex"
        style="clip-path: url(#mobile-nav-notch);"
      >
        <template v-for="tab in bottomTabs" :key="tab.labelKey">
          <!-- Center slot: space for FAB + label below -->
          <div
            v-if="tab.cta"
            class="tw:flex-1 tw:flex tw:flex-col tw:items-center tw:justify-end tw:pb-2"
            style="padding-top: 40px;"
          >
            <span class="tw:text-[10px] tw:font-medium tw:text-emerald-400">
              {{ t(tab.labelKey) }}
            </span>
          </div>

          <!-- More tab: opens drawer -->
          <button
            v-else-if="tab.more"
            type="button"
            class="tw:flex-1 tw:flex tw:flex-col tw:items-center tw:justify-center tw:gap-1 tw:py-2.5 tw:transition-colors tw:min-h-14 tw:text-muted tw:bg-transparent tw:border-0 tw:cursor-pointer"
            :class="moreVisible ? 'tw:text-emerald-400' : 'tw:text-muted'"
            @click="moreVisible = true"
          >
            <iconify :icon="tab.icon" class="tw:text-[22px]" />
            <span class="tw:text-[10px] tw:font-medium tw:leading-none">{{ t(tab.labelKey) }}</span>
          </button>

          <!-- Notification bell tab -->
          <div
            v-else-if="tab.bell"
            class="tw:flex-1 tw:flex tw:flex-col tw:items-center tw:justify-center tw:gap-1 tw:py-2.5 tw:min-h-14"
          >
            <notification-bell />
            <span class="tw:text-[10px] tw:font-medium tw:leading-none tw:text-muted">{{ t(tab.labelKey) }}</span>
          </div>

          <!-- Regular tab -->
          <router-link
            v-else
            :to="tab.to"
            class="tw:flex-1 tw:flex tw:flex-col tw:items-center tw:justify-center tw:gap-1 tw:py-2.5 tw:no-underline tw:transition-colors tw:min-h-14"
            :class="isTabActive(tab) ? 'tw:text-emerald-400' : 'tw:text-muted'"
          >
            <div class="tw:relative tw:flex tw:items-center tw:justify-center">
              <iconify :icon="tab.icon" class="tw:text-[22px]" />
              <span
                v-if="isTabActive(tab) && tab.to?.name !== 'mobileProfile'"
                class="tw:absolute tw:-bottom-1.5 tw:left-1/2 tw:-translate-x-1/2 tw:w-1 tw:h-1 tw:rounded-full tw:bg-emerald-400"
              />
            </div>
            <span class="tw:text-[10px] tw:font-medium tw:leading-none tw:truncate tw:max-w-full tw:px-0.5">
              {{ t(tab.labelKey) }}
            </span>
          </router-link>
        </template>
      </nav>
    </div>

    <!-- More drawer -->
    <prime-drawer
      v-model:visible="moreVisible"
      position="bottom"
      :style="{ height: '88dvh' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
    >
      <template #header>
        <div class="tw:flex tw:items-center tw:justify-between tw:w-full">
          <div class="tw:flex tw:items-center tw:gap-3">
            <prime-avatar
              v-if="avatarImage"
              :image="avatarImage"
              shape="circle"
              class="tw:w-9! tw:h-9!"
            />
            <prime-avatar
              v-else
              :label="avatarLabel"
              shape="circle"
              class="tw:w-9! tw:h-9! tw:text-xs! tw:bg-emerald-500/20! tw:text-emerald-300!"
            />
            <div>
              <p class="tw:text-sm tw:font-semibold">{{ fullName }}</p>
              <p class="tw:text-xs tw:text-muted">{{ roleLabel }}</p>
            </div>
          </div>
          <prime-button severity="danger" variant="text" size="small" @click="auth.logout()">
            <iconify icon="ph:sign-out-bold" />
            <span>{{ t('auth.logout') }}</span>
          </prime-button>
        </div>
      </template>

      <div class="tw:overflow-y-auto tw:pb-6 tw:space-y-5">
        <div v-for="group in visibleNavGroups" :key="group.labelKey">
          <p
            class="tw:text-[10px] tw:font-semibold tw:uppercase tw:tracking-widest tw:text-muted tw:mb-2 tw:px-1"
          >
            {{ t(group.labelKey) }}
          </p>
          <div class="tw:grid tw:grid-cols-4 tw:gap-1.5">
            <router-link
              v-for="item in group.items"
              :key="item.labelKey"
              :to="item.to"
              class="tw:flex tw:flex-col tw:items-center tw:gap-1.5 tw:rounded-xl tw:p-3 tw:no-underline tw:transition-colors tw:active:scale-95"
              :class="isNavItemActive(item)
                ? 'tw:bg-emerald-500/10 tw:text-emerald-400'
                : 'tw:text-muted tw:active:bg-black/5 tw:dark:active:bg-white/5'"
              @click="moreVisible = false"
            >
              <iconify :icon="item.icon" class="tw:text-2xl" />
              <span class="tw:text-[10px] tw:font-medium tw:text-center tw:leading-tight">
                {{ t(item.labelKey) }}
              </span>
            </router-link>
          </div>
        </div>
      </div>
    </prime-drawer>
  </div>
</template>
