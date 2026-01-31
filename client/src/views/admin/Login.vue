<script setup></script>

<template>
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
      <form
        class="auth-card tw:w-full tw:rounded-3xl tw:border tw:border-white/10 tw:bg-slate-900/80 tw:p-8 tw:text-slate-100 tw:shadow-2xl tw:backdrop-blur"
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
          <label class="tw:block">
            <span class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-slate-400"
              >Email</span
            >
            <prime-input-text
              type="email"
              placeholder="you@cafe.com"
              class="tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:border-white/10 tw:bg-slate-950/60 tw:px-4 tw:py-3 tw:text-sm tw:text-slate-100 tw:placeholder-slate-500 tw:transition focus:tw:border-emerald-400 focus:tw:outline-none focus:tw:ring-2 focus:tw:ring-emerald-300/40"
            />
          </label>
          <label class="tw:block">
            <span class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-slate-400"
              >Password</span
            >
            <prime-input-text
              type="password"
              placeholder="••••••••"
              class="tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:border-white/10 tw:bg-slate-950/60 tw:px-4 tw:py-3 tw:text-sm tw:text-slate-100 tw:placeholder-slate-500 tw:transition focus:tw:border-emerald-400 focus:tw:outline-none focus:tw:ring-2 focus:tw:ring-emerald-300/40"
            />
          </label>
        </div>

        <div class="tw:mt-6 tw:flex tw:items-center tw:justify-between">
          <label class="tw:flex tw:items-center tw:gap-2 tw:text-sm tw:text-slate-300">
            <input
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
          type="button"
          class="tw:mt-8 tw:w-full tw:rounded-xl tw:bg-emerald-500 tw:px-4 tw:py-3 tw:text-sm tw:font-semibold tw:text-slate-950 tw:shadow-lg tw:shadow-emerald-500/30 tw:transition hover:tw:bg-emerald-400"
        >
          Access dashboard
        </prime-button>

        <div class="tw:mt-6 tw:flex tw:items-center tw:justify-between tw:text-sm">
          <span class="tw:text-slate-400">New to the crew?</span>
          <router-link class="tw:text-emerald-300 hover:tw:text-emerald-200" to="/admin/register">
            Create an account
          </router-link>
        </div>
      </form>

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
  animation: slide-up 0.6s ease both;
}

.auth-card--delay {
  animation-delay: 0.15s;
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
