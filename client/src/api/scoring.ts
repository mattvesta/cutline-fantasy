import type { LiveTeamResponse, WeeklyMatchupResponse } from './types'

const BASE = '/api'

export const scoringApi = {
  getLiveTeam: (leagueId: string, teamId: string): Promise<LiveTeamResponse> =>
    fetch(`${BASE}/leagues/${leagueId}/scoring/teams/${teamId}`).then(r => {
      if (!r.ok) throw new Error('Failed to load live scoring')
      return r.json()
    }),

  getLiveLeague: (leagueId: string): Promise<{ leagueId: string; season: number; weekNumber: number; teams: { teamId: string; teamName: string; points: number }[] }> =>
    fetch(`${BASE}/leagues/${leagueId}/scoring/live`).then(r => {
      if (!r.ok) throw new Error('Failed to load league scores')
      return r.json()
    }),

  getMatchup: (leagueId: string): Promise<WeeklyMatchupResponse> =>
    fetch(`${BASE}/leagues/${leagueId}/scoring/matchup`).then(r => {
      if (!r.ok) throw new Error('Failed to load matchup')
      return r.json()
    }),
}
