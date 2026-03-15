import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi, type AuthManager } from '../api/auth'
import { setAuthToken } from '../api/client'

const TOKEN_KEY = 'auth_token'

function decodeJwt(token: string): Record<string, unknown> {
  try {
    const payload = token.split('.')[1]
    return JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')))
  } catch {
    return {}
  }
}

export const useAuthStore = defineStore('auth', () => {
  const token   = ref<string | null>(localStorage.getItem(TOKEN_KEY))
  const manager = ref<AuthManager | null>(null)

  const managerId  = computed(() => manager.value?.id ?? null)
  const isLoggedIn = computed(() => !!token.value && !!manager.value)
  // Prefer the JWT claim (available instantly from localStorage); fall back to
  // manager.isAdmin for tokens issued before the claim was added.
  const isAdmin = computed(() => {
    if (!token.value) return false
    const payload = decodeJwt(token.value)
    if (payload.is_admin !== undefined) return payload.is_admin === 'true'
    return manager.value?.isAdmin === true
  })

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

  let _initPromise: Promise<void> | null = null

  function init(): Promise<void> {
    if (_initPromise) return _initPromise
    _initPromise = (async () => {
      if (!token.value) return
      setAuthToken(token.value)
      try {
        manager.value = await authApi.me(token.value)
      } catch {
        logout()
      }
    })()
    return _initPromise
  }

  return { token, manager, managerId, isLoggedIn, isAdmin, login, register, logout, init }
})
