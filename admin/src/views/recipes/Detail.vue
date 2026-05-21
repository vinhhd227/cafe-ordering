<script setup>
import { marked } from 'marked'
import DOMPurify from 'dompurify'
import { getRecipe, deleteRecipe, chatRecipe } from '@/services/recipe.service'

const { t }   = useI18n()
const { can } = usePermission()
const router  = useRouter()
const route   = useRoute()
const toast   = useToast()

const id = computed(() => parseInt(route.params.id))

// ── Load ───────────────────────────────────────────────────────────
const loading      = ref(false)
const errorMessage = ref('')
const recipe       = ref(null)
const recipeContentHtml = computed(() =>
  recipe.value?.content ? DOMPurify.sanitize(marked.parse(recipe.value.content)) : ''
)

const load = async () => {
  loading.value = true
  try {
    const res = await getRecipe(id.value)
    recipe.value = res.data
  } catch {
    errorMessage.value = t('recipes.error.loadDetailFailed')
  } finally {
    loading.value = false
  }
}

// ── Tag helpers ────────────────────────────────────────────────────
const typeTag = (type) => type === 'BaseIngredient'
  ? { label: t('recipes.type.BaseIngredient'), severity: 'warn' }
  : { label: t('recipes.type.Drink'),          severity: 'info' }

// ── Delete ─────────────────────────────────────────────────────────
const handleDelete = async () => {
  try {
    await deleteRecipe(id.value)
    toast.add({ severity: 'success', summary: t('common.deleted'), life: 2500 })
    router.push({ name: 'recipes' })
  } catch {
    toast.add({ severity: 'error', summary: t('recipes.error.deleteFailed'), life: 3000 })
  }
}

// ── Chat ───────────────────────────────────────────────────────────
const chatMessages = ref([])
const chatInput    = ref('')
const chatLoading  = ref(false)
const chatBody     = ref(null)

const scrollChat = async () => {
  await nextTick()
  if (chatBody.value) chatBody.value.scrollTop = chatBody.value.scrollHeight
}

const sendChat = async () => {
  const text = chatInput.value.trim()
  if (!text || chatLoading.value) return

  chatMessages.value.push({ role: 'user', text })
  chatInput.value = ''
  chatLoading.value = true
  await scrollChat()

  try {
    const history = chatMessages.value.slice(0, -1).map(m => ({ role: m.role, text: m.text }))
    const res = await chatRecipe(id.value, { history, message: text })
    chatMessages.value.push({ role: 'model', text: res.data.reply })
    await scrollChat()
  } catch {
    chatMessages.value.push({ role: 'error', text: t('recipes.error.chatFailed') })
    await scrollChat()
  } finally {
    chatLoading.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="tw:space-y-6">

    <!-- Header -->
    <div class="tw:flex tw:flex-wrap tw:items-start tw:justify-between tw:gap-4">
      <div>
        <button
          class="tw:flex tw:items-center tw:gap-1.5 tw:text-xs tw:text-muted tw:mb-3 tw:cursor-pointer tw:bg-transparent tw:border-0 tw:p-0 tw:hover:text-primary-500 tw:transition-colors"
          @click="router.push({ name: 'recipes' })"
        >
          <iconify icon="ph:arrow-left-bold" />
          {{ t('recipes.detail.backToList') }}
        </button>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-primary-300">{{ t('nav.groups.operations') }}</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ recipe?.name ?? '...' }}</h1>

        <!-- Type + Category tags -->
        <div v-if="recipe" class="tw:mt-2 tw:flex tw:gap-2 tw:flex-wrap">
          <prime-tag :value="typeTag(recipe.type).label" :severity="typeTag(recipe.type).severity" />
          <prime-tag :value="t(`recipes.category.${recipe.category}`)" severity="secondary" />
        </div>
      </div>

      <div v-if="recipe" class="tw:flex tw:gap-2">
        <prime-button
          v-if="can('recipe.delete')"
          severity="danger" outlined size="small"
          @click="handleDelete"
        >
          <iconify icon="ph:trash-bold" />
          <span>{{ t('recipes.actions.delete') }}</span>
        </prime-button>
        <prime-button
          v-if="can('recipe.update')"
          severity="primary" size="small"
          @click="router.push({ name: 'recipeEdit', params: { id } })"
        >
          <iconify icon="ph:pencil-bold" />
          <span>{{ t('recipes.detail.editBtn') }}</span>
        </prime-button>
      </div>
    </div>

    <!-- Error -->
    <prime-alert v-if="errorMessage" severity="error" variant="accent" closable @close="errorMessage = ''">
      {{ errorMessage }}
    </prime-alert>

    <!-- Skeleton -->
    <div v-if="loading" class="tw:grid tw:grid-cols-1 tw:lg:grid-cols-3 tw:gap-6">
      <div class="tw:lg:col-span-2 tw:space-y-4">
        <prime-skeleton height="2rem" width="8rem" />
        <prime-skeleton height="1rem" />
        <prime-skeleton height="14rem" />
        <prime-skeleton height="3rem" />
      </div>
      <prime-skeleton height="540px" />
    </div>

    <!-- Content -->
    <div v-else-if="recipe" class="tw:grid tw:grid-cols-1 tw:lg:grid-cols-3 tw:gap-6 tw:items-start">

      <!-- ── Left: Recipe view (2/3) ──────────────────────────────── -->
      <div class="tw:lg:col-span-2 tw:space-y-4">

        <!-- Yield -->
        <div v-if="recipe.yield" :class="appCard" class="tw:rounded-xl tw:px-4 tw:py-3 tw:flex tw:items-center tw:gap-3">
          <iconify icon="ph:beaker-bold" class="tw:text-lg tw:text-primary-400 tw:shrink-0" />
          <div>
            <p class="tw:text-[11px] tw:uppercase tw:tracking-widest app-text-subtle">{{ t('recipes.form.yield') }}</p>
            <p class="tw:text-sm tw:font-medium tw:mt-0.5">{{ recipe.yield }}</p>
          </div>
        </div>

        <!-- Content -->
        <div :class="appCard" class="tw:rounded-2xl tw:p-5 tw:space-y-2">
          <p class="tw:text-[11px] tw:uppercase tw:tracking-widest app-text-subtle">{{ t('recipes.form.content') }}</p>
          <div class="tw:prose tw:prose-invert tw:prose-sm tw:max-w-none" v-html="recipeContentHtml" />
        </div>

        <!-- Notes -->
        <div v-if="recipe.notes" :class="appCard" class="tw:rounded-xl tw:px-4 tw:py-3 tw:flex tw:items-start tw:gap-3">
          <iconify icon="ph:note-bold" class="tw:text-lg tw:text-amber-400 tw:shrink-0 tw:mt-0.5" />
          <div>
            <p class="tw:text-[11px] tw:uppercase tw:tracking-widest app-text-subtle">{{ t('recipes.form.notes') }}</p>
            <pre class="tw:text-sm tw:leading-relaxed tw:whitespace-pre-wrap tw:font-sans tw:m-0 tw:mt-0.5">{{ recipe.notes }}</pre>
          </div>
        </div>

      </div>

      <!-- ── Right: AI Chat (1/3) ──────────────────────────────────── -->
      <div :class="appCard" class="tw:rounded-2xl tw:p-5 tw:flex tw:flex-col tw:gap-3" style="height: 540px">

        <div class="tw:flex tw:items-center tw:gap-2 tw:shrink-0">
          <div class="tw:w-8 tw:h-8 tw:rounded-lg tw:bg-primary-500/15 tw:flex tw:items-center tw:justify-center">
            <iconify icon="ph:robot-bold" class="tw:text-primary-500" />
          </div>
          <p class="tw:text-sm tw:font-semibold">{{ t('recipes.detail.chat.title') }}</p>
        </div>

        <!-- Messages -->
        <div ref="chatBody" class="tw:flex-1 tw:overflow-y-auto tw:flex tw:flex-col tw:gap-3 tw:pr-1">
          <div v-if="chatMessages.length === 0" class="tw:flex tw:flex-col tw:items-center tw:justify-center tw:h-full tw:gap-3 tw:text-center">
            <iconify icon="ph:chat-circle-dots-bold" class="tw:text-3xl tw:text-slate-400" />
            <p class="tw:text-sm tw:text-muted">{{ t('recipes.detail.chat.placeholder') }}</p>
          </div>

          <template v-for="(msg, i) in chatMessages" :key="i">
            <div v-if="msg.role === 'user'" class="tw:flex tw:justify-end">
              <div class="tw:max-w-[85%] tw:rounded-2xl tw:rounded-tr-sm tw:bg-primary-500 tw:text-white tw:px-3.5 tw:py-2 tw:text-sm tw:leading-relaxed tw:whitespace-pre-wrap">
                {{ msg.text }}
              </div>
            </div>
            <div v-else-if="msg.role === 'model'" class="tw:flex tw:justify-start">
              <div class="tw:max-w-[85%] tw:rounded-2xl tw:rounded-tl-sm tw:bg-slate-100 tw:dark:bg-white/8 tw:px-3.5 tw:py-2 tw:text-sm tw:leading-relaxed tw:whitespace-pre-wrap">
                {{ msg.text }}
              </div>
            </div>
            <div v-else class="tw:text-xs tw:text-red-400 tw:text-center tw:italic">{{ msg.text }}</div>
          </template>

          <div v-if="chatLoading" class="tw:flex tw:justify-start">
            <div class="tw:rounded-2xl tw:rounded-tl-sm tw:bg-slate-100 tw:dark:bg-white/8 tw:px-3.5 tw:py-2.5 tw:flex tw:gap-1 tw:items-center">
              <span class="tw:w-1.5 tw:h-1.5 tw:rounded-full tw:bg-slate-400 tw:animate-bounce" style="animation-delay:0ms"></span>
              <span class="tw:w-1.5 tw:h-1.5 tw:rounded-full tw:bg-slate-400 tw:animate-bounce" style="animation-delay:150ms"></span>
              <span class="tw:w-1.5 tw:h-1.5 tw:rounded-full tw:bg-slate-400 tw:animate-bounce" style="animation-delay:300ms"></span>
            </div>
          </div>
        </div>

        <!-- Input -->
        <div class="tw:shrink-0 tw:flex tw:gap-2 tw:items-end">
          <prime-textarea
            v-model="chatInput"
            :placeholder="t('recipes.detail.chat.placeholder')"
            class="app-input tw:flex-1 tw:text-sm tw:resize-none"
            :rows="2"
            auto-resize
            :disabled="chatLoading"
            @keydown.enter.exact.prevent="sendChat"
          />
          <prime-button
            severity="primary" size="small"
            :loading="chatLoading"
            :disabled="!chatInput.trim()"
            :class="btnIcon"
            @click="sendChat"
          >
            <iconify icon="ph:paper-plane-tilt-bold" />
          </prime-button>
        </div>

      </div>

    </div>
  </section>
</template>
