<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { tradesApi } from '../api/trades'
import { teamsApi } from '../api/teams'
import type { TradeDetail, Team, RosterSlot } from '../api/types'
import TradeCard from '../components/TradeCard.vue'

const route    = useRoute()
const leagueId = route.params.leagueId as string
const teamId   = route.params.teamId as string

// ── State ──────────────────────────────────────────────────────────────────
const trades    = ref<TradeDetail[]>([])
const myTeam    = ref<Team | null>(null)
const allTeams  = ref<Team[]>([])
const isLoading = ref(true)
const error     = ref<string | null>(null)
const actionErr = ref<string | null>(null)

// ── Tabs ───────────────────────────────────────────────────────────────────
type Tab = 'inbox' | 'sent' | 'history'
const tab = ref<Tab>('inbox')

const inbox   = computed(() => trades.value.filter(t => t.receiverTeam.id === teamId && t.status === 'Pending'))
const sent    = computed(() => trades.value.filter(t => t.initiatorTeam.id === teamId && t.status === 'Pending'))
const history = computed(() => trades.value.filter(t => t.status !== 'Pending'))

// ── Propose trade form ─────────────────────────────────────────────────────
const showPropose      = ref(false)
const targetTeam       = ref<Team | null>(null)
const targetTeamRoster = ref<RosterSlot[]>([])
const offeredIds       = ref<Set<string>>(new Set())
const requestedIds     = ref<Set<string>>(new Set())
const tradeMessage     = ref('')
const isSubmitting     = ref(false)
const proposeErr       = ref<string | null>(null)

const otherActiveTeams = computed(() =>
  allTeams.value.filter(t => t.id !== teamId && !t.isEliminated)
)

async function selectTargetTeam(t: Team) {
  targetTeam.value   = t
  requestedIds.value = new Set()
  try {
    const loaded = await teamsApi.getById(leagueId, t.id)
    targetTeamRoster.value = loaded.rosterSlots ?? []
  } catch {
    targetTeamRoster.value = []
  }
}

function toggleOffered(playerId: string) {
  const s = new Set(offeredIds.value)
  if (s.has(playerId)) s.delete(playerId); else s.add(playerId)
  offeredIds.value = s
}

function toggleRequested(playerId: string) {
  const s = new Set(requestedIds.value)
  if (s.has(playerId)) s.delete(playerId); else s.add(playerId)
  requestedIds.value = s
}

function resetPropose() {
  showPropose.value      = false
  targetTeam.value       = null
  targetTeamRoster.value = []
  offeredIds.value       = new Set()
  requestedIds.value     = new Set()
  tradeMessage.value     = ''
  proposeErr.value       = null
}

async function submitTrade() {
  proposeErr.value = null
  if (!targetTeam.value) { proposeErr.value = 'Select a team.'; return }
  if (offeredIds.value.size === 0 && requestedIds.value.size === 0) {
    proposeErr.value = 'Include at least one player.'
    return
  }
  isSubmitting.value = true
  try {
    await tradesApi.propose(leagueId, {
      initiatorTeamId:   teamId,
      receiverTeamId:    targetTeam.value.id,
      offeredPlayerIds:  [...offeredIds.value],
      requestedPlayerIds: [...requestedIds.value],
      message: tradeMessage.value.trim() || undefined,
    })
    resetPropose()
    await reload()
    tab.value = 'sent'
  } catch (e) {
    proposeErr.value = e instanceof Error ? e.message : 'Failed to propose trade.'
  } finally {
    isSubmitting.value = false
  }
}

// ── Actions ─────────────────────────────────────────────────────────────────
async function accept(tradeId: string) {
  actionErr.value = null
  try {
    await tradesApi.accept(leagueId, tradeId, teamId)
    await reload()
  } catch (e) {
    actionErr.value = e instanceof Error ? e.message : 'Failed to accept trade.'
  }
}

async function reject(tradeId: string) {
  actionErr.value = null
  try {
    await tradesApi.reject(leagueId, tradeId, teamId)
    await reload()
  } catch (e) {
    actionErr.value = e instanceof Error ? e.message : 'Failed to reject trade.'
  }
}

async function cancel(tradeId: string) {
  actionErr.value = null
  try {
    await tradesApi.cancel(leagueId, tradeId, teamId)
    await reload()
  } catch (e) {
    actionErr.value = e instanceof Error ? e.message : 'Failed to cancel trade.'
  }
}

// ── Data loading ────────────────────────────────────────────────────────────
async function reload() {
  trades.value = await tradesApi.list(leagueId, teamId)
}

onMounted(async () => {
  try {
    const [t, all] = await Promise.all([
      teamsApi.getById(leagueId, teamId),
      teamsApi.getByLeague(leagueId),
    ])
    myTeam.value   = t
    allTeams.value = all
    await reload()
  } catch {
    error.value = 'Failed to load trades.'
  } finally {
    isLoading.value = false
  }
})

// ── Helpers ─────────────────────────────────────────────────────────────────
function playerName(p: { firstName: string; lastName: string } | null) {
  return p ? `${p.firstName} ${p.lastName}` : 'Unknown'
}

const POS_COLOR: Record<string, string> = {
  QB: '#f97316', RB: '#22c55e', WR: '#3b82f6', TE: '#a855f7',
  K: '#eab308', DEF: '#64748b',
}
function posColor(pos: string) { return POS_COLOR[pos] ?? '#6b6784' }

// Slots with an actual player on them, sorted starter-first
function rosterPlayers(slots: RosterSlot[]) {
  return slots.filter(s => s.player).sort((a, b) => {
    if (a.isStarter && !b.isStarter) return -1
    if (!a.isStarter && b.isStarter) return 1
    return 0
  })
}
</script>

<template>
  <div class="page max-w-3xl">
    <RouterLink
      :to="`/leagues/${leagueId}/teams/${teamId}`"
      class="text-sm text-[var(--text-muted)] hover:text-white transition-colors mb-8 inline-flex items-center gap-1.5"
    >
      ← {{ myTeam?.name ?? 'Team' }}
    </RouterLink>

    <div class="flex items-end justify-between mb-8">
      <div>
        <h1 class="text-3xl font-bold tracking-tight mb-1" style="letter-spacing: -0.03em">Trades</h1>
        <p class="text-sm text-[var(--text-muted)]">Propose and manage trades with other managers.</p>
      </div>
      <button
        v-if="!showPropose"
        class="btn btn-primary text-sm"
        @click="showPropose = true; tab = 'inbox'"
      >
        + Propose Trade
      </button>
    </div>

    <div v-if="isLoading" class="space-y-3">
      <div v-for="i in 3" :key="i" class="h-24 rounded-xl bg-[var(--surface)] animate-pulse" />
    </div>

    <div v-else-if="error" class="text-[var(--red)] text-sm">{{ error }}</div>

    <template v-else>
      <!-- ── Propose trade form ───────────────────────────────────────── -->
      <div
        v-if="showPropose"
        class="rounded-xl mb-8 overflow-hidden"
        style="background: var(--surface); border: 1px solid var(--border)"
      >
        <div class="px-5 py-4" style="border-bottom: 1px solid var(--border-subtle)">
          <div class="flex items-center justify-between">
            <h2 class="font-semibold">Propose a Trade</h2>
            <button class="text-[var(--text-muted)] hover:text-white text-lg leading-none" @click="resetPropose">✕</button>
          </div>
        </div>

        <!-- Step 1: pick a team -->
        <div class="px-5 py-4" style="border-bottom: 1px solid var(--border-subtle)">
          <p class="label mb-3">Trade with</p>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="t in otherActiveTeams"
              :key="t.id"
              class="text-sm px-3 py-1.5 rounded-lg transition-colors"
              :style="targetTeam?.id === t.id
                ? 'background: var(--accent); color: white'
                : 'background: var(--surface-raised); color: var(--text-secondary)'"
              @click="selectTargetTeam(t)"
            >
              {{ t.name }}
            </button>
          </div>
        </div>

        <!-- Step 2: select players -->
        <div v-if="targetTeam" class="grid grid-cols-2 divide-x" style="border-bottom: 1px solid var(--border-subtle); --tw-divide-opacity: 1; border-color: var(--border-subtle)">
          <!-- Your offer -->
          <div class="px-5 py-4">
            <p class="label mb-3">
              You offer
              <span v-if="offeredIds.size > 0" class="ml-1 text-[var(--accent)]">{{ offeredIds.size }} selected</span>
            </p>
            <div class="space-y-1.5">
              <button
                v-for="slot in rosterPlayers(myTeam?.rosterSlots ?? [])"
                :key="slot.id"
                class="w-full flex items-center gap-2 text-xs rounded-lg px-2.5 py-2 transition-colors text-left"
                :style="offeredIds.has(slot.player!.id)
                  ? 'background: var(--accent-dim); border: 1px solid var(--accent)'
                  : 'background: var(--surface-raised); border: 1px solid transparent'"
                @click="toggleOffered(slot.player!.id)"
              >
                <span class="font-bold text-[10px] w-7 shrink-0 text-center" :style="{ color: posColor(slot.player!.position) }">
                  {{ slot.player!.position }}
                </span>
                <span class="flex-1 truncate font-medium">{{ playerName(slot.player) }}</span>
                <span class="shrink-0 text-[var(--text-muted)]">{{ slot.isStarter ? 'STR' : 'BN' }}</span>
              </button>
            </div>
          </div>

          <!-- You want -->
          <div class="px-5 py-4">
            <p class="label mb-3">
              You want
              <span v-if="requestedIds.size > 0" class="ml-1 text-[var(--accent)]">{{ requestedIds.size }} selected</span>
            </p>
            <div class="space-y-1.5">
              <button
                v-for="slot in rosterPlayers(targetTeamRoster)"
                :key="slot.id"
                class="w-full flex items-center gap-2 text-xs rounded-lg px-2.5 py-2 transition-colors text-left"
                :style="requestedIds.has(slot.player!.id)
                  ? 'background: var(--accent-dim); border: 1px solid var(--accent)'
                  : 'background: var(--surface-raised); border: 1px solid transparent'"
                @click="toggleRequested(slot.player!.id)"
              >
                <span class="font-bold text-[10px] w-7 shrink-0 text-center" :style="{ color: posColor(slot.player!.position) }">
                  {{ slot.player!.position }}
                </span>
                <span class="flex-1 truncate font-medium">{{ playerName(slot.player) }}</span>
                <span class="shrink-0 text-[var(--text-muted)]">{{ slot.isStarter ? 'STR' : 'BN' }}</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Message + submit -->
        <div v-if="targetTeam" class="px-5 py-4 flex flex-col gap-3">
          <input
            v-model="tradeMessage"
            class="input text-sm"
            placeholder="Optional message to the other manager…"
            maxlength="500"
          />
          <div v-if="proposeErr" class="text-[var(--red)] text-xs">{{ proposeErr }}</div>
          <div class="flex gap-3 justify-end">
            <button class="btn btn-ghost text-sm" @click="resetPropose">Cancel</button>
            <button
              class="btn btn-primary text-sm"
              :disabled="isSubmitting"
              @click="submitTrade"
            >
              {{ isSubmitting ? 'Sending…' : 'Send Trade Offer' }}
            </button>
          </div>
        </div>

        <div v-else class="px-5 py-4 text-sm text-[var(--text-muted)]">
          Select a team above to see their roster.
        </div>
      </div>

      <!-- ── Action error ─────────────────────────────────────────────── -->
      <div v-if="actionErr" class="text-[var(--red)] text-sm mb-4 p-3 rounded-lg" style="background: rgba(220,38,38,0.08)">
        {{ actionErr }}
      </div>

      <!-- ── Tabs ────────────────────────────────────────────────────── -->
      <div class="flex gap-1 mb-6 p-1 rounded-lg w-fit" style="background: var(--surface)">
        <button
          v-for="(label, key) in { inbox: 'Inbox', sent: 'Sent', history: 'History' }"
          :key="key"
          class="text-sm px-4 py-1.5 rounded-md transition-colors font-medium"
          :style="tab === key
            ? 'background: var(--surface-raised); color: var(--text)'
            : 'color: var(--text-muted)'"
          @click="tab = (key as Tab)"
        >
          {{ label }}
          <span
            v-if="key === 'inbox' && inbox.length > 0"
            class="ml-1.5 text-[10px] font-bold px-1.5 py-0.5 rounded-full"
            style="background: var(--accent); color: white"
          >{{ inbox.length }}</span>
        </button>
      </div>

      <!-- ── Inbox ───────────────────────────────────────────────────── -->
      <div v-if="tab === 'inbox'">
        <div v-if="inbox.length === 0" class="text-center py-12 text-[var(--text-muted)] text-sm">
          No incoming trade offers.
        </div>
        <div v-else class="space-y-4">
          <TradeCard
            v-for="trade in inbox"
            :key="trade.id"
            :trade="trade"
            :my-team-id="teamId"
          >
            <template #actions>
              <button class="btn text-sm px-4 py-1.5 rounded-lg font-medium" style="background: var(--accent); color: white" @click="accept(trade.id)">Accept</button>
              <button class="btn btn-ghost text-sm" @click="reject(trade.id)">Decline</button>
            </template>
          </TradeCard>
        </div>
      </div>

      <!-- ── Sent ────────────────────────────────────────────────────── -->
      <div v-if="tab === 'sent'">
        <div v-if="sent.length === 0" class="text-center py-12 text-[var(--text-muted)] text-sm">
          No outgoing trade offers.
        </div>
        <div v-else class="space-y-4">
          <TradeCard
            v-for="trade in sent"
            :key="trade.id"
            :trade="trade"
            :my-team-id="teamId"
          >
            <template #actions>
              <button class="btn btn-ghost text-sm" @click="cancel(trade.id)">Cancel</button>
            </template>
          </TradeCard>
        </div>
      </div>

      <!-- ── History ─────────────────────────────────────────────────── -->
      <div v-if="tab === 'history'">
        <div v-if="history.length === 0" class="text-center py-12 text-[var(--text-muted)] text-sm">
          No completed trades yet.
        </div>
        <div v-else class="space-y-4">
          <TradeCard
            v-for="trade in history"
            :key="trade.id"
            :trade="trade"
            :my-team-id="teamId"
          />
        </div>
      </div>
    </template>
  </div>
</template>
