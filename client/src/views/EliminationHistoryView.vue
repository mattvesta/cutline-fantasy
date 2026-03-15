<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import type { EliminationEvent } from '../api/types'

const route    = useRoute()
const leagueId = route.params.leagueId as string

const history   = ref<EliminationEvent[]>([])
const isLoading = ref(true)
const error     = ref<string | null>(null)
const expanded  = ref<Set<number>>(new Set())

onMounted(async () => {
  try {
    const res = await fetch(`/api/leagues/${leagueId}/history`)
    if (!res.ok) throw new Error(`${res.status}`)
    history.value = await res.json()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load history'
  } finally {
    isLoading.value = false
  }
})

function toggleScores(week: number) {
  if (expanded.value.has(week)) expanded.value.delete(week)
  else expanded.value.add(week)
  // trigger reactivity
  expanded.value = new Set(expanded.value)
}

const POS_COLOR: Record<string, string> = {
  QB: '#f97316', RB: '#22c55e', WR: '#3b82f6', TE: '#a855f7',
  K: '#eab308', DEF: '#64748b',
}
function posColor(pos: string) { return POS_COLOR[pos] ?? '#6b6784' }
</script>

<template>
  <div class="page max-w-2xl">
    <RouterLink
      :to="`/leagues/${leagueId}`"
      class="text-sm text-[var(--text-muted)] hover:text-white transition-colors mb-8 inline-flex items-center gap-1.5"
    >
      ← League
    </RouterLink>

    <h1 class="text-3xl font-bold tracking-tight mb-1" style="letter-spacing: -0.03em">
      Elimination History
    </h1>
    <p class="text-sm text-[var(--text-muted)] mb-10">Who got cut, when, and what dropped to waivers.</p>

    <!-- Loading -->
    <div v-if="isLoading" class="space-y-4">
      <div v-for="i in 3" :key="i" class="h-32 rounded-xl bg-[var(--surface)] animate-pulse" />
    </div>

    <div v-else-if="error" class="text-[var(--red)] text-sm">{{ error }}</div>

    <!-- Empty -->
    <div
      v-else-if="history.length === 0"
      class="text-center py-16 text-[var(--text-muted)] text-sm"
    >
      No eliminations yet. The blade hasn't fallen.
    </div>

    <!-- Timeline -->
    <div v-else class="relative">
      <!-- Vertical connecting line -->
      <div
        class="absolute left-[1.4rem] top-6 bottom-6 w-px"
        style="background: var(--border)"
      />

      <div class="space-y-6">
        <div
          v-for="event in history"
          :key="event.weekNumber"
          class="relative flex gap-5"
        >
          <!-- Week badge -->
          <div class="relative shrink-0 flex flex-col items-center">
            <div
              class="w-11 h-11 rounded-full flex items-center justify-center z-10 text-xs font-black shrink-0"
              style="background: var(--accent); color: white; letter-spacing: -0.03em"
            >
              <span>W{{ event.weekNumber }}</span>
            </div>
          </div>

          <!-- Card -->
          <div
            class="flex-1 rounded-xl mb-1 overflow-hidden"
            style="background: var(--surface); border: 1px solid var(--border)"
          >
            <!-- Eliminated team header -->
            <div class="px-5 pt-4 pb-3" style="border-bottom: 1px solid var(--border-subtle)">
              <div class="flex items-start justify-between gap-3">
                <div>
                  <p class="text-[10px] font-semibold uppercase tracking-widest text-[var(--text-muted)] mb-0.5">
                    ✂ Eliminated
                  </p>
                  <RouterLink
                    v-if="event.eliminatedTeam"
                    :to="`/leagues/${leagueId}/teams/${event.eliminatedTeam.id}`"
                    class="text-xl font-bold transition-colors"
                    style="color: var(--text); letter-spacing: -0.02em"
                    onmouseover="this.style.color='var(--accent)'"
                    onmouseout="this.style.color='var(--text)'"
                  >
                    {{ event.eliminatedTeam.name }}
                  </RouterLink>
                  <span v-else class="text-xl font-bold text-[var(--text-muted)]">Unknown</span>
                </div>
                <div class="text-right shrink-0">
                  <p class="text-2xl font-black tabular-nums" style="letter-spacing: -0.04em; color: var(--accent)">
                    {{ event.losingScore?.toFixed(1) ?? '—' }}
                    <span class="text-sm font-normal text-[var(--text-muted)]">pts</span>
                  </p>
                  <p v-if="event.survivalGap != null" class="text-xs text-[var(--text-muted)] mt-0.5">
                    <span
                      :class="event.survivalGap < 5 ? 'text-orange-400' : 'text-[var(--text-muted)]'"
                    >
                      Lost by {{ event.survivalGap.toFixed(1) }} pts
                    </span>
                  </p>
                </div>
              </div>

              <!-- Toggle all scores -->
              <button
                class="mt-2.5 text-[10px] font-medium text-[var(--text-muted)] hover:text-white transition-colors flex items-center gap-1"
                @click="toggleScores(event.weekNumber)"
              >
                {{ expanded.has(event.weekNumber) ? '▲' : '▼' }}
                {{ expanded.has(event.weekNumber) ? 'Hide' : 'Show' }} all scores ({{ event.allScores.length }} teams)
              </button>

              <!-- All scores (expandable) -->
              <div v-if="expanded.has(event.weekNumber)" class="mt-3 space-y-1">
                <div
                  v-for="score in event.allScores"
                  :key="score.teamId"
                  class="flex items-center justify-between text-xs"
                  :class="score.teamId === event.eliminatedTeam?.id
                    ? 'text-red-400 font-semibold'
                    : 'text-[var(--text-secondary)]'"
                >
                  <span class="flex items-center gap-1.5">
                    <span class="tabular-nums w-4 text-right text-[var(--text-muted)]">#{{ score.rank }}</span>
                    {{ score.teamName }}
                    <span v-if="score.teamId === event.eliminatedTeam?.id" class="text-[9px]">✂</span>
                  </span>
                  <span class="tabular-nums font-medium">{{ score.points.toFixed(1) }}</span>
                </div>
              </div>
            </div>

            <!-- Dropped players -->
            <div class="px-5 py-3">
              <p class="text-[10px] font-semibold uppercase tracking-widest text-[var(--text-muted)] mb-2.5">
                Released to waivers
                <span class="normal-case font-normal ml-1">{{ event.droppedPlayers.length }} players</span>
              </p>

              <div v-if="event.droppedPlayers.length === 0" class="text-xs text-[var(--text-muted)]">
                No roster data recorded for this week.
              </div>

              <div v-else class="space-y-1.5">
                <div
                  v-for="(drop, i) in event.droppedPlayers"
                  :key="i"
                  class="flex items-center gap-2 text-xs"
                  :class="drop.claimedBy ? '' : 'opacity-40'"
                >
                  <!-- Position badge -->
                  <span
                    v-if="drop.player"
                    class="shrink-0 text-[10px] font-bold w-8 text-center"
                    :style="{ color: posColor(drop.player.position) }"
                  >
                    {{ drop.player.position }}
                  </span>
                  <span v-else class="shrink-0 w-8" />

                  <!-- Player name -->
                  <span class="flex-1 truncate font-medium">
                    {{ drop.player ? `${drop.player.firstName} ${drop.player.lastName}` : 'Unknown' }}
                    <span v-if="drop.player?.nflTeam" class="text-[var(--text-muted)] font-normal ml-1">
                      {{ drop.player.nflTeam }}
                    </span>
                  </span>

                  <!-- Claim result -->
                  <div v-if="drop.claimedBy" class="shrink-0 flex items-center gap-1.5">
                    <span class="text-[var(--text-muted)]">→</span>
                    <span class="text-[var(--text-secondary)]">{{ drop.claimedBy.name }}</span>
                    <span v-if="drop.faabBid != null" class="font-bold tabular-nums" style="color: var(--accent)">
                      ${{ drop.faabBid.toFixed(0) }}
                    </span>
                  </div>
                  <span v-else class="shrink-0 text-[var(--text-muted)] italic">Unclaimed</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
