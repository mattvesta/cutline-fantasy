import type { Draft, Player } from './types'

const BASE = '/api'

export const draftsApi = {
  getByLeague: (leagueId: string): Promise<Draft> =>
    fetch(`${BASE}/leagues/${leagueId}/draft`).then(r => {
      if (!r.ok) throw new Error('Draft not found')
      return r.json()
    }),

  create: (leagueId: string): Promise<Draft> =>
    fetch(`${BASE}/leagues/${leagueId}/draft`, { method: 'POST' }).then(r => r.json()),

  start: (leagueId: string): Promise<Draft> =>
    fetch(`${BASE}/leagues/${leagueId}/draft/start`, { method: 'POST' }).then(r => r.json()),

  pick: (leagueId: string, teamId: string, playerId: string): Promise<{ pick: Draft['picks'][0]; draft: Draft }> =>
    fetch(`${BASE}/leagues/${leagueId}/draft/pick`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ teamId, playerId }),
    }).then(r => {
      if (!r.ok) return r.json().then(e => Promise.reject(e.message ?? 'Pick failed'))
      return r.json()
    }),

  autoPick: (leagueId: string): Promise<{ pick: Draft['picks'][0]; draft: Draft }> =>
    fetch(`${BASE}/leagues/${leagueId}/draft/autopick`, { method: 'POST' }).then(r => {
      if (!r.ok) return r.json().then(e => Promise.reject(e.message ?? 'Auto-pick failed'))
      return r.json()
    }),

  getAvailable: (leagueId: string): Promise<Player[]> =>
    fetch(`${BASE}/leagues/${leagueId}/draft/available`).then(r => r.json()),
}
