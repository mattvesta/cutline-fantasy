<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { useLeagueStore } from '../stores/league'

const route    = useRoute()
const leagueId = route.params.leagueId as string
const store    = useLeagueStore()

onMounted(() => store.fetchLeague(leagueId))

const standings = computed(() => {
  if (!store.current) return []
  const active    = store.current.teams.filter(t => !t.isEliminated).sort((a, b) => a.name.localeCompare(b.name))
  const eliminated = store.current.teams.filter(t => t.isEliminated).sort((a, b) => (b.eliminatedWeek ?? 0) - (a.eliminatedWeek ?? 0))
  return [...active, ...eliminated]
})
</script>

<template>
  <div class="page">
    <RouterLink to="/" class="text-sm text-[var(--text-muted)] hover:text-white transition-colors mb-8 inline-flex items-center gap-1.5">
      ← Leagues
    </RouterLink>

    <div v-if="store.isLoading" class="space-y-3">
      <div class="h-8 w-64 bg-[var(--surface)] rounded animate-pulse" />
      <div class="h-4 w-40 bg-[var(--surface)] rounded animate-pulse" />
    </div>
    <div v-else-if="store.error" class="text-[var(--red)] text-sm">{{ store.error }}</div>

    <template v-else-if="store.current">
      <!-- Header -->
      <div class="flex items-start justify-between mb-10">
        <div>
          <div class="flex items-center gap-3 mb-1">
            <div
              class="w-10 h-10 rounded-lg flex items-center justify-center text-sm font-bold shrink-0"
              style="background: var(--accent-dim); color: var(--accent)"
            >
              {{ store.current.name[0].toUpperCase() }}
            </div>
            <h1 class="text-3xl font-bold">{{ store.current.name }}</h1>
          </div>
          <p class="text-sm text-[var(--text-muted)] ml-[3.25rem]">
            Season {{ store.current.season }} · {{ store.current.teams?.length ?? 0 }} teams
          </p>
        </div>

        <span
          class="text-xs font-medium px-2.5 py-1 rounded-full mt-1"
          :class="{
            'text-yellow-400 bg-yellow-400/10': store.current.status === 'Drafting',
            'text-green-400 bg-green-400/10':   store.current.status === 'Active',
            'text-[var(--text-muted)] bg-[var(--surface)]': store.current.status === 'Setup' || store.current.status === 'Completed',
          }"
        >
          {{ store.current.status }}
        </span>
      </div>

      <!-- Standings -->
      <section>
        <h2 class="label mb-4">Standings</h2>
        <div class="card overflow-hidden">
          <table class="data-table">
            <thead>
              <tr>
                <th class="pl-5 w-10">#</th>
                <th>Team</th>
                <th class="hidden sm:table-cell">Owner</th>
                <th class="text-right pr-5">Status</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="(team, i) in standings"
                :key="team.id"
                :class="{ 'opacity-35': team.isEliminated }"
              >
                <td class="pl-5 text-[var(--text-muted)]">{{ i + 1 }}</td>
                <td>
                  <RouterLink
                    :to="`/leagues/${leagueId}/teams/${team.id}`"
                    class="font-medium transition-colors"
                    style="color: var(--text)"
                    onmouseover="this.style.color='var(--accent)'"
                    onmouseout="this.style.color='var(--text)'"
                  >
                    {{ team.name }}
                  </RouterLink>
                </td>
                <td class="text-[var(--text-muted)] hidden sm:table-cell">{{ team.ownerUserId }}</td>
                <td class="text-right pr-5">
                  <span v-if="team.isEliminated" class="text-xs px-2 py-0.5 rounded-full bg-red-500/10 text-red-400">
                    Cut — Wk {{ team.eliminatedWeek }}
                  </span>
                  <span v-else class="text-xs px-2 py-0.5 rounded-full bg-green-500/10 text-green-400">
                    Active
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </template>
  </div>
</template>
