<script setup lang="ts">
import type { TradeDetail } from '../api/types'

const props = defineProps<{
  trade: TradeDetail
  myTeamId: string
}>()

defineSlots<{ actions(): unknown }>()

const POS_COLOR: Record<string, string> = {
  QB: '#f97316', RB: '#22c55e', WR: '#3b82f6', TE: '#a855f7',
  K: '#eab308', DEF: '#64748b',
}

const STATUS_COLOR: Record<string, string> = {
  Pending:   'text-yellow-400 bg-yellow-400/10',
  Accepted:  'text-green-400 bg-green-400/10',
  Rejected:  'text-red-400 bg-red-400/10',
  Cancelled: 'text-[var(--text-muted)] bg-[var(--surface)]',
}

function posColor(pos: string) { return POS_COLOR[pos] ?? '#6b6784' }

function playerName(p: { firstName: string; lastName: string } | null) {
  return p ? `${p.firstName} ${p.lastName}` : 'Unknown'
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

// Players offered by the initiator (going to receiver)
const theyOffer = props.trade.items.filter(i => i.fromTeamId === props.trade.initiatorTeam.id)
// Players requested by the initiator (coming from receiver)
const theyWant  = props.trade.items.filter(i => i.fromTeamId === props.trade.receiverTeam.id)

// From my perspective: what I give vs what I get
const iAmInitiator = props.myTeamId === props.trade.initiatorTeam.id
const iGive   = iAmInitiator ? theyOffer : theyWant
const iReceive = iAmInitiator ? theyWant  : theyOffer

const counterpart = iAmInitiator ? props.trade.receiverTeam : props.trade.initiatorTeam
</script>

<template>
  <div
    class="rounded-xl overflow-hidden"
    style="background: var(--surface); border: 1px solid var(--border)"
  >
    <!-- Header -->
    <div class="px-5 pt-4 pb-3" style="border-bottom: 1px solid var(--border-subtle)">
      <div class="flex items-center justify-between gap-3">
        <div class="flex items-center gap-2">
          <span class="text-sm font-semibold">
            {{ iAmInitiator ? 'You' : trade.initiatorTeam.name }}
            <span class="text-[var(--text-muted)] font-normal mx-1">↔</span>
            {{ iAmInitiator ? counterpart.name : 'You' }}
          </span>
        </div>
        <div class="flex items-center gap-2">
          <span class="text-[10px] text-[var(--text-muted)]">{{ formatDate(trade.proposedAt) }}</span>
          <span
            class="text-[10px] font-bold px-2 py-0.5 rounded-full"
            :class="STATUS_COLOR[trade.status]"
          >
            {{ trade.status }}
          </span>
        </div>
      </div>
      <p v-if="trade.message" class="text-xs text-[var(--text-muted)] mt-1.5 italic">
        "{{ trade.message }}"
      </p>
    </div>

    <!-- Trade sides -->
    <div class="grid grid-cols-2 divide-x" style="border-color: var(--border-subtle)">
      <!-- What I give -->
      <div class="px-4 py-3">
        <p class="text-[10px] font-semibold uppercase tracking-widest text-[var(--text-muted)] mb-2">
          {{ iAmInitiator ? 'You give' : `${trade.initiatorTeam.name} gives` }}
        </p>
        <div v-if="iGive.length === 0" class="text-xs text-[var(--text-muted)] italic">Nothing</div>
        <div v-else class="space-y-1">
          <div v-for="item in iGive" :key="item.playerId" class="flex items-center gap-1.5 text-xs">
            <span
              class="text-[10px] font-bold w-7 text-center shrink-0"
              :style="{ color: posColor(item.player?.position ?? '') }"
            >
              {{ item.player?.position ?? '?' }}
            </span>
            <span class="font-medium truncate">{{ playerName(item.player) }}</span>
            <span v-if="item.player?.nflTeam" class="text-[var(--text-muted)] ml-auto shrink-0">
              {{ item.player.nflTeam }}
            </span>
          </div>
        </div>
      </div>

      <!-- What I get -->
      <div class="px-4 py-3">
        <p class="text-[10px] font-semibold uppercase tracking-widest text-[var(--text-muted)] mb-2">
          {{ iAmInitiator ? 'You receive' : `You give` }}
        </p>
        <div v-if="iReceive.length === 0" class="text-xs text-[var(--text-muted)] italic">Nothing</div>
        <div v-else class="space-y-1">
          <div v-for="item in iReceive" :key="item.playerId" class="flex items-center gap-1.5 text-xs">
            <span
              class="text-[10px] font-bold w-7 text-center shrink-0"
              :style="{ color: posColor(item.player?.position ?? '') }"
            >
              {{ item.player?.position ?? '?' }}
            </span>
            <span class="font-medium truncate">{{ playerName(item.player) }}</span>
            <span v-if="item.player?.nflTeam" class="text-[var(--text-muted)] ml-auto shrink-0">
              {{ item.player.nflTeam }}
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- Actions slot -->
    <div
      v-if="$slots.actions"
      class="px-5 py-3 flex items-center gap-3"
      style="border-top: 1px solid var(--border-subtle)"
    >
      <slot name="actions" />
    </div>
  </div>
</template>
