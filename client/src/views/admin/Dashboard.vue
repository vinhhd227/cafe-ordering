<script setup></script>

<template>
  <section class="tw:space-y-8">
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">
          Today overview
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">Dashboard</h1>
        <p class="tw:mt-2 tw:text-sm tw:text-slate-400">
          Snapshot of orders, inventory, and staff performance.
        </p>
      </div>
      <div class="tw:flex tw:items-center tw:gap-3">
        <button
          type="button"
          class="tw:rounded-xl tw:border tw:border-white/10 tw:px-4 tw:py-2 tw:text-sm tw:text-slate-200 tw:transition hover:tw:bg-white/5"
        >
          Download report
        </button>
        <button
          type="button"
          class="tw:rounded-xl tw:bg-emerald-500 tw:px-4 tw:py-2 tw:text-sm tw:font-semibold tw:text-slate-950 tw:shadow-lg tw:shadow-emerald-500/30 tw:transition hover:tw:bg-emerald-400"
        >
          New order
        </button>
      </div>
    </div>

    <div class="tw:grid tw:grid-cols-1 tw:gap-6 lg:tw:grid-cols-3">
      <div class="tw:rounded-2xl tw:border tw:border-white/10 tw:bg-slate-900/60 tw:p-6">
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-slate-400">Orders</p>
        <p class="tw:mt-3 tw:text-3xl tw:font-semibold">128</p>
        <p class="tw:mt-2 tw:text-sm tw:text-emerald-300">+12% vs yesterday</p>
      </div>
      <div class="tw:rounded-2xl tw:border tw:border-white/10 tw:bg-slate-900/60 tw:p-6">
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-slate-400">Revenue</p>
        <p class="tw:mt-3 tw:text-3xl tw:font-semibold">$2,410</p>
        <p class="tw:mt-2 tw:text-sm tw:text-emerald-300">Peak at 9:30 AM</p>
      </div>
      <div class="tw:rounded-2xl tw:border tw:border-white/10 tw:bg-slate-900/60 tw:p-6">
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-slate-400">
          Avg ticket
        </p>
        <p class="tw:mt-3 tw:text-3xl tw:font-semibold">$18.85</p>
        <p class="tw:mt-2 tw:text-sm tw:text-emerald-300">Most add-ons at 2 PM</p>
      </div>
    </div>

    <div class="tw:grid tw:grid-cols-1 tw:gap-6 lg:tw:grid-cols-2">
      <div class="tw:rounded-2xl tw:border tw:border-white/10 tw:bg-slate-900/60 tw:p-6">
        <div class="tw:flex tw:items-center tw:justify-between">
          <h3 class="tw:text-lg tw:font-semibold">Live queue</h3>
          <span class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-slate-400">
            6 waiting
          </span>
        </div>
        <div class="tw:mt-4 tw:space-y-3">
          <div class="tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:bg-slate-950/60 tw:p-4">
            <div>
              <p class="tw:text-sm tw:font-semibold">Order #1048</p>
              <p class="tw:text-xs tw:text-slate-400">2 lattes, 1 croissant</p>
            </div>
            <span class="tw:text-xs tw:text-emerald-300">In prep</span>
          </div>
          <div class="tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:bg-slate-950/60 tw:p-4">
            <div>
              <p class="tw:text-sm tw:font-semibold">Order #1049</p>
              <p class="tw:text-xs tw:text-slate-400">Americano, matcha latte</p>
            </div>
            <span class="tw:text-xs tw:text-emerald-300">Queued</span>
          </div>
          <div class="tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:bg-slate-950/60 tw:p-4">
            <div>
              <p class="tw:text-sm tw:font-semibold">Order #1050</p>
              <p class="tw:text-xs tw:text-slate-400">Iced mocha, almond croissant</p>
            </div>
            <span class="tw:text-xs tw:text-emerald-300">Queued</span>
          </div>
        </div>
      </div>

      <div class="tw:rounded-2xl tw:border tw:border-white/10 tw:bg-slate-900/60 tw:p-6">
        <div class="tw:flex tw:items-center tw:justify-between">
          <h3 class="tw:text-lg tw:font-semibold">Team focus</h3>
          <button
            type="button"
            class="tw:rounded-xl tw:border tw:border-white/10 tw:px-3 tw:py-1 tw:text-xs tw:text-slate-300"
          >
            Manage
          </button>
        </div>
        <div class="tw:mt-4 tw:space-y-4">
          <div class="tw:flex tw:items-center tw:justify-between">
            <div>
              <p class="tw:text-sm tw:font-semibold">Front counter</p>
              <p class="tw:text-xs tw:text-slate-400">2 staff online</p>
            </div>
            <span class="tw:text-xs tw:text-emerald-300">Ready</span>
          </div>
          <div class="tw:h-2 tw:w-full tw:overflow-hidden tw:rounded-full tw:bg-slate-950/60">
            <div class="tw:h-full tw:w-2/3 tw:rounded-full tw:bg-emerald-500"></div>
          </div>
          <div class="tw:flex tw:items-center tw:justify-between">
            <div>
              <p class="tw:text-sm tw:font-semibold">Espresso bar</p>
              <p class="tw:text-xs tw:text-slate-400">3 staff online</p>
            </div>
            <span class="tw:text-xs tw:text-emerald-300">Busy</span>
          </div>
          <div class="tw:h-2 tw:w-full tw:overflow-hidden tw:rounded-full tw:bg-slate-950/60">
            <div class="tw:h-full tw:w-4/5 tw:rounded-full tw:bg-emerald-500"></div>
          </div>
          <div class="tw:flex tw:items-center tw:justify-between">
            <div>
              <p class="tw:text-sm tw:font-semibold">Pickup handoff</p>
              <p class="tw:text-xs tw:text-slate-400">1 runner on standby</p>
            </div>
            <span class="tw:text-xs tw:text-emerald-300">Stable</span>
          </div>
          <div class="tw:h-2 tw:w-full tw:overflow-hidden tw:rounded-full tw:bg-slate-950/60">
            <div class="tw:h-full tw:w-1/2 tw:rounded-full tw:bg-emerald-500"></div>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>
