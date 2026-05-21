import '@/assets/styles/tailwind.css'
import { registerPlugins } from '@/plugins'


import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'

const p = window._setLoadPct ?? (() => {})

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
p(40)
registerPlugins(app)
p(60)
app.use(router)
p(70)

const authStore = useAuthStore(pinia)

;(async () => {
    await authStore.hydrateFromRefresh()
    p(90)
    const themeStore = useThemeStore(pinia)
    themeStore.init()
    p(100)
    app.mount('#app')
    await new Promise(r => setTimeout(r, 400)) // chờ Vue render + transition 100% xong
    const splash = document.getElementById('app-loading')
    if (splash) {
        if (window._loadingMsgInterval) clearInterval(window._loadingMsgInterval)
        splash.classList.add('hidden')
        splash.addEventListener('transitionend', () => splash.remove(), { once: true })
    }
})()
