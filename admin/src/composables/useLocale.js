export function useLocale() {
  const { locale } = useI18n()

  const locales = [
    { code: 'en', label: 'EN', name: 'English' },
    { code: 'vi', label: 'VI', name: 'Tiếng Việt' },
  ]

  function setLocale(code) {
    locale.value = code
    localStorage.setItem('locale', code)
  }

  return { locale, locales, setLocale }
}
