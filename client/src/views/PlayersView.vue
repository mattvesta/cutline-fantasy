<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { RouterLink } from 'vue-router'
import { playersApi } from '../api/players'
import type { Player, PlayerPage } from '../api/types'

const POSITIONS = ['All', 'QB', 'RB', 'WR', 'TE', 'K', 'DEF']

const PAGE_SIZES = [25, 50, 100]

const STATUS_LABEL: Record<string, string> = {
  Active:         'Active',
  Injured:        'Injured',
  InjuredReserve: 'Injured Reserve',
  Inactive:       'Inactive',
  Unknown:        'Unknown',
}

const STATUS_COLOR: Record<string, string> = {
  Active:         'text-green-400',
  Injured:        'text-yellow-400',
  InjuredReserve: 'text-red-400',
  Inactive:       'text-gray-500',
  Unknown:        'text-gray-600',
}

const selectedPosition = ref('All')
const searchInput      = ref('')
const sortBy           = ref('adp')
const sortDesc         = ref(false)
const page             = ref(1)
const pageSize         = ref(50)

const result    = ref<PlayerPage | null>(null)
const isLoading = ref(false)
const error     = ref<string | null>(null)

let debounceTimer: ReturnType<typeof setTimeout>

async function fetchPlayers() {
  isLoading.value = true
  error.value = null
  try {
    result.value = await playersApi.search({
      position: selectedPosition.value === 'All' ? undefined : selectedPosition.value,
      search:   searchInput.value.trim() || undefined,
      sortBy:   sortBy.value,
      sortDesc: sortDesc.value,
      page:     page.value,
      pageSize: pageSize.value,
    })
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load players'
  } finally {
    isLoading.value = false
  }
}

function onSearchInput() {
  page.value = 1
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(fetchPlayers, 300)
}

function onFilterChange() {
  page.value = 1
  fetchPlayers()
}

function setSort(column: string) {
  if (sortBy.value === column) {
    sortDesc.value = !sortDesc.value
  } else {
    sortBy.value = column
    sortDesc.value = false
  }
  page.value = 1
  fetchPlayers()
}

function goToPage(n: number) {
  page.value = n
  fetchPlayers()
}

watch([selectedPosition, pageSize], onFilterChange)
watch(page, fetchPlayers)

// kick off initial load
fetchPlayers()

const totalPages = computed(() =>
  result.value ? Math.ceil(result.value.totalCount / result.value.pageSize) : 0
)

const rangeStart = computed(() =>
  result.value ? (result.value.page - 1) * result.value.pageSize + 1 : 0
)

const rangeEnd = computed(() =>
  result.value ? Math.min(result.value.page * result.value.pageSize, result.value.totalCount) : 0
)

function formatAdp(adp: number): string {
  return Number.isInteger(adp) ? String(adp) : adp.toFixed(1)
}

type SortableColumn = 'adp' | 'name' | 'position' | 'team' | 'status'

const columns: { key: SortableColumn; label: string }[] = [
  { key: 'adp',      label: 'ADP'      },
  { key: 'name',     label: 'Name'     },
  { key: 'position', label: 'Pos'      },
  { key: 'team',     label: 'Team'     },
  { key: 'status',   label: 'Status'   },
]
</script>

<template>
  <div class="page">
    <RouterLink to="/" class="text-sm text-[var(--text-muted)] hover:text-white transition-colors mb-8 inline-flex items-center gap-1.5">
      ← Home
    </RouterLink>

    <div class="flex items-start justify-between mb-6">
      <h1 class="text-3xl font-bold">Players</h1>
      <span v-if="result && !isLoading" class="text-sm text-gray-500 mt-2">
        {{ result.totalCount.toLocaleString() }} players
      </span>
    </div>

    <!-- Filters -->
    <div class="flex flex-col sm:flex-row gap-3 mb-5">
      <div class="flex gap-1 flex-wrap">
        <button
          v-for="pos in POSITIONS"
          :key="pos"
          class="px-3 py-1 rounded text-sm transition-colors"
          :style="selectedPosition === pos
            ? 'background: var(--accent); color: #fff; font-weight: 700'
            : 'background: var(--surface); color: var(--text-secondary)'"
          @click="selectedPosition = pos"
        >
          {{ pos }}
        </button>
      </div>

      <input
        v-model="searchInput"
        type="text"
        placeholder="Search by name..."
        class="input sm:w-56 py-1.5 text-sm"
        @input="onSearchInput"
      />

      <div class="flex items-center gap-2 ml-auto text-sm text-[var(--text-muted)]">
        <span>Per page</span>
        <select v-model="pageSize" class="input py-1 text-sm" style="width: auto">
          <option v-for="n in PAGE_SIZES" :key="n" :value="n">{{ n }}</option>
        </select>
      </div>
    </div>

    <div v-if="error" class="text-[var(--red)] text-sm mb-4">{{ error }}</div>

    <!-- Table -->
    <div class="card overflow-x-auto">
      <table class="data-table">
        <thead>
          <tr>
            <th
              v-for="col in columns"
              :key="col.key"
              class="cursor-pointer select-none hover:text-white transition-colors whitespace-nowrap first:pl-5"
              @click="setSort(col.key)"
            >
              {{ col.label }}
              <span v-if="sortBy === col.key" class="ml-1" style="color: var(--accent)">
                {{ sortDesc ? '↓' : '↑' }}
              </span>
              <span v-else class="ml-1 opacity-0 group-hover:opacity-100">↕</span>
            </th>
          </tr>
        </thead>
        <tbody>
          <template v-if="isLoading">
            <tr v-for="n in pageSize" :key="n">
              <td v-for="col in columns" :key="col.key" class="first:pl-5">
                <div class="h-3.5 bg-[var(--surface)] rounded animate-pulse my-1" :style="{ width: `${40 + Math.random() * 40}%` }" />
              </td>
            </tr>
          </template>
          <template v-else-if="result && result.items.length > 0">
            <RouterLink
              v-for="player in result.items"
              :key="player.id"
              :to="`/players/${player.id}`"
              custom
              v-slot="{ navigate }"
            >
              <tr
                class="cursor-pointer transition-colors"
                :class="{ 'opacity-35': player.status === 'Inactive' || player.status === 'Unknown' }"
                @click="navigate"
              >
                <td class="pl-5 tabular-nums text-[var(--text-muted)]">
                  {{ player.adp != null ? formatAdp(player.adp) : '—' }}
                </td>
                <td class="font-medium">{{ player.firstName }} {{ player.lastName }}</td>
                <td class="text-[var(--text-muted)]">{{ player.position }}</td>
                <td class="text-[var(--text-muted)]">{{ player.nflTeam ?? '—' }}</td>
                <td class="text-xs" :class="STATUS_COLOR[player.status] ?? 'text-[var(--text-muted)]'">
                  {{ STATUS_LABEL[player.status] ?? player.status }}
                </td>
              </tr>
            </RouterLink>
          </template>
          <tr v-else-if="result">
            <td :colspan="columns.length" class="py-12 pl-5 text-[var(--text-muted)] text-sm">
              No players found. Run the Sleeper sync first:
              <code class="bg-gray-800 px-1 rounded ml-1">dotnet run --project src/Cutline.Ingest</code>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div v-if="result && totalPages > 1" class="flex items-center justify-between mt-5 text-sm">
      <span class="text-[var(--text-muted)]">
        Showing {{ rangeStart.toLocaleString() }}–{{ rangeEnd.toLocaleString() }} of {{ result.totalCount.toLocaleString() }}
      </span>

      <div class="flex items-center gap-1">
        <button
          class="btn btn-ghost px-3 py-1 text-xs"
          :disabled="page === 1"
          @click="goToPage(page - 1)"
        >
          ← Prev
        </button>

        <template v-for="n in totalPages" :key="n">
          <template v-if="n === 1 || n === totalPages || Math.abs(n - page) <= 1">
            <button
              class="w-7 h-7 rounded text-xs transition-colors"
              :style="n === page
                ? 'background: var(--accent); color: #fff; font-weight: 700'
                : 'color: var(--text-muted)'"
              @click="goToPage(n)"
            >{{ n }}</button>
          </template>
          <span v-else-if="n === page - 2 || n === page + 2" class="text-[var(--text-muted)] px-0.5 text-xs">…</span>
        </template>

        <button
          class="btn btn-ghost px-3 py-1 text-xs"
          :disabled="page === totalPages"
          @click="goToPage(page + 1)"
        >
          Next →
        </button>
      </div>
    </div>
  </div>
</template>
