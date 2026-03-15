<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import * as signalR from '@microsoft/signalr'
import { draftsApi } from '../api/drafts'
import { leaguesApi } from '../api/leagues'
import type { Draft, DraftPick, Player, League } from '../api/types'

const route    = useRoute()
const leagueId = route.params.leagueId as string

const league    = ref<League | null>(null)
const draft     = ref<Draft | null>(null)
const available = ref<Player[]>([])
const isLoading = ref(true)
const error     = ref<string | null>(null)
const pickError = ref<string | null>(null)
const isPicking = ref(false)

// ── Position filter ────────────────────────────────────────────────────────
const posFilter   = ref<string>('ALL')
const POS_FILTERS = ['ALL', 'QB', 'RB', 'WR', 'TE', 'K', 'DEF']

const filteredAvailable = computed(() => {
  if (posFilter.value === 'ALL') return available.value
  return available.value.filter(p => p.position === posFilter.value)
})

// ── Countdown clock ────────────────────────────────────────────────────────
const secondsLeft = ref<number | null>(null)
let clockInterval: ReturnType<typeof setInterval> | null = null

function startClock() {
  if (clockInterval) clearInterval(clockInterval)
  clockInterval = setInterval(() => {
    if (!draft.value || draft.value.status !== 'InProgress' || !draft.value.currentPickStartedAt) {
      secondsLeft.value = null
      return
    }
    const elapsed = (Date.now() - new Date(draft.value.currentPickStartedAt).getTime()) / 1000
    const remaining = Math.max(0, draft.value.pickTimeLimitSeconds - elapsed)
    secondsLeft.value = Math.ceil(remaining)
  }, 500)
}

const clockPercent = computed(() => {
  if (secondsLeft.value === null || !draft.value) return 100
  return (secondsLeft.value / draft.value.pickTimeLimitSeconds) * 100
})

const clockColor = computed(() => {
  if (secondsLeft.value === null) return '#ffffff'
  if (secondsLeft.value > 30) return '#ffffff'
  if (secondsLeft.value > 10) return '#facc15'
  return '#ef4444'
})

// ── Draft board helpers ────────────────────────────────────────────────────
const teams = computed(() => {
  if (!draft.value) return []
  const round1 = draft.value.picks.filter(p => p.round === 1).sort((a, b) => a.roundPick - b.roundPick)
  return round1.map(p => p.team)
})

const totalRounds = computed(() => {
  if (!draft.value || draft.value.picks.length === 0) return 0
  return Math.max(...draft.value.picks.map(p => p.round))
})

const boardCells = computed<Map<string, DraftPick>>(() => {
  const map = new Map<string, DraftPick>()
  if (!draft.value) return map
  for (const pick of draft.value.picks) {
    map.set(`${pick.round}:${pick.teamId}`, pick)
  }
  return map
})

function boardPick(round: number, teamId: string) {
  return boardCells.value.get(`${round}:${teamId}`) ?? null
}

const currentPick = computed(() => {
  if (!draft.value) return null
  return draft.value.picks.find(p => p.pickNumber === draft.value!.currentPickNumber) ?? null
})

const onTheClockTeam = computed(() => currentPick.value?.team ?? null)

// ── Recent picks ───────────────────────────────────────────────────────────
const recentPicks = computed(() => {
  if (!draft.value) return []
  return [...draft.value.picks]
    .filter(p => p.pickedAt)
    .sort((a, b) => b.pickNumber - a.pickNumber)
    .slice(0, 30)
})

// Track newly picked cell for flash animation
const flashPickId = ref<string | null>(null)

// ── SignalR ────────────────────────────────────────────────────────────────
let connection: signalR.HubConnection | null = null

async function connectSignalR(draftId: string) {
  connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/draft')
    .withAutomaticReconnect()
    .build()

  connection.on('PickMade', (payload: { pick: DraftPick; draft: Draft }) => {
    draft.value = payload.draft
    if (payload.pick.playerId) {
      available.value = available.value.filter(p => p.id !== payload.pick.playerId)
    }
    // Flash the newly picked cell
    const key = `${payload.pick.round}:${payload.pick.teamId}`
    flashPickId.value = key
    setTimeout(() => { flashPickId.value = null }, 1200)
    startClock()
  })

  connection.on('DraftStarted', (d: Draft) => { draft.value = d; startClock() })
  connection.on('DraftCompleted', (d: Draft) => {
    draft.value = d
    if (clockInterval) clearInterval(clockInterval)
    secondsLeft.value = null
  })

  await connection.start()
  await connection.invoke('JoinDraft', draftId)
}

// ── Pick actions ───────────────────────────────────────────────────────────
const selectedPlayerId = ref<string | null>(null)

async function makePick(player: Player) {
  if (!draft.value || !currentPick.value || isPicking.value) return
  if (draft.value.status !== 'InProgress') return
  selectedPlayerId.value = player.id
  pickError.value = null
  isPicking.value = true
  try {
    const { draft: updated } = await draftsApi.pick(leagueId, currentPick.value.teamId, player.id)
    draft.value = updated
    available.value = available.value.filter(p => p.id !== player.id)
    startClock()
  } catch (e: unknown) {
    pickError.value = typeof e === 'string' ? e : 'Pick failed'
  } finally {
    isPicking.value = false
    selectedPlayerId.value = null
  }
}

async function doAutoPick() {
  if (!draft.value || isPicking.value) return
  pickError.value = null
  isPicking.value = true
  try {
    const { draft: updated } = await draftsApi.autoPick(leagueId)
    draft.value = updated
    available.value = await draftsApi.getAvailable(leagueId)
    startClock()
  } catch (e: unknown) {
    pickError.value = typeof e === 'string' ? e : 'Auto-pick failed'
  } finally {
    isPicking.value = false
  }
}

// ── Lifecycle ──────────────────────────────────────────────────────────────
onMounted(async () => {
  try {
    ;[league.value, draft.value] = await Promise.all([
      leaguesApi.getById(leagueId),
      draftsApi.getByLeague(leagueId).catch(() => null),
    ])
    if (draft.value) {
      available.value = await draftsApi.getAvailable(leagueId)
      await connectSignalR(draft.value.id)
      if (draft.value.status === 'InProgress') startClock()
    }
  } catch {
    error.value = 'Failed to load draft'
  } finally {
    isLoading.value = false
  }
})

onUnmounted(async () => {
  if (clockInterval) clearInterval(clockInterval)
  if (connection && draft.value) {
    await connection.invoke('LeaveDraft', draft.value.id).catch(() => {})
    await connection.stop()
  }
})

// ── Display helpers ────────────────────────────────────────────────────────
function playerLabel(p: Player | null, short = false) {
  if (!p) return ''
  return short
    ? `${p.firstName[0]}. ${p.lastName}`
    : `${p.firstName} ${p.lastName}`
}

// Position color palette — high-contrast for broadcast
const POS_COLORS: Record<string, { bg: string; text: string; glow: string }> = {
  QB:  { bg: 'rgba(251,146,60,0.15)',  text: '#fb923c', glow: 'rgba(251,146,60,0.4)'  },
  RB:  { bg: 'rgba(74,222,128,0.12)',  text: '#4ade80', glow: 'rgba(74,222,128,0.4)'  },
  WR:  { bg: 'rgba(96,165,250,0.12)',  text: '#60a5fa', glow: 'rgba(96,165,250,0.4)'  },
  TE:  { bg: 'rgba(192,132,252,0.12)', text: '#c084fc', glow: 'rgba(192,132,252,0.4)' },
  K:   { bg: 'rgba(250,204,21,0.10)',  text: '#facc15', glow: 'rgba(250,204,21,0.4)'  },
  DEF: { bg: 'rgba(148,163,184,0.10)', text: '#94a3b8', glow: 'rgba(148,163,184,0.4)' },
}
function posStyle(pos: string) {
  return POS_COLORS[pos] ?? { bg: 'rgba(255,255,255,0.05)', text: '#94a3b8', glow: 'transparent' }
}

const clockDisplay = computed(() => {
  if (secondsLeft.value === null) return '--:--'
  const m = Math.floor(secondsLeft.value / 60)
  const s = secondsLeft.value % 60
  return `${String(m).padStart(2,'0')}:${String(s).padStart(2,'0')}`
})

const picksCompleted = computed(() =>
  draft.value ? draft.value.picks.filter(p => p.pickedAt).length : 0
)
</script>

<template>
  <!--
    Full-viewport draft room. Designed to be broadcast on a TV.
    Fills the screen below the nav (100vh - 4rem) with no page scroll.
  -->
  <div
    class="draft-room"
    style="
      height: calc(100vh - 3.5rem);
      display: flex;
      flex-direction: column;
      overflow: hidden;
      background: var(--bg);
    "
  >

    <!-- ── Loading / Error ── -->
    <div v-if="isLoading" class="flex-1 flex items-center justify-center">
      <div class="space-y-3 text-center">
        <div class="w-10 h-10 rounded-full border-2 border-t-transparent animate-spin mx-auto"
          style="border-color: var(--border); border-top-color: var(--accent)"
        />
        <p class="text-sm" style="color: var(--text-muted)">Loading draft…</p>
      </div>
    </div>
    <div v-else-if="error" class="flex-1 flex items-center justify-center">
      <p style="color: var(--red)">{{ error }}</p>
    </div>
    <div v-else-if="!draft" class="flex-1 flex items-center justify-center">
      <div class="text-center space-y-3">
        <p class="text-xl font-bold tracking-tight">No draft scheduled yet.</p>
        <RouterLink :to="`/leagues/${leagueId}`" class="btn btn-ghost text-sm">← Back to league</RouterLink>
      </div>
    </div>

    <template v-else>

      <!-- ════════════════════════════════════════════════════════════════
           TOP STATUS BAR — always visible
           ════════════════════════════════════════════════════════════════ -->
      <div
        class="status-bar flex items-center gap-4 px-4 shrink-0"
        style="
          height: 52px;
          border-bottom: 1px solid rgba(255,255,255,0.07);
          background: rgba(26,24,38,0.9);
          backdrop-filter: blur(8px);
        "
      >
        <!-- Back -->
        <RouterLink
          :to="`/leagues/${leagueId}`"
          class="text-xs shrink-0 transition-colors flex items-center gap-1"
          style="color: var(--text-muted)"
          onmouseover="this.style.color='var(--text)'"
          onmouseout="this.style.color='var(--text-muted)'"
        >
          ← {{ league?.name ?? 'League' }}
        </RouterLink>

        <div class="w-px h-5 shrink-0" style="background: rgba(255,255,255,0.1)" />

        <!-- League / draft title -->
        <span class="font-bold text-sm tracking-tight shrink-0" style="letter-spacing: -0.02em">
          Draft Room
        </span>

        <!-- Pick info — center -->
        <div v-if="draft.status === 'InProgress' && currentPick" class="flex items-center gap-3 flex-1 justify-center">
          <span class="label text-xs" style="color: var(--text-muted)">
            Round {{ currentPick.round }} · Pick {{ currentPick.pickNumber }} of {{ draft.picks.length }}
          </span>
          <div class="w-px h-4" style="background: rgba(255,255,255,0.1)" />
          <span class="text-sm font-semibold" style="color: var(--text)">
            {{ onTheClockTeam?.name }}
          </span>
          <!-- Mini clock -->
          <span
            v-if="secondsLeft !== null"
            class="text-sm font-mono font-bold tabular-nums"
            :style="{ color: clockColor }"
          >{{ clockDisplay }}</span>
        </div>
        <div v-else-if="draft.status === 'Completed'" class="flex-1 flex justify-center">
          <span class="text-sm font-semibold" style="color: var(--green)">Draft Complete</span>
        </div>
        <div v-else class="flex-1" />

        <!-- Pick progress + autopick -->
        <div class="flex items-center gap-3 shrink-0">
          <span class="text-xs tabular-nums" style="color: var(--text-muted)">
            {{ picksCompleted }} / {{ draft.picks.length }} picks
          </span>
          <button
            v-if="draft.status === 'InProgress'"
            class="btn btn-ghost text-xs py-1 px-3"
            :disabled="isPicking"
            @click="doAutoPick"
          >
            {{ isPicking ? 'Picking…' : 'Auto-pick' }}
          </button>
        </div>
      </div>

      <p v-if="pickError" class="text-xs px-4 py-2 shrink-0" style="color: var(--red); background: rgba(227,30,36,0.08)">
        {{ pickError }}
      </p>

      <!-- ════════════════════════════════════════════════════════════════
           MAIN 3-COLUMN LAYOUT
           ════════════════════════════════════════════════════════════════ -->
      <div class="flex flex-1 overflow-hidden min-h-0">

        <!-- ── LEFT: Available Players ────────────────────────────────── -->
        <div
          v-if="draft.status !== 'Completed'"
          class="available-panel flex flex-col shrink-0"
          style="
            width: 220px;
            border-right: 1px solid rgba(255,255,255,0.07);
            background: rgba(26,24,38,0.6);
          "
        >
          <!-- Panel header + filter -->
          <div class="px-3 pt-3 pb-2 shrink-0" style="border-bottom: 1px solid rgba(255,255,255,0.06)">
            <p class="label mb-2.5" style="color: var(--text-muted)">Available</p>
            <div class="flex flex-wrap gap-1">
              <button
                v-for="pos in POS_FILTERS"
                :key="pos"
                class="text-xs px-2 py-0.5 rounded font-medium transition-colors"
                :style="posFilter === pos
                  ? 'background: var(--accent); color: #fff'
                  : 'background: rgba(255,255,255,0.07); color: var(--text-muted)'"
                @click="posFilter = pos"
              >{{ pos }}</button>
            </div>
          </div>

          <!-- Player list -->
          <div class="flex-1 overflow-y-auto">
            <div
              v-for="player in filteredAvailable"
              :key="player.id"
              class="flex items-center gap-2 px-3 py-2 border-b transition-colors"
              style="border-color: rgba(255,255,255,0.04)"
              :class="[
                draft.status === 'InProgress' ? 'cursor-pointer' : 'cursor-default',
                selectedPlayerId === player.id ? 'bg-white/5' : '',
              ]"
              onmouseover="this.style.background='rgba(255,255,255,0.04)'"
              onmouseout="this.style.background=''"
              @click="makePick(player)"
            >
              <span
                class="text-xs font-bold w-7 text-center shrink-0 rounded py-px"
                :style="`color: ${posStyle(player.position).text}; background: ${posStyle(player.position).bg}`"
              >{{ player.position }}</span>
              <div class="flex-1 min-w-0">
                <p class="text-xs font-semibold truncate" style="color: var(--text)">
                  {{ player.firstName[0] }}. {{ player.lastName }}
                </p>
                <p class="text-xs truncate" style="color: var(--text-muted)">{{ player.nflTeam ?? '—' }}</p>
              </div>
              <span class="text-xs shrink-0 tabular-nums" style="color: var(--text-muted)">
                {{ player.adp != null ? `#${Math.round(player.adp)}` : '' }}
              </span>
            </div>
            <div v-if="filteredAvailable.length === 0" class="p-6 text-center text-xs" style="color: var(--text-muted)">
              None available.
            </div>
          </div>

          <div v-if="draft.status === 'InProgress'" class="px-3 py-2 text-xs shrink-0" style="color: var(--text-muted); border-top: 1px solid rgba(255,255,255,0.06)">
            Click to draft for {{ onTheClockTeam?.name }}
          </div>
        </div>

        <!-- ── CENTER: Draft Board ─────────────────────────────────────── -->
        <div class="flex-1 overflow-auto min-w-0" style="background: var(--bg)">
          <table class="board-table" style="border-collapse: collapse; table-layout: fixed; min-width: 100%">
            <!-- Sticky team headers -->
            <thead>
              <tr style="position: sticky; top: 0; z-index: 10; background: var(--bg); border-bottom: 1px solid rgba(255,255,255,0.1)">
                <th
                  class="text-xs font-semibold"
                  style="
                    width: 44px;
                    min-width: 44px;
                    padding: 10px 8px;
                    color: var(--text-muted);
                    text-align: center;
                    border-right: 1px solid rgba(255,255,255,0.07);
                    position: sticky; left: 0;
                    background: var(--bg);
                    z-index: 11;
                  "
                >Rd</th>
                <th
                  v-for="team in teams"
                  :key="team.id"
                  class="text-xs font-semibold text-center truncate"
                  style="
                    padding: 10px 6px;
                    min-width: 120px;
                    max-width: 160px;
                    border-right: 1px solid rgba(255,255,255,0.05);
                  "
                  :style="onTheClockTeam?.id === team.id && draft.status === 'InProgress'
                    ? 'color: #fff'
                    : 'color: var(--text-muted)'"
                >
                  <span class="truncate block" :title="team.name">{{ team.name }}</span>
                  <!-- Indicator dot for the team currently on the clock -->
                  <span
                    v-if="onTheClockTeam?.id === team.id && draft.status === 'InProgress'"
                    class="inline-block w-1 h-1 rounded-full mt-1 animate-pulse"
                    style="background: var(--accent)"
                  />
                </th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="round in totalRounds"
                :key="round"
                style="border-bottom: 1px solid rgba(255,255,255,0.04)"
              >
                <!-- Round number — sticky left -->
                <td
                  class="text-xs font-bold text-center"
                  style="
                    padding: 4px 8px;
                    color: var(--text-muted);
                    border-right: 1px solid rgba(255,255,255,0.07);
                    position: sticky; left: 0;
                    background: var(--bg);
                    z-index: 1;
                  "
                >{{ round }}</td>

                <!-- Pick cells -->
                <td
                  v-for="team in teams"
                  :key="team.id"
                  class="board-cell"
                  style="padding: 3px 4px; border-right: 1px solid rgba(255,255,255,0.04); vertical-align: top"
                >
                  <div
                    class="cell-inner"
                    style="border-radius: 4px; padding: 6px 8px; height: 52px; display: flex; flex-direction: column; justify-content: center; position: relative; overflow: hidden; transition: background 0.3s"
                    :style="boardPick(round, team.id)?.player
                      ? `background: ${posStyle(boardPick(round, team.id)!.player!.position).bg}`
                      : 'background: rgba(255,255,255,0.025)'"
                    :class="{
                      'cell-flash': flashPickId === `${round}:${team.id}`,
                      'cell-current': boardPick(round, team.id)?.pickNumber === draft.currentPickNumber && draft.status === 'InProgress',
                    }"
                  >
                    <template v-if="boardPick(round, team.id)?.player">
                      <p
                        class="font-semibold truncate leading-tight"
                        style="font-size: 0.72rem; color: #fff"
                      >{{ playerLabel(boardPick(round, team.id)!.player, true) }}</p>
                      <p
                        class="font-bold leading-tight mt-0.5"
                        style="font-size: 0.65rem"
                        :style="{ color: posStyle(boardPick(round, team.id)!.player!.position).text }"
                      >
                        {{ boardPick(round, team.id)!.player!.position }}
                        <span style="opacity: 0.6; font-weight: 400">· {{ boardPick(round, team.id)!.player!.nflTeam ?? '' }}</span>
                      </p>
                    </template>
                    <!-- Empty / upcoming cell -->
                    <template v-else>
                      <span
                        style="font-size: 0.6rem; color: rgba(255,255,255,0.15); font-variant-numeric: tabular-nums"
                      >{{ boardPick(round, team.id)?.pickNumber ?? '' }}</span>
                    </template>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- ── RIGHT: On the Clock + Recent Picks ─────────────────────── -->
        <div
          class="otc-panel flex flex-col shrink-0"
          style="
            width: 280px;
            border-left: 1px solid rgba(255,255,255,0.07);
            background: rgba(26,24,38,0.6);
          "
        >

          <!-- On the Clock section -->
          <div
            v-if="draft.status === 'InProgress' && currentPick"
            class="otc-clock shrink-0 px-5 py-5"
            style="border-bottom: 1px solid rgba(255,255,255,0.07)"
          >
            <p class="label mb-4" style="color: var(--text-muted)">On the Clock</p>

            <!-- Team name -->
            <p
              class="font-black tracking-tight leading-tight mb-1"
              style="font-size: 1.5rem; letter-spacing: -0.03em; color: #fff"
            >{{ onTheClockTeam?.name }}</p>
            <p class="text-xs mb-5" style="color: var(--text-muted)">
              Round {{ currentPick.round }} · Pick {{ currentPick.roundPick }} of {{ teams.length }}
            </p>

            <!-- Big countdown clock -->
            <div
              class="text-center py-4 rounded-lg mb-4"
              style="background: rgba(0,0,0,0.3)"
            >
              <span
                class="font-black font-mono tabular-nums"
                style="font-size: 3.5rem; line-height: 1; letter-spacing: -0.04em; transition: color 0.5s"
                :style="{ color: clockColor }"
              >{{ clockDisplay }}</span>
              <!-- Progress bar -->
              <div class="mx-4 mt-3 rounded-full overflow-hidden" style="height: 3px; background: rgba(255,255,255,0.1)">
                <div
                  class="h-full rounded-full transition-all duration-500"
                  :style="{ width: `${clockPercent}%`, background: clockColor }"
                />
              </div>
            </div>

            <button
              class="btn btn-ghost w-full text-sm justify-center"
              :disabled="isPicking"
              @click="doAutoPick"
            >
              {{ isPicking ? 'Picking…' : '⚡ Auto-pick' }}
            </button>
          </div>

          <!-- Draft complete state -->
          <div
            v-else-if="draft.status === 'Completed'"
            class="shrink-0 px-5 py-6 text-center"
            style="border-bottom: 1px solid rgba(255,255,255,0.07)"
          >
            <div class="w-12 h-12 rounded-full flex items-center justify-center mx-auto mb-3"
              style="background: rgba(74,222,128,0.15); border: 1px solid rgba(74,222,128,0.3)"
            >
              <span style="font-size: 1.25rem">✓</span>
            </div>
            <p class="font-bold text-lg tracking-tight mb-1" style="color: var(--green); letter-spacing: -0.02em">Draft Complete</p>
            <p class="text-xs" style="color: var(--text-muted)">All {{ draft.picks.length }} picks made</p>
          </div>

          <!-- Pending state -->
          <div
            v-else-if="draft.status === 'Pending'"
            class="shrink-0 px-5 py-6 text-center"
            style="border-bottom: 1px solid rgba(255,255,255,0.07)"
          >
            <p class="font-bold text-base tracking-tight mb-1" style="letter-spacing: -0.02em">Draft Pending</p>
            <p class="text-xs" style="color: var(--text-muted)">Waiting for the draft to start</p>
          </div>

          <!-- Recent Picks log -->
          <div class="flex flex-col flex-1 overflow-hidden min-h-0">
            <p class="label px-5 py-3 shrink-0" style="color: var(--text-muted); border-bottom: 1px solid rgba(255,255,255,0.06)">
              Recent Picks
            </p>
            <div class="flex-1 overflow-y-auto">
              <div
                v-for="pick in recentPicks"
                :key="pick.id"
                class="flex items-center gap-3 px-4 py-2.5"
                style="border-bottom: 1px solid rgba(255,255,255,0.04)"
              >
                <!-- Position badge -->
                <span
                  class="text-xs font-bold w-8 text-center shrink-0 py-0.5 rounded"
                  :style="`color: ${posStyle(pick.player?.position ?? '').text}; background: ${posStyle(pick.player?.position ?? '').bg}`"
                >{{ pick.player?.position }}</span>

                <div class="flex-1 min-w-0">
                  <p class="text-xs font-semibold truncate" style="color: var(--text)">
                    {{ playerLabel(pick.player, true) }}
                  </p>
                  <p class="text-xs truncate" style="color: var(--text-muted)">{{ pick.team.name }}</p>
                </div>

                <div class="shrink-0 text-right">
                  <p class="text-xs tabular-nums" style="color: var(--text-muted)">
                    {{ pick.round }}.{{ pick.roundPick }}
                  </p>
                  <span v-if="pick.isAutoPick" class="text-xs" style="color: var(--text-muted); font-style: italic; font-size: 0.6rem">auto</span>
                </div>
              </div>
              <div v-if="recentPicks.length === 0" class="p-6 text-center text-xs" style="color: var(--text-muted)">
                No picks yet.
              </div>
            </div>
          </div>

        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
/* Current pick cell — pulsing outline */
.cell-current {
  outline: 1.5px solid var(--accent);
  outline-offset: -1.5px;
  animation: pulse-outline 1.5s ease-in-out infinite;
}

@keyframes pulse-outline {
  0%, 100% { outline-color: rgba(227,30,36,0.9); }
  50%       { outline-color: rgba(227,30,36,0.35); }
}

/* Flash animation when a pick lands via SignalR */
.cell-flash {
  animation: pick-flash 1.2s ease-out forwards;
}

@keyframes pick-flash {
  0%   { filter: brightness(3) saturate(0.5); }
  15%  { filter: brightness(2) saturate(1.5); }
  100% { filter: brightness(1) saturate(1); }
}

/* Board scrollbar styling */
.board-table td, .board-table th {
  box-sizing: border-box;
}

/* Slim scrollbars on all panels */
.available-panel, .otc-panel, div[class="flex-1 overflow-auto min-w-0"] {
  scrollbar-width: thin;
  scrollbar-color: var(--border) transparent;
}
</style>
