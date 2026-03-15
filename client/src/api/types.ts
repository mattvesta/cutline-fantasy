export interface Manager {
  id: string
  displayName: string
  email: string
  avatarUrl: string | null
  createdAt: string
  teams: Team[]
  leagueManagers: LeagueManager[]
}

export interface LeagueManager {
  leagueId: string
  managerId: string
  manager: Manager
  league: { id: string; name: string; season: number; status: string } | null
  isCommissioner: boolean
  joinedAt: string
}

export interface League {
  id: string
  name: string
  season: number
  status: 'Setup' | 'Drafting' | 'Active' | 'Completed'
  teams: Team[]
  leagueManagers: LeagueManager[]
}

export interface Team {
  id: string
  leagueId: string
  name: string
  ownerUserId: string
  managerId: string | null
  manager: Manager | null
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

export interface DraftPick {
  id: string
  draftId: string
  pickNumber: number
  round: number
  roundPick: number
  teamId: string
  team: { id: string; name: string }
  playerId: string | null
  player: Player | null
  pickedAt: string | null
  isAutoPick: boolean
}

export interface PlayerStats {
  points: number
  gameStatus: 'Scheduled' | 'InProgress' | 'Final' | 'Bye'
  opponent: string | null
  lastUpdated: string
  // Passing
  passingYards: number | null
  passingTDs: number | null
  interceptions: number | null
  passingAttempts: number | null
  passingCompletions: number | null
  // Rushing
  rushingYards: number | null
  rushingTDs: number | null
  rushingAttempts: number | null
  fumbles: number | null
  // Receiving
  receptions: number | null
  targets: number | null
  receivingYards: number | null
  receivingTDs: number | null
  // Kicker
  fieldGoalsMade: number | null
  fieldGoalsAttempted: number | null
  longFieldGoal: number | null
  extraPointsMade: number | null
  // Defense
  sacks: number | null
  defensiveInts: number | null
  fumblesRecovered: number | null
  defensiveTDs: number | null
  pointsAllowed: number | null
  safeties: number | null
}

export interface WaiverPlayer {
  id: string
  firstName: string
  lastName: string
  position: string
  nflTeam: string | null
  status: string
  adp: number | null
  byeWeek: number | null
}

export interface WaiverClaimPlayer {
  id: string
  firstName: string
  lastName: string
  position: string
  nflTeam: string | null
}

export interface WaiverClaim {
  id: string
  priority: number
  faabBid: number | null
  status: 'Pending' | 'Processed' | 'Rejected'
  rejectionReason?: string | null
  weekNumber?: number
  addPlayer: WaiverClaimPlayer
  dropPlayer: WaiverClaimPlayer | null
}

export interface WaiverRosterSlot {
  slotId: string
  slotType: string
  isStarter: boolean
  player: WaiverClaimPlayer | null
}

export interface WaiverState {
  weekNumber: number | null
  weekStatus: string | null
  claimsOpen: boolean
  useFaab: boolean
  faabBudget: number
  faabRemaining: number
  availablePlayers: {
    items: WaiverPlayer[]
    totalCount: number
    page: number
    pageSize: number
  }
  myClaims: WaiverClaim[] | null
  recentResults: WaiverClaim[] | null
  teamRoster: WaiverRosterSlot[] | null
}

export interface MatchupTeamRow {
  teamId: string
  teamName: string
  managerName: string | null
  points: number | null
  rank: number | null
  isEliminated: boolean
  isOnCutLine: boolean
  eliminatedWeek?: number | null
}

export interface WeeklyMatchupResponse {
  leagueId: string
  season: number
  weekNumber: number
  weekStatus: string
  teams: MatchupTeamRow[]
}

export interface LiveRosterSlot {
  slotId: string
  slotType: string
  isStarter: boolean
  player: { id: string; name: string; position: string; team: string | null } | null
  stats: PlayerStats | null
}

export interface LiveTeamResponse {
  teamId: string
  teamName: string
  season: number
  weekNumber: number
  totalPoints: number
  roster: LiveRosterSlot[]
}

export interface Draft {
  id: string
  leagueId: string
  status: 'Pending' | 'InProgress' | 'Paused' | 'Completed'
  pickTimeLimitSeconds: number
  currentPickNumber: number
  startedAt: string | null
  completedAt: string | null
  currentPickStartedAt: string | null
  picks: DraftPick[]
}
