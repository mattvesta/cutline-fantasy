export interface League {
  id: string
  name: string
  season: number
  status: 'Setup' | 'Drafting' | 'Active' | 'Completed'
  teams: Team[]
}

export interface Team {
  id: string
  leagueId: string
  name: string
  ownerUserId: string
  isEliminated: boolean
  eliminatedWeek: number | null
}

export interface TeamScore {
  teamId: string
  weekNumber: number
  points: number
  isLocked: boolean
}

export interface Player {
  id: string
  gsisId: string
  sleeperId: string | null
  espnId: string | null
  firstName: string
  lastName: string
  position: string
  nflTeam: string | null
  status: 'Active' | 'Injured' | 'InjuredReserve' | 'Inactive' | 'Unknown'
  byeWeek: number | null
}
