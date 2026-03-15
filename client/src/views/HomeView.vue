<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { leaguesApi } from '../api/leagues'
import { useAuthStore } from '../stores/auth'
import type { League } from '../api/types'

const auth      = useAuthStore()
const leagues   = ref<League[]>([])
const isLoading = ref(true)
const error     = ref<string | null>(null)

onMounted(async () => {
  if (!auth.isLoggedIn) { isLoading.value = false; return }
  try { leagues.value = await leaguesApi.getAll() }
  catch { error.value = 'Failed to load leagues' }
  finally { isLoading.value = false }
})

const STATUS: Record<string, { dot: string; label: string }> = {
  Setup:     { dot: '#6b6784', label: 'Setup'     },
  Drafting:  { dot: '#facc15', label: 'Drafting'  },
  Active:    { dot: '#4ade80', label: 'Active'     },
  Completed: { dot: '#6b6784', label: 'Completed' },
}
</script>

<template>
  <div>

    <!-- ── Hero ─────────────────────────────────────────────────────────── -->
    <section class="relative overflow-hidden" style="min-height: 88vh; display: flex; align-items: center">

      <!-- Railway-style multi-layer gradient glow -->
      <div class="pointer-events-none absolute inset-0" style="
        background:
          radial-gradient(ellipse 80% 50% at 50% -10%, rgba(227,30,36,0.18) 0%, transparent 60%),
          radial-gradient(ellipse 50% 40% at 80% 20%, rgba(249,115,22,0.10) 0%, transparent 55%),
          radial-gradient(ellipse 40% 30% at 20% 40%, rgba(161,30,227,0.07) 0%, transparent 50%),
          #13111c;
      " />

      <!-- Faint grid lines (subtle Railway texture) -->
      <div class="pointer-events-none absolute inset-0" style="
        background-image:
          linear-gradient(rgba(255,255,255,0.025) 1px, transparent 1px),
          linear-gradient(90deg, rgba(255,255,255,0.025) 1px, transparent 1px);
        background-size: 64px 64px;
        mask-image: radial-gradient(ellipse 80% 60% at 50% 50%, black 20%, transparent 80%);
      " />

      <div class="page relative w-full">
        <div class="flex flex-col lg:flex-row items-center gap-16 lg:gap-24">

          <!-- Copy -->
          <div class="flex-1 text-center lg:text-left">

            <!-- Pill badge -->
            <div class="inline-flex items-center gap-2 mb-8 px-3 py-1.5 rounded-full text-xs font-medium"
              style="background: rgba(227,30,36,0.12); border: 1px solid rgba(227,30,36,0.2); color: #f87171"
            >
              <span class="w-1.5 h-1.5 rounded-full bg-red-500 animate-pulse inline-block" />
              Guillotine Fantasy Football
            </div>

            <!-- Main heading -->
            <h1 class="font-black tracking-tight mb-6"
              style="
                font-size: clamp(3rem, 8vw, 5.5rem);
                line-height: 1.0;
                letter-spacing: -0.04em;
                color: #fff;
              "
            >
              Will you<br>
              <span style="
                background: linear-gradient(135deg, #e31e24 0%, #f97316 50%, #e31e24 100%);
                -webkit-background-clip: text;
                -webkit-text-fill-color: transparent;
                background-clip: text;
              ">make the cut?</span>
            </h1>

            <!-- Body -->
            <p class="text-lg mb-10 max-w-md mx-auto lg:mx-0 leading-relaxed"
              style="color: var(--text-secondary)"
            >
              Every week the lowest scorer is eliminated and their roster
              drops to waivers. Last team standing wins.
            </p>

            <!-- CTAs -->
            <div class="flex items-center gap-3 justify-center lg:justify-start flex-wrap">
              <RouterLink to="/leagues/create" class="btn btn-primary px-6 py-2.5 text-sm font-semibold">
                Create a League
              </RouterLink>
              <RouterLink to="/players" class="btn btn-ghost px-6 py-2.5 text-sm">
                Browse Players
              </RouterLink>
            </div>
          </div>

          <!-- Logo / visual -->
          <div class="shrink-0 hidden lg:flex items-center justify-center">
            <div class="relative">
              <!-- Glow behind image -->
              <div class="absolute inset-0 rounded-2xl"
                style="
                  background: radial-gradient(circle, rgba(227,30,36,0.3) 0%, transparent 70%);
                  filter: blur(40px);
                  transform: scale(1.2);
                "
              />
              <img
                src="/hero-logo.png"
                alt="The Cutline Fantasy"
                class="relative w-64 h-64 object-cover rounded-2xl"
                style="
                  border: 1px solid rgba(255,255,255,0.08);
                  box-shadow: 0 0 0 1px rgba(227,30,36,0.15), 0 32px 80px rgba(0,0,0,0.6);
                "
              />
            </div>
          </div>

        </div>

        <!-- Stats row -->
        <div
          class="mt-20 pt-8 grid grid-cols-3 gap-px"
          style="border-top: 1px solid rgba(255,255,255,0.06)"
        >
          <div class="text-center lg:text-left px-4 first:pl-0">
            <p class="text-2xl font-bold tracking-tight mb-1" style="letter-spacing: -0.03em">Weekly</p>
            <p class="text-xs" style="color: var(--text-muted)">Eliminations</p>
          </div>
          <div class="text-center px-4">
            <p class="text-2xl font-bold tracking-tight mb-1" style="color: var(--accent); letter-spacing: -0.03em">Snake</p>
            <p class="text-xs" style="color: var(--text-muted)">Draft format</p>
          </div>
          <div class="text-center lg:text-right px-4 last:pr-0">
            <p class="text-2xl font-bold tracking-tight mb-1" style="letter-spacing: -0.03em">One</p>
            <p class="text-xs" style="color: var(--text-muted)">Survivor wins</p>
          </div>
        </div>

      </div>
    </section>

    <!-- ── Leagues ───────────────────────────────────────────────────────── -->
    <section class="page" style="padding-top: 4rem">

      <!-- Not logged in -->
      <div v-if="!auth.isLoggedIn"
        class="card p-16 text-center"
        style="background: linear-gradient(135deg, var(--surface) 0%, rgba(44,41,64,0.4) 100%)"
      >
        <p class="text-xl font-bold tracking-tight mb-2" style="letter-spacing: -0.02em">Sign in to see your leagues</p>
        <p class="text-sm mb-8 max-w-xs mx-auto" style="color: var(--text-muted)">
          Create an account or sign in to manage your guillotine leagues.
        </p>
        <div class="flex items-center gap-3 justify-center">
          <RouterLink to="/login" class="btn btn-primary">Sign in</RouterLink>
          <RouterLink to="/register" class="btn btn-ghost">Create account</RouterLink>
        </div>
      </div>

      <template v-else>
        <div class="flex items-center justify-between mb-8">
          <div>
            <h2 class="text-2xl font-bold tracking-tight mb-1" style="letter-spacing: -0.03em">
              Your Leagues
            </h2>
            <p class="text-sm" style="color: var(--text-muted)">Manage and track your guillotine leagues</p>
          </div>
          <RouterLink to="/leagues/create" class="btn btn-ghost text-xs py-1.5">
            + New league
          </RouterLink>
        </div>

        <!-- Loading skeleton -->
        <div v-if="isLoading" class="grid sm:grid-cols-2 lg:grid-cols-3 gap-3">
          <div v-for="n in 3" :key="n" class="card p-5 space-y-4 animate-pulse">
            <div class="flex items-center gap-3">
              <div class="w-8 h-8 rounded-lg" style="background: var(--surface-raised)" />
              <div class="space-y-2 flex-1">
                <div class="h-3.5 w-2/5 rounded" style="background: var(--surface-raised)" />
                <div class="h-3 w-1/3 rounded" style="background: var(--surface-raised)" />
              </div>
            </div>
          </div>
        </div>

        <div v-else-if="error" class="text-sm" style="color: var(--red)">{{ error }}</div>

        <!-- Empty state -->
        <div v-else-if="leagues.length === 0"
          class="card p-16 text-center"
          style="background: linear-gradient(135deg, var(--surface) 0%, rgba(44,41,64,0.4) 100%)"
        >
          <div class="w-14 h-14 rounded-xl mx-auto mb-5 flex items-center justify-center text-2xl"
            style="background: var(--accent-dim); border: 1px solid rgba(227,30,36,0.15)"
          >
            🏈
          </div>
          <p class="text-xl font-bold tracking-tight mb-2" style="letter-spacing: -0.02em">No leagues yet</p>
          <p class="text-sm mb-8 max-w-xs mx-auto" style="color: var(--text-muted)">
            Create your first guillotine league and send the blade swinging.
          </p>
          <RouterLink to="/leagues/create" class="btn btn-primary">
            Create a League
          </RouterLink>
        </div>

        <!-- League grid -->
        <div v-else class="grid sm:grid-cols-2 lg:grid-cols-3 gap-3">
        <RouterLink
          v-for="league in leagues"
          :key="league.id"
          :to="`/leagues/${league.id}`"
          class="card p-5 block transition-all duration-150"
          style="text-decoration: none"
          onmouseover="this.style.borderColor='var(--border-hover)'; this.style.background='var(--surface-raised)'"
          onmouseout="this.style.borderColor='var(--border)'; this.style.background='var(--surface)'"
        >
          <div class="flex items-start justify-between mb-4">
            <div
              class="w-9 h-9 rounded-lg flex items-center justify-center text-sm font-bold shrink-0"
              style="background: var(--accent-dim); color: var(--accent); border: 1px solid rgba(227,30,36,0.15)"
            >
              {{ league.name[0].toUpperCase() }}
            </div>

            <!-- Status pill -->
            <div class="flex items-center gap-1.5">
              <span
                class="w-1.5 h-1.5 rounded-full shrink-0"
                :style="`background: ${STATUS[league.status]?.dot ?? 'var(--text-muted)'}`"
              />
              <span class="text-xs font-medium" :style="`color: ${STATUS[league.status]?.dot ?? 'var(--text-muted)'}`">
                {{ STATUS[league.status]?.label ?? league.status }}
              </span>
            </div>
          </div>

          <p class="font-semibold text-sm mb-1 tracking-tight" style="color: var(--text); letter-spacing: -0.01em">
            {{ league.name }}
          </p>
          <p class="text-xs" style="color: var(--text-muted)">
            Season {{ league.season }} · {{ league.teams?.length ?? 0 }} teams
          </p>
        </RouterLink>
        </div>
      </template>
    </section>

  </div>
</template>
