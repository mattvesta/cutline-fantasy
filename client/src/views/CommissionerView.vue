<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { commissionerApi } from '../api/commissioner'
import type { CommissionerState, CommissionerTeam, CommissionerWeek } from '../api/types'

const route    = useRoute()
const leagueId = route.params.leagueId as string

const state     = ref<CommissionerState | null>(null)
const isLoading = ref(true)
const error     = ref<string | null>(null)

// ── Settings ────────────────────────────────────────────────────────────────
const settings = reactive({
  name: '',
  receptionPoints: 1,
  useFaab: false,
  faabBudget: 100,
  minFaabBid: 0,
})
const settingsSaving = ref(false)
const settingsError  = ref<string | null>(null)
const settingsSaved  = ref(false)

async function saveSettings() {
  settingsSaving.value = true
  settingsError.value  = null
  settingsSaved.value  = false
  try {
    await commissionerApi.updateSettings(leagueId, {
      name:            settings.name,
      receptionPoints: settings.receptionPoints,
      useFaab:         settings.useFaab,
      faabBudget:      settings.faabBudget,
      minFaabBid:      settings.minFaabBid,
    })
    settingsSaved.value = true
    setTimeout(() => { settingsSaved.value = false }, 2500)
    await reload()
  } catch (e) {
    settingsError.value = e instanceof Error ? e.message : 'Failed to save settings.'
  } finally {
    settingsSaving.value = false
  }
}

// ── Teams ────────────────────────────────────────────────────────────────────
const teamErr          = ref<string | null>(null)
const eliminateForm    = ref<{ teamId: string; weekNumber: number } | null>(null)
const eliminateLoading = ref(false)

function openEliminate(team: CommissionerTeam) {
  const latestWeek = state.value?.weeks.at(-1)?.weekNumber ?? 1
  eliminateForm.value = { teamId: team.id, weekNumber: latestWeek }
  teamErr.value = null
}

async function confirmEliminate() {
  if (!eliminateForm.value) return
  eliminateLoading.value = true
  teamErr.value = null
  try {
    await commissionerApi.forceEliminate(leagueId, eliminateForm.value.teamId, eliminateForm.value.weekNumber)
    eliminateForm.value = null
    await reload()
  } catch (e) {
    teamErr.value = e instanceof Error ? e.message : 'Failed to eliminate team.'
  } finally {
    eliminateLoading.value = false
  }
}

async function restoreTeam(teamId: string) {
  teamErr.value = null
  try {
    await commissionerApi.restoreTeam(leagueId, teamId)
    await reload()
  } catch (e) {
    teamErr.value = e instanceof Error ? e.message : 'Failed to restore team.'
  }
}

async function toggleLock(team: CommissionerTeam) {
  teamErr.value = null
  try {
    await commissionerApi.setTeamLock(leagueId, team.id, !team.isLocked)
    await reload()
  } catch (e) {
    teamErr.value = e instanceof Error ? e.message : 'Failed to update lock.'
  }
}

// ── Weeks ────────────────────────────────────────────────────────────────────
const expandedWeeks  = ref<Set<number>>(new Set())
// Score edits: weekNumber → teamId → edited points string
const scoreEdits     = ref<Record<number, Record<string, string>>>({})
const weekSaving     = ref<number | null>(null)
const weekErr        = ref<string | null>(null)

const WEEK_STATUSES = ['Upcoming', 'InProgress', 'Scoring', 'Eliminated', 'Completed']

function toggleWeek(weekNumber: number) {
  const s = new Set(expandedWeeks.value)
  if (s.has(weekNumber)) s.delete(weekNumber); else s.add(weekNumber)
  expandedWeeks.value = s
}

function scoreEdit(weekNumber: number, teamId: string): string {
  return scoreEdits.value[weekNumber]?.[teamId] ?? ''
}

function onScoreInput(weekNumber: number, teamId: string, val: string) {
  if (!scoreEdits.value[weekNumber]) scoreEdits.value[weekNumber] = {}
  scoreEdits.value[weekNumber][teamId] = val
}

function initScoreEdits(week: CommissionerWeek) {
  if (!scoreEdits.value[week.weekNumber]) {
    scoreEdits.value[week.weekNumber] = {}
    for (const s of week.scores) {
      scoreEdits.value[week.weekNumber][s.teamId] = s.points.toFixed(2)
    }
  }
}

// Add a new score row for a team with no score yet
function ensureTeamInWeek(week: CommissionerWeek, teamId: string, teamName: string) {
  if (!week.scores.find(s => s.teamId === teamId)) {
    week.scores.push({ teamId, teamName, points: 0, isLocked: false })
    onScoreInput(week.weekNumber, teamId, '0')
  }
}

async function setWeekStatus(week: CommissionerWeek, status: string) {
  weekErr.value = null
  try {
    await commissionerApi.setWeekStatus(leagueId, week.weekNumber, status)
    await reload()
  } catch (e) {
    weekErr.value = e instanceof Error ? e.message : 'Failed to update week status.'
  }
}

async function saveScores(week: CommissionerWeek) {
  weekErr.value = null
  weekSaving.value = week.weekNumber
  const edits = scoreEdits.value[week.weekNumber] ?? {}
  try {
    await Promise.all(
      Object.entries(edits).map(([teamId, pts]) => {
        const parsed = parseFloat(pts)
        if (isNaN(parsed)) return Promise.resolve()
        return commissionerApi.overrideScore(leagueId, week.weekNumber, teamId, parsed)
      })
    )
    delete scoreEdits.value[week.weekNumber]
    await reload()
  } catch (e) {
    weekErr.value = e instanceof Error ? e.message : 'Failed to save scores.'
  } finally {
    weekSaving.value = null
  }
}

// ── Load ─────────────────────────────────────────────────────────────────────
async function reload() {
  const fresh = await commissionerApi.getState(leagueId)
  state.value = fresh
  settings.name            = fresh.name
  settings.receptionPoints = fresh.receptionPoints
  settings.useFaab         = fresh.useFaab
  settings.faabBudget      = fresh.faabBudget
  settings.minFaabBid      = fresh.minFaabBid
}

onMounted(async () => {
  try {
    await reload()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Access denied or league not found.'
  } finally {
    isLoading.value = false
  }
})

// ── Helpers ──────────────────────────────────────────────────────────────────
const RECEPTION_PRESETS = [
  { label: 'Standard', value: 0 },
  { label: 'Half PPR', value: 0.5 },
  { label: 'Full PPR', value: 1 },
]

const STATUS_COLOR: Record<string, string> = {
  Upcoming:  'text-[var(--text-muted)] bg-[var(--surface-raised)]',
  InProgress: 'text-blue-400 bg-blue-400/10',
  Scoring:    'text-yellow-400 bg-yellow-400/10',
  Eliminated: 'text-[var(--accent)] bg-[var(--accent-dim)]',
  Completed:  'text-green-400 bg-green-400/10',
}

const activeTeams     = computed(() => state.value?.teams.filter(t => !t.isEliminated) ?? [])
const eliminatedTeams = computed(() => state.value?.teams.filter(t => t.isEliminated) ?? [])
</script>

<template>
  <div class="page max-w-3xl">
    <RouterLink
      :to="`/leagues/${leagueId}`"
      class="text-sm text-[var(--text-muted)] hover:text-white transition-colors mb-8 inline-flex items-center gap-1.5"
    >
      ← League
    </RouterLink>

    <div class="mb-8">
      <h1 class="text-3xl font-bold tracking-tight mb-1" style="letter-spacing: -0.03em">Commissioner Tools</h1>
      <p class="text-sm text-[var(--text-muted)]">Manage settings, scores, eliminations, and team locks.</p>
    </div>

    <div v-if="isLoading" class="space-y-4">
      <div v-for="i in 3" :key="i" class="h-40 rounded-xl bg-[var(--surface)] animate-pulse" />
    </div>

    <div v-else-if="error" class="text-[var(--red)] text-sm p-4 rounded-xl" style="background: rgba(220,38,38,0.08)">
      {{ error }}
    </div>

    <template v-else-if="state">

      <!-- ── League Settings ─────────────────────────────────────────── -->
      <section class="mb-6">
        <div class="rounded-xl overflow-hidden" style="background: var(--surface); border: 1px solid var(--border)">
          <div class="px-5 py-4" style="border-bottom: 1px solid var(--border-subtle)">
            <h2 class="font-semibold">League Settings</h2>
          </div>
          <div class="px-5 py-5 space-y-5">

            <!-- Name -->
            <div>
              <label class="label block mb-1.5">League Name</label>
              <input v-model="settings.name" class="input w-full" maxlength="100" />
            </div>

            <!-- Scoring -->
            <div>
              <label class="label block mb-1.5">Scoring Format</label>
              <div class="flex gap-2">
                <button
                  v-for="p in RECEPTION_PRESETS"
                  :key="p.label"
                  class="flex-1 py-2 rounded-lg text-sm font-medium transition-colors"
                  :style="settings.receptionPoints === p.value
                    ? 'background: var(--accent); color: white'
                    : 'background: var(--surface-raised); color: var(--text-secondary); border: 1px solid var(--border)'"
                  @click="settings.receptionPoints = p.value"
                >
                  {{ p.label }}
                </button>
              </div>
              <div class="flex items-center gap-2 mt-2">
                <span class="text-xs text-[var(--text-muted)]">Reception pts:</span>
                <input
                  v-model.number="settings.receptionPoints"
                  type="number" min="0" max="2" step="0.25"
                  class="input w-20 text-sm py-1"
                />
              </div>
            </div>

            <!-- FAAB -->
            <div>
              <label class="label block mb-1.5">Waiver System</label>
              <div class="flex items-center gap-3 mb-3">
                <button
                  class="relative w-10 h-5 rounded-full transition-colors shrink-0"
                  :style="settings.useFaab ? 'background: var(--accent)' : 'background: var(--border)'"
                  @click="settings.useFaab = !settings.useFaab"
                >
                  <span
                    class="absolute top-0.5 w-4 h-4 rounded-full bg-white transition-transform"
                    :style="settings.useFaab ? 'transform: translateX(1.25rem)' : 'transform: translateX(0.125rem)'"
                  />
                </button>
                <span class="text-sm">{{ settings.useFaab ? 'FAAB blind auction' : 'Waiver priority' }}</span>
              </div>
              <div v-if="settings.useFaab" class="grid grid-cols-2 gap-3">
                <div>
                  <label class="text-xs text-[var(--text-muted)] block mb-1">Budget ($)</label>
                  <input v-model.number="settings.faabBudget" type="number" min="0" class="input w-full text-sm" />
                </div>
                <div>
                  <label class="text-xs text-[var(--text-muted)] block mb-1">Min bid ($)</label>
                  <input v-model.number="settings.minFaabBid" type="number" min="0" class="input w-full text-sm" />
                </div>
              </div>
            </div>

            <div v-if="settingsError" class="text-[var(--red)] text-xs">{{ settingsError }}</div>
            <div class="flex items-center gap-3">
              <button
                class="btn btn-primary text-sm"
                :disabled="settingsSaving"
                @click="saveSettings"
              >
                {{ settingsSaving ? 'Saving…' : 'Save Settings' }}
              </button>
              <span v-if="settingsSaved" class="text-xs text-green-400">Saved ✓</span>
            </div>
          </div>
        </div>
      </section>

      <!-- ── Teams ──────────────────────────────────────────────────── -->
      <section class="mb-6">
        <div class="rounded-xl overflow-hidden" style="background: var(--surface); border: 1px solid var(--border)">
          <div class="px-5 py-4" style="border-bottom: 1px solid var(--border-subtle)">
            <h2 class="font-semibold">Teams</h2>
          </div>

          <div v-if="teamErr" class="px-5 py-3 text-xs text-red-400">{{ teamErr }}</div>

          <!-- Active teams -->
          <div
            v-for="team in activeTeams"
            :key="team.id"
            class="px-5 py-3.5"
            style="border-bottom: 1px solid var(--border-subtle)"
          >
            <div class="flex items-center gap-3">
              <div class="flex-1 min-w-0">
                <p class="font-medium">{{ team.name }}</p>
                <p v-if="team.managerName" class="text-xs text-[var(--text-muted)]">{{ team.managerName }}</p>
              </div>

              <span class="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-green-500/10 text-green-400 shrink-0">
                Active
              </span>

              <!-- Lock toggle -->
              <button
                class="text-xs px-2.5 py-1.5 rounded transition-colors shrink-0"
                :style="team.isLocked
                  ? 'background: rgba(239,68,68,0.12); color: #f87171; border: 1px solid rgba(239,68,68,0.2)'
                  : 'background: var(--surface-raised); color: var(--text-muted); border: 1px solid var(--border)'"
                @click="toggleLock(team)"
              >
                {{ team.isLocked ? '🔒 Locked' : 'Lock' }}
              </button>

              <!-- Eliminate -->
              <button
                v-if="!eliminateForm || eliminateForm.teamId !== team.id"
                class="text-xs px-2.5 py-1.5 rounded transition-colors shrink-0"
                style="background: rgba(239,68,68,0.08); color: #f87171; border: 1px solid rgba(239,68,68,0.15)"
                onmouseover="this.style.background='rgba(239,68,68,0.18)'"
                onmouseout="this.style.background='rgba(239,68,68,0.08)'"
                @click="openEliminate(team)"
              >
                Eliminate
              </button>
            </div>

            <!-- Inline eliminate form -->
            <div
              v-if="eliminateForm?.teamId === team.id"
              class="mt-3 flex items-center gap-3 p-3 rounded-lg text-sm"
              style="background: rgba(239,68,68,0.06); border: 1px solid rgba(239,68,68,0.2)"
            >
              <span class="text-xs text-red-300 flex-1">Force-eliminate <strong>{{ team.name }}</strong> on week:</span>
              <input
                v-model.number="eliminateForm.weekNumber"
                type="number" min="1" class="input w-20 text-sm py-1"
              />
              <button
                class="text-xs px-3 py-1.5 rounded font-semibold"
                style="background: var(--accent); color: white"
                :disabled="eliminateLoading"
                @click="confirmEliminate"
              >
                {{ eliminateLoading ? '…' : 'Confirm' }}
              </button>
              <button
                class="text-xs text-[var(--text-muted)] hover:text-white"
                @click="eliminateForm = null"
              >
                Cancel
              </button>
            </div>
          </div>

          <!-- Eliminated teams -->
          <div
            v-for="team in eliminatedTeams"
            :key="team.id"
            class="px-5 py-3.5 opacity-60"
            style="border-bottom: 1px solid var(--border-subtle)"
          >
            <div class="flex items-center gap-3">
              <div class="flex-1 min-w-0">
                <p class="font-medium">{{ team.name }}</p>
                <p class="text-xs text-[var(--text-muted)]">{{ team.managerName ?? '' }}</p>
              </div>
              <span class="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-red-500/10 text-red-400 shrink-0">
                ✂ Wk {{ team.eliminatedWeek }}
              </span>
              <button
                class="text-xs px-2.5 py-1.5 rounded transition-colors shrink-0"
                :style="team.isLocked
                  ? 'background: rgba(239,68,68,0.12); color: #f87171; border: 1px solid rgba(239,68,68,0.2)'
                  : 'background: var(--surface-raised); color: var(--text-muted); border: 1px solid var(--border)'"
                @click="toggleLock(team)"
              >
                {{ team.isLocked ? '🔒 Locked' : 'Lock' }}
              </button>
              <button
                class="text-xs px-2.5 py-1.5 rounded transition-colors shrink-0"
                style="background: var(--surface-raised); color: var(--text-secondary); border: 1px solid var(--border)"
                onmouseover="this.style.color='white'"
                onmouseout="this.style.color='var(--text-secondary)'"
                @click="restoreTeam(team.id)"
              >
                Restore
              </button>
            </div>
          </div>

          <div v-if="!activeTeams.length && !eliminatedTeams.length" class="px-5 py-8 text-center text-sm text-[var(--text-muted)]">
            No teams yet.
          </div>
        </div>
      </section>

      <!-- ── Weeks ──────────────────────────────────────────────────── -->
      <section>
        <div class="rounded-xl overflow-hidden" style="background: var(--surface); border: 1px solid var(--border)">
          <div class="px-5 py-4" style="border-bottom: 1px solid var(--border-subtle)">
            <h2 class="font-semibold">Weeks &amp; Scores</h2>
          </div>

          <div v-if="weekErr" class="px-5 py-3 text-xs text-red-400">{{ weekErr }}</div>

          <div v-if="state.weeks.length === 0" class="px-5 py-8 text-center text-sm text-[var(--text-muted)]">
            No weeks created yet.
          </div>

          <div
            v-for="week in state.weeks"
            :key="week.weekNumber"
            style="border-bottom: 1px solid var(--border-subtle)"
          >
            <!-- Week row -->
            <div class="px-5 py-3.5 flex items-center gap-3">
              <span class="text-sm font-bold w-14 shrink-0" style="color: var(--accent)">
                Week {{ week.weekNumber }}
              </span>

              <!-- Status select -->
              <select
                :value="week.status"
                class="input text-xs py-1 flex-1"
                @change="setWeekStatus(week, ($event.target as HTMLSelectElement).value)"
              >
                <option v-for="s in WEEK_STATUSES" :key="s" :value="s">{{ s }}</option>
              </select>

              <span
                class="text-[10px] font-semibold px-2 py-0.5 rounded-full shrink-0"
                :class="STATUS_COLOR[week.status] ?? 'text-[var(--text-muted)]'"
              >
                {{ week.status }}
              </span>

              <!-- Toggle scores -->
              <button
                class="text-xs px-2.5 py-1.5 rounded transition-colors shrink-0"
                style="background: var(--surface-raised); color: var(--text-muted); border: 1px solid var(--border)"
                @click="toggleWeek(week.weekNumber); initScoreEdits(week)"
              >
                {{ expandedWeeks.has(week.weekNumber) ? '▲ Scores' : '▼ Scores' }}
              </button>
            </div>

            <!-- Scores panel -->
            <div v-if="expandedWeeks.has(week.weekNumber)" class="px-5 pb-4">
              <!-- Add missing teams -->
              <template v-if="state">
                <template v-for="t in state.teams" :key="t.id">
                  <span style="display:none">{{ ensureTeamInWeek(week, t.id, t.name) }}</span>
                </template>
              </template>

              <div class="space-y-2 mb-3">
                <div
                  v-for="score in week.scores"
                  :key="score.teamId"
                  class="flex items-center gap-3"
                >
                  <span class="text-sm flex-1 truncate">{{ score.teamName }}</span>
                  <input
                    :value="scoreEdit(week.weekNumber, score.teamId) || score.points.toFixed(2)"
                    type="number"
                    min="0"
                    step="0.1"
                    class="input w-24 text-sm py-1 text-right tabular-nums"
                    @input="onScoreInput(week.weekNumber, score.teamId, ($event.target as HTMLInputElement).value)"
                  />
                </div>
              </div>

              <div class="flex items-center gap-3">
                <button
                  class="btn btn-primary text-xs px-3 py-1.5"
                  :disabled="weekSaving === week.weekNumber"
                  @click="saveScores(week)"
                >
                  {{ weekSaving === week.weekNumber ? 'Saving…' : 'Save Scores' }}
                </button>
                <p class="text-[10px] text-[var(--text-muted)]">
                  Changes override existing scores. Run elimination after saving.
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>

    </template>
  </div>
</template>
