import { createI18n } from 'vue-i18n'

const savedLocale = localStorage.getItem('locale') ?? 'en'

const enModules = import.meta.glob('./locales/en/*.json', { eager: true })
const viModules = import.meta.glob('./locales/vi/*.json', { eager: true })

const merge = (modules) =>
  Object.values(modules).reduce((acc, mod) => ({ ...acc, ...mod.default }), {})

export const i18n = createI18n({
  legacy: false,
  locale: savedLocale,
  fallbackLocale: 'en',
  messages: { en: merge(enModules), vi: merge(viModules) },
})
