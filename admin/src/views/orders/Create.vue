<script setup>
const { isMobile } = useBreakpoint()
const DesktopView = defineAsyncComponent(() => import('./Create.desktop.vue'))
const MobileView  = defineAsyncComponent(() => import('./Create.mobile.vue'))
</script>

<template>
  <component :is="isMobile ? MobileView : DesktopView" />
</template>
