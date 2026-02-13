<script setup></script>

<template>
  <section
    class="app-shell tw:relative tw:flex tw:min-h-screen tw:items-center tw:justify-center tw:overflow-hidden"
  >
    <div
      class="app-glow-emerald tw:pointer-events-none tw:absolute tw:-left-24 tw:top-10 tw:h-72 tw:w-72 tw:rounded-full tw:blur-[120px]"
    ></div>
    <div
      class="app-glow-amber tw:pointer-events-none tw:absolute tw:bottom-0 tw:right-0 tw:h-96 tw:w-96 tw:rounded-full tw:blur-[130px]"
    ></div>

    <div class="tw:relative tw:z-10 tw:w-full tw:max-w-2xl tw:p-6 lg:tw:p-12">
      <form
        class="auth-card app-card tw:w-full tw:rounded-3xl tw:border tw:p-8 tw:shadow-2xl tw:backdrop-blur"
      >
        <div class="tw:space-y-3">
          <span class="tw:text-xs tw:uppercase tw:tracking-[0.4em] tw:text-emerald-300"
            >Cafe Ordering</span
          >
          <h2 class="tw:text-3xl tw:font-semibold">Forgot password</h2>
          <p class="tw:text-sm app-text-muted">
            Enter your email and we will send a reset link.
          </p>
        </div>

        <div class="tw:mt-8 tw:space-y-5">
          <label class="tw:block">
            <span class="app-label tw:text-xs tw:uppercase tw:tracking-[0.3em]"
              >Email</span
            >
            <prime-input-text
              type="email"
              placeholder="you@cafe.com"
              class="app-input tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:px-4 tw:py-3 tw:text-sm tw:transition focus:tw:border-emerald-400 focus:tw:outline-none focus:tw:ring-2 focus:tw:ring-emerald-300/40"
            />
          </label>
        </div>

        <prime-button
          type="button"
          class="tw:mt-8 tw:w-full tw:rounded-xl tw:bg-emerald-500 tw:px-4 tw:py-3 tw:text-sm tw:font-semibold tw:text-slate-950 tw:shadow-lg tw:shadow-emerald-500/30 tw:transition hover:tw:bg-emerald-400"
        >
          Send reset link
        </prime-button>

        <div class="tw:mt-6 tw:flex tw:items-center tw:justify-between tw:text-sm">
          <span class="app-text-subtle">Remembered your password?</span>
          <router-link class="tw:text-emerald-300 hover:tw:text-emerald-200" to="/admin/login">
            Back to login
          </router-link>
        </div>
      </form>
    </div>
  </section>
</template>

<style scoped>
.auth-card {
  animation: slide-up 0.6s ease both;
}

@keyframes slide-up {
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
