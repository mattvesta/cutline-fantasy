<script setup lang="ts">
import { useRoute } from 'vue-router'
const route = useRoute()
</script>

<template>
  <header
    class="fixed top-0 inset-x-0 z-50 h-14"
    style="
      background: rgba(19,17,28,0.75);
      backdrop-filter: blur(20px) saturate(180%);
      -webkit-backdrop-filter: blur(20px) saturate(180%);
      border-bottom: 1px solid rgba(255,255,255,0.06);
    "
  >
    <div class="max-w-[72rem] mx-auto px-5 h-full flex items-center gap-8">

      <!-- Brand -->
      <RouterLink
        to="/"
        class="flex items-center gap-2.5 shrink-0 mr-2"
        style="text-decoration: none"
      >
        <img
          src="/logo.png"
          alt="Cutline"
          class="h-7 w-7 rounded-md object-cover"
        />
        <span class="font-bold text-sm tracking-tight" style="color: var(--text); letter-spacing: -0.02em">
          Cutline
        </span>
      </RouterLink>

      <!-- Nav links -->
      <nav class="flex items-center gap-1 flex-1">
        <RouterLink
          to="/"
          class="text-sm px-3 py-1.5 rounded-md transition-colors"
          :style="route.path === '/'
            ? 'color: var(--text); background: rgba(255,255,255,0.07)'
            : 'color: var(--text-muted)'"
          onmouseover="if(!this.style.background) this.style.color='var(--text)'"
          onmouseout="if(!this.style.background) this.style.color='var(--text-muted)'"
        >
          Leagues
        </RouterLink>
        <RouterLink
          to="/players"
          class="text-sm px-3 py-1.5 rounded-md transition-colors"
          :style="route.path.startsWith('/players')
            ? 'color: var(--text); background: rgba(255,255,255,0.07)'
            : 'color: var(--text-muted)'"
        >
          Players
        </RouterLink>
      </nav>

      <!-- CTA -->
      <RouterLink to="/leagues/create" class="btn btn-primary text-xs py-1.5 px-4">
        New League
      </RouterLink>

    </div>
  </header>
</template>
