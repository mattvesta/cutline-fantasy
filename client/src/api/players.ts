import { api } from './client'
import type { Player, PlayerPage, PlayerSeasonStats } from './types'

export interface PlayerSearchParams {
  position?: string
  search?: string
  sortBy?: string
  sortDesc?: boolean
  page?: number
  pageSize?: number
}

export const playersApi = {
  search: (params: PlayerSearchParams = {}) => {
    const qs = new URLSearchParams()
    if (params.position) qs.set('position', params.position)
    if (params.search) qs.set('search', params.search)
    if (params.sortBy) qs.set('sortBy', params.sortBy)
    if (params.sortDesc) qs.set('sortDesc', 'true')
    if (params.page) qs.set('page', String(params.page))
    if (params.pageSize) qs.set('pageSize', String(params.pageSize))
    const query = qs.toString()
    return api.get<PlayerPage>(`/players${query ? `?${query}` : ''}`)
  },

  getById: (id: string) => api.get<Player>(`/players/${id}`),

  getSeasonStats: (id: string) => api.get<PlayerSeasonStats[]>(`/players/${id}/stats`),
}
