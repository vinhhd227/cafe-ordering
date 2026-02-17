import PrimeVuePlugin from './primeVue'
import IconifyPlugin from './iconify'

export function registerPlugins(app) {
    app.use(PrimeVuePlugin)
    app.use(IconifyPlugin)
}