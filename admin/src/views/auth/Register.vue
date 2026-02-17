<script setup>
import { computed, ref, watch } from "vue";
import { useForm } from "vee-validate";
import { toTypedSchema } from "@vee-validate/zod";
import { z } from "zod";
import { useToast } from "primevue/usetoast";
import { checkUsername, register } from "@/services/auth.service.js";

const toast = useToast();
const submitError = ref("");
let validateTimer;
let emailCheckTimer;
let emailRequestId = 0;

const schema = toTypedSchema(
  z
    .object({
      firstName: z.string().min(1, "First name is required."),
      lastName: z.string().min(1, "Last name is required."),
      email: z.string().min(1, "Email is required.").email("Email is invalid."),
      password: z.string().min(1, "Password is required."),
      confirmPassword: z.string().min(1, "Please re-enter your password."),
      agree: z.boolean().refine((value) => value, { message: "You must accept the policy." })
    })
    .refine((data) => data.password === data.confirmPassword, {
      message: "Passwords do not match.",
      path: ["confirmPassword"]
    })
);

const { errors, defineField, handleSubmit, isSubmitting, meta, setFieldError, validate, values } =
  useForm({
    validationSchema: schema,
    initialValues: {
      firstName: "",
      lastName: "",
      email: "",
      password: "",
      confirmPassword: "",
      agree: false
    }
  });

const [firstName, firstNameAttrs] = defineField("firstName");
const [lastName, lastNameAttrs] = defineField("lastName");
const [email, emailAttrs] = defineField("email");
const [password, passwordAttrs] = defineField("password");
const [confirmPassword, confirmPasswordAttrs] = defineField("confirmPassword");
const [agree, agreeAttrs] = defineField("agree");

const strength = computed(() => {
  const value = password.value;
  if (!value) return { score: 0, label: "Empty" };

  let score = 0;
  if (value.length >= 8) score += 1;
  if (/[A-Z]/.test(value)) score += 1;
  if (/[a-z]/.test(value)) score += 1;
  if (/\d/.test(value)) score += 1;
  if (/[^A-Za-z0-9]/.test(value)) score += 1;

  if (score <= 2) return { score: 1, label: "Weak" };
  if (score === 3) return { score: 2, label: "Fair" };
  if (score === 4) return { score: 3, label: "Good" };
  return { score: 4, label: "Strong" };
});

const canSubmit = computed(() => meta.value.valid);

const onSubmit = handleSubmit(async (formValues) => {
  submitError.value = "";
  try {
    const response = await register({
      email: formValues.email.trim(),
      password: formValues.password,
      firstname: formValues.firstName.trim(),
      lastname: formValues.lastName.trim()
    });
    toast.add({
      severity: "success",
      summary: "Registration successful",
      detail: response?.message ?? "Your account has been created successfully.",
      life: 3000
    });
  } catch (err) {
    const errorMessage = err?.response?.data?.message || "Register failed. Please try again.";
    submitError.value = errorMessage;
    toast.add({
      severity: "error",
      summary: "Registration failed",
      detail: errorMessage,
      life: 3000
    });
  }
});

watch(
  values,
  () => {
    clearTimeout(validateTimer);
    validateTimer = setTimeout(() => {
      validate();
    }, 1000);
  },
  { deep: true }
);

watch(email, (value) => {
  clearTimeout(emailCheckTimer);
  if (!value) return;
  const parsed = z.string().email().safeParse(value);
  if (!parsed.success) return;

  emailCheckTimer = setTimeout(async () => {
    const requestId = ++emailRequestId;
    try {
      const response = await checkUsername({ email: value.trim() });
      if (requestId !== emailRequestId) return;
      if (response?.exists) {
        setFieldError("email", "Email already exists.");
      } else if (errors.value.email === "Email already exists.") {
        setFieldError("email", undefined);
      }
    } catch {
      // Ignore availability errors to avoid blocking submit.
    }
  }, 1000);
});
</script>

<template>
  <prime-toast />
  <section
    class="app-shell tw:relative tw:flex tw:min-h-screen tw:items-center tw:justify-center tw:overflow-hidden"
  >
    <div
      class="app-glow-primary tw:pointer-events-none tw:absolute tw:-left-24 tw:top-10 tw:h-72 tw:w-72 tw:rounded-full tw:blur-[120px]"
    ></div>
    <div
      class="app-glow-amber tw:pointer-events-none tw:absolute tw:bottom-0 tw:right-0 tw:h-96 tw:w-96 tw:rounded-full tw:blur-[130px]"
    ></div>

    <div class="tw:relative tw:z-10 tw:w-full tw:max-w-2xl tw:p-6 lg:tw:p-12">
      <prime-form
        class="auth-card app-card tw:w-full tw:rounded-3xl tw:border tw:p-8 tw:shadow-2xl tw:backdrop-blur"
        @submit="onSubmit"
      >
        <div class="tw:space-y-3">
          <span class="tw:text-xs tw:uppercase tw:tracking-[0.4em] tw:text-primary-300"
          >Cafe Ordering</span
          >
          <h2 class="tw:text-3xl tw:font-semibold">Create account</h2>
          <p class="tw:text-sm app-text-muted">
            Set up your team profile and start taking orders in minutes.
          </p>
        </div>

        <div class="tw:mt-8 tw:grid tw:grid-cols-1 tw:gap-5 md:tw:grid-cols-2">
          <label for="firstName" class="tw:block">
            <span class="app-label tw:text-xs tw:uppercase tw:tracking-[0.3em]"
            >First name</span
            >
            <prime-input-text
              id="firstName"
              type="text"
              placeholder="Ava"
              v-model="firstName"
              v-bind="firstNameAttrs"
              class="app-input tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:px-4 tw:py-3 tw:text-sm tw:transition focus:tw:border-primary-400 focus:tw:outline-none focus:tw:ring-2 focus:tw:ring-primary-300/40"
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
          <label for="lastName" class="tw:block">
            <span class="app-label tw:text-xs tw:uppercase tw:tracking-[0.3em]"
            >Last name</span
            >
            <prime-input-text
              id="lastName"
              type="text"
              placeholder="Nguyen"
              v-model="lastName"
              v-bind="lastNameAttrs"
              class="app-input tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:px-4 tw:py-3 tw:text-sm tw:transition focus:tw:border-primary-400 focus:tw:outline-none focus:tw:ring-2 focus:tw:ring-primary-300/40"
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
          <label for="email" class="tw:block">
            <span class="app-label tw:text-xs tw:uppercase tw:tracking-[0.3em]"
            >Email</span
            >
            <prime-input-text
              id="email"
              type="email"
              placeholder="team@cafe.com"
              v-model="email"
              v-bind="emailAttrs"
              class="app-input tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:px-4 tw:py-3 tw:text-sm tw:transition focus:tw:border-primary-400 focus:tw:outline-none focus:tw:ring-2 focus:tw:ring-primary-300/40"
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
          <label for="password" class="tw:block">
            <span class="app-label tw:text-xs tw:uppercase tw:tracking-[0.3em]"
            >Password</span
            >
            <prime-password
              id="password"
              type="password"

              placeholder="Create a strong password"
              v-model="password"
              v-bind="passwordAttrs"
              class="app-input tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:px-4 tw:py-3 tw:text-sm tw:transition focus:tw:border-primary-400 focus:tw:outline-none focus:tw:ring-2 focus:tw:ring-primary-300/40"
            >
              <template #header>
                <div class="font-semibold text-xm mb-4">Reset Password</div>
              </template>
              <template #footer>
                <prime-divider />
                <ul class="pl-2 my-0 leading-normal text-sm">
                  <li>At least one lowercase</li>
                  <li>At least one uppercase</li>
                  <li>At least one numeric</li>
                  <li>Minimum 8 characters</li>
                </ul>
              </template>
            </prime-password>
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
              <div class="app-panel tw:h-1.5 tw:w-full tw:overflow-hidden tw:rounded-full tw:border">
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
                    strength.score >= 3 && 'tw:bg-primary-500',
                  ]"
                ></div>
              </div>
              <p class="tw:text-xs app-text-subtle">
                Strength: <span class="tw:font-semibold">{{ strength.label }}</span>
              </p>
            </div>
          </label>
          <label for="confirmPassword" class="tw:block">
            <span class="app-label tw:text-xs tw:uppercase tw:tracking-[0.3em]"
            >Confirm password</span
            >
            <prime-input-text
              id="confirmPassword"
              type="password"
              placeholder="Re-enter your password"
              v-model="confirmPassword"
              v-bind="confirmPasswordAttrs"
              class="app-input tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:px-4 tw:py-3 tw:text-sm tw:transition focus:tw:border-primary-400 focus:tw:outline-none focus:tw:ring-2 focus:tw:ring-primary-300/40"
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

        <label for="agree" class="tw:mt-6 tw:flex tw:items-start tw:gap-3 tw:text-sm app-text-muted">
          <input
            id="agree"
            v-model="agree"
            v-bind="agreeAttrs"
            type="checkbox"
            class="app-panel tw:mt-1 tw:h-4 tw:w-4 tw:rounded tw:border"
          />
          <span>
            I agree to the team guidelines and
            <router-link class="tw:text-primary-300 hover:tw:text-primary-200" to="/policy">
              data policy
            </router-link>
            for this workspace.
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
          class="tw:mt-8 tw:w-full tw:rounded-xl tw:px-4 tw:py-3 tw:text-sm tw:font-semibold tw:transition"
          :class="[
            canSubmit && !isSubmitting
              ? 'tw:bg-primary-500 tw:text-slate-950 tw:shadow-lg tw:shadow-primary-500/30 hover:tw:bg-primary-400'
              : 'tw:bg-slate-400 tw:text-white tw:cursor-not-allowed',
          ]"
          :disabled="isSubmitting || !canSubmit"
        >
          Create workspace
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
          <span class="app-text-subtle">Already have access?</span>
          <router-link class="tw:text-primary-300 hover:tw:text-primary-200" :to="{name: 'login'}">
            Sign in
          </router-link>
        </div>
      </prime-form>

      <div
        class="auth-card auth-card--delay app-card tw:mt-6 tw:rounded-2xl tw:border tw:p-6 tw:backdrop-blur"
      >
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-subtle">Shift tip</p>
        <p class="tw:mt-3 tw:text-sm app-text-muted">
          Prep a simple onboarding checklist to keep new staff confident.
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
