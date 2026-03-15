import { api } from './client'
import type { Manager, LeagueManager, Team } from './types'

export const managersApi = {
  create: (data: { displayName: string; email: string; avatarUrl?: string }) =>
    api.post<Manager>('/managers', data),

  getById: (managerId: string) =>
    api.get<Manager>(`/managers/${managerId}`),

  update: (managerId: string, data: { displayName?: string; avatarUrl?: string }) =>
    api.put<Manager>(`/managers/${managerId}`, data),

  // League-scoped
  getByLeague: (leagueId: string) =>
    api.get<LeagueManager[]>(`/leagues/${leagueId}/managers`),

  joinLeague: (leagueId: string, managerId: string) =>
    api.post<LeagueManager>(`/leagues/${leagueId}/managers`, { managerId }),

  removeFromLeague: (leagueId: string, managerId: string) =>
    api.delete(`/leagues/${leagueId}/managers/${managerId}`),

  assignTeam: (leagueId: string, managerId: string, teamId: string) =>
    api.put<Team>(`/leagues/${leagueId}/managers/${managerId}/team`, { teamId }),

  setCommissioner: (leagueId: string, managerId: string) =>
    api.put<LeagueManager>(`/leagues/${leagueId}/managers/${managerId}/commissioner`, {}),
}
