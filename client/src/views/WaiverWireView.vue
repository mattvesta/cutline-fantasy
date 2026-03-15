<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { waiversApi } from '../api/waivers'
import type { WaiverState, WaiverPlayer, WaiverRosterSlot } from '../api/types'

const route    = useRoute()
const leagueId = route.params.leagueId as string
const teamId   = route.params.teamId   as string

// ── State ─────────────────────────────────────────────────────────────────
const state     = ref<WaiverState | null>(null)
const isLoading = ref(true)
const error     = ref<string | null>(null)

const posFilter = ref('ALL')
const search    = ref('')
const page      = ref(1)
const searching = ref(false)

const POSITIONS = ['ALL', 'QB', 'RB', 'WR', 'TE', 'K', 'DEF']

// ── Modal ─────────────────────────────────────────────────────────────────
const modal = ref<{
  player: WaiverPlayer
  dropSlotId: string | null
  bidAmount: number
  submitting: boolean
  error: string | null
} | null>(null)

// ── Load ──────────────────────────────────────────────────────────────────
async function load() {
  searching.value = true
  error.value     = null
  try {
    state.value = await waiversApi.getState(leagueId, teamId, {
      position: posFilter.value,
      search:   search.value || undefined,
      page:     page.value,
    })
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to load waiver wire'
  } finally {
    isLoading.value = false
    searching.value = false
  }
}

onMounted(() => load())

let searchTimer: ReturnType<typeof setTimeout>
watch(search, () => {
  clearTimeout(searchTimer)
  page.value = 1
  searchTimer = setTimeout(() => load(), 350)
})
watch(posFilter, () => { page.value = 1; load() })

// ── Computed ──────────────────────────────────────────────────────────────
const totalPages = computed(() =>
  state.value ? Math.ceil(state.value.availablePlayers.totalCount / state.value.availablePlayers.pageSize) : 1
)

const rosterForDrop = computed<WaiverRosterSlot[]>(() =>
  (state.value?.teamRoster ?? []).filter(rs => rs.player !== null)
)

const faabRemaining = computed(() => state.value?.faabRemaining ?? 0)
const faabBudget    = computed(() => state.value?.faabBudget ?? 100)
const faabPct       = computed(() => Math.max(2, (faabRemaining.value / faabBudget.value) * 100))

// Quick-bid presets as % of remaining
const BID_PRESETS = [
  { label: '$1',   value: (r: number) => 1 },
  { label: '25%',  value: (r: number) => Math.floor(r * 0.25) },
  { label: '50%',  value: (r: number) => Math.floor(r * 0.5) },
  { label: 'Max',  value: (r: number) => r },
]

// Slider fill %
const bidSliderPct = computed(() => {
  if (!modal.value || !faabRemaining.value) return 0
  return Math.min(100, (modal.value.bidAmount / faabRemaining.value) * 100)
})

// ── Actions ───────────────────────────────────────────────────────────────
function openModal(player: WaiverPlayer) {
  modal.value = { player, dropSlotId: null, bidAmount: 0, submitting: false, error: null }
}

function closeModal() { modal.value = null }

function setBid(n: number) {
  if (!modal.value) return
  modal.value.bidAmount = Math.min(Math.max(0, n), faabRemaining.value)
}

async function submitClaim() {
  if (!modal.value) return
  const m = modal.value
  m.submitting = true
  m.error      = null

  const dropPlayer = m.dropSlotId
    ? (rosterForDrop.value.find(rs => rs.slotId === m.dropSlotId)?.player ?? null)
    : null

  const faabBid = state.value?.useFaab ? m.bidAmount : null

  try {
    await waiversApi.submitClaim(leagueId, teamId, m.player.id, dropPlayer?.id ?? null, faabBid)
    modal.value = null
    await load()
  } catch (e: unknown) {
    m.error = e instanceof Error ? e.message : 'Failed to submit claim'
    m.submitting = false
  }
}

async function cancelClaim(claimId: string) {
  try {
    await waiversApi.cancelClaim(leagueId, claimId, teamId)
  } finally {
    await load()
  }
}

function changePage(n: number) {
  page.value = n
  load()
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

// ── Helpers ───────────────────────────────────────────────────────────────
const POS_COLOR: Record<string, string> = {
  QB: '#f97316', RB: '#22c55e', WR: '#3b82f6', TE: '#a855f7',
  K: '#eab308', DEF: '#64748b',
}
function posColor(pos: string) { return POS_COLOR[pos] ?? '#6b6784' }
function playerName(p: { firstName: string; lastName: string }) {
  return `${p.firstName} ${p.lastName}`
}

const STATUS_ICON:  Record<string, string> = { Processed: '✓', Rejected: '✗', Pending: '…' }
const STATUS_COLOR: Record<string, string> = {
  Processed: 'text-green-400', Rejected: 'text-red-400', Pending: 'text-[var(--text-muted)]',
}

function claimBid(playerId: string) {
  return state.value?.myClaims?.find(c => c.addPlayer.id === playerId)?.faabBid ?? null
}
function alreadyClaimed(playerId: string) {
  return state.value?.myClaims?.some(c => c.addPlayer.id === playerId) ?? false
}
</script>

<template>
  <div class="page">
    <RouterLink
      :to="`/leagues/${leagueId}/teams/${teamId}`"
      class="text-sm text-[var(--text-muted)] hover:text-white transition-colors mb-8 inline-flex items-center gap-1.5"
    >
      ← Roster
    </RouterLink>

    <!-- Loading -->
    <div v-if="isLoading" class="space-y-3">
      <div class="h-8 w-48 bg-[var(--surface)] rounded animate-pulse" />
      <div class="h-16 bg-[var(--surface)] rounded animate-pulse" />
      <div v-for="i in 8" :key="i" class="h-12 bg-[var(--surface)] rounded animate-pulse" />
    </div>
    <div v-else-if="error && !state" class="text-[var(--red)] text-sm">{{ error }}</div>

    <template v-else-if="state">

      <!-- ── Header ──────────────────────────────────────────────────────── -->
      <div class="flex items-start justify-between mb-6">
        <div>
          <h1 class="text-3xl font-bold tracking-tight" style="letter-spacing: -0.03em">Waiver Wire</h1>
          <p v-if="state.weekNumber" class="text-sm text-[var(--text-muted)] mt-0.5">Week {{ state.weekNumber }}</p>
        </div>
        <span
          class="text-xs font-medium px-2.5 py-1 rounded-full mt-1.5"
          :class="state.claimsOpen ? 'bg-green-400/10 text-green-400' : 'bg-[var(--surface)] text-[var(--text-muted)]'"
        >
          {{ state.claimsOpen ? 'Open' : 'Closed' }}
        </span>
      </div>

      <!-- ── FAAB balance card ───────────────────────────────────────────── -->
      <div
        v-if="state.useFaab"
        class="mb-8 p-4 rounded-xl"
        style="background: var(--surface); border: 1px solid var(--border)"
      >
        <div class="flex items-center justify-between mb-3">
          <div>
            <p class="text-xs text-[var(--text-muted)] uppercase tracking-wider font-medium mb-0.5">FAAB Balance</p>
            <div class="flex items-baseline gap-1.5">
              <span class="text-3xl font-black tabular-nums" style="letter-spacing: -0.04em; color: var(--accent)">
                ${{ faabRemaining.toFixed(0) }}
              </span>
              <span class="text-sm text-[var(--text-muted)]">of ${{ faabBudget.toFixed(0) }}</span>
            </div>
          </div>
          <div class="text-right">
            <p class="text-xs text-[var(--text-muted)] mb-0.5">Spent</p>
            <p class="text-lg font-bold tabular-nums text-[var(--text-secondary)]">
              ${{ (faabBudget - faabRemaining).toFixed(0) }}
            </p>
          </div>
        </div>
        <!-- Balance bar -->
        <div class="h-2 rounded-full overflow-hidden" style="background: var(--border)">
          <div
            class="h-full rounded-full transition-all duration-500"
            :style="{
              width: faabPct + '%',
              background: faabPct > 50 ? 'var(--accent)' : faabPct > 25 ? '#f97316' : '#ef4444',
            }"
          />
        </div>
        <p class="text-xs text-[var(--text-muted)] mt-2">
          Blind auction — your bid is hidden from other managers. Highest bid wins.
        </p>
      </div>
      <div v-else class="mb-6" />

      <!-- ── Closed banner ──────────────────────────────────────────────── -->
      <div
        v-if="!state.claimsOpen"
        class="mb-8 px-4 py-3 rounded-lg text-sm text-[var(--text-muted)]"
        style="background: var(--surface); border: 1px solid var(--border)"
      >
        Waiver claims open after the week's eliminated team has been determined.
        <RouterLink :to="`/leagues/${leagueId}/matchup`" class="text-[var(--accent)] hover:underline ml-1">
          View this week's standings →
        </RouterLink>
      </div>

      <!-- ── My Pending Claims ───────────────────────────────────────────── -->
      <section v-if="state.myClaims && state.myClaims.length > 0" class="mb-8">
        <h2 class="label mb-3">My Pending Claims</h2>
        <div class="card overflow-hidden">
          <div
            v-for="claim in state.myClaims"
            :key="claim.id"
            class="flex items-center gap-3 px-4 py-3 border-b border-[var(--border-subtle)] last:border-0"
          >
            <!-- Add player -->
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2">
                <span class="text-xs font-bold shrink-0" :style="{ color: posColor(claim.addPlayer.position) }">
                  {{ claim.addPlayer.position }}
                </span>
                <span class="font-medium truncate">{{ playerName(claim.addPlayer) }}</span>
                <span class="text-xs text-[var(--text-muted)] shrink-0">{{ claim.addPlayer.nflTeam }}</span>
              </div>
              <div v-if="claim.dropPlayer" class="flex items-center gap-1.5 mt-0.5">
                <span class="text-xs text-[var(--text-muted)]">Drop:</span>
                <span class="text-xs text-red-400">{{ playerName(claim.dropPlayer) }}</span>
              </div>
            </div>

            <!-- FAAB bid — prominent -->
            <div v-if="state.useFaab" class="shrink-0 text-right">
              <p class="text-xs text-[var(--text-muted)]">Bid</p>
              <p class="text-lg font-black tabular-nums" style="color: var(--accent); letter-spacing: -0.03em">
                ${{ claim.faabBid?.toFixed(0) ?? '0' }}
              </p>
            </div>

            <!-- Cancel -->
            <button
              class="text-xs px-2.5 py-1.5 rounded transition-colors shrink-0"
              style="color: var(--text-muted); background: var(--surface-raised); border: 1px solid var(--border)"
              onmouseover="this.style.color='#f87171'; this.style.borderColor='rgba(239,68,68,0.3)'"
              onmouseout="this.style.color='var(--text-muted)'; this.style.borderColor='var(--border)'"
              @click="cancelClaim(claim.id)"
            >
              Cancel
            </button>
          </div>
        </div>
      </section>

      <!-- ── Available Players ──────────────────────────────────────────── -->
      <section class="mb-8">
        <div class="flex items-center justify-between mb-3">
          <h2 class="label">Available Players</h2>
          <span class="text-xs text-[var(--text-muted)]">{{ state.availablePlayers.totalCount }} free agents</span>
        </div>

        <!-- Position filters -->
        <div class="flex gap-2 mb-3 flex-wrap">
          <button
            v-for="pos in POSITIONS"
            :key="pos"
            class="text-xs px-3 py-1 rounded-full font-medium transition-colors"
            :style="posFilter === pos
              ? { background: posColor(pos) === '#6b6784' ? 'rgba(255,255,255,0.12)' : posColor(pos) + '22', color: posColor(pos) === '#6b6784' ? 'white' : posColor(pos), border: '1px solid ' + (posColor(pos) === '#6b6784' ? 'rgba(255,255,255,0.15)' : posColor(pos) + '44') }
              : { background: 'var(--surface)', color: 'var(--text-muted)', border: '1px solid var(--border)' }"
            @click="posFilter = pos"
          >{{ pos }}</button>
        </div>

        <!-- Search -->
        <div class="relative mb-4">
          <input
            v-model="search"
            type="text"
            placeholder="Search players…"
            class="w-full px-4 py-2.5 rounded-lg text-sm outline-none transition-colors"
            style="background: var(--surface); border: 1px solid var(--border); color: var(--text)"
            onfocus="this.style.borderColor='var(--border-hover)'"
            onblur="this.style.borderColor='var(--border)'"
          />
          <span v-if="searching" class="absolute right-3 top-2.5 text-xs text-[var(--text-muted)] animate-pulse">…</span>
        </div>

        <!-- Player list -->
        <div class="card overflow-hidden">
          <div v-if="state.availablePlayers.items.length === 0" class="px-4 py-8 text-sm text-[var(--text-muted)] text-center">
            No players found.
          </div>
          <div
            v-for="player in state.availablePlayers.items"
            :key="player.id"
            class="player-row flex items-center gap-3 px-4 py-3 border-b border-[var(--border-subtle)] last:border-0"
          >
            <span class="text-xs font-bold w-8 shrink-0" :style="{ color: posColor(player.position) }">
              {{ player.position }}
            </span>

            <div class="flex-1 min-w-0">
              <p class="font-medium truncate">{{ player.firstName }} {{ player.lastName }}</p>
              <p class="text-xs text-[var(--text-muted)]">
                {{ player.nflTeam ?? '—' }}
                <span v-if="player.byeWeek" class="ml-1">· Bye {{ player.byeWeek }}</span>
              </p>
            </div>

            <span class="text-xs text-[var(--text-muted)] shrink-0 hidden sm:block w-12 text-right">
              {{ player.adp != null ? player.adp.toFixed(0) : '—' }}
            </span>

            <!-- Claimed badge shows bid -->
            <div class="shrink-0">
              <div v-if="alreadyClaimed(player.id)" class="text-right">
                <p class="text-[10px] text-[var(--text-muted)]">{{ state.useFaab ? 'Bid' : 'Claimed' }}</p>
                <p v-if="state.useFaab" class="text-sm font-bold tabular-nums" style="color: var(--accent)">
                  ${{ claimBid(player.id)?.toFixed(0) ?? '0' }}
                </p>
              </div>
              <button
                v-else-if="state.claimsOpen"
                class="text-xs px-3 py-1.5 rounded font-medium transition-colors"
                style="background: var(--accent-dim); color: var(--accent)"
                onmouseover="this.style.background='var(--accent)'; this.style.color='white'"
                onmouseout="this.style.background='var(--accent-dim)'; this.style.color='var(--accent)'"
                @click="openModal(player)"
              >
                {{ state.useFaab ? 'Bid' : 'Add' }}
              </button>
            </div>
          </div>
        </div>

        <!-- Pagination -->
        <div v-if="totalPages > 1" class="flex items-center justify-center gap-2 mt-4">
          <button
            :disabled="page <= 1"
            class="text-xs px-3 py-1 rounded disabled:opacity-30"
            style="background: var(--surface); color: var(--text-muted)"
            @click="changePage(page - 1)"
          >←</button>
          <span class="text-xs text-[var(--text-muted)]">{{ page }} / {{ totalPages }}</span>
          <button
            :disabled="page >= totalPages"
            class="text-xs px-3 py-1 rounded disabled:opacity-30"
            style="background: var(--surface); color: var(--text-muted)"
            @click="changePage(page + 1)"
          >→</button>
        </div>
      </section>

      <!-- ── Recent Results ─────────────────────────────────────────────── -->
      <section v-if="state.recentResults && state.recentResults.length > 0">
        <h2 class="label mb-3">Week {{ state.recentResults[0]?.weekNumber }} Results</h2>
        <div class="card overflow-hidden">
          <div
            v-for="result in state.recentResults"
            :key="result.id"
            class="flex items-center gap-4 px-4 py-3 border-b border-[var(--border-subtle)] last:border-0"
          >
            <span class="text-base w-5 shrink-0 text-center" :class="STATUS_COLOR[result.status]">
              {{ STATUS_ICON[result.status] }}
            </span>
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2">
                <span class="text-xs font-bold" :style="{ color: posColor(result.addPlayer.position) }">
                  {{ result.addPlayer.position }}
                </span>
                <span class="font-medium truncate">{{ playerName(result.addPlayer) }}</span>
              </div>
              <div class="flex items-center gap-2 mt-0.5">
                <span v-if="result.dropPlayer" class="text-xs text-[var(--text-muted)]">
                  Dropped: {{ playerName(result.dropPlayer) }}
                </span>
                <span v-if="result.rejectionReason" class="text-xs text-red-400">
                  {{ result.rejectionReason }}
                </span>
              </div>
            </div>
            <div v-if="state.useFaab && result.faabBid != null" class="shrink-0 text-right">
              <p class="text-[10px] text-[var(--text-muted)]">Bid</p>
              <p class="text-sm font-bold tabular-nums" :class="STATUS_COLOR[result.status]">
                ${{ result.faabBid.toFixed(0) }}
              </p>
            </div>
            <span class="text-xs shrink-0" :class="STATUS_COLOR[result.status]">{{ result.status }}</span>
          </div>
        </div>
      </section>
    </template>

    <!-- ── Bid / Add Modal ───────────────────────────────────────────────── -->
    <Teleport to="body">
      <div
        v-if="modal"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-4"
        style="background: rgba(0,0,0,0.65); backdrop-filter: blur(4px)"
        @click.self="closeModal"
      >
        <div
          class="w-full max-w-md rounded-xl overflow-hidden"
          style="background: var(--surface-raised); border: 1px solid var(--border)"
        >
          <!-- Player banner -->
          <div class="px-6 pt-6 pb-4" style="border-bottom: 1px solid var(--border-subtle)">
            <div class="flex items-start justify-between">
              <div>
                <p class="text-xs text-[var(--text-muted)] mb-1">
                  {{ state?.useFaab ? 'Place a bid' : 'Add to roster' }}
                </p>
                <h3 class="text-2xl font-bold" style="letter-spacing: -0.03em">
                  {{ modal.player.firstName }} {{ modal.player.lastName }}
                </h3>
                <p class="text-sm mt-0.5 font-medium" :style="{ color: posColor(modal.player.position) }">
                  {{ modal.player.position }} · {{ modal.player.nflTeam ?? '—' }}
                  <span v-if="modal.player.adp" class="text-[var(--text-muted)] font-normal ml-2">
                    ADP {{ modal.player.adp.toFixed(0) }}
                  </span>
                </p>
              </div>
              <button
                class="text-[var(--text-muted)] hover:text-white transition-colors text-2xl leading-none"
                @click="closeModal"
              >×</button>
            </div>
          </div>

          <div class="px-6 py-5 space-y-5">

            <!-- ── FAAB bid section ── -->
            <div v-if="state?.useFaab">
              <!-- Remaining balance context -->
              <div class="flex items-center justify-between mb-3">
                <span class="text-xs text-[var(--text-muted)] uppercase tracking-wider font-medium">Your Bid</span>
                <span class="text-xs text-[var(--text-muted)]">
                  ${{ faabRemaining.toFixed(0) }} available
                </span>
              </div>

              <!-- Big bid display -->
              <div
                class="relative flex items-center justify-center rounded-xl mb-4 py-6"
                style="background: var(--surface); border: 1px solid var(--border)"
              >
                <span class="text-3xl font-black text-[var(--text-muted)] mr-1">$</span>
                <input
                  :value="modal.bidAmount"
                  type="number"
                  min="0"
                  :max="faabRemaining"
                  step="1"
                  class="text-5xl font-black tabular-nums w-32 text-center bg-transparent outline-none"
                  style="letter-spacing: -0.04em; color: var(--accent); -moz-appearance: textfield"
                  @input="setBid(Number(($event.target as HTMLInputElement).value))"
                />
              </div>

              <!-- Slider -->
              <input
                :value="modal.bidAmount"
                type="range"
                min="0"
                :max="faabRemaining"
                step="1"
                class="bid-slider w-full mb-3"
                @input="setBid(Number(($event.target as HTMLInputElement).value))"
              />

              <!-- Quick-bid buttons -->
              <div class="grid grid-cols-4 gap-2">
                <button
                  v-for="preset in BID_PRESETS"
                  :key="preset.label"
                  type="button"
                  class="py-2 rounded-lg text-xs font-semibold transition-colors"
                  :style="modal.bidAmount === preset.value(faabRemaining)
                    ? 'background: var(--accent); color: white'
                    : 'background: var(--surface); color: var(--text-secondary); border: 1px solid var(--border)'"
                  @click="setBid(preset.value(faabRemaining))"
                >
                  {{ preset.label }}
                  <span class="block text-[10px] font-normal opacity-70">
                    ${{ preset.value(faabRemaining) }}
                  </span>
                </button>
              </div>
            </div>

            <!-- ── Drop player ── -->
            <div>
              <label class="text-xs font-medium text-[var(--text-muted)] uppercase tracking-wider block mb-2">
                Drop player <span class="normal-case font-normal">(optional)</span>
              </label>
              <select
                v-model="modal.dropSlotId"
                class="w-full px-3 py-2.5 rounded-lg text-sm outline-none"
                style="background: var(--surface); border: 1px solid var(--border); color: var(--text)"
              >
                <option :value="null">None — keep full roster</option>
                <option
                  v-for="slot in rosterForDrop"
                  :key="slot.slotId"
                  :value="slot.slotId"
                >
                  {{ slot.player!.position }} · {{ slot.player!.firstName }} {{ slot.player!.lastName }}
                  {{ slot.isStarter ? '(Starter)' : '(Bench)' }}
                </option>
              </select>
            </div>

            <!-- Error -->
            <p v-if="modal.error" class="text-xs text-red-400">{{ modal.error }}</p>

            <!-- Actions -->
            <div class="flex gap-3">
              <button
                class="flex-1 py-2.5 rounded-lg text-sm font-medium"
                style="background: var(--surface); color: var(--text-muted)"
                @click="closeModal"
              >
                Cancel
              </button>
              <button
                class="flex-1 py-2.5 rounded-lg text-sm font-semibold transition-opacity"
                style="background: var(--accent); color: white"
                :class="{ 'opacity-60 cursor-not-allowed': modal.submitting }"
                :disabled="modal.submitting"
                @click="submitClaim"
              >
                {{ modal.submitting ? 'Submitting…' : state?.useFaab ? `Bid $${modal.bidAmount}` : 'Add Player' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.player-row {
  transition: background 0.1s ease;
}
.player-row:hover {
  background: var(--surface-raised);
}

/* Bid slider */
.bid-slider {
  -webkit-appearance: none;
  appearance: none;
  height: 4px;
  border-radius: 2px;
  background: var(--border);
  outline: none;
  cursor: pointer;
}
.bid-slider::-webkit-slider-thumb {
  -webkit-appearance: none;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: var(--accent);
  cursor: pointer;
  border: 2px solid var(--surface-raised);
  box-shadow: 0 0 0 2px var(--accent);
}
.bid-slider::-moz-range-thumb {
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: var(--accent);
  cursor: pointer;
  border: 2px solid var(--surface-raised);
}

/* Hide number input spinners */
input[type=number]::-webkit-inner-spin-button,
input[type=number]::-webkit-outer-spin-button {
  -webkit-appearance: none;
}
</style>
