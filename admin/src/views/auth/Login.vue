<script setup>
import { computed, onBeforeUnmount, ref, watch } from "vue";
import { useForm } from "vee-validate";
import { toTypedSchema } from "@vee-validate/zod";
import { z } from "zod";
import { useToast } from "primevue/usetoast";
import { useAuthStore } from "@/stores/auth.js";
import router from "@/router/index.js";
import { inputCustom, labelCustom, passwordCustom } from "@/layout/ui";
import { useThemeStore } from "@/stores/theme";

const authStore = useAuthStore();
const themeStore = useThemeStore();
const toast = useToast();
const submitError = ref("");
let validateTimer;
let redirectTimer;
const isDark = computed(() => themeStore.isDark);

const schema = toTypedSchema(
  z.object({
    username: z.string().min(1, "Username is required."),
    password: z.string().min(1, "Password is required."),
    rememberMe: z.boolean(),
  }),
);

const {
  errors,
  defineField,
  handleSubmit,
  isSubmitting,
  meta,
  validate,
  values,
} = useForm({
  validationSchema: schema,
  initialValues: {
    username: "",
    password: "",
    rememberMe: false,
  },
});

const [username, usernameAttrs] = defineField("username");
const [password, passwordAttrs] = defineField("password");
const [rememberMe, rememberMeAttrs] = defineField("rememberMe");

const canSubmit = computed(() => meta.value.valid);

const onSubmit = handleSubmit(async (formValues) => {
  submitError.value = "";
  try {
    await authStore.login(formValues);

    toast.add({
      severity: "success",
      summary: "Login successful",
      detail: "Welcome back! Redirecting to dashboard...",
      life: 3000,
    });

    // Redirect to dashboard after 500ms
    redirectTimer = setTimeout(() => {
      router.push({ name: "dashboard" });
    }, 500);
  } catch (err) {
    console.error(err);
    const errorMessage =
      err?.response?.data?.message || "Login failed. Please try again.";
    submitError.value = errorMessage;
    toast.add({
      severity: "error",
      summary: "Login failed",
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

onBeforeUnmount(() => {
  clearTimeout(validateTimer);
  clearTimeout(redirectTimer);
});
</script>

<template>
  <prime-toast />
  <section
    class="app-shell tw:relative tw:flex tw:min-h-screen tw:items-center tw:justify-center tw:overflow-hidden"
  >
    <div
      class="app-glow-emerald tw:pointer-events-none tw:absolute tw:-left-24 tw:-top-20 tw:h-72 tw:w-72 tw:rounded-full tw:blur-3xl"
    ></div>
    <div
      class="app-glow-amber tw:pointer-events-none tw:absolute tw:bottom-0 tw:right-0 tw:h-96 tw:w-96 tw:rounded-full tw:blur-[120px]"
    ></div>

    <div class="tw:relative tw:z-10 tw:w-full tw:max-w-2xl tw:p-6 lg:tw:p-12">
      <prime-form
        class="auth-card app-card tw:w-full tw:rounded-3xl tw:border tw:p-8 tw:shadow-2xl tw:backdrop-blur"
        @submit="onSubmit"
      >
        <div class="tw:space-y-3">
          <div class="tw:flex tw:items-center tw:justify-between">
            <span
              class="tw:text-xs tw:uppercase tw:tracking-[0.4em] tw:text-primary-300"
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
          <h2 class="tw:text-3xl tw:font-semibold">Sign in</h2>
          <p class="tw:text-sm app-text-muted">
            Use your staff account to access the floor.
          </p>
        </div>

        <div class="tw:mt-8 tw:space-y-5">
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
          <label for="password" :class="labelCustom"> Password </label>
          <prime-password
            placeholder="&#9679;&#9679;&#9679;&#9679;&#9679;&#9679;&#9679;&#9679;"
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
        </div>

        <div class="tw:mt-6 tw:flex tw:items-center tw:gap-2">
          <prime-checkbox
            id="rememberMe"
            v-model="rememberMe"
            v-bind="rememberMeAttrs"
            binary
            class="app-panel"
            size="small"
          />
          <label for="rememberMe" class="tw:text-sm app-text-muted">
            Keep me signed in
          </label>
          <router-link
            class="tw:text-sm tw:text-primary-300 hover:tw:text-primary-200 tw:ml-auto!"
            to="/forgot-password"
          >
            Forgot password?
          </router-link>
        </div>

        <prime-button
          type="submit"
          class="tw:mt-8 tw:w-full tw:rounded-xl tw:px-4 tw:py-3 tw:text-sm tw:font-semibold tw:transition tw:border-0!"
          :class="[
            canSubmit && !isSubmitting
              ? 'tw:shadow-lg tw:shadow-primary-500/30 :tw:hover:bg-primary-400'
              : 'tw:cursor-not-allowed!',
          ]"
          :disabled="isSubmitting || !canSubmit"
        >
          Access dashboard
        </prime-button>

        <div
          class="tw:mt-6 tw:flex tw:items-center tw:justify-between tw:text-sm"
        >
          <span class="app-text-subtle">New to the crew?</span>
          <router-link
            class="tw:text-primary-300 hover:tw:text-primary-200"
            to="/register"
          >
            Create an account
          </router-link>
        </div>
      </prime-form>

      <div
        class="auth-card auth-card--delay app-card tw:mt-6 tw:rounded-2xl tw:border tw:p-6 tw:backdrop-blur"
      >
        <p
          class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-subtle"
        >
          Shift tip
        </p>
        <p class="tw:mt-3 tw:text-sm app-text-muted">
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
