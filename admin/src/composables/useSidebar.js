import { ref } from 'vue'

const LG = 1024 // Tailwind lg breakpoint

// Singleton refs - shared across all components
const isOpen = ref(false)
const isCollapsed = ref(false)

// On mobile, collapsed icon-only mode doesn't apply — reset when resizing down
const _onResize = () => {
  if (window.innerWidth < LG) isCollapsed.value = false
}
window.addEventListener('resize', _onResize)

export function useSidebar() {
  return {
    isOpen,
    isCollapsed,
    toggle: () => { isOpen.value = !isOpen.value },
    close: () => { isOpen.value = false },
    toggleCollapse: () => { isCollapsed.value = !isCollapsed.value },
  }
}
