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

const commissioner = computed(() =>
  store.current?.leagueManagers?.find(lm => lm.isCommissioner)?.manager ?? null
)

function isCommissioner(managerId: string) {
  return store.current?.leagueManagers?.find(lm => lm.managerId === managerId)?.isCommissioner ?? false
}
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
            <h1 class="text-3xl font-bold tracking-tight" style="letter-spacing: -0.03em">{{ store.current.name }}</h1>
          </div>
          <div class="flex items-center gap-3 text-sm text-[var(--text-muted)] ml-[3.25rem]">
            <span>Season {{ store.current.season }} · {{ store.current.teams?.length ?? 0 }} teams</span>
            <span v-if="commissioner">
              · Commissioner:
              <RouterLink
                :to="`/managers/${commissioner.id}`"
                class="hover:text-white transition-colors ml-1"
              >{{ commissioner.displayName }}</RouterLink>
            </span>
          </div>
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
        <div class="flex items-baseline justify-between mb-4">
          <h2 class="label">Standings</h2>
          <div class="flex items-center gap-4">
            <RouterLink
              v-if="store.current.status === 'Drafting'"
              :to="`/leagues/${leagueId}/draft`"
              class="text-xs font-medium px-2.5 py-1 rounded-full bg-yellow-400/10 text-yellow-400 hover:bg-yellow-400/20 transition-colors"
            >
              Enter Draft Room →
            </RouterLink>
            <RouterLink
              v-if="store.current.status === 'Active'"
              :to="`/leagues/${leagueId}/matchup`"
              class="text-xs font-medium px-2.5 py-1 rounded-full bg-green-400/10 text-green-400 hover:bg-green-400/20 transition-colors flex items-center gap-1.5"
            >
              <span class="relative flex h-1.5 w-1.5">
                <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75" />
                <span class="relative inline-flex rounded-full h-1.5 w-1.5 bg-green-500" />
              </span>
              This Week →
            </RouterLink>
            <RouterLink
              v-if="store.current.teams.some(t => t.isEliminated)"
              :to="`/leagues/${leagueId}/history`"
              class="text-xs text-[var(--text-muted)] hover:text-white transition-colors"
            >
              Elimination History →
            </RouterLink>
            <RouterLink
              v-if="commissioner"
              :to="`/leagues/${leagueId}/commissioner`"
              class="text-xs text-[var(--text-muted)] hover:text-white transition-colors"
            >
              Commissioner Tools →
            </RouterLink>
            <RouterLink
              :to="`/leagues/${leagueId}/managers`"
              class="text-xs text-[var(--text-muted)] hover:text-white transition-colors"
            >
              View all managers →
            </RouterLink>
          </div>
        </div>
        <div class="card overflow-hidden">
          <table class="data-table">
            <thead>
              <tr>
                <th class="pl-5 w-10">#</th>
                <th>Team</th>
                <th class="hidden sm:table-cell">Manager</th>
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
                <td class="hidden sm:table-cell">
                  <RouterLink
                    v-if="team.manager"
                    :to="`/managers/${team.manager.id}`"
                    class="inline-flex items-center gap-1.5 transition-colors text-[var(--text-muted)] hover:text-white"
                  >
                    {{ team.manager.displayName }}
                    <span
                      v-if="isCommissioner(team.manager.id)"
                      class="text-xs font-bold px-1.5 py-0.5 rounded"
                      style="background: var(--accent-dim); color: var(--accent)"
                    >
                      C
                    </span>
                  </RouterLink>
                  <span v-else class="text-[var(--text-muted)]">—</span>
                </td>
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
