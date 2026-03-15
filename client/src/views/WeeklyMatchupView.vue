<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'
import { scoringApi } from '../api/scoring'
import type { WeeklyMatchupResponse, MatchupTeamRow } from '../api/types'

const route    = useRoute()
const leagueId = route.params.leagueId as string

const data      = ref<WeeklyMatchupResponse | null>(null)
const isLoading = ref(true)
const error     = ref<string | null>(null)
const flashIds  = ref<Set<string>>(new Set())

// ── SignalR ────────────────────────────────────────────────────────────────
const connection = new HubConnectionBuilder()
  .withUrl('/hubs/scoring')
  .withAutomaticReconnect()
  .build()

connection.on('TeamScoreUpdate', (update: { teamId: string; points: number }) => {
  if (!data.value) return
  const row = data.value.teams.find(t => t.teamId === update.teamId)
  if (!row) return

  row.points = update.points

  // Re-sort active teams by points, keep eliminated at bottom
  const active    = data.value.teams.filter(t => !t.isEliminated)
  const eliminated = data.value.teams.filter(t => t.isEliminated)
  active.sort((a, b) => (b.points ?? 0) - (a.points ?? 0))
  active.forEach((t, i) => {
    t.rank        = i + 1
    t.isOnCutLine = i === active.length - 1 && active.length > 1
  })
  data.value.teams = [...active, ...eliminated]

  // Flash updated row
  flashIds.value = new Set([...flashIds.value, update.teamId])
  setTimeout(() => {
    flashIds.value = new Set([...flashIds.value].filter(id => id !== update.teamId))
  }, 1200)
})

onMounted(async () => {
  try {
    data.value = await scoringApi.getMatchup(leagueId)
  } catch {
    error.value = 'Failed to load matchup data'
  } finally {
    isLoading.value = false
  }

  try {
    await connection.start()
    await connection.invoke('JoinLeague', leagueId)
  } catch {
    // SignalR optional
  }
})

onUnmounted(async () => {
  if (connection.state === HubConnectionState.Connected) {
    await connection.invoke('LeaveLeague', leagueId).catch(() => {})
    await connection.stop().catch(() => {})
  }
})

// ── Computed ───────────────────────────────────────────────────────────────
const active    = computed(() => data.value?.teams.filter(t => !t.isEliminated) ?? [])
const eliminated = computed(() => data.value?.teams.filter(t => t.isEliminated) ?? [])

const isLive = computed(() => data.value?.weekStatus === 'InProgress')

const cutLineTeam = computed(() => active.value.find(t => t.isOnCutLine) ?? null)

// Gap in points between last-safe and cut-line team
const cutLineGap = computed(() => {
  const a = active.value
  if (a.length < 2) return null
  const secondLast = a[a.length - 2]
  const last       = a[a.length - 1]
  if (secondLast.points == null || last.points == null) return null
  return (secondLast.points - last.points).toFixed(2)
})

function fmtPts(n: number | null | undefined) {
  if (n == null) return '—'
  return n % 1 === 0 ? n.toString() : n.toFixed(2)
}

function rankLabel(i: number) {
  if (i === 0) return '1st'
  if (i === 1) return '2nd'
  if (i === 2) return '3rd'
  return `${i + 1}th`
}

// Points bar: width as % of leader's score
function barWidth(pts: number | null) {
  const leader = active.value[0]?.points ?? 0
  if (!pts || !leader) return 0
  return Math.max(4, (pts / leader) * 100)
}
</script>

<template>
  <div class="page">
    <RouterLink
      :to="`/leagues/${leagueId}`"
      class="text-sm text-[var(--text-muted)] hover:text-white transition-colors mb-8 inline-flex items-center gap-1.5"
    >
      ← League
    </RouterLink>

    <!-- Loading -->
    <div v-if="isLoading" class="space-y-3">
      <div class="h-8 w-64 bg-[var(--surface)] rounded animate-pulse" />
      <div v-for="i in 8" :key="i" class="h-16 bg-[var(--surface)] rounded animate-pulse" />
    </div>
    <div v-else-if="error" class="text-[var(--red)] text-sm">{{ error }}</div>

    <template v-else-if="data">
      <!-- Header ─────────────────────────────────────────────────────────── -->
      <div class="flex items-start justify-between mb-1">
        <h1 class="text-3xl font-bold tracking-tight" style="letter-spacing: -0.03em">
          Week {{ data.weekNumber }}
        </h1>
        <div class="flex items-center gap-2 mt-1.5">
          <template v-if="isLive">
            <span class="relative flex h-2 w-2">
              <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75" />
              <span class="relative inline-flex rounded-full h-2 w-2 bg-green-500" />
            </span>
            <span class="text-xs font-medium text-green-400">Live</span>
          </template>
          <span v-else class="text-xs text-[var(--text-muted)]">{{ data.weekStatus }}</span>
        </div>
      </div>
      <p class="text-sm text-[var(--text-muted)] mb-8">Season {{ data.season }}</p>

      <!-- Cut line callout ─────────────────────────────────────────────── -->
      <div
        v-if="cutLineTeam"
        class="mb-8 px-4 py-3 rounded-lg flex items-center justify-between gap-4"
        style="background: rgba(239,68,68,0.07); border: 1px solid rgba(239,68,68,0.2)"
      >
        <div class="flex items-center gap-3">
          <span class="text-lg">✂️</span>
          <div>
            <p class="text-sm font-semibold text-red-400">On the cut line</p>
            <p class="text-xs text-[var(--text-muted)]">
              <span class="font-medium text-white">{{ cutLineTeam.teamName }}</span>
              is currently in last place
              <template v-if="cutLineGap"> — {{ cutLineGap }} pts behind safety</template>
            </p>
          </div>
        </div>
        <RouterLink
          :to="`/leagues/${leagueId}/teams/${cutLineTeam.teamId}/live`"
          class="text-xs px-3 py-1.5 rounded-lg shrink-0 transition-colors"
          style="background: rgba(239,68,68,0.12); color: #f87171; border: 1px solid rgba(239,68,68,0.25)"
          onmouseover="this.style.background='rgba(239,68,68,0.2)'"
          onmouseout="this.style.background='rgba(239,68,68,0.12)'"
        >
          Watch Live →
        </RouterLink>
      </div>

      <!-- Scoreboard ──────────────────────────────────────────────────── -->
      <section class="mb-10">
        <h2 class="label mb-3">Scoreboard</h2>
        <div class="card overflow-hidden">
          <template v-for="(team, i) in active" :key="team.teamId">
            <!-- Cut line divider — drawn before the last row -->
            <div
              v-if="i === active.length - 1 && active.length > 1"
              class="cut-line-divider flex items-center gap-3 px-4 py-1"
            >
              <div class="flex-1 border-t border-dashed border-red-500/40" />
              <span class="text-[10px] font-bold tracking-widest uppercase text-red-500/70 shrink-0">
                Cut Line
              </span>
              <div class="flex-1 border-t border-dashed border-red-500/40" />
            </div>

            <!-- Team row -->
            <div
              class="matchup-row flex items-center gap-4 px-4 py-3 border-b border-[var(--border-subtle)] last:border-0"
              :class="{
                'row-flash':   flashIds.has(team.teamId),
                'cut-line-row': team.isOnCutLine,
              }"
            >
              <!-- Rank -->
              <span
                class="text-sm font-bold w-8 shrink-0 tabular-nums"
                :class="i === 0 ? 'text-yellow-400' : i === 1 ? 'text-slate-300' : i === 2 ? 'text-amber-600' : 'text-[var(--text-muted)]'"
              >
                {{ rankLabel(i) }}
              </span>

              <!-- Team info + bar -->
              <div class="flex-1 min-w-0">
                <div class="flex items-baseline gap-2 mb-1">
                  <RouterLink
                    :to="`/leagues/${leagueId}/teams/${team.teamId}`"
                    class="font-medium text-white hover:text-[var(--accent)] transition-colors truncate"
                  >
                    {{ team.teamName }}
                  </RouterLink>
                  <span v-if="team.managerName" class="text-xs text-[var(--text-muted)] truncate shrink-0">
                    {{ team.managerName }}
                  </span>
                </div>
                <!-- Points bar -->
                <div class="h-1 rounded-full overflow-hidden" style="background: var(--border)">
                  <div
                    class="h-full rounded-full transition-all duration-500"
                    :style="{
                      width: barWidth(team.points) + '%',
                      background: team.isOnCutLine
                        ? 'rgba(239,68,68,0.7)'
                        : i === 0
                          ? 'var(--accent)'
                          : 'rgba(255,255,255,0.25)',
                    }"
                  />
                </div>
              </div>

              <!-- Points -->
              <div class="text-right shrink-0 flex items-center gap-2">
                <RouterLink
                  v-if="isLive"
                  :to="`/leagues/${leagueId}/teams/${team.teamId}/live`"
                  class="text-[10px] text-[var(--text-muted)] hover:text-white transition-colors"
                  title="Watch live"
                >
                  →
                </RouterLink>
                <span
                  class="text-xl font-black tabular-nums"
                  :style="{ letterSpacing: '-0.03em', color: team.isOnCutLine ? '#f87171' : 'var(--text)' }"
                >
                  {{ fmtPts(team.points) }}
                </span>
              </div>
            </div>
          </template>
        </div>
      </section>

      <!-- Eliminated teams ────────────────────────────────────────────── -->
      <section v-if="eliminated.length > 0">
        <h2 class="label mb-3">Previously Eliminated</h2>
        <div class="card overflow-hidden">
          <div
            v-for="team in eliminated"
            :key="team.teamId"
            class="flex items-center gap-4 px-4 py-3 border-b border-[var(--border-subtle)] last:border-0 opacity-40"
          >
            <span class="text-[var(--text-muted)] w-8 shrink-0 text-sm">✂</span>
            <div class="flex-1 min-w-0">
              <RouterLink
                :to="`/leagues/${leagueId}/teams/${team.teamId}`"
                class="font-medium text-white hover:text-[var(--accent)] transition-colors"
              >
                {{ team.teamName }}
              </RouterLink>
              <p v-if="team.managerName" class="text-xs text-[var(--text-muted)]">{{ team.managerName }}</p>
            </div>
            <span class="text-xs text-red-400 shrink-0">Cut — Wk {{ team.eliminatedWeek }}</span>
          </div>
        </div>
      </section>
    </template>
  </div>
</template>

<style scoped>
.matchup-row {
  transition: background 0.1s ease;
}
.matchup-row:hover {
  background: var(--surface-raised);
}
.cut-line-row {
  background: rgba(239, 68, 68, 0.04);
}
.cut-line-row:hover {
  background: rgba(239, 68, 68, 0.08);
}

@keyframes score-flash {
  0%   { background: color-mix(in srgb, #22c55e 18%, transparent); }
  100% { background: transparent; }
}
.row-flash {
  animation: score-flash 1.2s ease-out forwards;
}
.cut-line-row.row-flash {
  animation: score-flash 1.2s ease-out forwards;
}
</style>
