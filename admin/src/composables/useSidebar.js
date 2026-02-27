import { ref } from 'vue'

// Singleton refs - shared across all components
const isOpen = ref(false)       // mobile: sidebar visible or hidden
const isCollapsed = ref(false)  // desktop: full width or icon-only

export function useSidebar() {
  return {
    isOpen,
    isCollapsed,
    toggle: () => { isOpen.value = !isOpen.value },
    close: () => { isOpen.value = false },
    toggleCollapse: () => { isCollapsed.value = !isCollapsed.value },
  }
}
