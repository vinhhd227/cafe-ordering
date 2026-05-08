const LG = 1024

export function useBreakpoint() {
  // Reuse the isMobile ref provided by LayoutShell if available,
  // otherwise create a local one (e.g. for pages outside LayoutShell)
  const isMobile = inject(
    'isMobile',
    ref(typeof window !== 'undefined' && window.innerWidth < LG),
  )
  return { isMobile }
}
