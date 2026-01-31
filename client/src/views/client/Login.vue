<script setup></script>

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
      <form class="auth-card tw:w-full tw:rounded-3xl tw:bg-white tw:p-8 tw:shadow-2xl">
        <div class="tw:space-y-3">
          <span class="tw:text-xs tw:uppercase tw:tracking-[0.4em] tw:text-amber-600"
            >Cafe Ordering</span
          >
          <h2 class="tw:text-3xl tw:font-semibold">Customer login</h2>
          <p class="tw:text-sm tw:text-slate-500">
            Sign in to track orders and manage favorites.
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
              class="tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:border-slate-200 tw:bg-slate-50 tw:px-4 tw:py-3 tw:text-sm tw:text-slate-900 tw:placeholder-slate-400 tw:transition focus:tw:border-amber-400 focus:tw:outline-none focus:tw:ring-2 focus:tw:ring-amber-200"
            />
          </label>
          <label class="tw:block">
            <span class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-slate-400"
              >Password</span
            >
            <prime-input-text
              type="password"
              placeholder="••••••••"
              class="tw:mt-3 tw:w-full tw:rounded-xl tw:border tw:border-slate-200 tw:bg-slate-50 tw:px-4 tw:py-3 tw:text-sm tw:text-slate-900 tw:placeholder-slate-400 tw:transition focus:tw:border-amber-400 focus:tw:outline-none focus:tw:ring-2 focus:tw:ring-amber-200"
            />
          </label>
        </div>

        <div class="tw:mt-6 tw:flex tw:items-center tw:justify-between">
          <label class="tw:flex tw:items-center tw:gap-2 tw:text-sm tw:text-slate-500">
            <input type="checkbox" class="tw:h-4 tw:w-4 tw:rounded tw:border-slate-300" />
            Keep me signed in
          </label>
          <a class="tw:text-sm tw:text-amber-600 hover:tw:text-amber-700" href="#">
            Forgot password?
          </a>
        </div>

        <prime-button
          type="button"
          class="tw:mt-8 tw:w-full tw:rounded-xl tw:bg-amber-500 tw:px-4 tw:py-3 tw:text-sm tw:font-semibold tw:text-white tw:shadow-lg tw:shadow-amber-500/30 tw:transition hover:tw:bg-amber-400"
        >
          Sign in
        </prime-button>

        <div class="tw:mt-6 tw:flex tw:items-center tw:justify-between tw:text-sm">
          <span class="tw:text-slate-500">New here?</span>
          <router-link class="tw:text-amber-600 hover:tw:text-amber-700" to="/register">
            Create an account
          </router-link>
        </div>
      </form>
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
