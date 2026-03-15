<script setup lang="ts">
import { ref, watch } from 'vue'

defineProps<{ show: boolean }>()
const emit = defineEmits<{ select: [url: string] }>()

// Configure your Tenor API key via VITE_TENOR_API_KEY in .env
const TENOR_KEY = (import.meta.env.VITE_TENOR_API_KEY as string) ?? ''

interface TenorGif {
  id: string
  title: string
  media_formats: {
    tinygif?: { url: string }
    gif?: { url: string }
  }
}

const query   = ref('')
const gifs    = ref<TenorGif[]>([])
const loading = ref(false)
const error   = ref<string | null>(null)

let timer: ReturnType<typeof setTimeout>
watch(query, () => {
  clearTimeout(timer)
  timer = setTimeout(fetchGifs, 380)
})

watch(() => TENOR_KEY, (key) => { if (key) fetchGifs() }, { immediate: true })

async function fetchGifs() {
  if (!TENOR_KEY) return
  loading.value = true
  error.value   = null
  try {
    const base = 'https://tenor.googleapis.com/v2'
    const path = query.value ? '/search' : '/featured'
    const params = new URLSearchParams({
      key:          TENOR_KEY,
      limit:        '24',
      media_filter: 'tinygif',
      ...(query.value && { q: query.value }),
    })
    const res  = await fetch(`${base}${path}?${params}`)
    const data = await res.json()
    gifs.value = data.results ?? []
  } catch {
    error.value = 'Failed to load GIFs.'
  } finally {
    loading.value = false
  }
}

function select(gif: TenorGif) {
  const url = gif.media_formats.tinygif?.url ?? gif.media_formats.gif?.url
  if (url) emit('select', url)
}
</script>

<template>
  <div
    v-if="show"
    class="rounded-xl overflow-hidden shadow-2xl"
    style="background: var(--surface-raised); border: 1px solid var(--border); width: 340px; max-height: 380px; display: flex; flex-direction: column"
  >
    <!-- Search bar -->
    <div class="p-2" style="border-bottom: 1px solid var(--border-subtle)">
      <input
        v-model="query"
        class="input w-full text-sm py-1.5"
        placeholder="Search GIFs…"
        autofocus
      />
    </div>

    <!-- No key configured -->
    <div
      v-if="!TENOR_KEY"
      class="flex-1 flex items-center justify-center p-6 text-center text-sm text-[var(--text-muted)]"
    >
      Add <code class="mx-1 px-1.5 py-0.5 rounded text-xs" style="background: var(--surface)">VITE_TENOR_API_KEY</code>
      to your <code class="mx-1 px-1.5 py-0.5 rounded text-xs" style="background: var(--surface)">.env</code>
      to enable GIFs.
    </div>

    <!-- Loading -->
    <div v-else-if="loading && gifs.length === 0" class="flex-1 flex items-center justify-center p-4">
      <span class="text-sm text-[var(--text-muted)] animate-pulse">Loading…</span>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="flex-1 flex items-center justify-center p-4 text-sm text-red-400">
      {{ error }}
    </div>

    <!-- Grid -->
    <div v-else class="overflow-y-auto flex-1 p-2">
      <div v-if="gifs.length === 0" class="text-center py-8 text-sm text-[var(--text-muted)]">No GIFs found.</div>
      <div class="grid grid-cols-3 gap-1.5">
        <button
          v-for="gif in gifs"
          :key="gif.id"
          class="rounded-lg overflow-hidden aspect-video bg-[var(--surface)] hover:ring-2 transition-all"
          style="--tw-ring-color: var(--accent)"
          :title="gif.title"
          @click="select(gif)"
        >
          <img
            :src="gif.media_formats.tinygif?.url"
            :alt="gif.title"
            class="w-full h-full object-cover"
            loading="lazy"
          />
        </button>
      </div>
    </div>

    <!-- Attribution -->
    <div
      v-if="TENOR_KEY && gifs.length > 0"
      class="px-3 py-1.5 text-[10px] text-[var(--text-muted)] text-right"
      style="border-top: 1px solid var(--border-subtle)"
    >
      Powered by Tenor
    </div>
  </div>
</template>
