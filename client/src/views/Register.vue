<script setup>
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import { useToast } from 'primevue/usetoast'
import { useRouter } from 'vue-router'
import { useThemeStore } from '@/stores/theme'
import { register } from '@/services/auth.service.js'
import { inputCustom, labelCustom, passwordCustom } from '@/layout/ui'

const { t } = useI18n()
const { locale, locales, setLocale } = useLocale()
const toast = useToast()
const router = useRouter()
const themeStore = useThemeStore()
const submitError = ref('')
const registrationSuccess = ref(false)
const registeredUsername = ref('')
const countdown = ref(5)
let validateTimer, countdownTimer, redirectTimer

const countdownProgress = computed(() => ((5 - countdown.value) / 5) * 100)

const schema = computed(() =>
  toTypedSchema(
    z.object({
      fullName: z.string().min(1, t('register.validation.fullNameRequired')),
      phone:    z.string().min(1, t('register.validation.phoneRequired')),
      password:  z.string().min(1, t('register.validation.passwordRequired')),
      confirmPassword: z.string().min(1, t('register.validation.confirmPasswordRequired')),
      agree: z.boolean().refine((v) => v, { message: t('register.validation.agreeRequired') }),
    }).refine((d) => d.password === d.confirmPassword, {
      message: t('register.validation.passwordsMismatch'),
      path: ['confirmPassword'],
    })
  )
)

const { errors, defineField, handleSubmit, isSubmitting, meta, validate, values } = useForm({
  validationSchema: schema,
  initialValues: { fullName: '', phone: '', password: '', confirmPassword: '', agree: false },
})

const [fullName, fullNameAttrs] = defineField('fullName')
const [phone,    phoneAttrs]    = defineField('phone')
const [password,  passwordAttrs]  = defineField('password')
const [confirmPassword, confirmPasswordAttrs] = defineField('confirmPassword')
const [agree,     agreeAttrs]     = defineField('agree')

const strength = computed(() => {
  const v = password.value
  if (!v) return { score: 0, label: 'Empty' }
  let s = 0
  if (v.length >= 8)          s++
  if (/[A-Z]/.test(v))        s++
  if (/[a-z]/.test(v))        s++
  if (/\d/.test(v))           s++
  if (/[^A-Za-z0-9]/.test(v)) s++
  if (s <= 2) return { score: 1, label: 'Weak' }
  if (s === 3) return { score: 2, label: 'Fair' }
  if (s === 4) return { score: 3, label: 'Good' }
  return { score: 4, label: 'Strong' }
})

const canSubmit = computed(() => meta.value.valid)

const onSubmit = handleSubmit(async (formValues) => {
  submitError.value = ''
  try {
    await register({
      username: formValues.phone.trim(),
      email:    '',
      fullName: formValues.fullName.trim(),
      password: formValues.password,
    })
    registeredUsername.value = formValues.phone.trim()
    registrationSuccess.value = true
    countdownTimer = setInterval(() => {
      countdown.value -= 1
      if (countdown.value <= 0) clearInterval(countdownTimer)
    }, 1000)
    redirectTimer = setTimeout(() => router.push({ name: 'login' }), 5000)
  } catch (err) {
    const msg = err?.message || t('register.toast.errorFallback')
    submitError.value = msg
    toast.add({ severity: 'error', summary: t('register.toast.errorTitle'), detail: msg, life: 4000 })
  }
})

watch(values, () => {
  clearTimeout(validateTimer)
  validateTimer = setTimeout(() => validate(), 800)
}, { deep: true })

onBeforeUnmount(() => {
  clearTimeout(validateTimer)
  clearInterval(countdownTimer)
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

      <!-- ── Success screen ── -->
      <div
        v-if="registrationSuccess"
        class="auth-card app-panel tw:w-full tw:rounded-3xl tw:border tw:p-10 tw:shadow-2xl tw:backdrop-blur tw:text-center"
      >
        <div class="tw:flex tw:flex-col tw:items-center tw:gap-4">
          <iconify icon="ph:check-circle-bold" class="tw:text-6xl tw:text-emerald-400" />
          <div class="tw:space-y-1">
            <h2 class="tw:text-3xl tw:font-semibold">{{ t('register.success.title') }}</h2>
            <p class="tw:text-sm app-text-muted">
              {{ t('register.success.welcome', { username: registeredUsername }) }}
            </p>
          </div>
        </div>
        <div class="tw:mt-8 tw:space-y-3">
          <p class="tw:text-sm app-text-muted">
            {{ t('register.success.countdown', { seconds: countdown }) }}
          </p>
          <div class="app-panel tw:h-1.5 tw:w-full tw:overflow-hidden tw:rounded-full tw:border">
            <div
              class="tw:h-full tw:rounded-full tw:bg-orange-500 tw:transition-all tw:duration-1000 tw:ease-linear"
              :style="{ width: `${countdownProgress}%` }"
            />
          </div>
        </div>
        <div class="tw:mt-8">
          <router-link :to="{ name: 'login' }" class="tw:text-sm tw:text-emerald-400 hover:tw:text-emerald-300">
            {{ t('register.success.signInNow') }}
          </router-link>
        </div>
      </div>

      <!-- ── Register form ── -->
      <div v-else class="auth-card app-panel tw:w-full tw:rounded-3xl tw:border tw:p-8 tw:shadow-2xl tw:backdrop-blur">
        <!-- Header -->
        <div class="tw:space-y-3">
          <div class="tw:flex tw:items-center tw:justify-between">
            <span class="tw:text-xs tw:uppercase tw:tracking-[0.4em] tw:text-emerald-400">Cafe Ordering</span>
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
              <button type="button" class="tw:rounded-md tw:p-1 tw:text-xs tw:transition-all app-text-subtle hover:tw:bg-white/5" @click="themeStore.toggleTheme()">
                <iconify :icon="themeStore.isDark ? 'ph:sun-bold' : 'ph:moon-bold'" class="tw:text-sm" />
              </button>
            </div>
          </div>
          <h2 class="tw:text-3xl tw:font-semibold">{{ t('register.title') }}</h2>
          <p class="tw:text-sm app-text-muted">{{ t('register.subtitle') }}</p>
        </div>

        <prime-form class="tw:mt-8" @submit="onSubmit">
          <div class="tw:space-y-5">
            <!-- Full name -->
            <label for="fullName" :class="labelCustom">{{ t('register.fullName') }}</label>
            <prime-input-text
              id="fullName"
              type="text"
              fluid
              :placeholder="t('register.fullNamePlaceholder')"
              v-model="fullName"
              v-bind="fullNameAttrs"
              :class="inputCustom"
            />
            <prime-message v-if="errors.fullName" severity="error" size="small" variant="simple" :closable="false">
              {{ errors.fullName }}
            </prime-message>

            <!-- Phone -->
            <label for="phone" :class="labelCustom">{{ t('auth.phone') }}</label>
            <prime-input-text
              id="phone"
              type="tel"
              fluid
              :placeholder="t('register.phonePlaceholder')"
              v-model="phone"
              v-bind="phoneAttrs"
              :class="inputCustom"
            />
            <prime-message v-if="errors.phone" severity="error" size="small" variant="simple" :closable="false">
              {{ errors.phone }}
            </prime-message>

            <!-- Password -->
            <label for="password" :class="labelCustom">{{ t('auth.password') }}</label>
            <prime-password
              :placeholder="t('register.passwordPlaceholder')"
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
            <!-- Strength bar -->
            <div class="tw:space-y-1.5">
              <div class="app-panel tw:h-1.5 tw:w-full tw:overflow-hidden tw:rounded-full tw:border">
                <div
                  class="tw:h-full tw:rounded-full tw:transition-all"
                  :class="[
                    strength.score === 0 && 'tw:w-0',
                    strength.score === 1 && 'tw:w-1/4 tw:bg-rose-500',
                    strength.score === 2 && 'tw:w-1/2 tw:bg-amber-400',
                    strength.score === 3 && 'tw:w-3/4 tw:bg-orange-400',
                    strength.score === 4 && 'tw:w-full tw:bg-emerald-500',
                  ]"
                />
              </div>
              <p class="tw:text-xs app-text-subtle">{{ t('register.strength', { label: strength.label }) }}</p>
            </div>

            <!-- Confirm password -->
            <label for="confirmPassword" :class="labelCustom">{{ t('register.confirmPassword') }}</label>
            <prime-input-text
              id="confirmPassword"
              type="password"
              fluid
              :placeholder="t('register.confirmPasswordPlaceholder')"
              v-model="confirmPassword"
              v-bind="confirmPasswordAttrs"
              :class="inputCustom"
            />
            <prime-message v-if="errors.confirmPassword" severity="error" size="small" variant="simple" :closable="false">
              {{ errors.confirmPassword }}
            </prime-message>
          </div>

          <!-- Agree checkbox -->
          <div class="tw:mt-5 tw:flex tw:items-start tw:gap-2">
            <prime-checkbox
              id="agree"
              v-model="agree"
              v-bind="agreeAttrs"
              binary
              class="app-panel tw:mt-0.5"
              size="small"
            />
            <label for="agree" class="tw:text-sm app-text-muted">
              {{ t('register.agreePrefix') }}
              <router-link class="tw:text-emerald-400 hover:tw:text-emerald-300" to="/policy">{{ t('register.agreePolicy') }}</router-link>
            </label>
          </div>
          <prime-message v-if="errors.agree" severity="error" size="small" variant="simple" :closable="false" class="tw:mt-2">
            {{ errors.agree }}
          </prime-message>

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
            {{ t('register.submit') }}
          </prime-button>

          <div class="tw:mt-5 tw:flex tw:items-center tw:justify-between tw:text-sm">
            <span class="app-text-subtle">{{ t('register.alreadyHave') }}</span>
            <router-link class="tw:text-emerald-400 hover:tw:text-emerald-300" :to="{ name: 'login' }">
              {{ t('register.signIn') }}
            </router-link>
          </div>
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
