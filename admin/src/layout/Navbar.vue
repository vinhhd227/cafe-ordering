<script setup>
import { navItems } from "@/layout/nav";
</script>

<template>
  <aside class="app-sidebar tw:w-64 tw:border-r tw:p-6 tw:backdrop-blur">
    <div class="tw:space-y-6">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.4em] tw:text-emerald-300">
          Cafe Ordering
        </p>
        <h1 class="tw:mt-2 tw:text-lg tw:font-semibold">Admin Panel</h1>
      </div>

      <nav class="tw:space-y-2">
        <router-link
          v-for="item in navItems"
          :key="item.to"
          class="tw:block tw:rounded-xl tw:px-4 tw:py-2 tw:text-sm app-text-muted tw:transition hover:tw:bg-white/5 hover:tw:text-white"
          :to="item.to"
        >
          {{ item.label }}
        </router-link>
      </nav>
    </div>
  </aside>
</template>
