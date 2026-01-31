<script setup>
import { computed, ref, watch } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { z } from 'zod'
import { checkUsername, register } from '@/services/auth.service'

const submitError = ref('')
let validateTimer
let emailCheckTimer
let emailRequestId = 0

const schema = toTypedSchema(
  z
    .object({
      firstName: z.string().min(1, 'First name is required.'),
      lastName: z.string().min(1, 'Last name is required.'),
      email: z.string().min(1, 'Email is required.').email('Email is invalid.'),
      password: z.string().min(1, 'Password is required.'),
      confirmPassword: z.string().min(1, 'Please re-enter your password.'),
      agree: z.boolean().refine((value) => value, { message: 'You must accept the policy.' }),
    })
    .refine((data) => data.password === data.confirmPassword, {
      message: 'Passwords do not match.',
      path: ['confirmPassword'],
    })
)

const { errors, defineField, handleSubmit, isSubmitting, meta, setFieldError, validate, values } =
  useForm({
  validationSchema: schema,
  initialValues: {
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    confirmPassword: '',
    agree: false,
  },
})

const [firstName, firstNameAttrs] = defineField('firstName')
const [lastName, lastNameAttrs] = defineField('lastName')
const [email, emailAttrs] = defineField('email')
const [password, passwordAttrs] = defineField('password')
const [confirmPassword, confirmPasswordAttrs] = defineField('confirmPassword')
const [agree, agreeAttrs] = defineField('agree')

const strength = computed(() => {
  const value = password.value
  if (!value) return { score: 0, label: 'Empty' }

  let score = 0
  if (value.length >= 8) score += 1
  if (/[A-Z]/.test(value)) score += 1
  if (/[a-z]/.test(value)) score += 1
  if (/\d/.test(value)) score += 1
  if (/[^A-Za-z0-9]/.test(value)) score += 1

  if (score <= 2) return { score: 1, label: 'Weak' }
  if (score === 3) return { score: 2, label: 'Fair' }
  if (score === 4) return { score: 3, label: 'Good' }
  return { score: 4, label: 'Strong' }
})

const canSubmit = computed(() => meta.value.valid)

const onSubmit = handleSubmit(async (formValues) => {
  submitError.value = ''
  try {
    await register({
      email: formValues.email.trim(),
      password: formValues.password,
      firstname: formValues.firstName.trim(),
      lastname: formValues.lastName.trim(),
    })
  } catch (err) {
    submitError.value = err?.response?.data?.message || 'Register failed. Please try again.'
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

watch(email, (value) => {
  clearTimeout(emailCheckTimer)
  if (!value) return
  const parsed = z.string().email().safeParse(value)
  if (!parsed.success) return

  emailCheckTimer = setTimeout(async () => {
    const requestId = ++emailRequestId
    try {
      const response = await checkUsername({ email: value.trim() })
      if (requestId !== emailRequestId) return
      if (response?.exists) {
        setFieldError('email', 'Email already exists.')
      } else if (errors.value.email === 'Email already exists.') {
        setFieldError('email', undefined)
      }
    } catch {
      // Ignore availability errors to avoid blocking submit.
    }
  }, 1000)
})
</script>

<template>
  <section
    class="tw:relative tw:flex tw:min-h-screen tw:items-center tw:justify-center tw:overflow-hidden tw:bg-amber-50 tw:text-slate-900"
  >
    <div
      class="tw:pointer-events-none tw:absolute tw:-left-24 tw:top-10 tw:h-72 tw:w-72 tw:rounded-full tw:bg-amber-200/70 tw:blur-[120px]"
    ></div>
    <div
      class="tw:pointer-events-none tw:absolute tw:bottom-0 tw:right-0 tw:h-96 tw:w-96 tw:rounded-full tw:bg-rose-200/70 tw:blur-[130px]"
    ></div>

    <div class="tw:relative tw:z-10 tw:w-full tw:max-w-2xl tw:p-6 lg:tw:p-12">
      <prime-form
        class="auth-card tw:w-full tw:rounded-3xl tw:bg-white tw:p-8 tw:shadow-2xl"
        @submit.prevent="onSubmit"
      >
        <div class="tw:space-y-3">
          <span class="tw:text-xs tw:uppercase tw:tracking-[0.4em] tw:text-amber-600"
            >Cafe Ordering</span
          >
          <h2 class="tw:text-3xl tw:font-semibold">Customer register</h2>
          <p class="tw:text-sm tw:text-slate-500">
            Create an account to save orders and earn rewards.
          </p>
        </div>

        <div class="tw:mt-8 tw:grid tw:grid-cols-1 tw:gap-5 md:tw:grid-cols-2">
          <label class="tw:block">
            <span class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-slate-400"
              >First name</span
            >
            <prime-input-text
              type="text"
              placeholder="Ava"
              v-model="firstName"
              v-bind="firstNameAttrs"
              class="tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:border-slate-200 tw:bg-slate-50 tw:px-4 tw:py-3 tw:text-sm tw:text-slate-900 tw:placeholder-slate-400 tw:transition focus:tw:border-amber-400 focus:tw:outline-none focus:tw:ring-2 focus:tw:ring-amber-200"
            />
            <prime-message
              v-if="errors.firstName"
              severity="error"
              size="small"
              variant="simple"
              :closable="false"
              class="tw:mt-2"
            >
              {{ errors.firstName }}
            </prime-message>
          </label>
          <label class="tw:block">
            <span class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-slate-400"
              >Last name</span
            >
            <prime-input-text
              type="text"
              placeholder="Nguyen"
              v-model="lastName"
              v-bind="lastNameAttrs"
              class="tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:border-slate-200 tw:bg-slate-50 tw:px-4 tw:py-3 tw:text-sm tw:text-slate-900 tw:placeholder-slate-400 tw:transition focus:tw:border-amber-400 focus:tw:outline-none focus:tw:ring-2 focus:tw:ring-amber-200"
            />
            <prime-message
              v-if="errors.lastName"
              severity="error"
              size="small"
              variant="simple"
              :closable="false"
              class="tw:mt-2"
            >
              {{ errors.lastName }}
            </prime-message>
          </label>
        </div>

        <div class="tw:mt-5 tw:space-y-5">
          <label class="tw:block">
            <span class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-slate-400"
              >Email</span
            >
            <prime-input-text
              type="email"
              placeholder="you@cafe.com"
              v-model="email"
              v-bind="emailAttrs"
              class="tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:border-slate-200 tw:bg-slate-50 tw:px-4 tw:py-3 tw:text-sm tw:text-slate-900 tw:placeholder-slate-400 tw:transition focus:tw:border-amber-400 focus:tw:outline-none focus:tw:ring-2 focus:tw:ring-amber-200"
            />
            <prime-message
              v-if="errors.email"
              severity="error"
              size="small"
              variant="simple"
              :closable="false"
              class="tw:mt-2"
            >
              {{ errors.email }}
            </prime-message>
          </label>
          <label class="tw:block">
            <span class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-slate-400"
              >Password</span
            >
            <prime-input-text
              type="password"
              placeholder="Create a strong password"
              v-model="password"
              v-bind="passwordAttrs"
              class="tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:border-slate-200 tw:bg-slate-50 tw:px-4 tw:py-3 tw:text-sm tw:text-slate-900 tw:placeholder-slate-400 tw:transition focus:tw:border-amber-400 focus:tw:outline-none focus:tw:ring-2 focus:tw:ring-amber-200"
            />
            <prime-message
              v-if="errors.password"
              severity="error"
              size="small"
              variant="simple"
              :closable="false"
              class="tw:mt-2"
            >
              {{ errors.password }}
            </prime-message>
            <div class="tw:mt-3 tw:space-y-2">
              <div class="tw:h-1.5 tw:w-full tw:overflow-hidden tw:rounded-full tw:bg-amber-100">
                <div
                  class="tw:h-full tw:rounded-full tw:transition-all"
                  :class="[
                    strength.score === 1 && 'tw:w-1/4',
                    strength.score === 2 && 'tw:w-1/2',
                    strength.score === 3 && 'tw:w-3/4',
                    strength.score === 4 && 'tw:w-full',
                    strength.score === 0 && 'tw:w-0',
                    strength.score === 1 && 'tw:bg-rose-500',
                    strength.score === 2 && 'tw:bg-amber-400',
                    strength.score >= 3 && 'tw:bg-emerald-500',
                  ]"
                ></div>
              </div>
              <p class="tw:text-xs tw:text-slate-500">
                Strength: <span class="tw:font-semibold">{{ strength.label }}</span>
              </p>
            </div>
          </label>
          <label class="tw:block">
            <span class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-slate-400"
              >Confirm password</span
            >
            <prime-input-text
              type="password"
              placeholder="Re-enter your password"
              v-model="confirmPassword"
              v-bind="confirmPasswordAttrs"
              class="tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:border-slate-200 tw:bg-slate-50 tw:px-4 tw:py-3 tw:text-sm tw:text-slate-900 tw:placeholder-slate-400 tw:transition focus:tw:border-amber-400 focus:tw:outline-none focus:tw:ring-2 focus:tw:ring-amber-200"
            />
            <prime-message
              v-if="errors.confirmPassword"
              severity="error"
              size="small"
              variant="simple"
              :closable="false"
              class="tw:mt-2"
            >
              {{ errors.confirmPassword }}
            </prime-message>
          </label>
        </div>

        <label class="tw:mt-6 tw:flex tw:items-start tw:gap-3 tw:text-sm tw:text-slate-500">
          <input
            v-model="agree"
            v-bind="agreeAttrs"
            type="checkbox"
            class="tw:mt-1 tw:h-4 tw:w-4 tw:rounded tw:border-slate-300"
          />
          <span>
            I agree to the customer terms and
            <router-link class="tw:text-amber-600 hover:tw:text-amber-700" to="/policy">
              data policy
            </router-link>
            for this cafe.
          </span>
        </label>
        <prime-message
          v-if="errors.agree"
          severity="error"
          size="small"
          variant="simple"
          :closable="false"
          class="tw:mt-2"
        >
          {{ errors.agree }}
        </prime-message>

        <prime-button
          type="submit"
          class="tw:mt-8 tw:w-full tw:rounded-xl tw:px-4 tw:py-3 tw:text-sm tw:font-semibold tw:text-white tw:transition"
          :class="[
            canSubmit && !isSubmitting
              ? 'tw:bg-amber-500 tw:shadow-lg tw:shadow-amber-500/30 hover:tw:bg-amber-400'
              : 'tw:bg-slate-400 tw:text-white tw:cursor-not-allowed',
          ]"
          :disabled="isSubmitting || !canSubmit"
        >
          Create account
        </prime-button>

        <prime-message
          v-if="submitError"
          severity="error"
          size="small"
          variant="simple"
          :closable="false"
          class="tw:mt-4"
        >
          {{ submitError }}
        </prime-message>

        <div class="tw:mt-6 tw:flex tw:items-center tw:justify-between tw:text-sm">
          <span class="tw:text-slate-500">Already have an account?</span>
          <router-link class="tw:text-amber-600 hover:tw:text-amber-700" to="/login">
            Sign in
          </router-link>
        </div>
      </prime-form>
    </div>
  </section>
</template>

<style scoped>
.auth-card {
  animation: fade-up 0.6s ease both;
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
