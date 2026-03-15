<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { playersApi } from '../api/players'
import type { Player, PlayerSeasonStats } from '../api/types'

const route = useRoute()
const player = ref<Player | null>(null)
const seasonStats = ref<PlayerSeasonStats[]>([])
const isLoading = ref(true)
const error = ref<string | null>(null)

onMounted(async () => {
  const id = route.params.playerId as string
  try {
    [player.value, seasonStats.value] = await Promise.all([
      playersApi.getById(id),
      playersApi.getSeasonStats(id),
    ])
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load player'
  } finally {
    isLoading.value = false
  }
})

const statusColor: Record<string, string> = {
  Active: 'text-green-400',
  Injured: 'text-yellow-400',
  InjuredReserve: 'text-red-400',
  Inactive: 'text-gray-500',
  Unknown: 'text-gray-600',
}

const statusLabel: Record<string, string> = {
  Active:         'Active',
  Injured:        'Injured',
  InjuredReserve: 'Injured Reserve',
  Inactive:       'Inactive',
  Unknown:        'Unknown',
}

function photoUrl(sleeperId: string | null): string | null {
  return sleeperId ? `https://sleepercdn.com/content/nfl/players/thumb/${sleeperId}.jpg` : null
}

function formatAdp(adp: number): string {
  return Number.isInteger(adp) ? String(adp) : adp.toFixed(1)
}
</script>

<template>
  <div class="page">
    <RouterLink to="/players" class="text-sm text-[var(--text-muted)] hover:text-white transition-colors mb-8 inline-flex items-center gap-1.5">
      ← Players
    </RouterLink>

    <div v-if="isLoading" class="text-[var(--text-muted)] text-sm mt-8">Loading...</div>
    <div v-else-if="error" class="text-[var(--red)] text-sm mt-8">{{ error }}</div>

    <div v-else-if="player" class="mt-4">
      <!-- Header -->
      <div class="flex items-start gap-6 mb-8">
        <img
          v-if="photoUrl(player.sleeperId)"
          :src="photoUrl(player.sleeperId)!"
          :alt="`${player.firstName} ${player.lastName}`"
          class="w-24 h-24 rounded-lg object-cover bg-gray-900"
          @error="($event.target as HTMLImageElement).style.display = 'none'"
        />
        <div v-else class="w-24 h-24 rounded-lg flex items-center justify-center text-3xl font-black" style="background: var(--accent-dim); color: var(--accent)">
          {{ player.firstName[0] }}{{ player.lastName[0] }}
        </div>

        <div>
          <div class="flex items-baseline gap-3 mb-1">
            <h1 class="text-3xl font-bold tracking-tight" style="letter-spacing: -0.03em">{{ player.firstName }} {{ player.lastName }}</h1>
            <span v-if="player.jerseyNumber" class="text-gray-500 text-xl">#{{ player.jerseyNumber }}</span>
          </div>
          <div class="flex items-center gap-3 text-sm mb-3">
            <span class="px-2 py-0.5 rounded font-bold text-xs tracking-wide" style="background: var(--accent-dim); color: var(--accent)">{{ player.position }}</span>
            <span v-if="player.nflTeam" class="text-gray-300">{{ player.nflTeam }}</span>
            <span :class="statusColor[player.status] ?? 'text-gray-500'">
              {{ statusLabel[player.status] ?? player.status }}
            </span>
          </div>
          <div v-if="player.adp != null" class="flex items-baseline gap-2">
            <span class="text-3xl font-bold tabular-nums">{{ formatAdp(player.adp) }}</span>
            <span class="text-sm text-gray-500">Overall ADP</span>
          </div>
          <div v-else class="text-sm text-gray-600">Unranked</div>
        </div>
      </div>

      <!-- Metadata grid -->
      <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3 mb-8">
        <div v-if="player.age" class="card p-4">
          <div class="label mb-1">Age</div>
          <div class="text-lg font-semibold">{{ player.age }}</div>
        </div>
        <div v-if="player.height" class="card p-4">
          <div class="label mb-1">Height</div>
          <div class="text-lg font-semibold">{{ player.height }}</div>
        </div>
        <div v-if="player.weight" class="card p-4">
          <div class="label mb-1">Weight</div>
          <div class="text-lg font-semibold">{{ player.weight }} <span class="text-sm font-normal text-[var(--text-muted)]">lbs</span></div>
        </div>
        <div v-if="player.yearsExperience !== null" class="card p-4">
          <div class="label mb-1">Experience</div>
          <div class="text-lg font-semibold">
            {{ player.yearsExperience === 0 ? 'Rookie' : `${player.yearsExperience} yr${player.yearsExperience !== 1 ? 's' : ''}` }}
          </div>
        </div>
        <div v-if="player.college" class="card p-4">
          <div class="label mb-1">College</div>
          <div class="text-base font-semibold">{{ player.college }}</div>
        </div>
        <div v-if="player.byeWeek" class="card p-4">
          <div class="label mb-1">Bye Week</div>
          <div class="text-lg font-semibold">Week {{ player.byeWeek }}</div>
        </div>
        <div v-if="player.depthChartOrder" class="card p-4">
          <div class="label mb-1">Depth Chart</div>
          <div class="text-lg font-semibold">#{{ player.depthChartOrder }}</div>
        </div>
      </div>

      <!-- Season stats -->
      <section>
        <h2 class="label mb-4">Career Stats</h2>

        <div v-if="seasonStats.length === 0" class="card p-8 text-center text-[var(--text-muted)] text-sm">
          No stats recorded yet
        </div>

        <!-- QB -->
        <div v-else-if="player.position === 'QB'" class="card overflow-hidden">
          <table class="data-table">
            <thead>
              <tr>
                <th class="pl-5">Season</th>
                <th>G</th>
                <th class="hidden sm:table-cell">Fpts</th>
                <th>Cmp/Att</th>
                <th>Yds</th>
                <th>TD</th>
                <th>INT</th>
                <th class="hidden md:table-cell">Rush Yds</th>
                <th class="hidden md:table-cell">Rush TD</th>
                <th class="pr-5 hidden md:table-cell">Fum</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="s in seasonStats" :key="s.season">
                <td class="pl-5 font-medium">{{ s.season }}</td>
                <td>{{ s.gamesPlayed }}</td>
                <td class="hidden sm:table-cell">{{ s.fantasyPoints.toFixed(1) }}</td>
                <td>{{ s.passingCompletions }}/{{ s.passingAttempts }}</td>
                <td>{{ s.passingYards.toLocaleString() }}</td>
                <td>{{ s.passingTDs }}</td>
                <td>{{ s.interceptions }}</td>
                <td class="hidden md:table-cell">{{ s.rushingYards }}</td>
                <td class="hidden md:table-cell">{{ s.rushingTDs }}</td>
                <td class="pr-5 hidden md:table-cell">{{ s.fumbles }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- RB -->
        <div v-else-if="player.position === 'RB'" class="card overflow-hidden">
          <table class="data-table">
            <thead>
              <tr>
                <th class="pl-5">Season</th>
                <th>G</th>
                <th class="hidden sm:table-cell">Fpts</th>
                <th>Att</th>
                <th>Rush Yds</th>
                <th>Rush TD</th>
                <th class="hidden sm:table-cell">Rec</th>
                <th class="hidden sm:table-cell">Tgt</th>
                <th class="hidden md:table-cell">Rec Yds</th>
                <th class="hidden md:table-cell">Rec TD</th>
                <th class="pr-5 hidden md:table-cell">Fum</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="s in seasonStats" :key="s.season">
                <td class="pl-5 font-medium">{{ s.season }}</td>
                <td>{{ s.gamesPlayed }}</td>
                <td class="hidden sm:table-cell">{{ s.fantasyPoints.toFixed(1) }}</td>
                <td>{{ s.rushingAttempts }}</td>
                <td>{{ s.rushingYards.toLocaleString() }}</td>
                <td>{{ s.rushingTDs }}</td>
                <td class="hidden sm:table-cell">{{ s.receptions }}</td>
                <td class="hidden sm:table-cell">{{ s.targets }}</td>
                <td class="hidden md:table-cell">{{ s.receivingYards }}</td>
                <td class="hidden md:table-cell">{{ s.receivingTDs }}</td>
                <td class="pr-5 hidden md:table-cell">{{ s.fumbles }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- WR / TE -->
        <div v-else-if="player.position === 'WR' || player.position === 'TE'" class="card overflow-hidden">
          <table class="data-table">
            <thead>
              <tr>
                <th class="pl-5">Season</th>
                <th>G</th>
                <th class="hidden sm:table-cell">Fpts</th>
                <th>Rec</th>
                <th>Tgt</th>
                <th>Rec Yds</th>
                <th>Rec TD</th>
                <th class="hidden md:table-cell">Rush Yds</th>
                <th class="pr-5 hidden md:table-cell">Rush TD</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="s in seasonStats" :key="s.season">
                <td class="pl-5 font-medium">{{ s.season }}</td>
                <td>{{ s.gamesPlayed }}</td>
                <td class="hidden sm:table-cell">{{ s.fantasyPoints.toFixed(1) }}</td>
                <td>{{ s.receptions }}</td>
                <td>{{ s.targets }}</td>
                <td>{{ s.receivingYards.toLocaleString() }}</td>
                <td>{{ s.receivingTDs }}</td>
                <td class="hidden md:table-cell">{{ s.rushingYards }}</td>
                <td class="pr-5 hidden md:table-cell">{{ s.rushingTDs }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- K -->
        <div v-else-if="player.position === 'K'" class="card overflow-hidden">
          <table class="data-table">
            <thead>
              <tr>
                <th class="pl-5">Season</th>
                <th>G</th>
                <th class="hidden sm:table-cell">Fpts</th>
                <th>FGM</th>
                <th>FGA</th>
                <th>FG%</th>
                <th>XPM</th>
                <th class="pr-5">XPA</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="s in seasonStats" :key="s.season">
                <td class="pl-5 font-medium">{{ s.season }}</td>
                <td>{{ s.gamesPlayed }}</td>
                <td class="hidden sm:table-cell">{{ s.fantasyPoints.toFixed(1) }}</td>
                <td>{{ s.fieldGoalsMade }}</td>
                <td>{{ s.fieldGoalsAttempted }}</td>
                <td>{{ s.fieldGoalsAttempted > 0 ? ((s.fieldGoalsMade / s.fieldGoalsAttempted) * 100).toFixed(1) + '%' : '—' }}</td>
                <td>{{ s.extraPointsMade }}</td>
                <td class="pr-5">{{ s.extraPointsAttempted }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- DEF -->
        <div v-else-if="player.position === 'DEF'" class="card overflow-hidden">
          <table class="data-table">
            <thead>
              <tr>
                <th class="pl-5">Season</th>
                <th>G</th>
                <th class="hidden sm:table-cell">Fpts</th>
                <th>Sacks</th>
                <th>INT</th>
                <th class="hidden sm:table-cell">FR</th>
                <th>TD</th>
                <th class="hidden md:table-cell">PA</th>
                <th class="pr-5 hidden md:table-cell">Saf</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="s in seasonStats" :key="s.season">
                <td class="pl-5 font-medium">{{ s.season }}</td>
                <td>{{ s.gamesPlayed }}</td>
                <td class="hidden sm:table-cell">{{ s.fantasyPoints.toFixed(1) }}</td>
                <td>{{ s.sacks }}</td>
                <td>{{ s.defensiveInterceptions }}</td>
                <td class="hidden sm:table-cell">{{ s.fumblesRecovered }}</td>
                <td>{{ s.defensiveTDs }}</td>
                <td class="hidden md:table-cell">{{ s.pointsAllowed }}</td>
                <td class="pr-5 hidden md:table-cell">{{ s.safeties }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Fallback for any other position -->
        <div v-else class="card overflow-hidden">
          <table class="data-table">
            <thead>
              <tr>
                <th class="pl-5">Season</th>
                <th>G</th>
                <th class="pr-5">Fpts</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="s in seasonStats" :key="s.season">
                <td class="pl-5 font-medium">{{ s.season }}</td>
                <td>{{ s.gamesPlayed }}</td>
                <td class="pr-5">{{ s.fantasyPoints.toFixed(1) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </div>
  </div>
</template>
