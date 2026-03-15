<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'
import { scoringApi } from '../api/scoring'
import type { LiveTeamResponse, LiveRosterSlot, PlayerStats } from '../api/types'

const route    = useRoute()
const leagueId = route.params.leagueId as string
const teamId   = route.params.teamId as string

const data       = ref<LiveTeamResponse | null>(null)
const isLoading  = ref(true)
const error      = ref<string | null>(null)
const flashIds   = ref<Set<string>>(new Set())

interface LeagueEntry { teamId: string; teamName: string; points: number }
const leagueScores  = ref<LeagueEntry[]>([])
const flashTeamIds  = ref<Set<string>>(new Set())

// ── SignalR ────────────────────────────────────────────────────────────────
const connection = new HubConnectionBuilder()
  .withUrl('/hubs/scoring')
  .withAutomaticReconnect()
  .build()

connection.on('PlayerStatUpdate', (update: Record<string, unknown>) => {
  if (!data.value) return
  const pid  = update.playerId as string
  const slot = data.value.roster.find(r => r.player?.id === pid)
  if (!slot) return
  slot.stats = { ...(slot.stats ?? {}), ...update } as PlayerStats
  flashIds.value = new Set([...flashIds.value, slot.slotId])
  setTimeout(() => {
    flashIds.value = new Set([...flashIds.value].filter(id => id !== slot.slotId))
  }, 1200)
})

connection.on('TeamScoreUpdate', (update: { teamId: string; points: number }) => {
  // Update my own team's total
  if (data.value && update.teamId === teamId) {
    data.value.totalPoints = update.points
  }
  // Update leaderboard
  const entry = leagueScores.value.find(e => e.teamId === update.teamId)
  if (entry) {
    entry.points = update.points
    leagueScores.value = [...leagueScores.value].sort((a, b) => b.points - a.points)
    flashTeamIds.value = new Set([...flashTeamIds.value, update.teamId])
    setTimeout(() => {
      flashTeamIds.value = new Set([...flashTeamIds.value].filter(id => id !== update.teamId))
    }, 1200)
  }
})

onMounted(async () => {
  try {
    const [team, league] = await Promise.all([
      scoringApi.getLiveTeam(leagueId, teamId),
      scoringApi.getLiveLeague(leagueId).catch(() => null),
    ])
    data.value = team
    if (league) {
      leagueScores.value = [...league.teams].sort((a, b) => b.points - a.points)
    }
  } catch {
    error.value = 'Failed to load live scoring data'
  } finally {
    isLoading.value = false
  }

  try {
    await connection.start()
    await connection.invoke('JoinTeam', teamId)
    await connection.invoke('JoinLeague', leagueId)
  } catch {
    // SignalR optional — view still works without it
  }
})

onUnmounted(async () => {
  if (connection.state === HubConnectionState.Connected) {
    await connection.invoke('LeaveTeam', teamId).catch(() => {})
    await connection.stop().catch(() => {})
  }
})

// ── Display helpers ────────────────────────────────────────────────────────
const SLOT_ORDER = ['QB', 'RB', 'WR', 'TE', 'Flex', 'SuperFlex', 'K', 'DEF']

const starters = computed(() =>
  (data.value?.roster ?? [])
    .filter(r => r.isStarter)
    .sort((a, b) => SLOT_ORDER.indexOf(a.slotType) - SLOT_ORDER.indexOf(b.slotType))
)
const bench = computed(() =>
  (data.value?.roster ?? []).filter(r => !r.isStarter)
)

const myRank = computed(() => {
  const idx = leagueScores.value.findIndex(e => e.teamId === teamId)
  return idx === -1 ? null : idx + 1
})

const POS_COLOR: Record<string, string> = {
  QB: '#f97316', RB: '#22c55e', WR: '#3b82f6', TE: '#a855f7',
  K: '#eab308', DEF: '#64748b',
}

function posColor(pos: string | undefined) {
  return pos ? (POS_COLOR[pos] ?? '#6b6784') : '#6b6784'
}

type GameStatus = 'Scheduled' | 'InProgress' | 'Final' | 'Bye'

const STATUS_LABEL: Record<GameStatus, string> = {
  Scheduled: 'Scheduled', InProgress: 'LIVE', Final: 'Final', Bye: 'Bye',
}

function statusClass(s: GameStatus | undefined) {
  if (!s) return 'text-[var(--text-muted)]'
  if (s === 'InProgress') return 'text-green-400 font-bold'
  if (s === 'Final')      return 'text-[var(--text-muted)]'
  return 'text-[var(--text-muted)]'
}

function statLine(slot: LiveRosterSlot): string {
  const s = slot.stats
  const p = slot.player
  if (!s || !p) return ''

  switch (p.position) {
    case 'QB': {
      const parts: string[] = []
      if (s.passingYards  != null) parts.push(`${s.passingCompletions ?? 0}/${s.passingAttempts ?? 0} ${s.passingYards} yds`)
      if (s.passingTDs    != null && s.passingTDs > 0) parts.push(`${s.passingTDs} TD`)
      if (s.interceptions != null && s.interceptions > 0) parts.push(`${s.interceptions} INT`)
      if (s.rushingYards  != null && s.rushingYards > 0) parts.push(`${s.rushingYards} rush`)
      return parts.join('  ·  ')
    }
    case 'RB': {
      const parts: string[] = []
      if (s.rushingYards  != null) parts.push(`${s.rushingAttempts ?? 0} car, ${s.rushingYards} yds`)
      if (s.rushingTDs    != null && s.rushingTDs > 0) parts.push(`${s.rushingTDs} TD`)
      if (s.receptions    != null) parts.push(`${s.receptions}/${s.targets ?? 0} rec, ${s.receivingYards ?? 0} yds`)
      if (s.receivingTDs  != null && s.receivingTDs > 0) parts.push(`${s.receivingTDs} TD`)
      return parts.join('  ·  ')
    }
    case 'WR':
    case 'TE': {
      const parts: string[] = []
      if (s.receptions    != null) parts.push(`${s.receptions}/${s.targets ?? 0} rec, ${s.receivingYards ?? 0} yds`)
      if (s.receivingTDs  != null && s.receivingTDs > 0) parts.push(`${s.receivingTDs} TD`)
      return parts.join('  ·  ')
    }
    case 'K': {
      const parts: string[] = []
      if (s.fieldGoalsMade  != null) parts.push(`${s.fieldGoalsMade}/${s.fieldGoalsAttempted ?? 0} FG`)
      if (s.longFieldGoal   != null && s.longFieldGoal > 0) parts.push(`long ${s.longFieldGoal}`)
      if (s.extraPointsMade != null) parts.push(`${s.extraPointsMade} XP`)
      return parts.join('  ·  ')
    }
    case 'DEF': {
      const parts: string[] = []
      if (s.sacks            != null && s.sacks > 0) parts.push(`${s.sacks} sack${s.sacks > 1 ? 's' : ''}`)
      if (s.defensiveInts    != null && s.defensiveInts > 0) parts.push(`${s.defensiveInts} INT`)
      if (s.fumblesRecovered != null && s.fumblesRecovered > 0) parts.push(`${s.fumblesRecovered} FR`)
      if (s.defensiveTDs     != null && s.defensiveTDs > 0) parts.push(`${s.defensiveTDs} TD`)
      if (s.pointsAllowed    != null) parts.push(`${s.pointsAllowed} PA`)
      return parts.join('  ·  ')
    }
    default: return ''
  }
}

function fmtPts(n: number | null | undefined) {
  if (n == null) return '—'
  return n % 1 === 0 ? n.toString() : n.toFixed(2)
}

function rankLabel(i: number) {
  if (i === 1) return '1st'
  if (i === 2) return '2nd'
  if (i === 3) return '3rd'
  return `${i}th`
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
      <div class="h-8 w-64 bg-[var(--surface)] rounded animate-pulse" />
      <div v-for="i in 8" :key="i" class="h-14 bg-[var(--surface)] rounded animate-pulse" />
    </div>
    <div v-else-if="error" class="text-[var(--red)] text-sm">{{ error }}</div>

    <template v-else-if="data">
      <!-- Header ─────────────────────────────────────────────────────────── -->
      <div class="flex items-start justify-between mb-1">
        <div>
          <h1 class="text-3xl font-bold tracking-tight" style="letter-spacing: -0.03em">
            {{ data.teamName }}
          </h1>
          <div v-if="myRank" class="text-sm text-[var(--text-muted)] mt-0.5">
            {{ rankLabel(myRank) }} place of {{ leagueScores.length }}
          </div>
        </div>
        <!-- Total points -->
        <div class="text-right">
          <div class="text-4xl font-black tabular-nums" style="letter-spacing: -0.04em; color: var(--accent)">
            {{ fmtPts(data.totalPoints) }}
          </div>
          <div class="text-xs text-[var(--text-muted)] mt-0.5">starter pts · wk {{ data.weekNumber }}</div>
        </div>
      </div>

      <!-- Live indicator -->
      <div class="flex items-center gap-2 mb-8">
        <span class="relative flex h-2 w-2">
          <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75" />
          <span class="relative inline-flex rounded-full h-2 w-2 bg-green-500" />
        </span>
        <span class="text-xs text-green-400 font-medium">Live scoring · Season {{ data.season }}</span>
      </div>

      <!-- Two-column layout on wider screens ─────────────────────────────── -->
      <div class="lg:grid lg:grid-cols-[1fr_280px] lg:gap-8 lg:items-start">

        <!-- Left: roster ─────────────────────────────────────────────────── -->
        <div>
          <!-- Starters -->
          <section class="mb-8">
            <h2 class="label mb-3">Starters</h2>
            <div class="card overflow-hidden">
              <div
                v-for="slot in starters"
                :key="slot.slotId"
                class="score-row flex items-center border-b border-[var(--border-subtle)] last:border-0"
                :class="{ 'row-flash': flashIds.has(slot.slotId) }"
              >
                <!-- Position badge -->
                <div class="w-14 flex items-center justify-center shrink-0 self-stretch border-r border-[var(--border-subtle)]">
                  <span class="text-xs font-bold" :style="{ color: posColor(slot.player?.position) }">
                    {{ slot.slotType }}
                  </span>
                </div>

                <!-- Player info -->
                <div class="flex-1 min-w-0 px-4 py-3">
                  <div class="flex items-baseline gap-2">
                    <span class="font-medium truncate">{{ slot.player ? slot.player.name : 'Empty' }}</span>
                    <span class="text-xs text-[var(--text-muted)] shrink-0">{{ slot.player?.team ?? '' }}</span>
                    <span
                      v-if="slot.stats"
                      class="text-xs shrink-0 ml-1"
                      :class="statusClass(slot.stats.gameStatus)"
                    >
                      {{ STATUS_LABEL[slot.stats.gameStatus] }}
                      <span v-if="slot.stats.opponent" class="text-[var(--text-muted)] font-normal ml-0.5">
                        {{ slot.stats.opponent }}
                      </span>
                    </span>
                  </div>
                  <div class="text-xs text-[var(--text-muted)] mt-0.5 tabular-nums truncate">
                    {{ statLine(slot) || (slot.stats?.gameStatus === 'Scheduled' ? 'Not started' : '—') }}
                  </div>
                </div>

                <!-- Points -->
                <div class="w-20 text-right pr-4 shrink-0">
                  <span
                    class="text-lg font-bold tabular-nums"
                    :class="slot.stats?.gameStatus === 'InProgress' ? 'text-green-400' : 'text-white'"
                  >
                    {{ fmtPts(slot.stats?.points) }}
                  </span>
                </div>
              </div>
            </div>
          </section>

          <!-- Bench -->
          <section v-if="bench.length > 0">
            <h2 class="label mb-3">Bench</h2>
            <div class="card overflow-hidden">
              <div
                v-for="slot in bench"
                :key="slot.slotId"
                class="score-row flex items-center border-b border-[var(--border-subtle)] last:border-0 opacity-60"
              >
                <div class="w-14 flex items-center justify-center shrink-0 self-stretch border-r border-[var(--border-subtle)]">
                  <span class="text-xs font-medium text-[var(--text-muted)]">BN</span>
                </div>
                <div class="flex-1 min-w-0 px-4 py-3">
                  <div class="flex items-baseline gap-2">
                    <span class="font-medium truncate text-sm">{{ slot.player ? slot.player.name : 'Empty' }}</span>
                    <span class="text-xs text-[var(--text-muted)] shrink-0">
                      {{ slot.player?.position }} · {{ slot.player?.team ?? '' }}
                    </span>
                  </div>
                  <div class="text-xs text-[var(--text-muted)] mt-0.5 truncate">{{ statLine(slot) || '—' }}</div>
                </div>
                <div class="w-20 text-right pr-4 shrink-0">
                  <span class="text-sm font-medium text-[var(--text-muted)] tabular-nums">
                    {{ fmtPts(slot.stats?.points) }}
                  </span>
                </div>
              </div>
            </div>
          </section>
        </div>

        <!-- Right: leaderboard ───────────────────────────────────────────── -->
        <aside v-if="leagueScores.length > 0" class="mt-8 lg:mt-0">
          <h2 class="label mb-3">League Standings</h2>
          <div class="card overflow-hidden">
            <div
              v-for="(entry, i) in leagueScores"
              :key="entry.teamId"
              class="leaderboard-row flex items-center gap-3 px-4 py-2.5 border-b border-[var(--border-subtle)] last:border-0"
              :class="{
                'is-mine': entry.teamId === teamId,
                'leaderboard-flash': flashTeamIds.has(entry.teamId),
              }"
            >
              <!-- Rank -->
              <span
                class="text-xs font-bold w-6 text-center shrink-0 tabular-nums"
                :class="i === 0 ? 'text-yellow-400' : i === 1 ? 'text-slate-300' : i === 2 ? 'text-amber-600' : 'text-[var(--text-muted)]'"
              >
                {{ i + 1 }}
              </span>

              <!-- Team name -->
              <span
                class="flex-1 text-sm truncate"
                :class="entry.teamId === teamId ? 'font-semibold text-white' : 'text-[var(--text-secondary)]'"
              >
                {{ entry.teamName }}
                <span v-if="entry.teamId === teamId" class="text-[var(--accent)] text-xs ml-1">you</span>
              </span>

              <!-- Points -->
              <span
                class="text-sm font-bold tabular-nums shrink-0"
                :class="entry.teamId === teamId ? 'text-[var(--accent)]' : 'text-white'"
              >
                {{ fmtPts(entry.points) }}
              </span>
            </div>
          </div>
        </aside>

      </div>
    </template>
  </div>
</template>

<style scoped>
.score-row {
  transition: background 0.1s ease;
}
.score-row:hover {
  background: var(--surface-raised);
}

.leaderboard-row {
  transition: background 0.15s ease;
}
.leaderboard-row.is-mine {
  background: color-mix(in srgb, var(--accent) 6%, transparent);
}
.leaderboard-row:hover {
  background: var(--surface-raised);
}

@keyframes score-flash {
  0%   { background: color-mix(in srgb, var(--accent) 20%, transparent); }
  60%  { background: color-mix(in srgb, var(--accent) 10%, transparent); }
  100% { background: transparent; }
}
.row-flash {
  animation: score-flash 1.2s ease-out forwards;
}

@keyframes leaderboard-flash {
  0%   { background: color-mix(in srgb, #22c55e 18%, transparent); }
  100% { background: transparent; }
}
.leaderboard-flash {
  animation: leaderboard-flash 1.2s ease-out forwards;
}
.leaderboard-row.is-mine.leaderboard-flash {
  animation: leaderboard-flash 1.2s ease-out forwards;
}
</style>
