<script setup>
import { computed, onBeforeUnmount, ref, watch } from "vue";
import { useForm } from "vee-validate";
import { toTypedSchema } from "@vee-validate/zod";
import { z } from "zod";
import { useToast } from "primevue/usetoast";
import { checkUsername, register } from "@/services/auth.service.js";
import { inputCustom, labelCustom, passwordCustom } from "@/layout/ui";
import { useThemeStore } from "@/stores/theme";

const themeStore = useThemeStore();
const toast = useToast();
const submitError = ref("");
const isDark = computed(() => themeStore.isDark);
let validateTimer;
let usernameCheckTimer;
let usernameRequestId = 0;

// ── Success state ──────────────────────────────────────────────────────────
const registrationSuccess = ref(false);
const registeredUsername  = ref("");
const countdown           = ref(5);
let countdownTimer;
let redirectTimer;
const countdownProgress = computed(() => ((5 - countdown.value) / 5) * 100);
// ──────────────────────────────────────────────────────────────────────────

const schema = toTypedSchema(
  z
    .object({
      fullName: z.string().min(1, "Full name is required."),
      username: z
        .string()
        .min(3, "Username must be at least 3 characters.")
        .max(50, "Username is too long.")
        .regex(/^[a-zA-Z0-9_\-.]+$/, "Only letters, numbers, _, - and . are allowed."),
      email: z.string().min(1, "Email is required.").email("Email is invalid."),
      password: z.string().min(1, "Password is required."),
      confirmPassword: z.string().min(1, "Please re-enter your password."),
      agree: z.boolean().refine((value) => value, { message: "You must accept the policy." }),
    })
    .refine((data) => data.password === data.confirmPassword, {
      message: "Passwords do not match.",
      path: ["confirmPassword"],
    }),
);

const { errors, defineField, handleSubmit, isSubmitting, meta, setFieldError, validate, values } =
  useForm({
    validationSchema: schema,
    initialValues: {
      fullName: "",
      username: "",
      email: "",
      password: "",
      confirmPassword: "",
      agree: false,
    },
  });

const [fullName, fullNameAttrs] = defineField("fullName");
const [username, usernameAttrs] = defineField("username");
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
    await register({
      username: formValues.username.trim(),
      email: formValues.email.trim(),
      password: formValues.password,
      fullName: formValues.fullName.trim(),
    });

    // Show success screen
    registeredUsername.value = formValues.username.trim();
    registrationSuccess.value = true;

    // Countdown 5 → 0
    countdownTimer = setInterval(() => {
      countdown.value -= 1;
      if (countdown.value <= 0) clearInterval(countdownTimer);
    }, 1000);

    // Redirect after 5s
    // Dùng window.location.replace thay vì router.push để bypass navigation guard
    // (guard có thể block nếu isAuthenticated = true do cookie refresh token từ session cũ)
    redirectTimer = setTimeout(() => {
      window.location.replace("/login");
    }, 5000);
  } catch (err) {
    const errorMessage = err?.response?.data?.message || "Register failed. Please try again.";
    submitError.value = errorMessage;
    toast.add({
      severity: "error",
      summary: "Registration failed",
      detail: errorMessage,
      life: 3000,
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
  { deep: true },
);

watch(username, (value) => {
  clearTimeout(usernameCheckTimer);
  if (!value || value.length < 3) return;

  usernameCheckTimer = setTimeout(async () => {
    const requestId = ++usernameRequestId;
    try {
      const response = await checkUsername({ username: value.trim() });
      if (requestId !== usernameRequestId) return;
      if (response?.data?.exists) {
        setFieldError("username", "Username is already taken.");
      } else if (errors.value.username === "Username is already taken.") {
        setFieldError("username", undefined);
      }
    } catch {
      // Ignore availability errors to avoid blocking submit.
    }
  }, 800);
});

onBeforeUnmount(() => {
  clearTimeout(validateTimer);
  clearTimeout(usernameCheckTimer);
  clearInterval(countdownTimer);
  clearTimeout(redirectTimer);
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

      <!-- ── Success screen ── -->
      <div
        v-if="registrationSuccess"
        class="auth-card app-card tw:w-full tw:rounded-3xl tw:border tw:p-10 tw:shadow-2xl tw:backdrop-blur tw:text-center"
      >
        <div class="tw:flex tw:flex-col tw:items-center tw:gap-4">
          <iconify
            icon="ph:check-circle-bold"
            class="tw:text-6xl tw:text-primary-400"
          />
          <div class="tw:space-y-1">
            <h2 class="tw:text-3xl tw:font-semibold">Account created!</h2>
            <p class="tw:text-sm app-text-muted">
              Welcome, <span class="tw:font-medium tw:text-primary-300">@{{ registeredUsername }}</span>
            </p>
          </div>
        </div>

        <div class="tw:mt-8 tw:space-y-3">
          <p class="tw:text-sm app-text-muted">
            Signing you in automatically in
            <span class="tw:font-semibold tw:tabular-nums">{{ countdown }}s</span>…
          </p>

          <!-- Progress bar -->
          <div class="app-panel tw:h-1.5 tw:w-full tw:overflow-hidden tw:rounded-full tw:border">
            <div
              class="tw:h-full tw:rounded-full tw:bg-primary-500 tw:transition-all tw:duration-1000 tw:ease-linear"
              :style="{ width: `${countdownProgress}%` }"
            ></div>
          </div>
        </div>

        <div class="tw:mt-8">
          <router-link
            :to="{ name: 'login' }"
            class="tw:text-sm tw:text-primary-300 hover:tw:text-primary-200"
          >
            Sign in now →
          </router-link>
        </div>
      </div>

      <!-- ── Register form ── -->
      <template v-else>
        <prime-form
          class="auth-card app-card tw:w-full tw:rounded-3xl tw:border tw:p-8 tw:shadow-2xl tw:backdrop-blur"
          @submit="onSubmit"
        >
          <div class="tw:space-y-3">
            <div class="tw:flex tw:items-center tw:justify-between">
              <span class="tw:text-xs tw:uppercase tw:tracking-[0.4em] tw:text-primary-300"
                >Cafe Ordering</span
              >
              <prime-button
                severity="secondary"
                outlined
                size="small"
                @click="themeStore.toggleTheme()"
                aria-label="Toggle theme"
                class="tw:p-1!"
              >
                <iconify
                  :icon="isDark ? 'ph:sun-bold' : 'ph:moon-bold'"
                  class="tw:text-base"
                />
              </prime-button>
            </div>
            <h2 class="tw:text-3xl tw:font-semibold">Create account</h2>
            <p class="tw:text-sm app-text-muted">
              Set up your profile and start placing orders in minutes.
            </p>
          </div>

          <div class="tw:mt-8 tw:space-y-5">
            <label for="fullName" :class="labelCustom"> Full name </label>
            <prime-input-text
              id="fullName"
              type="text"
              fluid
              placeholder="Ava Nguyen"
              v-model="fullName"
              v-bind="fullNameAttrs"
              :class="inputCustom"
            />
            <prime-message
              v-if="errors.fullName"
              severity="error"
              size="small"
              variant="simple"
              :closable="false"
              class="tw:mb-1"
            >
              {{ errors.fullName }}
            </prime-message>

            <label for="username" :class="labelCustom"> Username </label>
            <prime-input-text
              id="username"
              type="text"
              fluid
              placeholder="your-username"
              v-model="username"
              v-bind="usernameAttrs"
              :class="inputCustom"
            />
            <prime-message
              v-if="errors.username"
              severity="error"
              size="small"
              variant="simple"
              :closable="false"
              class="tw:mb-1"
            >
              {{ errors.username }}
            </prime-message>

            <label for="email" :class="labelCustom"> Email </label>
            <prime-input-text
              id="email"
              type="email"
              fluid
              placeholder="you@cafe.com"
              v-model="email"
              v-bind="emailAttrs"
              :class="inputCustom"
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

            <label for="password" :class="labelCustom"> Password </label>
            <prime-password
              placeholder="Create a strong password"
              inputId="password"
              toggleMask
              showClear
              :feedback="false"
              fluid
              v-model="password"
              v-bind="passwordAttrs"
              :pt="passwordCustom"
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
            <div class="tw:space-y-2">
              <div class="app-panel tw:h-1.5 tw:w-full tw:overflow-hidden tw:rounded-full tw:border">
                <div
                  class="tw:h-full tw:rounded-full tw:transition-all"
                  :class="[
                    strength.score === 0 && 'tw:w-0',
                    strength.score === 1 && 'tw:w-1/4 tw:bg-rose-500',
                    strength.score === 2 && 'tw:w-1/2 tw:bg-amber-400',
                    strength.score === 3 && 'tw:w-3/4 tw:bg-primary-500',
                    strength.score === 4 && 'tw:w-full tw:bg-primary-500',
                  ]"
                ></div>
              </div>
              <p class="tw:text-xs app-text-subtle">
                Strength: <span class="tw:font-semibold">{{ strength.label }}</span>
              </p>
            </div>

            <label for="confirmPassword" :class="labelCustom"> Confirm password </label>
            <prime-input-text
              id="confirmPassword"
              type="password"
              fluid
              placeholder="Re-enter your password"
              v-model="confirmPassword"
              v-bind="confirmPasswordAttrs"
              :class="inputCustom"
            />
            <prime-message
              v-if="errors.confirmPassword"
              severity="error"
              size="small"
              variant="simple"
              :closable="false"
              class="tw:mb-1"
            >
              {{ errors.confirmPassword }}
            </prime-message>
          </div>

          <div class="tw:mt-6 tw:flex tw:items-center tw:gap-2">
            <prime-checkbox
              id="agree"
              v-model="agree"
              v-bind="agreeAttrs"
              binary
              class="app-panel"
              size="small"
            />
            <label for="agree" class="tw:text-sm app-text-muted">
              I agree to the team guidelines and
              <router-link class="tw:text-primary-300 hover:tw:text-primary-200" to="/policy">
                data policy
              </router-link>
            </label>
          </div>
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
            class="tw:mt-8 tw:w-full tw:rounded-xl tw:px-4 tw:py-3 tw:text-sm tw:font-semibold tw:transition tw:border-0!"
            :class="[
              canSubmit && !isSubmitting
                ? 'tw:shadow-lg tw:shadow-primary-500/30 hover:tw:bg-primary-400'
                : 'tw:cursor-not-allowed!',
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
            <span class="app-text-subtle">Already have access?</span>
            <router-link
              class="tw:text-primary-300 hover:tw:text-primary-200"
              :to="{ name: 'login' }"
            >
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
      </template>

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
