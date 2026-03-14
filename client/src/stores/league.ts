import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { League } from '../api/types'

export const useLeagueStore = defineStore('league', () => {
  const current = ref<League | null>(null)
  const isLoading = ref(false)

  async function fetchLeague(leagueId: string) {
    // TODO
  }

  return { current, isLoading, fetchLeague }
})
