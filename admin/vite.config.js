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
                'vue-router',
                'vee-validate',
                {
                    pinia: ['useStore'],
                },
                {
                    '@/layout/ui': ['btnIcon', 'inputCustom', 'labelCustom', 'passwordCustom'],
                },
                {
                    'primevue/usetoast': ['useToast'],
                    'primevue/useconfirm': ['useConfirm'],
                },
                {
                    '@vee-validate/zod': ['toTypedSchema'],
                },
                {
                    zod: ['z'],
                },
            ],
            dirs: ['src/composables', 'src/stores'],
            vueTemplate: true,
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
