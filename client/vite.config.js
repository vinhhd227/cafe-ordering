import {fileURLToPath, URL} from 'node:url'

import {defineConfig} from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import AutoImport from 'unplugin-auto-import/vite'

import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
    plugins: [vue(),
        tailwindcss(),
        vueDevTools(),
        AutoImport({
            imports: [
                'vue',
                'vue-i18n',
                {
                    pinia: ['useStore'],
                },
            ],
            dirs: ['src/composables'],
            vueTemplate: true,
            dts: 'src/auto-imports.d.ts',
        }),],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url)),
        },
    },
    server: {
        host: true,
        port: parseInt(process.env.PORT || '5174'),
        allowedHosts: ['demo.5-am-coffee.com'],
        proxy: {
            '/api': {
                target: 'http://localhost:5095',
                changeOrigin: true,
            },
            '/uploads': {
                target: 'http://localhost:5095',
                changeOrigin: true,
            },
        },
    },
})
