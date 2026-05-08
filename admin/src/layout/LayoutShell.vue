<script setup>
import { onMounted, onUnmounted, provide, ref } from 'vue'
import Layout from '@/layout/Layout.vue'
import MobileLayout from '@/layout/MobileLayout.vue'

const LG = 1024
const isMobile = ref(typeof window !== 'undefined' && window.innerWidth < LG)

const onResize = () => { isMobile.value = window.innerWidth < LG }
onMounted(() => window.addEventListener('resize', onResize))
onUnmounted(() => window.removeEventListener('resize', onResize))

provide('isMobile', isMobile)
</script>

<template>
  <MobileLayout v-if="isMobile" />
  <Layout v-else />
</template>
