import { api } from './client'
import type { League } from './types'

export interface ImportStatsResult {
  season: number
  rosterEntries: number
  gsisBackfilled: number
  playersCreated: number
  statRowsFetched: number
  inserted: number
  updated: number
  skipped: number
}

export interface SeedResult {
  message?: string
  leagueId?: string
  teams?: number
  managers?: number
}

export interface SeedDraftResult {
  leagueId?: string
  draftId?: string
  status?: string
  picksMade?: number
  totalPicks?: number
  message?: string
}

export const adminApi = {
  getLeagues: () => api.get<League[]>('/dev/leagues'),

  seed:              () => api.post<SeedResult>('/dev/seed', {}),
  clearSeed:         () => api.delete('/dev/seed'),
  seedDraft:         () => api.post<SeedDraftResult>('/dev/seed-draft', {}),
  clearSeedDraft:    () => api.delete('/dev/seed-draft'),
  seedScores:        () => api.post<{ leagueId: string; weekNumber: number; playersSeeded: number }>('/dev/seed-scores', {}),
  simulateScoreTick: () => api.post<{ playerId: string; playerName: string; position: string; points: number }>('/dev/simulate-score-tick', {}),

  importStats: (season: number) =>
    api.post<ImportStatsResult>(`/dev/import-player-stats?season=${season}`, {}),
}
