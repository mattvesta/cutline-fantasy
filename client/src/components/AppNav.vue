<script setup lang="ts">
import { useRoute } from 'vue-router'
const route = useRoute()
</script>

<template>
  <header
    class="fixed top-0 inset-x-0 z-50 h-14 border-b"
    style="background: rgba(12,12,18,0.92); backdrop-filter: blur(12px); border-color: var(--border)"
  >
    <div class="max-w-[72rem] mx-auto px-5 h-full flex items-center justify-between">

      <!-- Brand -->
      <RouterLink to="/" class="flex items-center gap-2.5 hover:opacity-90 transition-opacity">
        <img
          src="/logo.png"
          alt="The Cutline Fantasy"
          class="h-9 w-9 rounded-md object-cover"
          style="mix-blend-mode: multiply; background: transparent"
        />
        <div class="leading-none">
          <span class="block text-xs font-semibold tracking-[0.15em] uppercase" style="color: var(--text-muted)">The</span>
          <span class="block text-sm font-black tracking-[0.12em] uppercase" style="color: var(--text)">Cutline</span>
        </div>
      </RouterLink>

      <!-- Nav links -->
      <nav class="flex items-center gap-6 text-sm font-medium">
        <RouterLink
          to="/"
          class="transition-colors"
          :style="route.path === '/' ? 'color: var(--text)' : 'color: var(--text-muted)'"
        >
          Leagues
        </RouterLink>
        <RouterLink
          to="/players"
          class="transition-colors"
          :style="route.path.startsWith('/players') ? 'color: var(--text)' : 'color: var(--text-muted)'"
        >
          Players
        </RouterLink>
      </nav>

      <!-- CTA -->
      <RouterLink to="/leagues/create" class="btn btn-primary text-xs py-2 px-4">
        + Create League
      </RouterLink>

    </div>
  </header>
</template>
