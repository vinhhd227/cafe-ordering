<script setup>
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import { useToast } from 'primevue/usetoast'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth.js'
import { useThemeStore } from '@/stores/theme'
import { inputCustom, labelCustom, passwordCustom } from '@/layout/ui'

const { t } = useI18n()
const { locale, locales, setLocale } = useLocale()
const toast = useToast()
const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const themeStore = useThemeStore()
const submitError = ref('')
let validateTimer
let redirectTimer

const schema = computed(() =>
  toTypedSchema(
    z.object({
      username: z.string().min(1, t('login.validation.usernameRequired')),
      password: z.string().min(1, t('register.validation.passwordRequired')),
      rememberMe: z.boolean(),
    })
  )
)

const { errors, defineField, handleSubmit, isSubmitting, meta, validate, values } = useForm({
  validationSchema: schema,
  initialValues: { username: '', password: '', rememberMe: false },
})

const [username, usernameAttrs] = defineField('username')
const [password, passwordAttrs] = defineField('password')
const [rememberMe, rememberMeAttrs] = defineField('rememberMe')

const canSubmit = computed(() => meta.value.valid)

const onSubmit = handleSubmit(async (formValues) => {
  submitError.value = ''
  try {
    await authStore.login(formValues)
    toast.add({
      severity: 'success',
      summary: t('login.toast.successTitle'),
      detail: t('login.toast.successDetail'),
      life: 3000,
    })
    redirectTimer = setTimeout(() => {
      const redirect = route.query.redirect
      router.push(redirect ? String(redirect) : { name: 'tableSelect' })
    }, 500)
  } catch (err) {
    const msg = err?.message || t('login.toast.errorFallback')
    submitError.value = msg
    toast.add({ severity: 'error', summary: t('login.toast.errorTitle'), detail: msg, life: 4000 })
  }
})

watch(values, () => {
  clearTimeout(validateTimer)
  validateTimer = setTimeout(() => validate(), 800)
}, { deep: true })

onBeforeUnmount(() => {
  clearTimeout(validateTimer)
  clearTimeout(redirectTimer)
})
</script>

<template>
  <prime-toast position="top-right" />
  <section
    class="app-shell tw:relative tw:flex tw:min-h-screen tw:items-center tw:justify-center tw:overflow-hidden"
    :class="{ 'app-dark': themeStore.isDark }"
  >
    <div class="app-background tw:absolute tw:inset-0 tw:z-0" />

    <div class="tw:relative tw:z-10 tw:w-full tw:max-w-md tw:p-6">
      <div class="auth-card app-panel tw:w-full tw:rounded-3xl tw:border tw:p-8 tw:shadow-2xl tw:backdrop-blur">
        <!-- Header -->
        <div class="tw:space-y-3">
          <div class="tw:flex tw:items-center tw:justify-between">
            <span class="tw:text-xs tw:uppercase tw:tracking-[0.4em] tw:text-emerald-400">
              Cafe Ordering
            </span>
            <div class="tw:flex tw:items-center tw:gap-1">
              <button
                v-for="l in locales"
                :key="l.code"
                type="button"
                class="tw:rounded-md tw:px-2 tw:py-1 tw:text-xs tw:font-semibold tw:transition-all"
                :class="locale === l.code ? 'tw:bg-emerald-500/15 tw:text-emerald-400' : 'app-text-subtle hover:tw:bg-white/5'"
                @click="setLocale(l.code)"
              >
                {{ l.label }}
              </button>
              <button
                type="button"
                class="tw:rounded-md tw:p-1 tw:text-xs tw:transition-all app-text-subtle hover:tw:bg-white/5"
                @click="themeStore.toggleTheme()"
              >
                <iconify :icon="themeStore.isDark ? 'ph:sun-bold' : 'ph:moon-bold'" class="tw:text-sm" />
              </button>
            </div>
          </div>
          <h2 class="tw:text-3xl tw:font-semibold">{{ t('login.title') }}</h2>
          <p class="tw:text-sm app-text-muted">{{ t('login.subtitle') }}</p>
        </div>

        <!-- Form -->
        <prime-form class="tw:mt-8" @submit="onSubmit">
          <div class="tw:space-y-5">
            <label for="username" :class="labelCustom">{{ t('auth.phone') }}</label>
            <prime-input-text
              id="username"
              type="tel"
              fluid
              :placeholder="t('login.phonePlaceholder')"
              v-model="username"
              v-bind="usernameAttrs"
              :class="inputCustom"
            />
            <prime-message v-if="errors.username" severity="error" size="small" variant="simple" :closable="false">
              {{ errors.username }}
            </prime-message>

            <label for="password" :class="labelCustom">{{ t('auth.password') }}</label>
            <prime-password
              placeholder="••••••••"
              inputId="password"
              toggleMask
              showClear
              :feedback="false"
              fluid
              v-model="password"
              v-bind="passwordAttrs"
              :pt="passwordCustom"
            />
            <prime-message v-if="errors.password" severity="error" size="small" variant="simple" :closable="false">
              {{ errors.password }}
            </prime-message>
          </div>

          <div class="tw:mt-5 tw:flex tw:items-center tw:gap-2">
            <prime-checkbox
              id="rememberMe"
              v-model="rememberMe"
              v-bind="rememberMeAttrs"
              binary
              class="app-panel"
              size="small"
            />
            <label for="rememberMe" class="tw:text-sm app-text-muted">{{ t('login.rememberMe') }}</label>
          </div>

          <prime-message v-if="submitError" severity="error" size="small" variant="simple" :closable="false" class="tw:mt-4">
            {{ submitError }}
          </prime-message>

          <prime-button
            type="submit"
            class="tw:mt-6 tw:w-full tw:rounded-xl tw:border-0!"
            :class="canSubmit && !isSubmitting ? 'tw:shadow-lg tw:shadow-emerald-500/20' : 'tw:cursor-not-allowed!'"
            :disabled="isSubmitting || !canSubmit"
            :loading="isSubmitting"
          >
            {{ t('login.submit') }}
          </prime-button>

          <div class="tw:mt-5 tw:flex tw:items-center tw:justify-between tw:text-sm">
            <span class="app-text-subtle">{{ t('login.newUser') }}</span>
            <router-link class="tw:text-emerald-400 hover:tw:text-emerald-300" :to="{ name: 'register' }">
              {{ t('login.register') }}
            </router-link>
          </div>

          <div class="tw:mt-6 tw:flex tw:items-center tw:gap-3">
            <div class="tw:h-px tw:flex-1 app-border tw:border-t" />
            <span class="tw:text-xs app-text-subtle">{{ t('login.or') }}</span>
            <div class="tw:h-px tw:flex-1 app-border tw:border-t" />
          </div>

          <prime-button
            type="button"
            severity="secondary"
            variant="outlined"
            class="tw:mt-4 tw:w-full tw:rounded-xl"
            @click="router.push({ name: 'tableSelect' })"
          >
            {{ t('login.continueAsGuest') }}
          </prime-button>
        </prime-form>
      </div>
    </div>
  </section>
</template>

<style scoped>
.auth-card {
  animation: fade-up 0.5s ease both;
}

@keyframes fade-up {
  from { opacity: 0; transform: translateY(16px); }
  to   { opacity: 1; transform: translateY(0); }
}
</style>
