<script setup>
import AdminNavbar from "@/layout/Navbar.vue";
import AdminHeader from "@/layout/Header.vue";
</script>

<template>
  <div class="app-shell tw:min-h-screen">
    <div class="tw:flex tw:min-h-screen">
      <admin-navbar />

      <div class="tw:flex tw:flex-1 tw:flex-col">
        <admin-header />

        <main class="tw:flex-1 tw:px-8 tw:py-8">
          <router-view />
        </main>
      </div>
    </div>
  </div>
</template>
