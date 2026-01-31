<template>
  <header>
    <nav
        ref="navEl"
        class="tw:max-w-248 tw:md:max-w-280 tw:xl:max-w-340 tw:px-6 tw:fixed tw:top-4 tw:left-1/2 tw:-translate-x-1/2 tw:w-full tw:z-1000 tw:transition-all tw:duration-300"
    >
      <div
          ref="menuContainer"
          class="tw:py-4 tw:pl-4 tw:md:pl-7 tw:pr-4 tw:rounded-3xl tw:lg:rounded-full tw:border tw:border-transparent tw:transition-all tw:duration-300"
      >
        <div class="tw:flex tw:items-center tw:justify-between">
          <div class="tw:flex-1 tw:flex">
            <router-link :to="{ name: 'home' }" class="tw:w-fit" aria-current="page">
              <slot name="left">
                <div class="tw:flex tw:items-center"><img src="/logo.svg" alt="Logo" class="tw:max-w-10 tw:max-h-10" /> <span class="tw:font-bold tw:text-emerald-500">5AM Coffee</span></div>
              </slot>
            </router-link>
          </div>
          <slot name="center">
            <ul class="tw:flex-none tw:hidden tw:md:flex tw:items-center tw:gap-2">
              <li>
                <prime-button asChild v-slot="slotProps" variant="text" rounded>
                  <router-link
                      :to="{ name: 'about' }"
                      :class="slotProps.class"
                      class="tw:px-4! tw:py-2! tw:min-w-20 tw:leading-4.75 tw:hover:bg-white!"
                  >
                    About
                  </router-link>
                </prime-button>
              </li>
              <li>
                <prime-button asChild v-slot="slotProps" variant="text" rounded>
                  <router-link
                      :to="{ name: 'about' }"
                      :class="slotProps.class"
                      class="tw:px-4! tw:py-2! tw:min-w-20 tw:pb tw:leading-4.75 tw:border-0! tw:hover:bg-white!"
                  >
                    Blog
                  </router-link>
                </prime-button>
              </li>
              <li>
                <prime-button asChild v-slot="slotProps" variant="text" rounded>
                  <router-link
                      :to="{ name: 'about' }"
                      :class="slotProps.class"
                      class="tw:px-4! tw:py-2! tw:min-w-20 tw:leading-4.75 tw:border-0! tw:hover:bg-white!"
                  >
                    Contact
                  </router-link>
                </prime-button>
              </li>
            </ul>
          </slot>

          <div class="tw:flex-1 tw:hidden tw:md:flex tw:items-center tw:justify-end tw:gap-4">
            <prime-button asChild v-slot="slotProps" variant="text" rounded>
              <router-link
                  to="/"
                  :class="slotProps.class"
                  class="tw:px-4! tw:py-2! tw:min-w-20 tw:leading-4.75 tw:border-0! tw:bg-white! tw:hover:bg-gray-100 !"
              >
                Login
              </router-link>
            </prime-button>
            <prime-button asChild v-slot="slotProps" rounded>
              <router-link
                  to="/"
                  :class="slotProps.class"
                  class="tw:px-4! tw:py-2! tw:min-w-20 tw:leading-4.75 tw:border-0!"
              >
                Register
              </router-link>
            </prime-button>
            <!-- <a class="button-primary tw:py-2 tw:px-4 tw:min-w-20"> Register </a> -->
          </div>
          <button
              class="tw:flex tw:md:hidden tw:items-center tw:justify-center tw:rounded-lg tw:text-surface-950 tw:dark:text-surface-0 tw:w-9 tw:h-9 tw:border tw:border-surface-200 tw:dark:border-white/10 tw:hover:bg-surface-100 tw:dark:hover:bg-white/10 transition-all"
          >
            <iconify icon="heroicons-outline:menu-alt-2"></iconify>
          </button>
        </div>
        <div
            class="tw:md:hidden tw:block tw:transition-all tw:duration-300 tw:ease-out tw:overflow-hidden tw:opacity-0"
            style="max-height: 0px"
        >
          <div class="flex flex-col gap-8 transition-all">
            <ul class="flex flex-col gap-2">
              <li>
                <a
                    class="button py-2 bg-transparent shadow-none hover:bg-white dark:hover:bg-white/10 hover:shadow-sm border border-transparent hover:border-surface dark:hover:border-white/10"
                >About</a
                >
              </li>
              <li>
                <a
                    class="button py-2 bg-transparent shadow-none hover:bg-white dark:hover:bg-white/10 hover:shadow-sm border border-transparent hover:border-surface dark:hover:border-white/10"
                >Pricing</a
                >
              </li>
              <li>
                <a
                    class="button py-2 bg-transparent shadow-none hover:bg-white dark:hover:bg-white/10 hover:shadow-sm border border-transparent hover:border-surface dark:hover:border-white/10"
                >Contact</a
                >
              </li>
            </ul>
            <div class="tw:flex tw:flex-col tw:items-center tw:gap-4">
              <a class="button tw:py-2 tw:w-full tw:border tw:dark:border-white/10"> Login </a
              ><a class="button-primary tw:w-full tw:py-2"> Register </a>
            </div>
          </div>
        </div>
      </div>
    </nav>
  </header>
</template>

<script setup>
const navEl = ref(null)
const menuContainer = ref(null)

const navExtraClasses = [
  'tw:md:min-w-[360px]!',
  'tw:max-w-[320px]!',
  'tw:md:max-w-[720px]!',
  'tw:lg:max-w-[900px]!',
]
const extraClasses = [
  'tw:backdrop-blur-[90px]',
  'tw:border-surface-200',
  'tw:dark:border-white/10s',
  'tw:bg-white/64',
  'tw:dark:!bg-white/12',
  'tw:shadow-sm',
]
const threshold = 1 // px, tăng nếu muốn chờ nhiều hơn trước khi active

let lastKnownScrollY = 0
let ticking = false

function update() {
  const y = lastKnownScrollY
  const scrolled = y > threshold

  if (!navEl.value) {
    ticking = false
    return
  }

  // thêm hoặc xóa tất cả classes trong extraClasses
  if (scrolled) {
    navEl.value.classList.add(...navExtraClasses)
    menuContainer.value.classList.add(...extraClasses)
  } else {
    navEl.value.classList.remove(...navExtraClasses)
    menuContainer.value.classList.remove(...extraClasses)
  }

  ticking = false
}

function onScrollHandler() {
  lastKnownScrollY = window.scrollY || window.pageYOffset
  if (!ticking) {
    ticking = true
    window.requestAnimationFrame(update)
  }
}

onMounted(() => {
  // tôn trọng prefers-reduced-motion: nếu bật, bạn có thể bỏ transition ở CSS; vẫn toggle class.
  // kiểm tra initial position (nếu load không ở top)
  lastKnownScrollY = window.scrollY || window.pageYOffset
  update()

  window.addEventListener('scroll', onScrollHandler, { passive: true })
})

onBeforeUnmount(() => {
  window.removeEventListener('scroll', onScrollHandler)
})
</script>