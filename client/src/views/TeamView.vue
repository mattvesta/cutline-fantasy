<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { teamsApi } from '../api/teams'
import type { Team, RosterSlot } from '../api/types'

const route    = useRoute()
const leagueId = route.params.leagueId as string
const teamId   = route.params.teamId as string

const team      = ref<Team | null>(null)
const isLoading = ref(true)
const error     = ref<string | null>(null)
const isSaving  = ref(false)
const saveError = ref<string | null>(null)

const SLOT_ORDER = ['QB', 'RB', 'WR', 'TE', 'Flex', 'SuperFlex', 'K', 'DEF']

const starterList = ref<RosterSlot[]>([])
const benchList   = ref<RosterSlot[]>([])
const irList      = ref<RosterSlot[]>([])

function syncLists() {
  const slots = team.value?.rosterSlots ?? []
  starterList.value = slots
    .filter(s => s.isStarter)
    .sort((a, b) => SLOT_ORDER.indexOf(a.slotType) - SLOT_ORDER.indexOf(b.slotType))
  benchList.value = slots.filter(s => !s.isStarter && s.slotType !== 'IR')
  irList.value    = slots.filter(s => s.slotType === 'IR')
}

onMounted(async () => {
  try {
    team.value = await teamsApi.getById(leagueId, teamId)
    syncLists()
  } catch {
    error.value = 'Failed to load roster'
  } finally {
    isLoading.value = false
  }
})

// ── Position eligibility ───────────────────────────────────────────────────
// Can a player of `playerPos` occupy a slot of `slotType`?
function canFit(playerPos: string, slotType: string): boolean {
  if (slotType === 'Bench') return true
  if (slotType === 'Flex') return ['RB', 'WR', 'TE'].includes(playerPos)
  if (slotType === 'SuperFlex') return ['QB', 'RB', 'WR', 'TE'].includes(playerPos)
  return playerPos === slotType   // QB→QB, RB→RB, K→K, DEF→DEF, etc.
}

// A move is valid if the dragged player fits the target slot.
// If the target has a player too, that player must also fit back into the source slot.
function isValidMove(dragged: RosterSlot, target: RosterSlot): boolean {
  if (!dragged.player || dragged.id === target.id) return false
  if (!canFit(dragged.player.position, target.slotType)) return false
  // If target is occupied, the displaced player must fit back into the source slot
  if (target.player && !canFit(target.player.position, dragged.slotType)) return false
  return true
}

// ── Drag state ─────────────────────────────────────────────────────────────
const draggingSlot   = ref<RosterSlot | null>(null)
const dragOverSlotId = ref<string | null>(null)
const dragInvalid    = ref<string | null>(null)

function onDragStart(slot: RosterSlot, event: DragEvent) {
  draggingSlot.value = slot
  event.dataTransfer!.effectAllowed = 'move'
  // Custom ghost: clone only the player portion
  const el = (event.currentTarget as HTMLElement)
  event.dataTransfer!.setDragImage(el, el.offsetWidth / 2, el.offsetHeight / 2)
}

function onDragEnter(slot: RosterSlot) {
  if (!draggingSlot.value) return
  if (isValidMove(draggingSlot.value, slot)) {
    dragOverSlotId.value = slot.id
    dragInvalid.value    = null
  } else if (draggingSlot.value.id !== slot.id) {
    dragInvalid.value    = slot.id
    dragOverSlotId.value = null
  }
}

function onDragOver(slot: RosterSlot, event: DragEvent) {
  if (!draggingSlot.value) return
  if (isValidMove(draggingSlot.value, slot)) {
    event.preventDefault()   // allow drop
    event.dataTransfer!.dropEffect = 'move'
  }
  // Not calling preventDefault for invalid targets shows the "no-drop" cursor
}

function onDragLeave(event: DragEvent) {
  // Only clear if truly leaving the row (not entering a child element)
  const related = event.relatedTarget as HTMLElement | null
  if (!related || !(event.currentTarget as HTMLElement).contains(related)) {
    dragOverSlotId.value = null
    dragInvalid.value    = null
  }
}

function onDragEnd() {
  draggingSlot.value   = null
  dragOverSlotId.value = null
  dragInvalid.value    = null
}

async function onDrop(targetSlot: RosterSlot) {
  dragOverSlotId.value = null
  dragInvalid.value    = null

  if (!draggingSlot.value || !isValidMove(draggingSlot.value, targetSlot)) return

  const from = draggingSlot.value
  draggingSlot.value = null

  saveError.value = null
  isSaving.value  = true
  try {
    team.value = await teamsApi.swapSlots(leagueId, teamId, from.id, targetSlot.id)
    syncLists()
  } catch {
    saveError.value = 'Failed to save lineup change'
    try { team.value = await teamsApi.getById(leagueId, teamId); syncLists() } catch {}
  } finally {
    isSaving.value = false
  }
}

// ── Display helpers ────────────────────────────────────────────────────────
function playerName(slot: RosterSlot) {
  return slot.player ? `${slot.player.firstName} ${slot.player.lastName}` : 'Empty slot'
}
function playerMeta(slot: RosterSlot) {
  if (!slot.player) return ''
  return [slot.player.position, slot.player.nflTeam].filter(Boolean).join(' · ')
}

const STATUS_COLOR: Record<string, string> = {
  Injured:        'text-yellow-400',
  InjuredReserve: 'text-red-400',
  Inactive:       'text-[var(--text-muted)]',
  Unknown:        'text-[var(--text-muted)]',
}
const STATUS_LABEL: Record<string, string> = {
  Injured: 'Injured', InjuredReserve: 'IR', Inactive: 'Inactive', Unknown: 'Unknown',
}
</script>

<template>
  <div class="page">
    <RouterLink
      :to="`/leagues/${leagueId}`"
      class="text-sm text-[var(--text-muted)] hover:text-white transition-colors mb-8 inline-flex items-center gap-1.5"
    >
      ← Standings
    </RouterLink>

    <div v-if="isLoading" class="space-y-3">
      <div class="h-8 w-48 bg-[var(--surface)] rounded animate-pulse" />
      <div class="h-4 w-32 bg-[var(--surface)] rounded animate-pulse" />
    </div>
    <div v-else-if="error" class="text-[var(--red)] text-sm">{{ error }}</div>

    <template v-else-if="team">
      <!-- Header -->
      <div class="flex items-start justify-between mb-8">
        <div>
          <h1 class="text-3xl font-bold mb-1">{{ team.name }}</h1>
          <p class="text-sm text-[var(--text-muted)]">{{ team.ownerUserId }}</p>
        </div>
        <div class="flex items-center gap-3 mt-1">
          <span v-if="isSaving" class="text-xs text-[var(--text-muted)] animate-pulse">Saving…</span>
          <span v-if="saveError" class="text-xs text-[var(--red)]">{{ saveError }}</span>
          <span v-if="team.isEliminated" class="text-xs px-2.5 py-1 rounded-full bg-red-500/10 text-red-400">
            Eliminated — Week {{ team.eliminatedWeek }}
          </span>
          <span v-else class="text-xs px-2.5 py-1 rounded-full bg-green-500/10 text-green-400">Active</span>
        </div>
      </div>

      <p class="text-xs text-[var(--text-muted)] mb-4">
        Drag a player to a compatible slot to change your lineup.
      </p>

      <!-- ── Starters ── -->
      <section class="mb-8">
        <h2 class="label mb-3">Starters</h2>
        <div v-if="starterList.length === 0" class="text-sm text-[var(--text-muted)] py-2">
          Draft hasn't happened yet.
        </div>
        <div v-else class="card overflow-hidden">
          <div
            v-for="slot in starterList"
            :key="slot.id"
            class="slot-row flex items-stretch border-b border-[var(--border-subtle)] last:border-0"
            :class="{
              'drop-valid':   dragOverSlotId === slot.id,
              'drop-invalid': dragInvalid    === slot.id,
            }"
            @dragover="onDragOver(slot, $event)"
            @dragenter.prevent="onDragEnter(slot)"
            @dragleave="onDragLeave"
            @drop.prevent="onDrop(slot)"
          >
            <!-- Fixed position label — always stays in place -->
            <div class="slot-label flex items-center justify-center w-14 shrink-0 border-r border-[var(--border-subtle)]">
              <span class="text-xs font-bold" style="color: var(--accent)">{{ slot.slotType }}</span>
            </div>

            <!-- Player card (only draggable when occupied) -->
            <div
              :draggable="!!slot.player"
              class="player-card flex flex-1 items-center gap-3 px-4 py-3 select-none"
              :class="slot.player ? 'cursor-grab active:cursor-grabbing' : 'cursor-default'"
              @dragstart="slot.player && onDragStart(slot, $event)"
              @dragend="onDragEnd"
            >
              <div class="flex-1 min-w-0">
                <p class="font-medium truncate" :class="slot.player ? 'text-white' : 'text-[var(--text-muted)] italic text-sm'">
                  {{ playerName(slot) }}
                </p>
                <p v-if="slot.player" class="text-xs text-[var(--text-muted)] mt-0.5">{{ playerMeta(slot) }}</p>
              </div>
              <span
                v-if="slot.player && slot.player.status !== 'Active'"
                class="text-xs shrink-0"
                :class="STATUS_COLOR[slot.player.status]"
              >
                {{ STATUS_LABEL[slot.player.status] }}
              </span>
              <span v-if="slot.player" class="drag-handle shrink-0">⠿</span>
            </div>
          </div>
        </div>
      </section>

      <!-- ── Bench ── -->
      <section class="mb-8">
        <h2 class="label mb-3">Bench</h2>
        <div v-if="benchList.length === 0" class="text-sm text-[var(--text-muted)] py-2">Empty bench.</div>
        <div v-else class="card overflow-hidden">
          <div
            v-for="slot in benchList"
            :key="slot.id"
            class="slot-row flex items-stretch border-b border-[var(--border-subtle)] last:border-0"
            :class="{
              'drop-valid':   dragOverSlotId === slot.id,
              'drop-invalid': dragInvalid    === slot.id,
            }"
            @dragover="onDragOver(slot, $event)"
            @dragenter.prevent="onDragEnter(slot)"
            @dragleave="onDragLeave"
            @drop.prevent="onDrop(slot)"
          >
            <!-- Fixed label -->
            <div class="slot-label flex items-center justify-center w-14 shrink-0 border-r border-[var(--border-subtle)]">
              <span class="text-xs font-medium text-[var(--text-muted)]">BN</span>
            </div>

            <!-- Player card (only draggable when occupied) -->
            <div
              :draggable="!!slot.player"
              class="player-card flex flex-1 items-center gap-3 px-4 py-3 select-none"
              :class="slot.player ? 'cursor-grab active:cursor-grabbing' : 'cursor-default'"
              @dragstart="slot.player && onDragStart(slot, $event)"
              @dragend="onDragEnd"
            >
              <div class="flex-1 min-w-0">
                <p class="font-medium truncate" :class="slot.player ? 'text-white' : 'text-[var(--text-muted)] italic text-sm'">
                  {{ playerName(slot) }}
                </p>
                <p v-if="slot.player" class="text-xs text-[var(--text-muted)] mt-0.5">{{ playerMeta(slot) }}</p>
              </div>
              <span
                v-if="slot.player && slot.player.status !== 'Active'"
                class="text-xs shrink-0"
                :class="STATUS_COLOR[slot.player.status]"
              >
                {{ STATUS_LABEL[slot.player.status] }}
              </span>
              <span v-if="slot.player" class="drag-handle shrink-0">⠿</span>
            </div>
          </div>
        </div>
      </section>

      <!-- ── IR ── -->
      <section v-if="irList.length > 0">
        <h2 class="label mb-3">Injured Reserve</h2>
        <div class="card overflow-hidden">
          <div
            v-for="slot in irList"
            :key="slot.id"
            class="flex items-stretch border-b border-[var(--border-subtle)] last:border-0"
          >
            <div class="flex items-center justify-center w-14 shrink-0 border-r border-[var(--border-subtle)]">
              <span class="text-xs font-medium text-red-400">IR</span>
            </div>
            <div class="flex flex-1 items-center gap-3 px-4 py-3">
              <div class="flex-1 min-w-0">
                <p class="font-medium" :class="slot.player ? 'text-white' : 'text-[var(--text-muted)]'">
                  {{ playerName(slot) }}
                </p>
                <p v-if="slot.player" class="text-xs text-[var(--text-muted)] mt-0.5">{{ playerMeta(slot) }}</p>
              </div>
            </div>
          </div>
        </div>
      </section>
    </template>
  </div>
</template>

<style scoped>
.slot-row {
  transition: background 0.12s ease;
}
.slot-row:hover .player-card {
  background: var(--surface-raised);
}

/* Valid drop target */
.slot-row.drop-valid {
  background: color-mix(in srgb, var(--accent) 8%, transparent);
  outline: 1px solid var(--accent);
  outline-offset: -1px;
}
.slot-row.drop-valid .slot-label {
  border-color: color-mix(in srgb, var(--accent) 30%, var(--border-subtle));
}

/* Invalid drop target */
.slot-row.drop-invalid {
  background: rgba(239, 68, 68, 0.05);
  outline: 1px solid rgba(239, 68, 68, 0.3);
  outline-offset: -1px;
}

/* Player card being dragged (only this part moves with cursor) */
.player-card {
  transition: background 0.1s ease;
}

.drag-handle {
  color: var(--text-muted);
  opacity: 0.3;
  font-size: 0.875rem;
  transition: opacity 0.1s;
}
.player-card:hover .drag-handle {
  opacity: 0.6;
}
</style>
