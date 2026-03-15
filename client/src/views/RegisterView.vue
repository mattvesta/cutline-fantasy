<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth   = useAuthStore()

const displayName = ref('')
const email       = ref('')
const password    = ref('')
const confirm     = ref('')
const error       = ref<string | null>(null)
const loading     = ref(false)

async function submit() {
  error.value = null
  if (password.value !== confirm.value) {
    error.value = 'Passwords do not match.'
    return
  }
  if (password.value.length < 8) {
    error.value = 'Password must be at least 8 characters.'
    return
  }
  loading.value = true
  try {
    await auth.register(displayName.value.trim(), email.value.trim(), password.value)
    router.push('/')
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Registration failed.'
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
        <h1 class="text-xl font-bold mb-1" style="letter-spacing: -0.02em">Create account</h1>
        <p class="text-sm mb-6" style="color: var(--text-muted)">Join your league today.</p>

        <form class="space-y-4" @submit.prevent="submit">
          <div>
            <label class="label">Display name</label>
            <input
              v-model="displayName"
              type="text"
              class="input w-full mt-1"
              placeholder="Your name"
              autocomplete="name"
              required
            />
          </div>
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
              placeholder="Min. 8 characters"
              autocomplete="new-password"
              required
            />
          </div>
          <div>
            <label class="label">Confirm password</label>
            <input
              v-model="confirm"
              type="password"
              class="input w-full mt-1"
              placeholder="••••••••"
              autocomplete="new-password"
              required
            />
          </div>

          <div v-if="error" class="text-sm text-red-400 rounded-lg px-3 py-2" style="background: rgba(239,68,68,0.1)">
            {{ error }}
          </div>

          <button type="submit" class="btn btn-primary w-full" :disabled="loading">
            {{ loading ? 'Creating account…' : 'Create account' }}
          </button>
        </form>
      </div>

      <p class="text-center text-sm mt-4" style="color: var(--text-muted)">
        Already have an account?
        <RouterLink to="/login" class="hover:text-white transition-colors ml-1" style="color: var(--accent)">
          Sign in
        </RouterLink>
      </p>
    </div>
  </div>
</template>
