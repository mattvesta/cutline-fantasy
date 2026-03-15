import type { ChatMessage } from './types'
import { api } from './client'

export const chatApi = {
  getHistory(leagueId: string, opts?: { before?: string; limit?: number }): Promise<ChatMessage[]> {
    const params = new URLSearchParams()
    if (opts?.before) params.set('before', opts.before)
    if (opts?.limit)  params.set('limit', String(opts.limit))
    const qs = params.size ? `?${params}` : ''
    return fetch(`/api/leagues/${leagueId}/chat${qs}`).then(r => r.json())
  },

  send(leagueId: string, content: string, gifUrl?: string): Promise<ChatMessage> {
    return api.post(`/leagues/${leagueId}/chat`, { content: content || '', gifUrl: gifUrl ?? null })
  },
}
