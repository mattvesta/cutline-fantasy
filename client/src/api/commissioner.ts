import type { CommissionerState } from './types'
import { api } from './client'

export const commissionerApi = {
  getState: (leagueId: string): Promise<CommissionerState> =>
    api.get(`/leagues/${leagueId}/commissioner`),

  updateSettings: (leagueId: string, body: {
    name?: string
    receptionPoints?: number
    useFaab?: boolean
    faabBudget?: number
    minFaabBid?: number
  }): Promise<void> =>
    api.patch(`/leagues/${leagueId}/commissioner/settings`, body),

  setWeekStatus: (leagueId: string, weekNumber: number, status: string): Promise<void> =>
    api.patch(`/leagues/${leagueId}/commissioner/weeks/${weekNumber}/status`, { status }),

  overrideScore: (leagueId: string, weekNumber: number, teamId: string, points: number): Promise<void> =>
    api.put(`/leagues/${leagueId}/commissioner/weeks/${weekNumber}/scores/${teamId}`, { points }),

  forceEliminate: (leagueId: string, teamId: string, weekNumber: number): Promise<void> =>
    api.post(`/leagues/${leagueId}/commissioner/teams/${teamId}/eliminate`, { weekNumber }),

  restoreTeam: (leagueId: string, teamId: string): Promise<void> =>
    api.post(`/leagues/${leagueId}/commissioner/teams/${teamId}/restore`, {}),

  setTeamLock: (leagueId: string, teamId: string, isLocked: boolean): Promise<void> =>
    api.patch(`/leagues/${leagueId}/commissioner/teams/${teamId}/lock`, { isLocked }),
}
