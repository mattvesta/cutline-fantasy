import { api } from './client'
import type { League } from './types'

export const leaguesApi = {
  getAll: () => api.get<League[]>('/leagues'),
  getById: (leagueId: string) => api.get<League>(`/leagues/${leagueId}`),
  create: (payload: { name: string; season: number }) => api.post<League>('/leagues', payload),
}
