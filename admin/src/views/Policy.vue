<script setup>
import { useRouter } from 'vue-router'

const router = useRouter()

const goBack = () => {
  router.back()
}
</script>

<template>
  <section
    class="app-shell tw:flex tw:min-h-screen tw:items-center tw:justify-center tw:px-6"
  >
    <div
      class="app-card tw:w-full tw:max-w-3xl tw:rounded-3xl tw:border tw:p-10 tw:shadow-2xl tw:backdrop-blur"
    >
      <p class="tw:text-xs tw:uppercase tw:tracking-[0.4em] tw:text-emerald-300">
        Cafe Ordering
      </p>
      <h1 class="tw:mt-4 tw:text-3xl tw:font-semibold">Policy</h1>
      <p class="tw:mt-2 tw:text-sm app-text-muted">
        This is a placeholder policy page. Replace this content with your official
        terms, privacy, and data handling guidelines.
      </p>

      <div class="tw:mt-6 tw:space-y-4 tw:text-sm app-text-muted">
        <p>
          By using this service, you agree to follow the operational guidelines
          for the cafe and keep account information accurate.
        </p>
        <p>
          We only collect necessary data to fulfill orders and improve service.
          You can request updates or removal by contacting the team.
        </p>
        <p>
          Payment and delivery details are stored securely and are not shared
          with third parties outside of order fulfillment.
        </p>
      </div>

      <div class="tw:mt-8">
        <button
          type="button"
          class="tw:rounded-xl tw:bg-emerald-500 tw:px-4 tw:py-2 tw:text-sm tw:font-semibold tw:text-slate-950 tw:shadow-lg tw:shadow-emerald-500/30 tw:transition hover:tw:bg-emerald-400"
          @click="goBack"
        >
          Back
        </button>
      </div>
    </div>
  </section>
</template>
