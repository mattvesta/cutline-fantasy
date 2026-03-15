<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { managersApi } from '../api/managers'
import type { Manager } from '../api/types'

const route     = useRoute()
const managerId = route.params.managerId as string

const manager   = ref<Manager | null>(null)
const isLoading = ref(true)
const error     = ref<string | null>(null)

onMounted(async () => {
  try { manager.value = await managersApi.getById(managerId) }
  catch { error.value = 'Failed to load manager' }
  finally { isLoading.value = false }
})

function initials(name: string) {
  return name.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase()
}

const STATUS_COLOR: Record<string, string> = {
  Active:    'text-green-400',
  Drafting:  'text-yellow-400',
  Setup:     'text-[var(--text-muted)]',
  Completed: 'text-[var(--text-muted)]',
}
</script>

<template>
  <div class="page">
    <button
      class="text-sm text-[var(--text-muted)] hover:text-white transition-colors mb-8 inline-flex items-center gap-1.5"
      @click="$router.back()"
    >
      ← Back
    </button>

    <div v-if="isLoading" class="space-y-4">
      <div class="flex items-center gap-5">
        <div class="w-20 h-20 rounded-full animate-pulse" style="background: var(--surface-raised)" />
        <div class="space-y-2">
          <div class="h-6 w-48 rounded animate-pulse" style="background: var(--surface-raised)" />
          <div class="h-4 w-36 rounded animate-pulse" style="background: var(--surface-raised)" />
        </div>
      </div>
    </div>
    <div v-else-if="error" class="text-[var(--red)] text-sm">{{ error }}</div>

    <template v-else-if="manager">
      <!-- Header -->
      <div class="flex items-start gap-5 mb-10">
        <img
          v-if="manager.avatarUrl"
          :src="manager.avatarUrl"
          :alt="manager.displayName"
          class="w-20 h-20 rounded-full object-cover shrink-0"
        />
        <div
          v-else
          class="w-20 h-20 rounded-full flex items-center justify-center text-2xl font-black shrink-0"
          style="background: var(--accent-dim); color: var(--accent)"
        >
          {{ initials(manager.displayName) }}
        </div>

        <div>
          <h1 class="text-3xl font-bold tracking-tight" style="letter-spacing: -0.03em">{{ manager.displayName }}</h1>
          <p class="text-sm text-[var(--text-muted)]">{{ manager.email }}</p>
          <p class="text-xs text-[var(--text-muted)] mt-1">
            Member since {{ new Date(manager.createdAt).toLocaleDateString('en-US', { month: 'long', year: 'numeric' }) }}
          </p>
        </div>
      </div>

      <!-- Leagues -->
      <section v-if="manager.leagueManagers?.length">
        <h2 class="label mb-4">Leagues</h2>
        <div class="space-y-3">
          <RouterLink
            v-for="lm in manager.leagueManagers"
            :key="lm.leagueId"
            :to="`/leagues/${lm.leagueId}`"
            class="card p-4 flex items-center gap-4 block transition-colors"
            style="text-decoration: none"
            onmouseover="this.style.background='var(--surface-raised)'"
            onmouseout="this.style.background='var(--surface)'"
          >
            <div
              class="w-10 h-10 rounded-lg flex items-center justify-center text-sm font-black shrink-0"
              style="background: var(--accent-dim); color: var(--accent)"
            >
              {{ lm.league?.name?.[0]?.toUpperCase() ?? '?' }}
            </div>

            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 mb-0.5">
                <span class="font-semibold truncate">{{ lm.league?.name ?? 'League' }}</span>
                <span
                  v-if="lm.isCommissioner"
                  class="text-xs font-bold px-2 py-0.5 rounded-full shrink-0"
                  style="background: var(--accent-dim); color: var(--accent)"
                >
                  Commissioner
                </span>
              </div>
              <p v-if="lm.league" class="text-xs text-[var(--text-muted)]">
                Season {{ lm.league.season }}
                <span :class="STATUS_COLOR[lm.league.status]"> · {{ lm.league.status }}</span>
              </p>
            </div>

            <!-- Team in this league -->
            <div
              v-if="manager.teams?.find(t => t.leagueId === lm.leagueId)"
              class="shrink-0 text-right"
            >
              <p class="text-sm font-medium">
                {{ manager.teams.find(t => t.leagueId === lm.leagueId)!.name }}
              </p>
              <p
                v-if="manager.teams.find(t => t.leagueId === lm.leagueId)!.isEliminated"
                class="text-xs text-red-400"
              >
                Eliminated
              </p>
              <p v-else class="text-xs text-green-400">Active</p>
            </div>
          </RouterLink>
        </div>
      </section>

      <div v-else class="card p-8 text-center text-[var(--text-muted)] text-sm">
        Not in any leagues yet.
      </div>
    </template>
  </div>
</template>
