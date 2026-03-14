import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { TeamScore } from '../api/types'

export const useScoringStore = defineStore('scoring', () => {
  const scores = ref<TeamScore[]>([])

  function updateScore(score: TeamScore) {
    const idx = scores.value.findIndex(s => s.teamId === score.teamId)
    if (idx >= 0) scores.value[idx] = score
    else scores.value.push(score)
  }

  return { scores, updateScore }
})
