import { computed } from 'vue'
import { useLeagueStore } from '../stores/league'

export function useLeague(leagueId: string) {
  const store = useLeagueStore()

  const league = computed(() => store.current)
  const isLoading = computed(() => store.isLoading)

  async function load() {
    await store.fetchLeague(leagueId)
  }

  return { league, isLoading, load }
}
