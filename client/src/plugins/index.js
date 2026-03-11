import PrimeVuePlugin from './primeVue'
import IconifyPlugin from './iconify'
import I18nPlugin from './i18n'

export function registerPlugins(app) {
    app.use(I18nPlugin)
    app.use(PrimeVuePlugin)
    app.use(IconifyPlugin)
}