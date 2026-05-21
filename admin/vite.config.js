import {fileURLToPath, URL} from 'node:url'

import {defineConfig} from 'vitest/config'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import AutoImport from 'unplugin-auto-import/vite'
import Components from 'unplugin-vue-components/vite'

import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
    plugins: [vue(),
        tailwindcss(),
        vueDevTools(),
        Components({
            dirs: ['src/components'],
            extensions: ['vue'],
            deep: true,
            dts: 'src/components.d.ts',
        }),
        AutoImport({
            imports: [
                'vue',
                'vue-router',
                'vee-validate',
                'vue-i18n',
                {
                    pinia: ['useStore'],
                },
                {
                    '@/layout/ui': ['btnIcon', 'inputCustom', 'labelCustom', 'passwordCustom', 'bgGlass', 'borderGlass', 'appCard', 'cardRing'],
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
            dirs: ['src/composables', 'src/stores', 'src/constants'],
            vueTemplate: true,
            dts: 'src/auto-imports.d.ts',
        }),],
    optimizeDeps: {
        include: ['html-to-image', 'jspdf'],
    },
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url)),
        },
    },
    test: {
        environment: 'happy-dom',
        globals: true,
        setupFiles: ['src/tests/setup.js'],
        coverage: {
            provider: 'v8',
            reporter: ['text', 'html'],
            include: ['src/**/*.js', 'src/**/*.vue'],
            exclude: ['src/tests/**', 'src/main.js', 'src/auto-imports.d.ts'],
        },
    },
    server: {
        host: true,
        port: parseInt(process.env.PORT || '5173'),
        allowedHosts: ['admin-demo.5-am-coffee.com'],
        proxy: {
            '/api': {
                target: process.env.VITE_API_PROXY_TARGET || 'http://localhost:5095',
                changeOrigin: true,
            },
            '/uploads': {
                target: process.env.VITE_API_PROXY_TARGET || 'http://localhost:5095',
                changeOrigin: true,
            },
        },
    },
})
