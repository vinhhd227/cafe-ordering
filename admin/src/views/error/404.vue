<script setup></script>

<template>
  <section
    class="app-shell tw:flex tw:min-h-screen tw:items-center tw:justify-center tw:px-6"
  >
    <div class="app-card tw:w-full tw:max-w-xl tw:rounded-3xl tw:border tw:p-10 tw:text-center tw:shadow-2xl tw:backdrop-blur">
      <p class="tw:text-xs tw:uppercase tw:tracking-[0.4em] tw:text-primary-300">
        Cafe Ordering
      </p>
      <h1 class="tw:mt-4 tw:text-4xl tw:font-semibold">404</h1>
      <p class="tw:mt-2 tw:text-lg tw:text-muted">Page not found</p>
      <p class="tw:mt-4 tw:text-sm tw:text-muted">
        The page you are looking for does not exist or has been moved.
      </p>
      <div class="tw:mt-8 tw:flex tw:flex-wrap tw:justify-center tw:gap-3">
        <router-link
          class="tw:rounded-xl tw:bg-primary-500 tw:px-4 tw:py-2 tw:text-sm tw:font-semibold tw:text-slate-950 tw:shadow-lg tw:shadow-primary-500/30 tw:transition tw:hover:bg-primary-400"
          to="/"
        >
          Back to home
        </router-link>
        <router-link
          class="tw:rounded-xl tw:border app-border tw:px-4 tw:py-2 tw:text-sm tw:text-muted tw:transition tw:hover:bg-white/5"
          to="/"
        >
          Go to dashboard
        </router-link>
      </div>
    </div>
  </section>
</template>
