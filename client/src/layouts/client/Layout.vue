<template>
  <div class="tw:bg-emerald-100">
    <AppHeader />
    <main>
      <router-view />
    </main>
    <AppFooter />
  </div>
</template>

<script setup>
import AppHeader from './Header.vue'
import AppFooter from './Footer.vue'
</script>