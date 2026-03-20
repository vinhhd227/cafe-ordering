import { ref } from 'vue'

const isOpen = ref(false)
const isCollapsed = ref(false)

export function useSidebar() {
  return {
    isOpen,
    isCollapsed,
    toggle: () => { isOpen.value = !isOpen.value },
    close: () => { isOpen.value = false },
    toggleCollapse: () => { isCollapsed.value = !isCollapsed.value },
  }
}
