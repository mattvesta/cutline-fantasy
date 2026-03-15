<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { leaguesApi } from '../api/leagues'
import type { League } from '../api/types'

const leagues   = ref<League[]>([])
const isLoading = ref(true)
const error     = ref<string | null>(null)

onMounted(async () => {
  try { leagues.value = await leaguesApi.getAll() }
  catch { error.value = 'Failed to load leagues' }
  finally { isLoading.value = false }
})

const STATUS_COLOR: Record<string, { text: string; bg: string }> = {
  Setup:     { text: 'var(--text-muted)',  bg: 'var(--surface-raised)' },
  Drafting:  { text: '#f5b82a',            bg: 'rgba(245,184,42,0.1)'  },
  Active:    { text: 'var(--green)',        bg: 'rgba(34,197,94,0.1)'   },
  Completed: { text: 'var(--text-muted)',  bg: 'var(--surface-raised)' },
}
</script>

<template>
  <div>
    <!-- ── Hero ── -->
    <section class="relative border-b overflow-hidden" style="border-color: var(--border)">
      <!-- Subtle background glow -->
      <div class="pointer-events-none absolute inset-0" style="
        background:
          radial-gradient(ellipse 60% 80% at 75% 50%, rgba(249,115,22,0.10) 0%, transparent 60%),
          radial-gradient(ellipse 40% 60% at 75% 50%, rgba(227,30,36,0.07) 0%, transparent 50%);
      " />

      <div class="page relative py-16 sm:py-20">
        <div class="flex flex-col sm:flex-row items-center justify-between gap-10">

          <!-- Copy -->
          <div class="max-w-lg">
            <p class="label mb-3" style="color: var(--accent)">Guillotine Fantasy Football</p>
            <h1 class="text-5xl sm:text-6xl font-black tracking-tight leading-[1.0] mb-4" style="color: var(--text)">
              Will you<br>make the cut?
            </h1>
            <p class="text-base mb-7 leading-relaxed max-w-md" style="color: var(--text-secondary)">
              Every week the lowest scorer gets eliminated and their roster drops to waivers.
              One survivor. No mercy.
            </p>
            <div class="flex items-center gap-3">
              <RouterLink to="/leagues/create" class="btn btn-primary px-5 py-2.5 text-sm">
                Create a League
              </RouterLink>
              <RouterLink to="/players" class="btn btn-ghost px-5 py-2.5 text-sm">
                Browse Players
              </RouterLink>
            </div>
          </div>

          <!-- Logo -->
          <div class="shrink-0 hidden sm:block">
            <img
              src="/hero-logo.png"
              alt="The Cutline Fantasy"
              class="w-72 h-auto rounded-2xl"
              style="box-shadow: 0 0 60px rgba(227,30,36,0.25), 0 0 20px rgba(249,115,22,0.15)"
            />
          </div>

        </div>
      </div>
    </section>

    <!-- ── Leagues ── -->
    <section class="page">
      <div class="flex items-baseline justify-between mb-6">
        <h2 class="text-xl font-bold">Your Leagues</h2>
        <RouterLink
          to="/leagues/create"
          class="text-sm font-medium transition-colors"
          style="color: var(--text-muted)"
          onmouseover="this.style.color='var(--text)'"
          onmouseout="this.style.color='var(--text-muted)'"
        >
          + New league
        </RouterLink>
      </div>

      <!-- Loading skeleton -->
      <div v-if="isLoading" class="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <div v-for="n in 3" :key="n" class="card p-5 space-y-3 animate-pulse">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-lg" style="background: var(--surface-raised)" />
            <div class="space-y-2 flex-1">
              <div class="h-4 w-1/2 rounded" style="background: var(--surface-raised)" />
              <div class="h-3 w-1/3 rounded" style="background: var(--surface-raised)" />
            </div>
          </div>
        </div>
      </div>

      <div v-else-if="error" class="text-sm" style="color: var(--red)">{{ error }}</div>

      <!-- Empty state -->
      <div v-else-if="leagues.length === 0" class="card p-12 text-center">
        <img src="/logo.png" alt="" class="w-20 h-20 rounded-xl object-cover mx-auto mb-5 opacity-80" />
        <p class="text-lg font-bold mb-2">No leagues yet</p>
        <p class="text-sm mb-6" style="color: var(--text-muted)">
          Create your first guillotine league and send the blade swinging.
        </p>
        <RouterLink to="/leagues/create" class="btn btn-primary">
          Create a League
        </RouterLink>
      </div>

      <!-- League grid -->
      <div v-else class="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <RouterLink
          v-for="league in leagues"
          :key="league.id"
          :to="`/leagues/${league.id}`"
          class="card p-5 block group transition-colors"
          style="text-decoration: none"
          onmouseover="this.style.borderColor='var(--border-subtle)'; this.style.background='var(--surface-raised)'"
          onmouseout="this.style.borderColor='var(--border)'; this.style.background='var(--surface)'"
        >
          <div class="flex items-start justify-between mb-4">
            <!-- Initial avatar -->
            <div
              class="w-10 h-10 rounded-lg flex items-center justify-center text-base font-black"
              style="background: var(--accent-dim); color: var(--accent)"
            >
              {{ league.name[0].toUpperCase() }}
            </div>

            <!-- Status badge -->
            <span
              class="text-xs font-semibold px-2 py-0.5 rounded-full"
              :style="`color: ${STATUS_COLOR[league.status]?.text ?? 'var(--text-muted)'}; background: ${STATUS_COLOR[league.status]?.bg ?? 'var(--surface-raised)'}`"
            >
              {{ league.status }}
            </span>
          </div>

          <p class="font-bold mb-1 group-hover:text-white transition-colors" style="color: var(--text)">
            {{ league.name }}
          </p>
          <p class="text-sm" style="color: var(--text-muted)">
            Season {{ league.season }} · {{ league.teams?.length ?? 0 }} teams
          </p>
        </RouterLink>
      </div>
    </section>
  </div>
</template>
