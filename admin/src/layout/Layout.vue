<script setup>
import { watch } from 'vue'
import { useRoute } from 'vue-router'
import AdminNavbar from "@/layout/Navbar.vue";
import AdminHeader from "@/layout/Header.vue";
import { useSidebar } from "@/composables/useSidebar";

const { isOpen, isCollapsed, close } = useSidebar();
const route = useRoute();
watch(() => route.path, () => close());
</script>

<template>
  <div class="app-shell tw:min-h-screen">
    <prime-toast position="top-right" />
    <div class="app-bg tw:fixed tw:inset-0 tw:z-0" />
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

      <main class="tw:flex-1 tw:px-4 tw:py-5 tw:sm:px-8 tw:sm:py-8 tw:z-10">
        <div class="tw:max-w-10xl tw:px-0 tw:sm:px-5 tw:mx-auto tw:w-full">
          <router-view />
        </div>
      </main>

      <footer class="tw:z-10 tw:px-4 tw:sm:px-8 tw:py-4 tw:flex tw:items-center tw:justify-center tw:gap-1.5 tw:text-xs tw:text-muted tw:select-none">
        <span class="tw:flex tw:items-center tw:gap-1">
          © 2026 Crafted by Vestow
          <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" class="tw:shrink-0 tw:opacity-70"><path fill="currentColor" fill-rule="evenodd" d="M11.75 6.406c-1.48 0-1.628.157-2.394.157C8.718 6.563 6.802 5 5.845 5S3.77 5.563 3.77 7.188v1.875c.002.492.18 2 .88 1.597c-.827.978-.91 2.119-.899 3.223c-.223.064-.45.137-.671.212c-.684.234-1.41.532-1.737.744a.75.75 0 0 0 .814 1.26c.156-.101.721-.35 1.408-.585l.228-.075c.046.433.161.83.332 1.19l-.024.013c-.41.216-.79.465-1.032.623l-.113.074a.75.75 0 1 0 .814 1.26l.131-.086c.245-.16.559-.365.901-.545q.12-.064.231-.116C6.763 19.475 9.87 20 11.75 20s4.987-.525 6.717-2.148q.11.052.231.116c.342.18.656.385.901.545l.131.086a.75.75 0 0 0 .814-1.26l-.113-.074a13 13 0 0 0-1.032-.623l-.024-.013c.171-.36.286-.757.332-1.19l.228.075c.687.235 1.252.484 1.409.585a.75.75 0 0 0 .813-1.26c-.327-.212-1.053-.51-1.736-.744a16 16 0 0 0-.672-.213c.012-1.104-.072-2.244-.9-3.222c.7.403.88-1.105.881-1.598V7.188C19.73 5.563 18.613 5 17.655 5c-.957 0-2.873 1.563-3.51 1.563c-.767 0-.915-.157-2.395-.157m-.675 9.194c.202-.069.441-.1.675-.1s.473.031.676.1c.1.034.22.088.328.174a.62.62 0 0 1 .246.476c0 .23-.139.39-.246.476s-.229.14-.328.174c-.203.069-.442.1-.676.1s-.473-.031-.675-.1a1.1 1.1 0 0 1-.329-.174a.62.62 0 0 1-.246-.476c0-.23.139-.39.246-.476s.23-.14.329-.174m2.845-3.1c.137-.228.406-.5.81-.5s.674.272.81.5c.142.239.21.527.21.813s-.068.573-.21.811c-.136.229-.406.501-.81.501s-.673-.272-.81-.5a1.6 1.6 0 0 1-.21-.812c0-.286.068-.574.21-.812m-5.96 0c.137-.228.406-.5.81-.5s.674.272.81.5c.142.239.21.527.21.813s-.068.573-.21.811c-.136.229-.406.501-.81.501s-.673-.272-.81-.5a1.6 1.6 0 0 1-.21-.812c0-.286.068-.574.21-.812" clip-rule="evenodd"/></svg>
          × Claude
        </span>
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" class="tw:shrink-0 tw:opacity-70"><path fill="currentColor" d="M4.5 6h15v5H22v2h-2.5v3h-1v2H17v-2h-1v2h-1.5v-2h-5v2H8v-2H7v2H5.5v-2h-1v-3H2v-2h2.5ZM7 8v3h1V8Zm9 0v3h1V8Z"/></svg>
      </footer>
    </div>
  </div>
</template>