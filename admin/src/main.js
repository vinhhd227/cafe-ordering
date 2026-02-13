import '@/assets/styles/tailwind.css'
import { registerPlugins } from '@/plugins'


import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import { useAuthStore } from '@/stores/auth'
import { useThemeStore } from '@/stores/theme'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
registerPlugins(app)
app.use(router)

const authStore = useAuthStore(pinia)
authStore.hydrateFromRefresh()
const themeStore = useThemeStore(pinia)
themeStore.init()

app.mount('#app')
