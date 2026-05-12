<script setup>
import { defineAsyncComponent } from 'vue'

const { isMobile } = useBreakpoint()
const DesktopView = defineAsyncComponent(() => import('./Detail.desktop.vue'))
const MobileView  = defineAsyncComponent(() => import('./Detail.mobile.vue'))
</script>

<template>
  <component :is="isMobile ? MobileView : DesktopView" />
</template>
