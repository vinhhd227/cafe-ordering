export function useLocale() {
  const { locale } = useI18n()

  const locales = [
    { code: 'en', label: 'EN' },
    { code: 'vi', label: 'VI' },
  ]

  function setLocale(code) {
    locale.value = code
    localStorage.setItem('locale', code)
  }

  return { locale, locales, setLocale }
}
