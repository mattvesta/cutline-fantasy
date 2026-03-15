import { api } from './client'
import type { Team } from './types'

export const teamsApi = {
  getByLeague: (leagueId: string) =>
    api.get<Team[]>(`/leagues/${leagueId}/teams`),
  getById: (leagueId: string, teamId: string) =>
    api.get<Team>(`/leagues/${leagueId}/teams/${teamId}`),
  swapSlots: (leagueId: string, teamId: string, slotAId: string, slotBId: string) =>
    api.post<Team>(`/leagues/${leagueId}/teams/${teamId}/lineup/swap`, { slotAId, slotBId }),
}
