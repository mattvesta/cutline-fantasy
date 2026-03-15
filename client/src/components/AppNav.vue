<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const route  = useRoute()
const router = useRouter()
const auth   = useAuthStore()

function logout() {
  auth.logout()
  router.push('/')
}
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

      <!-- Auth area -->
      <div class="flex items-center gap-3">
        <template v-if="auth.isLoggedIn">
          <span class="text-sm hidden sm:block" style="color: var(--text-muted)">
            {{ auth.manager?.displayName }}
          </span>
          <button class="btn btn-ghost text-xs py-1.5 px-3" @click="logout">Sign out</button>
        </template>
        <template v-else>
          <RouterLink to="/login" class="btn btn-ghost text-xs py-1.5 px-3">Sign in</RouterLink>
          <RouterLink to="/register" class="btn btn-primary text-xs py-1.5 px-4">Register</RouterLink>
        </template>
      </div>

    </div>
  </header>
</template>
