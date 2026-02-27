<script setup>
import AdminNavbar from "@/layout/Navbar.vue";
import AdminHeader from "@/layout/Header.vue";
import { useSidebar } from "@/composables/useSidebar";

const { isOpen, isCollapsed, close } = useSidebar();
</script>

<template>
  <div class="app-shell tw:min-h-screen">
    <prime-toast position="top-right" />

    <!-- Mobile overlay: click to close sidebar -->
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

    <admin-navbar />

    <!-- Main content: offset by sidebar width on desktop -->
    <div
      class="tw:flex tw:min-h-screen tw:flex-col tw:transition-all tw:duration-300"
      :class="isCollapsed ? 'tw:lg:ml-16' : 'tw:lg:ml-64'"
    >
      <admin-header />

      <main class="tw:flex-1 tw:px-8 tw:py-8">
        <div class="tw:max-w-screen-xl tw:mx-auto tw:w-full">
          <router-view />
        </div>
      </main>
    </div>
  </div>
</template>
