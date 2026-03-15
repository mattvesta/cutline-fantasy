import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi, type AuthManager } from '../api/auth'
import { setAuthToken } from '../api/client'

const TOKEN_KEY = 'auth_token'

export const useAuthStore = defineStore('auth', () => {
  const token   = ref<string | null>(localStorage.getItem(TOKEN_KEY))
  const manager = ref<AuthManager | null>(null)

  const managerId  = computed(() => manager.value?.id ?? null)
  const isLoggedIn = computed(() => !!token.value && !!manager.value)

  function setToken(t: string | null) {
    token.value = t
    if (t) localStorage.setItem(TOKEN_KEY, t)
    else   localStorage.removeItem(TOKEN_KEY)
    setAuthToken(t)
  }

  async function login(email: string, password: string) {
    const res = await authApi.login(email, password)
    setToken(res.token)
    manager.value = res.manager
  }

  async function register(displayName: string, email: string, password: string) {
    const res = await authApi.register(displayName, email, password)
    setToken(res.token)
    manager.value = res.manager
  }

  function logout() {
    setToken(null)
    manager.value = null
  }

  async function init() {
    if (!token.value) return
    setAuthToken(token.value)
    try {
      manager.value = await authApi.me(token.value)
    } catch {
      logout()
    }
  }

  return { token, manager, managerId, isLoggedIn, login, register, logout, init }
})
