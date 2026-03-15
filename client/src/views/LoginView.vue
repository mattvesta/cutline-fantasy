<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute, RouterLink } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const route  = useRoute()
const auth   = useAuthStore()

const email    = ref('')
const password = ref('')
const error    = ref<string | null>(null)
const loading  = ref(false)

async function submit() {
  error.value   = null
  loading.value = true
  try {
    await auth.login(email.value.trim(), password.value)
    const redirect = (route.query.redirect as string) || '/'
    router.push(redirect)
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Login failed.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center px-4" style="background: var(--bg)">
    <div class="w-full max-w-sm">
      <!-- Logo -->
      <div class="flex justify-center mb-8">
        <RouterLink to="/" class="flex items-center gap-2.5">
          <img src="/logo.png" alt="Cutline" class="h-9 w-9 rounded-lg object-cover" />
          <span class="font-bold text-lg" style="letter-spacing: -0.03em">Cutline</span>
        </RouterLink>
      </div>

      <div class="card p-8">
        <h1 class="text-xl font-bold mb-1" style="letter-spacing: -0.02em">Sign in</h1>
        <p class="text-sm mb-6" style="color: var(--text-muted)">Welcome back to your league.</p>

        <form class="space-y-4" @submit.prevent="submit">
          <div>
            <label class="label">Email</label>
            <input
              v-model="email"
              type="email"
              class="input w-full mt-1"
              placeholder="you@example.com"
              autocomplete="email"
              required
            />
          </div>
          <div>
            <label class="label">Password</label>
            <input
              v-model="password"
              type="password"
              class="input w-full mt-1"
              placeholder="••••••••"
              autocomplete="current-password"
              required
            />
          </div>

          <div v-if="error" class="text-sm text-red-400 rounded-lg px-3 py-2" style="background: rgba(239,68,68,0.1)">
            {{ error }}
          </div>

          <button type="submit" class="btn btn-primary w-full" :disabled="loading">
            {{ loading ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>
      </div>

      <p class="text-center text-sm mt-4" style="color: var(--text-muted)">
        No account?
        <RouterLink to="/register" class="hover:text-white transition-colors ml-1" style="color: var(--accent)">
          Create one
        </RouterLink>
      </p>
    </div>
  </div>
</template>
