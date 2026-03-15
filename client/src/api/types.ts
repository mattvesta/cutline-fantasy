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
  rosterSlots: RosterSlot[]
}

export interface TeamScore {
  teamId: string
  weekNumber: number
  points: number
  isLocked: boolean
}

export interface RosterSlot {
  id: string
  teamId: string
  playerId: string | null
  player: Player | null
  slotType: 'QB' | 'RB' | 'WR' | 'TE' | 'Flex' | 'SuperFlex' | 'K' | 'DEF' | 'Bench' | 'IR'
  isStarter: boolean
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
  age: number | null
  college: string | null
  height: string | null
  weight: string | null
  jerseyNumber: number | null
  yearsExperience: number | null
  depthChartOrder: number | null
  adp: number | null
}

export interface PlayerPage {
  items: Player[]
  totalCount: number
  page: number
  pageSize: number
}
