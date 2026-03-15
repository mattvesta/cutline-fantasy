<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { adminApi } from '../api/admin'
import type { League } from '../api/types'

// ── Leagues ──────────────────────────────────────────────────────────────────
const leagues   = ref<League[]>([])
const leaguesBusy = ref(false)

async function loadLeagues() {
  leaguesBusy.value = true
  try { leagues.value = await adminApi.getLeagues() }
  finally { leaguesBusy.value = false }
}

onMounted(loadLeagues)

// ── Action state ─────────────────────────────────────────────────────────────
interface ActionState {
  busy: boolean
  result: string | null
  ok: boolean
}

function makeAction(): ActionState {
  return { busy: false, result: null, ok: true }
}

const actions = {
  seed:              ref(makeAction()),
  clearSeed:         ref(makeAction()),
  seedDraft:         ref(makeAction()),
  clearSeedDraft:    ref(makeAction()),
  seedScores:        ref(makeAction()),
  simulateTick:      ref(makeAction()),
}

// Per-season import state
const SEASONS = [2020, 2021, 2022, 2023, 2024, 2025]
const importState = ref(Object.fromEntries(SEASONS.map(s => [s, makeAction()])))
const importAllBusy = ref(false)
const importAllLog  = ref<string[]>([])

async function run(state: ActionState, fn: () => Promise<unknown>) {
  state.busy   = true
  state.result = null
  try {
    const res = await fn()
    state.ok     = true
    state.result = JSON.stringify(res, null, 2)
  } catch (e: unknown) {
    state.ok     = false
    state.result = e instanceof Error ? e.message : String(e)
  } finally {
    state.busy = false
  }
}

async function runSeed()           { await run(actions.seed.value,           adminApi.seed);              await loadLeagues() }
async function runClearSeed()      { await run(actions.clearSeed.value,      adminApi.clearSeed);         await loadLeagues() }
async function runSeedDraft()      { await run(actions.seedDraft.value,      adminApi.seedDraft);         await loadLeagues() }
async function runClearSeedDraft() { await run(actions.clearSeedDraft.value, adminApi.clearSeedDraft);    await loadLeagues() }
async function runSeedScores()     { await run(actions.seedScores.value,     adminApi.seedScores) }
async function runSimulateTick()   { await run(actions.simulateTick.value,   adminApi.simulateScoreTick) }

async function runImport(season: number) {
  await run(importState.value[season], () => adminApi.importStats(season))
}

async function runImportAll() {
  importAllBusy.value = true
  importAllLog.value  = []
  for (const season of SEASONS) {
    importAllLog.value.push(`Importing ${season}…`)
    const s = importState.value[season]
    await run(s, () => adminApi.importStats(season))
    importAllLog.value[importAllLog.value.length - 1] =
      s.ok
        ? `${season}: ${JSON.parse(s.result!).inserted} inserted, ${JSON.parse(s.result!).updated} updated, ${JSON.parse(s.result!).skipped} skipped`
        : `${season}: ERROR — ${s.result}`
  }
  importAllBusy.value = false
}

// ── Helpers ───────────────────────────────────────────────────────────────────
const STATUS: Record<string, { color: string; label: string }> = {
  Setup:     { color: '#6b6784', label: 'Setup'     },
  Drafting:  { color: '#facc15', label: 'Drafting'  },
  Active:    { color: '#4ade80', label: 'Active'     },
  Completed: { color: '#6b6784', label: 'Completed' },
}
</script>

<template>
  <div class="page" style="padding-top: 2.5rem; padding-bottom: 4rem; max-width: 960px">

    <div class="mb-8">
      <h1 class="text-2xl font-bold tracking-tight mb-1" style="letter-spacing:-0.03em">Admin</h1>
      <p class="text-sm" style="color:var(--text-muted)">Dev tools and system monitoring</p>
    </div>

    <!-- ── Player Stats Import ───────────────────────────────────────────── -->
    <section class="card p-6 mb-4">
      <div class="flex items-center justify-between mb-5">
        <div>
          <h2 class="font-semibold text-sm mb-0.5">Player Stats Import</h2>
          <p class="text-xs" style="color:var(--text-muted)">Fetches nflverse roster + stats for a season and upserts all rows</p>
        </div>
        <button
          class="btn btn-primary text-xs px-4 py-1.5"
          :disabled="importAllBusy"
          @click="runImportAll"
        >
          {{ importAllBusy ? 'Importing…' : 'Import All Seasons' }}
        </button>
      </div>

      <!-- Per-season rows -->
      <div class="space-y-2">
        <div
          v-for="season in SEASONS"
          :key="season"
          class="flex items-center gap-3 rounded-lg px-3 py-2.5"
          style="background:var(--bg); border:1px solid var(--border-subtle)"
        >
          <span class="text-sm font-mono font-semibold w-10 shrink-0">{{ season }}</span>

          <button
            class="btn btn-ghost text-xs px-3 py-1 shrink-0"
            :disabled="importState[season].busy"
            @click="runImport(season)"
          >
            {{ importState[season].busy ? 'Running…' : 'Import' }}
          </button>

          <!-- Result summary -->
          <span
            v-if="importState[season].result && importState[season].ok"
            class="text-xs font-mono truncate"
            style="color:var(--text-muted)"
          >
            {{
              (() => {
                const r = JSON.parse(importState[season].result!)
                return `${r.inserted} inserted · ${r.updated} updated · ${r.skipped} skipped · ${r.playersCreated ?? 0} players created`
              })()
            }}
          </span>
          <span
            v-else-if="importState[season].result && !importState[season].ok"
            class="text-xs truncate"
            style="color:var(--accent)"
          >
            {{ importState[season].result }}
          </span>
        </div>
      </div>

      <!-- Import-all log -->
      <div
        v-if="importAllLog.length"
        class="mt-4 rounded-lg p-3 font-mono text-xs space-y-0.5"
        style="background:var(--bg); border:1px solid var(--border-subtle); color:var(--text-muted)"
      >
        <div v-for="(line, i) in importAllLog" :key="i">{{ line }}</div>
      </div>
    </section>

    <!-- ── Test Leagues ──────────────────────────────────────────────────── -->
    <section class="card p-6 mb-4">
      <h2 class="font-semibold text-sm mb-4">Test Leagues</h2>
      <div class="grid grid-cols-2 gap-3">

        <ActionCard
          label="Seed test league"
          description="Creates 8 teams with rostered players from DB"
          :state="actions.seed.value"
          @run="runSeed"
        />
        <ActionCard
          label="Clear seed data"
          description="Wipes all leagues + managers created by seed"
          :state="actions.clearSeed.value"
          danger
          @run="runClearSeed"
        />
        <ActionCard
          label="Seed draft league"
          description="Creates a draft league, starts it, auto-picks 2 rounds"
          :state="actions.seedDraft.value"
          @run="runSeedDraft"
        />
        <ActionCard
          label="Clear draft seed"
          description="Wipes draft leagues + managers"
          :state="actions.clearSeedDraft.value"
          danger
          @run="runClearSeedDraft"
        />
      </div>
    </section>

    <!-- ── Live Scoring ──────────────────────────────────────────────────── -->
    <section class="card p-6 mb-8">
      <h2 class="font-semibold text-sm mb-4">Live Scoring</h2>
      <div class="grid grid-cols-2 gap-3">
        <ActionCard
          label="Seed scores (Week 8)"
          description="Creates an InProgress week and seeds mock PlayerGameStats"
          :state="actions.seedScores.value"
          @run="runSeedScores"
        />
        <ActionCard
          label="Simulate score tick"
          description="Increments a random player's stats and broadcasts via SignalR"
          :state="actions.simulateTick.value"
          @run="runSimulateTick"
        />
      </div>
    </section>

    <!-- ── Leagues ───────────────────────────────────────────────────────── -->
    <section>
      <div class="flex items-center justify-between mb-4">
        <h2 class="font-semibold text-sm">All Leagues</h2>
        <button
          class="btn btn-ghost text-xs px-3 py-1"
          :disabled="leaguesBusy"
          @click="loadLeagues"
        >
          {{ leaguesBusy ? 'Loading…' : 'Refresh' }}
        </button>
      </div>

      <div v-if="leaguesBusy && !leagues.length" class="card p-6 text-center text-sm" style="color:var(--text-muted)">
        Loading…
      </div>

      <div v-else-if="!leagues.length" class="card p-6 text-center text-sm" style="color:var(--text-muted)">
        No leagues found.
      </div>

      <table v-else class="data-table w-full">
        <thead>
          <tr>
            <th>League</th>
            <th>Season</th>
            <th>Status</th>
            <th>Teams</th>
            <th>Managers</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="league in leagues" :key="league.id">
            <td class="font-medium">{{ league.name }}</td>
            <td>{{ league.season }}</td>
            <td>
              <span class="flex items-center gap-1.5">
                <span
                  class="w-1.5 h-1.5 rounded-full shrink-0"
                  :style="`background:${STATUS[league.status]?.color ?? 'var(--text-muted)'}`"
                />
                <span class="text-xs" :style="`color:${STATUS[league.status]?.color ?? 'var(--text-muted)'}`">
                  {{ STATUS[league.status]?.label ?? league.status }}
                </span>
              </span>
            </td>
            <td>{{ league.teams?.length ?? 0 }}</td>
            <td>{{ league.leagueManagers?.length ?? 0 }}</td>
            <td>
              <RouterLink
                :to="`/leagues/${league.id}`"
                class="text-xs"
                style="color:var(--accent)"
              >
                View
              </RouterLink>
            </td>
          </tr>
        </tbody>
      </table>
    </section>

  </div>
</template>

<!-- ── ActionCard sub-component ──────────────────────────────────────────── -->
<script lang="ts">
import { defineComponent, type PropType } from 'vue'

interface ActionState { busy: boolean; result: string | null; ok: boolean }

export const ActionCard = defineComponent({
  name: 'ActionCard',
  props: {
    label:       { type: String,  required: true },
    description: { type: String,  required: true },
    state:       { type: Object as PropType<ActionState>, required: true },
    danger:      { type: Boolean, default: false },
  },
  emits: ['run'],
  template: `
    <div class="rounded-lg p-4 flex flex-col gap-3" style="background:var(--bg); border:1px solid var(--border-subtle)">
      <div>
        <p class="text-sm font-medium mb-0.5">{{ label }}</p>
        <p class="text-xs" style="color:var(--text-muted)">{{ description }}</p>
      </div>
      <div class="flex items-center gap-2">
        <button
          class="btn text-xs px-3 py-1.5 shrink-0"
          :class="danger ? 'btn-ghost' : 'btn-ghost'"
          :style="danger ? 'color:var(--accent)' : ''"
          :disabled="state.busy"
          @click="$emit('run')"
        >
          {{ state.busy ? 'Running…' : 'Run' }}
        </button>
        <span
          v-if="state.result"
          class="text-xs truncate"
          :style="state.ok ? 'color:var(--text-muted)' : 'color:var(--accent)'"
        >
          {{ state.ok ? '✓ ' + (JSON.parse(state.result)?.message ?? 'Done') : '✗ ' + state.result }}
        </span>
      </div>
    </div>
  `,
})
</script>
