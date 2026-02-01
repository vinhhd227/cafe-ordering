<script setup>
import { computed, ref, watch } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import { useToast } from 'primevue/usetoast'
import { login } from '@/services/auth.service'
import router from "@/router/index.js";

const toast = useToast()
const submitError = ref('')
let validateTimer

const schema = toTypedSchema(
  z.object({
    email: z.string().min(1, 'Email is required.').email('Email is invalid.'),
    password: z.string().min(1, 'Password is required.'),
    rememberMe: z.boolean(),
  })
)

const { errors, defineField, handleSubmit, isSubmitting, meta, validate, values } = useForm({
  validationSchema: schema,
  initialValues: {
    email: '',
    password: '',
    rememberMe: false,
  },
})

const [email, emailAttrs] = defineField('email')
const [password, passwordAttrs] = defineField('password')
const [rememberMe, rememberMeAttrs] = defineField('rememberMe')

const canSubmit = computed(() => { console.log(meta.value); return meta.value.valid } )

const onSubmit = handleSubmit(async (formValues) => {
  submitError.value = ''
  try {
    const response = await login({
      email: formValues.email.trim(),
      password: formValues.password,
      rememberMe: formValues.rememberMe,
    })

    // Save token to localStorage or sessionStorage based on rememberMe
    const storage = formValues.rememberMe ? localStorage : sessionStorage
    storage.setItem('access_token', response.accessToken)
    storage.setItem('token_expires_at', response.expiresAt)
    storage.setItem('user_roles', JSON.stringify(response.roles))

    toast.add({
      severity: 'success',
      summary: 'Login successful',
      detail: 'Welcome back! Redirecting to dashboard...',
      life: 3000,
    })

    // Redirect to dashboard after 500ms
    setTimeout(() => {
      router.push({ name: 'adminDashboard' })
    }, 500)
  } catch (err) {
    console.error(err)
    const errorMessage = err?.response?.data?.message || 'Login failed. Please try again.'
    submitError.value = errorMessage
    toast.add({
      severity: 'error',
      summary: 'Login failed',
      detail: errorMessage,
      life: 3000,
    })
  }
})

watch(
  values,
  () => {
    clearTimeout(validateTimer)
    validateTimer = setTimeout(() => {
      validate()
    }, 1000)
  },
  { deep: true }
)
</script>

<template>
  <prime-toast />
  <section
    class="tw:relative tw:flex tw:min-h-screen tw:items-center tw:justify-center tw:overflow-hidden tw:bg-slate-950 tw:text-slate-100"
  >
    <div
      class="tw:pointer-events-none tw:absolute tw:-left-24 tw:-top-20 tw:h-72 tw:w-72 tw:rounded-full tw:bg-emerald-400/30 tw:blur-3xl"
    ></div>
    <div
      class="tw:pointer-events-none tw:absolute tw:bottom-0 tw:right-0 tw:h-96 tw:w-96 tw:rounded-full tw:bg-orange-400/20 tw:blur-[120px]"
    ></div>

    <div class="tw:relative tw:z-10 tw:w-full tw:max-w-2xl tw:p-6 lg:tw:p-12">
      <prime-form
        class="auth-card tw:w-full tw:rounded-3xl tw:border tw:border-white/10 tw:bg-slate-900/80 tw:p-8 tw:text-slate-100 tw:shadow-2xl tw:backdrop-blur"
        @submit="onSubmit"
      >
        <div class="tw:space-y-3">
          <span class="tw:text-xs tw:uppercase tw:tracking-[0.4em] tw:text-emerald-300"
            >Cafe Ordering</span
          >
          <h2 class="tw:text-3xl tw:font-semibold">Sign in</h2>
          <p class="tw:text-sm tw:text-slate-300">
            Use your staff account to access the floor.
          </p>
        </div>

        <div class="tw:mt-8 tw:space-y-5">
          <label for="email">
            <span class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-slate-400"
              >Email</span
            >
          </label>
          <prime-input-text
              id="email"
              type="email"
              placeholder="you@cafe.com"
              v-model="email"
              v-bind="emailAttrs"
              class="tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:border-white/10 tw:bg-slate-950/60! tw:px-4 tw:py-3 tw:text-sm tw:text-white!  tw:placeholder-slate-500 tw:transition focus:tw:border-emerald-400 focus:tw:outline-none focus:tw:ring-2 focus:tw:ring-emerald-300/40"
          />
          <prime-message
              v-if="errors.email"
              severity="error"
              size="small"
              variant="simple"
              :closable="false"
              class="tw:mb-1"
          >
            {{ errors.email }}
          </prime-message>
          <label for="password">
            <span class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-slate-400"
            >
              Password</span>
          </label>
            <prime-password
                inputId="password"
                toggleMask
                :feedback="false"
                fluid
                v-model="password"
                v-bind="passwordAttrs"
                class="tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:border-white/10 tw:bg-slate-950/60! tw:text-sm tw:text-slate-100! tw:placeholder-slate-500 tw:transition :twfocus:border-emerald-400 :twfocus:outline-none :twfocus:ring-2 :twfocus:ring-emerald-300/40"
            />
          <prime-message
              v-if="errors.password"
              severity="error"
              size="small"
              variant="simple"
              :closable="false"
              class="tw:mb-1"
          >
            {{ errors.password }}
          </prime-message>

        </div>

        <div class="tw:mt-6 tw:flex tw:items-center tw:justify-between">
          <label for="rememberMe" class="tw:flex tw:items-center tw:gap-2 tw:text-sm tw:text-slate-300">
            <input
              id="rememberMe"
              v-model="rememberMe"
              v-bind="rememberMeAttrs"
              type="checkbox"
              class="tw:h-4 tw:w-4 tw:rounded tw:border-slate-600 tw:bg-slate-900"
            />
            Keep me signed in
          </label>
          <router-link
            class="tw:text-sm tw:text-emerald-300 hover:tw:text-emerald-200"
            to="/admin/forgot-password"
          >
            Forgot password?
          </router-link>
        </div>

        <prime-button
          type="submit"
          class="tw:mt-8 tw:w-full tw:rounded-xl tw:px-4 tw:py-3 tw:text-sm tw:font-semibold tw:transition"
          :class="[
            canSubmit && !isSubmitting
              ? 'tw:bg-emerald-500 tw:text-slate-950! tw:shadow-lg tw:shadow-emerald-500/30 :tw:hover:bg-emerald-400'
              : 'tw:bg-slate-700! tw:text-white! tw:cursor-not-allowed!',
          ]"
          :disabled="isSubmitting || !canSubmit"
        >
          Access dashboard
        </prime-button>

        <div class="tw:mt-6 tw:flex tw:items-center tw:justify-between tw:text-sm">
          <span class="tw:text-slate-400">New to the crew?</span>
          <router-link class="tw:text-emerald-300 hover:tw:text-emerald-200" to="/admin/register">
            Create an account
          </router-link>
        </div>
      </prime-form>

      <div
        class="auth-card auth-card--delay tw:mt-6 tw:rounded-2xl tw:border tw:border-white/10 tw:bg-slate-900/70 tw:p-6 tw:text-slate-100 tw:backdrop-blur"
      >
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-slate-400">Shift tip</p>
        <p class="tw:mt-3 tw:text-sm tw:text-slate-200">
          Keep the espresso shots ready ahead of the morning rush.
        </p>
      </div>
    </div>
  </section>
</template>

<style scoped>
.auth-card {
  animation: fade-up 0.6s ease both;
}

.auth-card--delay {
  animation-delay: 0.15s;
}

@keyframes fade-up {
  from {
    opacity: 0;
    transform: translateY(18px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
