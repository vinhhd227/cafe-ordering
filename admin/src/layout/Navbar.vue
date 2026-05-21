<script setup>
import { computed, reactive, watch } from "vue";
import { useRoute } from "vue-router";
import { useAuthStore } from "@/stores/auth";
import { navGroups } from "@/layout/nav";
import { useSidebar } from "@/composables/useSidebar";

const { t } = useI18n()
const route = useRoute();
const auth = useAuthStore();
const { isOpen, isCollapsed, close } = useSidebar();

const canAccess = (item) => {
  const user = auth.user
  if (!user) return false
  const isAdmin = user.roles?.includes('Admin')
  if (item.adminOnly) return isAdmin
  if (!item.requiredClaim) return true
  return isAdmin || user.permissions?.includes(item.requiredClaim)
}

const visibleNavGroups = computed(() =>
  navGroups
    .map(group => ({ ...group, items: group.items.filter(canAccess) }))
    .filter(group => group.items.length > 0)
)

const isActive = (to) => route.name === to.name;

const isParentActive = (item) => {
  const routeName = typeof route.name === "string" ? route.name : "";
  if (!routeName) return false;

  if (Array.isArray(item.matchNames) && item.matchNames.includes(routeName)) {
    return true;
  }

  if (typeof item.matchPrefix === "string" && routeName.startsWith(item.matchPrefix)) {
    return true;
  }

  return item.children?.some((child) => child.to?.name === routeName) ?? false;
};

const expandedMap = reactive({});

const isExpanded = (labelKey) => !!expandedMap[labelKey];

const toggleExpand = (labelKey) => { expandedMap[labelKey] = !expandedMap[labelKey]; };

const handleParentClick = (item) => {
  if (isCollapsed.value) {
    router.push(item.children[0].to);
    close();
  } else {
    toggleExpand(item.labelKey);
  }
};

watch(route, () => {
  visibleNavGroups.value.forEach(group => {
    group.items.forEach(item => {
      if (item.children && item.children.some(child => route.name === child.to.name)) {
        expandedMap[item.labelKey] = true;
      }
    });
  });
}, { immediate: true });

</script>

<template>
  <aside
    class="app-sidebar tw:fixed tw:inset-y-0 tw:left-0 tw:z-40 tw:flex tw:flex-col tw:border-r tw:backdrop-blur tw:transition-all tw:duration-300 tw:overflow-x-hidden"
    :class="[
      isCollapsed ? 'tw:w-16' : 'tw:w-64',
      isOpen ? 'tw:translate-x-0' : 'tw:-translate-x-full tw:lg:translate-x-0'
    ]"
  >

    <!-- Brand -->
    <div
      class="tw:shrink-0 tw:flex tw:border-b tw:transition-all tw:duration-300 tw:h-21"
      :class="isCollapsed ? 'tw:items-center tw:justify-center tw:px-2' : 'tw:flex-col tw:justify-center tw:px-6'"
      style="border-color: var(--app-border)"
    >
      <template v-if="!isCollapsed">
        <p class="tw:text-[10px] tw:font-semibold tw:uppercase tw:tracking-[0.4em] tw:text-primary-400">
          {{ t('sidebar.brand') }}
        </p>
        <h1 class="tw:mt-1 tw:text-base tw:font-semibold">{{ t('sidebar.title') }}</h1>
      </template>
      <iconify v-else icon="ph:coffee-bold" class="tw:text-xl tw:text-primary-400" />
    </div>

    <!-- Navigation groups -->
    <nav
      class="tw:flex-1 tw:overflow-y-auto tw:py-4 tw:transition-all tw:duration-300"
      :class="isCollapsed ? 'tw:px-2' : 'tw:px-3'"
    >
      <div
        v-for="group in visibleNavGroups"
        :key="group.labelKey"
        class="tw:mb-5 tw:last:mb-0"
      >
        <!-- Section label -->
        <template v-if="!isCollapsed">
          <p class="tw:mb-1.5 tw:px-3 tw:text-[10px] tw:font-semibold tw:uppercase tw:tracking-[0.15em] app-text-subtle">
            {{ t(group.labelKey) }}
          </p>
        </template>
        <div v-else class="tw:mb-2 tw:mx-1 tw:h-px" style="background: var(--app-border)" />

        <!-- Nav items -->
        <div class="tw:space-y-0.5">
          <template v-for="item in group.items" :key="item.labelKey">
            <!-- Item with children (expandable) -->
            <template v-if="item.children">
              <button
                type="button"
                class="tw:w-full tw:flex tw:items-center tw:rounded-lg tw:py-2.5 tw:text-sm tw:font-medium tw:transition-all tw:duration-150"
                :class="[
                  isCollapsed ? 'tw:justify-center tw:px-0' : 'tw:gap-3 tw:px-3',
                  isParentActive(item)
                    ? 'tw:bg-primary-500/10 tw:text-primary-400'
                    : 'tw:text-muted tw:hover:bg-black/5 tw:dark:hover:bg-white/5 tw:hover:text-gray-900 tw:dark:hover:text-white'
                ]"
                :title="isCollapsed ? t(item.labelKey) : undefined"
                @click="handleParentClick(item)"
              >
                <iconify :icon="item.icon" class="tw:shrink-0 tw:text-base" />
                <span v-if="!isCollapsed" class="tw:flex-1 tw:text-left">{{ t(item.labelKey) }}</span>
                <iconify
                  v-if="!isCollapsed"
                  icon="ph:caret-down-bold"
                  class="tw:shrink-0 tw:text-xs tw:transition-transform tw:duration-200"
                  :class="isExpanded(item.labelKey) ? 'tw:rotate-180' : ''"
                />
              </button>
              <!-- Children -->
              <div v-if="!isCollapsed && isExpanded(item.labelKey)" class="tw:ml-4 tw:mt-0.5 tw:space-y-0.5">
                <router-link
                  v-for="child in item.children"
                  :key="child.to.name"
                  :to="child.to"
                  class="tw:flex tw:items-center tw:rounded-lg tw:py-2 tw:px-3 tw:text-sm tw:font-medium tw:transition-all tw:duration-150 tw:no-underline tw:gap-3"
                  :class="isActive(child.to)
                    ? 'tw:bg-primary-500/10 tw:text-primary-400'
                    : 'tw:text-muted! tw:hover:bg-black/5 tw:dark:hover:bg-white/5 tw:hover:text-gray-900 tw:dark:hover:text-white'"
                  @click="close"
                >
                  <iconify :icon="child.icon" class="tw:shrink-0 tw:text-sm" />
                  <span>{{ t(child.labelKey) }}</span>
                </router-link>
              </div>
            </template>

            <!-- Regular item -->
            <router-link
              v-else
              :to="item.to"
              class="tw:flex tw:items-center tw:rounded-lg tw:py-2.5 tw:text-sm tw:font-medium tw:transition-all tw:duration-150 tw:no-underline"
              :class="[
                isCollapsed ? 'tw:justify-center tw:px-0' : 'tw:gap-3 tw:px-3',
                isActive(item.to)
                  ? 'tw:bg-primary-500/10 tw:text-primary-400'
                  : 'tw:text-muted tw:hover:bg-black/5 tw:dark:hover:bg-white/5 tw:hover:text-gray-900 tw:dark:hover:text-white'
              ]"
              :title="isCollapsed ? t(item.labelKey) : undefined"
              @click="close"
            >
              <iconify :icon="item.icon" class="tw:shrink-0 tw:text-base" />
              <span v-if="!isCollapsed">{{ t(item.labelKey) }}</span>
            </router-link>
          </template>
        </div>
      </div>
    </nav>


  </aside>
</template>
