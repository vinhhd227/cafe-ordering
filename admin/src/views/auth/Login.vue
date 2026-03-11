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
import { appCard, cardRing } from "../../layout/ui";

const authStore = useAuthStore();
const themeStore = useThemeStore();
const toast = useToast();
const { locale, locales, setLocale } = useLocale();
const { t } = useI18n();
const submitError = ref("");
let validateTimer;
let redirectTimer;
const isDark = computed(() => themeStore.isDark);

const schema = computed(() =>
  toTypedSchema(
    z.object({
      username: z.string().min(1, t("login.validation.usernameRequired")),
      password: z.string().min(1, t("login.validation.passwordRequired")),
      rememberMe: z.boolean(),
    }),
  ),
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
      summary: t("login.toast.successTitle"),
      detail: t("login.toast.successDetail"),
      life: 3000,
    });

    // Redirect to dashboard after 500ms
    redirectTimer = setTimeout(() => {
      router.push({ name: "dashboard" });
    }, 500);
  } catch (err) {
    console.error(err);
    const errorMessage =
      err?.response?.data?.message || t("login.toast.errorFallback");
    submitError.value = errorMessage;
    toast.add({
      severity: "error",
      summary: t("login.toast.errorTitle"),
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
    class="tw:relative tw:flex tw:min-h-screen tw:items-center tw:justify-center tw:overflow-hidden"
  >
    <div class="app-background tw:absolute tw:inset-0 tw:z-0" />

    <div class="tw:relative tw:z-10 tw:w-full tw:max-w-2xl tw:p-6 lg:tw:p-12">
      <prime-card
        :pt="{
          root: { class: `${appCard} ${cardRing} tw:p-8` },
          body: { class: 'tw:p-0!' },
        }"
      >
        <template #header>
          <div class="tw:space-y-3">
            <div class="tw:flex tw:items-center tw:justify-between">
              <span
                class="tw:text-xs tw:uppercase tw:tracking-[0.4em] tw:text-primary-300"
              >
                Cafe Ordering
              </span>
              <div class="tw:flex tw:items-center tw:gap-1.5">
                <div class="tw:flex tw:gap-0.5">
                  <button
                    v-for="l in locales"
                    :key="l.code"
                    type="button"
                    class="tw:rounded-md tw:px-2 tw:py-1 tw:text-xs tw:font-semibold tw:transition-all tw:duration-150"
                    :class="
                      locale === l.code
                        ? 'tw:bg-primary-500/15 tw:text-primary-400'
                        : 'app-text-subtle hover:tw:bg-white/5 hover:app-text-muted'
                    "
                    @click="setLocale(l.code)"
                  >
                    {{ l.label }}
                  </button>
                </div>
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
            </div>
            <h2 class="tw:text-3xl tw:font-semibold">
              {{ t("login.title") }}
            </h2>
            <p class="tw:text-sm app-text-muted">
              {{ t("login.subtitle") }}
            </p>
          </div>
        </template>
        <template #content>
          <prime-form @submit="onSubmit">
            <div class="tw:mt-8 tw:space-y-5">
              <label for="username" :class="labelCustom">{{
                t("auth.username")
              }}</label>
              <prime-input-text
                id="username"
                type="text"
                fluid
                :placeholder="t('login.usernamePlaceholder')"
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
              <label for="password" :class="labelCustom">{{
                t("auth.password")
              }}</label>
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
                {{ t("login.rememberMe") }}
              </label>
              <router-link
                class="tw:text-sm tw:text-primary-300 hover:tw:text-primary-200 tw:ml-auto!"
                to="/forgot-password"
              >
                {{ t("login.forgotPassword") }}
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
              {{ t("login.submit") }}
            </prime-button>

            <div
              class="tw:mt-6 tw:flex tw:items-center tw:justify-between tw:text-sm"
            >
              <span class="app-text-subtle">{{ t("login.newUser") }}</span>
              <router-link
                class="tw:text-primary-300 hover:tw:text-primary-200"
                to="/register"
              >
                {{ t("login.register") }}
              </router-link>
            </div>
          </prime-form>
        </template>
      </prime-card>
      <prime-card
        :pt="{
          root: { class: `${appCard} ${cardRing} tw:p-8 tw:mt-6` },
          body: { class: 'tw:p-0!' },
        }"
      >
        <template #content>
          <p
            class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-subtle"
          >
            {{ t("login.shiftTip.label") }}
          </p>
          <p class="tw:mt-3 tw:text-sm app-text-muted">
            {{ t("login.shiftTip.text") }}
          </p>
        </template>
      </prime-card>
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
