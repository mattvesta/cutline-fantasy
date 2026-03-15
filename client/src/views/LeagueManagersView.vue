<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { managersApi } from '../api/managers'
import { leaguesApi } from '../api/leagues'
import type { League, LeagueManager } from '../api/types'

const route    = useRoute()
const leagueId = route.params.leagueId as string

const league      = ref<League | null>(null)
const memberships = ref<LeagueManager[]>([])
const isLoading   = ref(true)
const error       = ref<string | null>(null)

onMounted(async () => {
  try {
    ;[league.value, memberships.value] = await Promise.all([
      leaguesApi.getById(leagueId),
      managersApi.getByLeague(leagueId),
    ])
  } catch {
    error.value = 'Failed to load managers'
  } finally {
    isLoading.value = false
  }
})

function initials(name: string) {
  return name.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase()
}

// The backend filters manager.teams to only include the team in this league
function teamInLeague(m: LeagueManager) {
  return m.manager.teams?.[0] ?? null
}
</script>

<template>
  <div class="page">
    <RouterLink
      :to="`/leagues/${leagueId}`"
      class="text-sm text-[var(--text-muted)] hover:text-white transition-colors mb-8 inline-flex items-center gap-1.5"
    >
      ← {{ league?.name ?? 'League' }}
    </RouterLink>

    <div class="flex items-baseline justify-between mb-8">
      <div>
        <h1 class="text-3xl font-bold tracking-tight" style="letter-spacing: -0.03em">Managers</h1>
        <p v-if="league" class="text-sm text-[var(--text-muted)] mt-1">
          {{ league.name }} · Season {{ league.season }}
        </p>
      </div>
      <span v-if="!isLoading" class="text-sm text-[var(--text-muted)]">
        {{ memberships.length }} {{ memberships.length === 1 ? 'manager' : 'managers' }}
      </span>
    </div>

    <div v-if="isLoading" class="space-y-3">
      <div v-for="n in 4" :key="n" class="card p-5 animate-pulse flex items-center gap-4">
        <div class="w-12 h-12 rounded-full" style="background: var(--surface-raised)" />
        <div class="space-y-2 flex-1">
          <div class="h-4 w-40 rounded" style="background: var(--surface-raised)" />
          <div class="h-3 w-28 rounded" style="background: var(--surface-raised)" />
        </div>
      </div>
    </div>
    <div v-else-if="error" class="text-[var(--red)] text-sm">{{ error }}</div>

    <div v-else-if="memberships.length === 0" class="card p-10 text-center text-[var(--text-muted)] text-sm">
      No managers yet.
    </div>

    <div v-else class="space-y-2">
      <RouterLink
        v-for="m in memberships"
        :key="m.managerId"
        :to="`/managers/${m.managerId}`"
        class="card p-4 flex items-center gap-4 block transition-colors"
        style="text-decoration: none"
        onmouseover="this.style.background='var(--surface-raised)'"
        onmouseout="this.style.background='var(--surface)'"
      >
        <!-- Avatar -->
        <div class="shrink-0">
          <img
            v-if="m.manager.avatarUrl"
            :src="m.manager.avatarUrl"
            :alt="m.manager.displayName"
            class="w-11 h-11 rounded-full object-cover"
          />
          <div
            v-else
            class="w-11 h-11 rounded-full flex items-center justify-center text-sm font-bold"
            style="background: var(--accent-dim); color: var(--accent)"
          >
            {{ initials(m.manager.displayName) }}
          </div>
        </div>

        <!-- Name + commissioner badge -->
        <div class="flex-1 min-w-0">
          <div class="flex items-center gap-2 mb-0.5">
            <span class="font-semibold truncate">{{ m.manager.displayName }}</span>
            <span
              v-if="m.isCommissioner"
              class="text-xs font-bold px-2 py-0.5 rounded-full shrink-0"
              style="background: var(--accent-dim); color: var(--accent)"
            >
              Commissioner
            </span>
          </div>
          <p class="text-sm text-[var(--text-muted)] truncate">{{ m.manager.email }}</p>
        </div>

        <!-- Team + join date -->
        <div class="shrink-0 text-right">
          <RouterLink
            v-if="teamInLeague(m)"
            :to="`/leagues/${leagueId}/teams/${teamInLeague(m)!.id}`"
            class="text-sm font-medium hover:text-[var(--accent)] transition-colors"
            style="color: var(--text)"
            @click.stop
          >
            {{ teamInLeague(m)!.name }}
          </RouterLink>
          <div v-else class="text-xs text-[var(--text-muted)] italic">No team</div>
          <div class="text-xs text-[var(--text-muted)] mt-0.5">
            Joined {{ new Date(m.joinedAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }) }}
          </div>
        </div>
      </RouterLink>
    </div>
  </div>
</template>
