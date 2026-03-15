import type { TradeDetail } from './types'

export const tradesApi = {
  list(leagueId: string, teamId: string): Promise<TradeDetail[]> {
    return fetch(`/api/leagues/${leagueId}/trades?teamId=${teamId}`).then(r => r.json())
  },

  propose(leagueId: string, body: {
    initiatorTeamId: string
    receiverTeamId: string
    offeredPlayerIds: string[]
    requestedPlayerIds: string[]
    message?: string
  }): Promise<{ tradeId: string }> {
    return fetch(`/api/leagues/${leagueId}/trades`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    }).then(async r => {
      if (!r.ok) throw new Error(await r.text())
      return r.json()
    })
  },

  accept(leagueId: string, tradeId: string, teamId: string): Promise<void> {
    return fetch(`/api/leagues/${leagueId}/trades/${tradeId}/accept?teamId=${teamId}`, {
      method: 'POST',
    }).then(async r => {
      if (!r.ok) throw new Error(await r.text())
    })
  },

  reject(leagueId: string, tradeId: string, teamId: string): Promise<void> {
    return fetch(`/api/leagues/${leagueId}/trades/${tradeId}/reject?teamId=${teamId}`, {
      method: 'POST',
    }).then(async r => {
      if (!r.ok) throw new Error(await r.text())
    })
  },

  cancel(leagueId: string, tradeId: string, teamId: string): Promise<void> {
    return fetch(`/api/leagues/${leagueId}/trades/${tradeId}?teamId=${teamId}`, {
      method: 'DELETE',
    }).then(async r => {
      if (!r.ok) throw new Error(await r.text())
    })
  },
}
