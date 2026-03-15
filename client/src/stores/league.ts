import { defineStore } from 'pinia'
import { ref } from 'vue'
import { leaguesApi } from '../api/leagues'
import type { League } from '../api/types'

export const useLeagueStore = defineStore('league', () => {
  const current = ref<League | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  async function fetchLeague(leagueId: string) {
    if (current.value?.id === leagueId) return
    isLoading.value = true
    error.value = null
    try {
      current.value = await leaguesApi.getById(leagueId)
    } catch {
      error.value = 'Failed to load league'
    } finally {
      isLoading.value = false
    }
  }

  return { current, isLoading, error, fetchLeague }
})
