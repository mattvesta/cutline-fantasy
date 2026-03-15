<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { leaguesApi } from '../api/leagues'

const router = useRouter()

// ─── League identity ─────────────────────────────────────────────────
const name        = ref('')
const season      = ref(new Date().getFullYear())
const logoFile    = ref<File | null>(null)
const logoPreview = ref<string | null>(null)
const logoInput   = ref<HTMLInputElement | null>(null)

function onLogoChange(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file) return
  logoFile.value = file
  logoPreview.value = URL.createObjectURL(file)
}

function onLogoDrop(e: DragEvent) {
  e.preventDefault()
  isDragging.value = false
  const file = e.dataTransfer?.files[0]
  if (!file || !file.type.startsWith('image/')) return
  logoFile.value = file
  logoPreview.value = URL.createObjectURL(file)
}

function clearLogo() {
  logoFile.value = null
  logoPreview.value = null
  if (logoInput.value) logoInput.value.value = ''
}

const isDragging = ref(false)

// ─── Scoring format ──────────────────────────────────────────────────
type ScoringPreset = 'standard' | 'half-ppr' | 'ppr'
const scoringPreset = ref<ScoringPreset>('ppr')

const presets: { key: ScoringPreset; label: string; sub: string }[] = [
  { key: 'standard',  label: 'Standard',  sub: '0 pts / reception' },
  { key: 'half-ppr',  label: 'Half PPR',  sub: '0.5 pts / reception' },
  { key: 'ppr',       label: 'Full PPR',  sub: '1 pt / reception'  },
]

// ─── Roster settings ─────────────────────────────────────────────────
const slots = ref({
  QB: 1, RB: 2, WR: 2, TE: 1,
  Flex: 1, SuperFlex: 0, K: 1, DEF: 1,
  Bench: 6, IR: 1,
})

// ─── League size ─────────────────────────────────────────────────────
const teamCount = ref(10)

// ─── Waivers ─────────────────────────────────────────────────────────
const useFaab    = ref(true)
const faabBudget = ref(100)
const minFaabBid = ref(0)

// ─── Submit ──────────────────────────────────────────────────────────
const isSubmitting = ref(false)
const submitError  = ref<string | null>(null)

const canSubmit = computed(() => name.value.trim().length >= 2)

const RECEPTION_PTS: Record<ScoringPreset, number> = {
  standard: 0, 'half-ppr': 0.5, ppr: 1,
}

async function submit() {
  if (!canSubmit.value) return
  isSubmitting.value = true
  submitError.value  = null
  try {
    const league = await leaguesApi.create({
      name:   name.value.trim(),
      season: season.value,
      receptionPoints:  RECEPTION_PTS[scoringPreset.value],
      qbSlots:          slots.value.QB,
      rbSlots:          slots.value.RB,
      wrSlots:          slots.value.WR,
      teSlots:          slots.value.TE,
      flexSlots:        slots.value.Flex,
      superFlexSlots:   slots.value.SuperFlex,
      kSlots:           slots.value.K,
      defSlots:         slots.value.DEF,
      benchSlots:       slots.value.Bench,
      irSlots:          slots.value.IR,
      useFaab:          useFaab.value,
      faabBudget:       useFaab.value ? faabBudget.value : 0,
      minFaabBid:       useFaab.value ? minFaabBid.value : 0,
    })
    router.push(`/leagues/${league.id}`)
  } catch (e) {
    submitError.value = e instanceof Error ? e.message : 'Failed to create league'
  } finally {
    isSubmitting.value = false
  }
}

const seasons = Array.from({ length: 5 }, (_, i) => new Date().getFullYear() + 1 - i)
</script>

<template>
  <div class="page max-w-2xl">
    <!-- Back -->
    <RouterLink to="/" class="text-sm text-[var(--text-muted)] hover:text-white transition-colors mb-8 inline-flex items-center gap-1.5">
      ← Leagues
    </RouterLink>

    <h1 class="text-3xl font-bold tracking-tight" style="letter-spacing: -0.03em">Create a League</h1>
    <p class="text-[var(--text-muted)] text-sm mb-10">
      Set up your guillotine league. You can change most settings before the season starts.
    </p>

    <form @submit.prevent="submit" class="space-y-10">

      <!-- ── Identity ── -->
      <section>
        <h2 class="text-sm font-semibold uppercase tracking-widest text-[var(--text-muted)] mb-5">League Identity</h2>

        <!-- Logo upload -->
        <div class="mb-5">
          <p class="label mb-2">League Logo</p>

          <!-- Preview -->
          <div v-if="logoPreview" class="flex items-center gap-4 mb-3">
            <img :src="logoPreview" class="w-16 h-16 rounded-xl object-cover border border-[var(--border)]" alt="Logo preview" />
            <button type="button" class="btn btn-ghost text-xs" @click="clearLogo">Remove</button>
          </div>

          <!-- Drop zone -->
          <div
            v-else
            class="relative flex flex-col items-center justify-center gap-2 rounded-xl border-2 border-dashed p-8 text-center cursor-pointer transition-colors"
            :class="isDragging
              ? 'border-[var(--accent)] bg-[var(--accent-dim)]'
              : 'border-[var(--border)] hover:border-[#2a2a2a]'"
            @click="logoInput?.click()"
            @dragover.prevent="isDragging = true"
            @dragleave.prevent="isDragging = false"
            @drop="onLogoDrop"
          >
            <div class="w-10 h-10 rounded-full flex items-center justify-center mb-1" style="background: var(--surface-raised)">
              <svg class="w-5 h-5 text-[var(--text-muted)]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                  d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5m-13.5-9L12 3m0 0l4.5 4.5M12 3v13.5" />
              </svg>
            </div>
            <p class="text-sm text-[var(--text-secondary)]">
              Drop an image or <span style="color: var(--accent)">browse</span>
            </p>
            <p class="text-xs text-[var(--text-muted)]">PNG, JPG, SVG — max 2 MB</p>
            <input
              ref="logoInput"
              type="file"
              accept="image/*"
              class="sr-only"
              @change="onLogoChange"
            />
          </div>
        </div>

        <!-- Name -->
        <div class="mb-4">
          <label class="label mb-2 block">League Name</label>
          <input
            v-model="name"
            type="text"
            placeholder="e.g. The Blade Falls 2025"
            class="input"
            maxlength="100"
            required
          />
        </div>

        <!-- Season -->
        <div>
          <label class="label mb-2 block">Season</label>
          <select v-model="season" class="input" style="max-width: 10rem">
            <option v-for="s in seasons" :key="s" :value="s">{{ s }}</option>
          </select>
        </div>
      </section>

      <hr class="divider" />

      <!-- ── Format ── -->
      <section>
        <h2 class="text-sm font-semibold uppercase tracking-widest text-[var(--text-muted)] mb-5">Scoring Format</h2>

        <div class="grid grid-cols-3 gap-3 mb-1">
          <button
            v-for="preset in presets"
            :key="preset.key"
            type="button"
            class="card p-4 text-left transition-colors cursor-pointer"
            :style="scoringPreset === preset.key
              ? 'border-color: var(--accent); background: var(--accent-dim); color: var(--text)'
              : 'color: var(--text-secondary)'"
            @click="scoringPreset = preset.key"
          >
            <p class="font-semibold text-sm mb-0.5">{{ preset.label }}</p>
            <p class="text-xs text-[var(--text-muted)]">{{ preset.sub }}</p>
          </button>
        </div>
        <p class="text-xs text-[var(--text-muted)] mt-3">
          Advanced scoring rules (passing TDs, rushing TDs, etc.) can be customized after the league is created.
        </p>
      </section>

      <hr class="divider" />

      <!-- ── Roster ── -->
      <section>
        <div class="flex items-baseline justify-between mb-5">
          <h2 class="text-sm font-semibold uppercase tracking-widest text-[var(--text-muted)]">Roster Settings</h2>
          <span class="text-xs text-[var(--text-muted)]">
            {{ Object.values(slots).reduce((a, b) => a + b, 0) }} total spots
          </span>
        </div>

        <div class="grid grid-cols-2 sm:grid-cols-3 gap-3 mb-6">
          <div v-for="(_, pos) in slots" :key="pos" class="card-raised p-3 flex items-center justify-between gap-3">
            <span class="text-xs font-medium text-[var(--text-secondary)]">{{ pos }}</span>
            <div class="flex items-center gap-2">
              <button
                type="button"
                class="w-6 h-6 rounded flex items-center justify-center text-sm font-bold leading-none transition-colors"
                style="background: var(--surface); color: var(--text-muted)"
                :disabled="slots[pos as keyof typeof slots] === 0"
                @click="slots[pos as keyof typeof slots]--"
              >−</button>
              <span class="text-sm w-4 text-center tabular-nums">{{ slots[pos as keyof typeof slots] }}</span>
              <button
                type="button"
                class="w-6 h-6 rounded flex items-center justify-center text-sm font-bold leading-none transition-colors"
                style="background: var(--surface); color: var(--text-muted)"
                @click="slots[pos as keyof typeof slots]++"
              >+</button>
            </div>
          </div>
        </div>

        <!-- Team count -->
        <div>
          <label class="label mb-2 block">Teams in League</label>
          <div class="flex items-center gap-3">
            <input
              v-model.number="teamCount"
              type="range"
              min="4"
              max="20"
              step="2"
              class="flex-1 accent-[var(--accent)]"
            />
            <span class="text-sm font-semibold w-8 text-right tabular-nums">{{ teamCount }}</span>
          </div>
          <p class="text-xs text-[var(--text-muted)] mt-1">4 – 20 teams</p>
        </div>
      </section>

      <hr class="divider" />

      <!-- ── Waivers ── -->
      <section>
        <h2 class="text-sm font-semibold uppercase tracking-widest text-[var(--text-muted)] mb-5">Waiver Wire</h2>

        <!-- FAAB toggle -->
        <div class="flex items-start justify-between gap-6 mb-5">
          <div>
            <p class="text-sm font-medium mb-0.5">Free Agent Acquisition Budget (FAAB)</p>
            <p class="text-xs text-[var(--text-muted)]">
              Teams bid blindly on free agents each week. Without FAAB, claims process in priority order.
            </p>
          </div>
          <!-- Toggle switch -->
          <button
            type="button"
            class="relative shrink-0 w-11 h-6 rounded-full transition-colors duration-200 focus:outline-none mt-0.5"
            :style="useFaab
              ? 'background: var(--accent)'
              : 'background: var(--border)'"
            :aria-checked="useFaab"
            role="switch"
            @click="useFaab = !useFaab"
          >
            <span
              class="absolute top-0.5 left-0.5 w-5 h-5 rounded-full bg-white shadow transition-transform duration-200"
              :style="useFaab ? 'transform: translateX(1.25rem)' : ''"
            />
          </button>
        </div>

        <!-- Budget amount (shown when FAAB is on) -->
        <Transition name="slide">
          <div v-if="useFaab" class="card p-4">
            <label class="label mb-3 block">Starting FAAB Budget</label>
            <div class="flex items-center gap-4">
              <div class="flex items-center gap-2 flex-1 max-w-xs">
                <span class="text-[var(--text-muted)] font-medium">$</span>
                <input
                  v-model.number="faabBudget"
                  type="number"
                  min="1"
                  max="10000"
                  step="1"
                  class="input flex-1"
                />
              </div>
              <!-- Quick presets -->
              <div class="flex gap-2">
                <button
                  v-for="preset in [50, 100, 200, 500]"
                  :key="preset"
                  type="button"
                  class="text-xs px-2.5 py-1 rounded transition-colors"
                  :style="faabBudget === preset
                    ? 'background: var(--accent-dim); color: var(--accent); border: 1px solid color-mix(in srgb, var(--accent) 30%, transparent)'
                    : 'background: var(--surface-raised); color: var(--text-muted); border: 1px solid var(--border)'"
                  @click="faabBudget = preset"
                >
                  ${{ preset }}
                </button>
              </div>
            </div>
            <p class="text-xs text-[var(--text-muted)] mt-2">
              Each team starts with ${{ faabBudget }} to spend on free agents over the season. Unspent budget does not carry over.
            </p>

            <hr class="my-4" style="border-color: var(--border-subtle)" />

            <label class="label mb-3 block">Minimum Bid</label>
            <div class="flex items-center gap-3">
              <div class="flex items-center gap-2 max-w-xs">
                <span class="text-[var(--text-muted)] font-medium">$</span>
                <input
                  v-model.number="minFaabBid"
                  type="number"
                  min="0"
                  :max="faabBudget"
                  step="1"
                  class="input w-24"
                />
              </div>
              <div class="flex gap-2">
                <button
                  v-for="preset in [0, 1]"
                  :key="preset"
                  type="button"
                  class="text-xs px-2.5 py-1 rounded transition-colors"
                  :style="minFaabBid === preset
                    ? 'background: var(--accent-dim); color: var(--accent); border: 1px solid color-mix(in srgb, var(--accent) 30%, transparent)'
                    : 'background: var(--surface-raised); color: var(--text-muted); border: 1px solid var(--border)'"
                  @click="minFaabBid = preset"
                >
                  {{ preset === 0 ? '$0 (free pickups)' : '$1 minimum' }}
                </button>
              </div>
            </div>
            <p class="text-xs text-[var(--text-muted)] mt-2">
              $0 allows free pickups when no one else bids. $1+ requires managers to commit budget on every claim.
            </p>
          </div>
        </Transition>

        <p v-if="!useFaab" class="text-xs text-[var(--text-muted)] mt-1">
          Priority waivers: the team with the highest waiver priority gets first pick of free agents each week.
        </p>
      </section>

      <hr class="divider" />

      <!-- Submit -->
      <div>
        <p v-if="submitError" class="text-sm text-[var(--red)] mb-4">{{ submitError }}</p>
        <div class="flex items-center gap-3">
          <button
            type="submit"
            class="btn btn-primary px-6 py-2.5"
            :disabled="!canSubmit || isSubmitting"
          >
            {{ isSubmitting ? 'Creating…' : 'Create League' }}
          </button>
          <RouterLink to="/" class="btn btn-ghost px-5 py-2.5">Cancel</RouterLink>
        </div>
        <p class="text-xs text-[var(--text-muted)] mt-3">
          You'll be taken to the league page to invite teams after creation.
        </p>
      </div>

    </form>
  </div>
</template>

<style scoped>
.slide-enter-active,
.slide-leave-active {
  transition: all 0.2s ease;
  overflow: hidden;
}
.slide-enter-from,
.slide-leave-to {
  opacity: 0;
  max-height: 0;
  margin-top: 0;
}
.slide-enter-to,
.slide-leave-from {
  opacity: 1;
  max-height: 200px;
}
</style>
