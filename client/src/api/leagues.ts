import { api } from './client'
import type { League } from './types'

export const leaguesApi = {
  getAll: () => api.get<League[]>('/leagues'),
  getById: (leagueId: string) => api.get<League>(`/leagues/${leagueId}`),
  create: (payload: {
    name: string
    season: number
    receptionPoints: number
    qbSlots: number; rbSlots: number; wrSlots: number; teSlots: number
    flexSlots: number; superFlexSlots: number; kSlots: number; defSlots: number
    benchSlots: number; irSlots: number
    useFaab: boolean
    faabBudget: number
    minFaabBid: number
  }) => api.post<League>('/leagues', payload),
}
