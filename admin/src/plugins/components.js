export default {
  install(app) {
    // Auto-register local components so SFC templates can use them without imports.
    // Registers by file name (PascalCase), which Vue templates can also reference
    // in kebab-case: `WidgetSettingsButton` -> `<widget-settings-button />`.
    // Vite's glob patterns must be absolute-from-project-root (`/src/...`) or `./...`.
    const modules = import.meta.glob('/src/components/**/*.vue', { eager: true })
    for (const path in modules) {
      const mod = modules[path]
      const component = mod?.default
      if (!component) continue

      const file = path.split('/').pop() || ''
      const name = file.replace(/\\.vue$/i, '')
      if (!name) continue

      app.component(name, component)
    }
  },
}
