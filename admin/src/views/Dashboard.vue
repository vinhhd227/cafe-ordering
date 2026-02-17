<script setup></script>

<template>
  <section class="tw:space-y-8">
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">
          Today overview
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">Dashboard</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          Snapshot of orders, inventory, and staff performance.
        </p>
      </div>
      <div class="tw:flex tw:items-center tw:gap-3">
        <prime-button
          label="Download report"
          severity="secondary"
          outlined
          size="small"
        />
        <prime-button
          label="New order"
          severity="success"
          size="small"
          class="tw:shadow-lg tw:shadow-emerald-500/30"
        />
      </div>
    </div>

    <div class="tw:grid tw:grid-cols-1 tw:gap-6 lg:tw:grid-cols-3">
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-subtle">
            Orders
          </p>
          <p class="tw:mt-3 tw:text-3xl tw:font-semibold">128</p>
          <div class="tw:mt-2">
            <prime-tag value="+12% vs yesterday" severity="success" />
          </div>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-subtle">
            Revenue
          </p>
          <p class="tw:mt-3 tw:text-3xl tw:font-semibold">$2,410</p>
          <div class="tw:mt-2">
            <prime-tag value="Peak at 9:30 AM" severity="info" />
          </div>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-subtle">
            Avg ticket
          </p>
          <p class="tw:mt-3 tw:text-3xl tw:font-semibold">$18.85</p>
          <div class="tw:mt-2">
            <prime-tag value="Most add-ons at 2 PM" severity="success" />
          </div>
        </template>
      </prime-card>
    </div>

    <div class="tw:grid tw:grid-cols-1 tw:gap-6 lg:tw:grid-cols-2">
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #title>Live queue</template>
        <template #subtitle>6 waiting</template>
        <template #content>
          <div class="tw:space-y-3">
            <div class="app-panel tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:border tw:p-4">
              <div>
                <p class="tw:text-sm tw:font-semibold">Order #1048</p>
                <p class="tw:text-xs app-text-muted">2 lattes, 1 croissant</p>
              </div>
              <prime-tag value="In prep" severity="warning" />
            </div>
            <div class="app-panel tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:border tw:p-4">
              <div>
                <p class="tw:text-sm tw:font-semibold">Order #1049</p>
                <p class="tw:text-xs app-text-muted">Americano, matcha latte</p>
              </div>
              <prime-tag value="Queued" severity="info" />
            </div>
            <div class="app-panel tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:border tw:p-4">
              <div>
                <p class="tw:text-sm tw:font-semibold">Order #1050</p>
                <p class="tw:text-xs app-text-muted">
                  Iced mocha, almond croissant
                </p>
              </div>
              <prime-tag value="Queued" severity="info" />
            </div>
          </div>
        </template>
      </prime-card>

      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #title>Team focus</template>
        <template #subtitle>Shift status</template>
        <template #content>
          <div class="tw:space-y-4">
            <div class="tw:flex tw:items-center tw:justify-between">
              <div>
                <p class="tw:text-sm tw:font-semibold">Front counter</p>
              <p class="tw:text-xs app-text-muted">2 staff online</p>
            </div>
            <prime-tag value="Ready" severity="success" />
          </div>
          <div class="app-panel tw:h-2 tw:w-full tw:overflow-hidden tw:rounded-full tw:border">
            <div class="tw:h-full tw:w-2/3 tw:rounded-full tw:bg-emerald-500"></div>
          </div>
          <div class="tw:flex tw:items-center tw:justify-between">
            <div>
              <p class="tw:text-sm tw:font-semibold">Espresso bar</p>
              <p class="tw:text-xs app-text-muted">3 staff online</p>
            </div>
            <prime-tag value="Busy" severity="warning" />
          </div>
          <div class="app-panel tw:h-2 tw:w-full tw:overflow-hidden tw:rounded-full tw:border">
            <div class="tw:h-full tw:w-4/5 tw:rounded-full tw:bg-emerald-500"></div>
          </div>
          <div class="tw:flex tw:items-center tw:justify-between">
            <div>
              <p class="tw:text-sm tw:font-semibold">Pickup handoff</p>
              <p class="tw:text-xs app-text-muted">1 runner on standby</p>
            </div>
            <prime-tag value="Stable" severity="info" />
          </div>
          <div class="app-panel tw:h-2 tw:w-full tw:overflow-hidden tw:rounded-full tw:border">
            <div class="tw:h-full tw:w-1/2 tw:rounded-full tw:bg-emerald-500"></div>
          </div>
          </div>
        </template>
        <template #footer>
          <div class="tw:flex tw:justify-end">
            <prime-button label="Manage" severity="secondary" outlined size="small" />
          </div>
        </template>
      </prime-card>
    </div>
  </section>
</template>
