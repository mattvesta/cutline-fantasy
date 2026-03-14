<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { leaguesApi } from '../api/leagues'
import { useSignalR } from '../composables/useSignalR'
import type { League } from '../api/types'

const leagues = ref<League[]>([])
const isLoading = ref(true)
const error = ref<string | null>(null)

const hubStatus = ref<'disconnected' | 'connecting' | 'connected' | 'error'>('disconnected')
let signalR: ReturnType<typeof useSignalR> | null = null

onMounted(async () => {
  try {
    leagues.value = await leaguesApi.getAll()
  } catch {
    error.value = 'Failed to load leagues'
  } finally {
    isLoading.value = false
  }
})

async function testHub(leagueId: string) {
  hubStatus.value = 'connecting'
  try {
    signalR = useSignalR(leagueId)
    await signalR.connect()
    hubStatus.value = 'connected'
  } catch {
    hubStatus.value = 'error'
  }
}
</script>

<template>
  <main class="min-h-screen bg-black text-white p-8">
    <h1 class="text-4xl font-bold mb-2">Cutline Fantasy</h1>
    <p class="text-gray-400 mb-8">Will you make the cut?</p>

    <div v-if="isLoading" class="text-gray-400">Loading leagues...</div>
    <div v-else-if="error" class="text-red-400">{{ error }}</div>
    <div v-else-if="leagues.length === 0" class="text-gray-400">
      No leagues yet.
      <span class="text-sm ml-2">
        (Run <code class="bg-gray-800 px-1 rounded">POST /api/dev/seed</code> to create one)
      </span>
    </div>

    <ul v-else class="space-y-3">
      <li
        v-for="league in leagues"
        :key="league.id"
        class="flex items-center justify-between bg-gray-900 rounded-lg p-4"
      >
        <div>
          <p class="font-semibold">{{ league.name }}</p>
          <p class="text-sm text-gray-400">Season {{ league.season }} · {{ league.status }} · {{ league.teams.length }} teams</p>
        </div>
        <button
          class="text-xs px-3 py-1 rounded border border-gray-600 hover:border-white transition-colors"
          :class="{
            'text-yellow-400 border-yellow-400': hubStatus === 'connecting',
            'text-green-400 border-green-400': hubStatus === 'connected',
            'text-red-400 border-red-400': hubStatus === 'error',
            'text-gray-300': hubStatus === 'disconnected',
          }"
          @click="testHub(league.id)"
        >
          {{ hubStatus === 'disconnected' ? 'Test Hub' : hubStatus }}
        </button>
      </li>
    </ul>
  </main>
</template>
