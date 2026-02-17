import {fileURLToPath, URL} from 'node:url'

import {defineConfig} from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import AutoImport from 'unplugin-auto-import/vite'

import tailwindcss from '@tailwindcss/vite'
import Unfonts from 'unplugin-fonts/vite'

// https://vite.dev/config/
export default defineConfig({
    plugins: [vue(),
        tailwindcss(),
        vueDevTools(),
        AutoImport({
            imports: [
                'vue',
                {
                    pinia: ['useStore'],
                },
            ],
            dts: 'src/auto-imports.d.ts',
        }),
        Unfonts({
            google: {
                families: ['Figtree'],
            },
        }),],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url)),
        },
    },
    server: {
        host: true,
        port: 5173,
        proxy: {
            '/api': {
                target: 'http://localhost:5095',
                changeOrigin: true,
            },
        },
    },
})
