import '@/assets/styles/tailwind.css'
import { registerPlugins } from '@/plugins'


import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'

const app = createApp(App)

app.use(createPinia())
app.use(router)

registerPlugins(app)

app.mount('#app')