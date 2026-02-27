<script setup>
import { computed, onMounted, ref } from "vue";
import { getAdminMenu } from "@/services/menu.service";
import { toggleCategoryActive } from "@/services/category.service";
import { toggleProductActive } from "@/services/product.service";

// ── State ──────────────────────────────────────────────────────────
const menuData = ref([]);
const loading = ref(false);
const errorMessage = ref("");
const toggling = ref(new Set()); // key = "cat-{id}" | "prod-{id}"
const openPanels = ref([]);

// ── Computed stats ─────────────────────────────────────────────────
const stats = computed(() => {
  let activeProducts = 0;
  let inactiveProducts = 0;
  for (const cat of menuData.value) {
    for (const p of cat.products) {
      if (p.isActive) activeProducts++;
      else inactiveProducts++;
    }
  }
  return {
    totalCategories: menuData.value.length,
    activeProducts,
    inactiveProducts,
  };
});

// ── Helpers ────────────────────────────────────────────────────────
const formatVnd = (value) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(value ?? 0);

const extractError = (err) =>
  err?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join("; ") ||
  err?.response?.data?.message;

// ── Load ──────────────────────────────────────────────────────────
const loadMenu = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await getAdminMenu();
    menuData.value = res?.data ?? [];
    // Open all panels by default
    openPanels.value = menuData.value.map((c) => String(c.id));
  } catch (err) {
    errorMessage.value = extractError(err) || "Failed to load menu.";
  } finally {
    loading.value = false;
  }
};

onMounted(loadMenu);

// ── Toggle category ───────────────────────────────────────────────
const toggleCategory = async (cat) => {
  const key = `cat-${cat.id}`;
  if (toggling.value.has(key)) return;

  const oldActive = cat.isActive;
  cat.isActive = !oldActive; // optimistic
  toggling.value = new Set([...toggling.value, key]);

  try {
    await toggleCategoryActive(cat.id);
  } catch (err) {
    cat.isActive = oldActive; // revert
    errorMessage.value = extractError(err) || "Failed to update category.";
  } finally {
    const next = new Set(toggling.value);
    next.delete(key);
    toggling.value = next;
  }
};

// ── Toggle product ─────────────────────────────────────────────────
const toggleProduct = async (product) => {
  const key = `prod-${product.id}`;
  if (toggling.value.has(key)) return;

  const oldActive = product.isActive;
  product.isActive = !oldActive; // optimistic
  toggling.value = new Set([...toggling.value, key]);

  try {
    await toggleProductActive(product.id);
  } catch (err) {
    product.isActive = oldActive; // revert
    errorMessage.value = extractError(err) || "Failed to update product.";
  } finally {
    const next = new Set(toggling.value);
    next.delete(key);
    toggling.value = next;
  }
};
</script>

<template>
  <section class="tw:space-y-6">
    <!-- ── Header ─────────────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p
          class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300"
        >
          Menu
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">Menu builder</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          Activate or deactivate categories and products at a glance.
        </p>
      </div>
      <prime-button
        severity="secondary"
        outlined
        size="small"
        :loading="loading"
        @click="loadMenu"
      >
        <iconify icon="ph:arrows-clockwise-bold" />
        <span>Refresh</span>
      </prime-button>
    </div>

    <!-- ── Summary Stats ──────────────────────────────────────────── -->
    <div class="tw:grid tw:grid-cols-3 tw:gap-3">
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p
            class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle"
          >
            Categories
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">
            {{ stats.totalCategories }}
          </p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p
            class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-emerald-400"
          >
            Active products
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">
            {{ stats.activeProducts }}
          </p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p
            class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-red-400"
          >
            Inactive products
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">
            {{ stats.inactiveProducts }}
          </p>
        </template>
      </prime-card>
    </div>

    <!-- ── Error alert ─────────────────────────────────────────────── -->
    <prime-alert
      v-if="errorMessage"
      severity="error"
      variant="accent"
      closable
      @close="errorMessage = ''"
      >{{ errorMessage }}</prime-alert
    >

    <!-- ── Loading skeleton ────────────────────────────────────────── -->
    <div v-if="loading" class="tw:space-y-4">
      <prime-card
        v-for="i in 3"
        :key="i"
        class="app-card tw:rounded-2xl tw:border"
      >
        <template #content>
          <div class="tw:flex tw:items-center tw:gap-3 tw:py-1">
            <prime-skeleton shape="circle" size="2rem" />
            <prime-skeleton width="10rem" height="1.25rem" />
            <prime-skeleton width="3rem" height="1.25rem" />
          </div>
          <div
            class="tw:mt-4 tw:grid tw:grid-cols-2 tw:gap-3 tw:sm:grid-cols-3 tw:lg:grid-cols-4"
          >
            <prime-skeleton
              v-for="j in 4"
              :key="j"
              height="7rem"
              class="tw:rounded-xl"
            />
          </div>
        </template>
      </prime-card>
    </div>

    <!-- ── Menu Accordion ─────────────────────────────────────────── -->
    <template v-else-if="menuData.length">
      <prime-accordion
        multiple
        v-model:value="openPanels"
        class="tw:flex tw:flex-col tw:gap-3"
      >
        <prime-accordion-panel
          v-for="cat in menuData"
          :key="cat.id"
          :value="String(cat.id)"
          class="app-card tw:rounded-2xl tw:border tw:overflow-hidden tw:transition-opacity"
          :class="{ 'tw:opacity-60': !cat.isActive }"
        >
          <!-- ── Category header ─────────────────────────────────── -->
          <prime-accordion-header>
            <div
              class="tw:flex tw:w-full tw:items-center tw:gap-3 tw:min-w-0 tw:py-0.5"
            >
              <!-- Toggle (stop propagation → won't collapse accordion) -->
              <div @click.stop class="tw:shrink-0">
                <prime-toggle-switch
                  :model-value="cat.isActive"
                  @update:model-value="toggleCategory(cat)"
                  :disabled="toggling.has(`cat-${cat.id}`)"
                />
              </div>

              <!-- Category name -->
              <span class="tw:font-semibold tw:text-base tw:truncate">
                {{ cat.name }}
              </span>

              <!-- Product count badge -->
              <prime-badge
                :value="cat.products.length"
                severity="secondary"
                class="tw:shrink-0"
              />

              <!-- Inactive tag -->
              <prime-tag
                v-if="!cat.isActive"
                value="Inactive"
                severity="danger"
                class="tw:shrink-0"
              />

              <!-- Spinner while toggling -->
              <iconify
                v-if="toggling.has(`cat-${cat.id}`)"
                icon="ph:circle-notch-bold"
                class="tw:animate-spin tw:shrink-0 app-text-muted"
              />

              <!-- Description (shown on wider screens) -->
              <span
                v-if="cat.description"
                class="tw:ml-auto tw:text-xs app-text-muted tw:truncate tw:hidden tw:md:block tw:max-w-xs"
              >
                {{ cat.description }}
              </span>
            </div>
          </prime-accordion-header>

          <!-- ── Product grid ────────────────────────────────────── -->
          <prime-accordion-content>
            <div class="tw:px-4 tw:pb-4 tw:pt-2">
              <!-- Empty state -->
              <div
                v-if="cat.products.length === 0"
                class="tw:flex tw:flex-col tw:items-center tw:py-8 app-text-muted"
              >
                <iconify icon="ph:coffee-bold" class="tw:text-2xl tw:mb-2" />

                <p class="tw:text-sm">No products in this category.</p>
              </div>

              <!-- Product cards grid -->
              <div
                v-else
                class="tw:grid tw:grid-cols-2 tw:gap-3 tw:sm:grid-cols-3 tw:lg:grid-cols-4 tw:xl:grid-cols-5"
              >
                <div
                  v-for="product in cat.products"
                  :key="product.id"
                  class="tw:rounded-xl tw:border tw:p-3 tw:flex tw:flex-col tw:gap-2 tw:transition-opacity"
                  :class="[
                    product.isActive
                      ? 'tw:border-white/10 tw:bg-white/5'
                      : 'tw:border-white/5 tw:bg-white/2 tw:opacity-50',
                  ]"
                >
                  <!-- Image row + toggle -->
                  <div
                    class="tw:flex tw:items-start tw:justify-between tw:gap-2"
                  >
                    <img
                      v-if="product.imageUrl"
                      :src="product.imageUrl"
                      :alt="product.name"
                      class="tw:h-12 tw:w-12 tw:rounded-lg tw:object-cover tw:shrink-0"
                    />
                    <div
                      v-else
                      class="tw:h-12 tw:w-12 tw:rounded-lg tw:bg-primary/20 tw:shrink-0 tw:flex tw:items-center tw:justify-center"
                    >
                      <iconify
                        icon="ph:coffee-bold"
                        class="tw:text-lg app-text-muted"
                      />
                    </div>

                    <!-- Toggle + busy indicator -->
                    <div class="tw:relative tw:shrink-0">
                      <prime-toggle-switch
                        :model-value="product.isActive"
                        @update:model-value="toggleProduct(product)"
                        :disabled="toggling.has(`prod-${product.id}`)"
                        class="tw:scale-90"
                      />
                      <iconify
                        v-if="toggling.has(`prod-${product.id}`)"
                        icon="ph:circle-notch-bold"
                        class="tw:animate-spin tw:absolute tw:-top-0.5 tw:-right-0.5 tw:text-xs app-text-muted"
                      />
                    </div>
                  </div>

                  <!-- Product name -->
                  <p
                    class="tw:text-sm tw:font-medium tw:leading-snug tw:line-clamp-2"
                  >
                    {{ product.name }}
                  </p>

                  <!-- Price + inactive tag -->
                  <div
                    class="tw:flex tw:items-center tw:justify-between tw:gap-1 tw:mt-auto"
                  >
                    <span class="tw:text-xs tw:font-semibold tw:tabular-nums">
                      {{ formatVnd(product.price) }}
                    </span>
                    <prime-tag
                      v-if="!product.isActive"
                      value="Off"
                      severity="danger"
                    />
                  </div>
                </div>
              </div>
            </div>
          </prime-accordion-content>
        </prime-accordion-panel>
      </prime-accordion>
    </template>

    <!-- ── Empty state ────────────────────────────────────────────── -->
    <prime-card v-else class="app-card tw:rounded-2xl tw:border">
      <template #content>
        <div
          class="tw:flex tw:flex-col tw:items-center tw:py-12 app-text-muted"
        >
          <iconify icon="ph:list-bold" class="tw:text-3xl tw:mb-2" />
          <p class="tw:text-sm">No categories found.</p>
        </div>
      </template>
    </prime-card>
  </section>
</template>
