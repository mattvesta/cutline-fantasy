<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { playersApi } from '../api/players'
import type { Player } from '../api/types'

const route = useRoute()
const player = ref<Player | null>(null)
const isLoading = ref(true)
const error = ref<string | null>(null)

onMounted(async () => {
  try {
    player.value = await playersApi.getById(route.params.playerId as string)
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
            <h1 class="text-3xl font-bold">{{ player.firstName }} {{ player.lastName }}</h1>
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

      <!-- Stats placeholder -->
      <div class="card p-8 text-center text-[var(--text-muted)] text-sm">
        Season stats coming soon
      </div>
    </div>
  </div>
</template>
