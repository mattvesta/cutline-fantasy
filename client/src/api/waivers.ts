import type { WaiverState } from './types'

const BASE = '/api'

async function req<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    headers: { 'Content-Type': 'application/json', ...init?.headers },
    ...init,
  })
  if (!res.ok) {
    const body = await res.json().catch(() => ({}))
    throw new Error(body.error ?? `Request failed (${res.status})`)
  }
  return res.json() as Promise<T>
}

export const waiversApi = {
  getState: (leagueId: string, teamId: string, opts?: { position?: string; search?: string; page?: number }): Promise<WaiverState> => {
    const params = new URLSearchParams({ teamId })
    if (opts?.position && opts.position !== 'ALL') params.set('position', opts.position)
    if (opts?.search) params.set('search', opts.search)
    if (opts?.page && opts.page > 1) params.set('page', String(opts.page))
    return req<WaiverState>(`/leagues/${leagueId}/waivers?${params}`)
  },

  submitClaim: (leagueId: string, teamId: string, addPlayerId: string, dropPlayerId: string | null, faabBid: number | null): Promise<{ claimId: string; weekNumber: number; priority: number }> =>
    req(`/leagues/${leagueId}/waivers/claims`, {
      method: 'POST',
      body: JSON.stringify({ teamId, addPlayerId, dropPlayerId, faabBid }),
    }),

  cancelClaim: (leagueId: string, claimId: string, teamId: string): Promise<void> =>
    req(`/leagues/${leagueId}/waivers/claims/${claimId}?teamId=${teamId}`, { method: 'DELETE' }),

  dropPlayer: (leagueId: string, teamId: string, playerId: string): Promise<void> =>
    req(`/leagues/${leagueId}/waivers/drop`, {
      method: 'POST',
      body: JSON.stringify({ teamId, playerId }),
    }),
}
