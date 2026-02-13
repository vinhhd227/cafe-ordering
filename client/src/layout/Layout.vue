<template>
  <div class="tw:min-h-screen tw:bg-slate-950 tw:text-slate-100">
    <div class="tw:flex tw:min-h-screen">
      <aside
        class="tw:w-64 tw:border-r tw:border-white/10 tw:bg-slate-900/80 tw:p-6 tw:backdrop-blur"
      >
        <div class="tw:space-y-6">
          <div>
            <p class="tw:text-xs tw:uppercase tw:tracking-[0.4em] tw:text-emerald-300">
              Cafe Ordering
            </p>
            <h1 class="tw:mt-2 tw:text-lg tw:font-semibold">Admin Panel</h1>
          </div>

          <nav class="tw:space-y-2">
            <router-link
              class="tw:block tw:rounded-xl tw:px-4 tw:py-2 tw:text-sm tw:text-slate-300 tw:transition hover:tw:bg-white/5 hover:tw:text-white"
              to="/admin/dashboard"
            >
              Dashboard
            </router-link>
            <router-link
              class="tw:block tw:rounded-xl tw:px-4 tw:py-2 tw:text-sm tw:text-slate-300 tw:transition hover:tw:bg-white/5 hover:tw:text-white"
              to="/admin/orders"
            >
              Orders
            </router-link>
            <router-link
              class="tw:block tw:rounded-xl tw:px-4 tw:py-2 tw:text-sm tw:text-slate-300 tw:transition hover:tw:bg-white/5 hover:tw:text-white"
              to="/admin/menu"
            >
              Menu
            </router-link>
            <router-link
              class="tw:block tw:rounded-xl tw:px-4 tw:py-2 tw:text-sm tw:text-slate-300 tw:transition hover:tw:bg-white/5 hover:tw:text-white"
              to="/admin/staff"
            >
              Staff
            </router-link>
          </nav>
        </div>
      </aside>

      <div class="tw:flex tw:flex-1 tw:flex-col">
        <header
          class="tw:flex tw:items-center tw:justify-between tw:border-b tw:border-white/10 tw:bg-slate-900/50 tw:px-8 tw:py-5 tw:backdrop-blur"
        >
          <div>
            <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-slate-400">
              Admin workspace
            </p>
            <h2 class="tw:text-lg tw:font-semibold">Good shift, team.</h2>
          </div>
          <div class="tw:flex tw:items-center tw:gap-4">
            <button
              type="button"
              class="tw:rounded-xl tw:border tw:border-white/10 tw:px-4 tw:py-2 tw:text-sm tw:text-slate-200 tw:transition hover:tw:bg-white/5"
            >
              Export
            </button>
            <div class="tw:flex tw:items-center tw:gap-3">
              <div class="tw:h-10 tw:w-10 tw:rounded-full tw:bg-emerald-500/20 tw:text-emerald-200 tw:flex tw:items-center tw:justify-center tw:text-sm">
                AO
              </div>
              <div class="tw:text-sm">
                <p class="tw:font-semibold">Avery Ong</p>
                <p class="tw:text-xs tw:text-slate-400">Shift lead</p>
              </div>
            </div>
          </div>
        </header>

        <main class="tw:flex-1 tw:px-8 tw:py-8">
          <router-view />
        </main>
      </div>
    </div>
  </div>
</template>
