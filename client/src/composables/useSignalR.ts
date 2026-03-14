import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr'
import { ref, onUnmounted } from 'vue'
import { useScoringStore } from '../stores/scoring'

export function useSignalR(leagueId: string) {
  const connection = ref<HubConnection | null>(null)
  const scoringStore = useScoringStore()

  async function connect() {
    connection.value = new HubConnectionBuilder()
      .withUrl('/hubs/scoring')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.value.on('ScoreUpdated', (score) => scoringStore.updateScore(score))

    await connection.value.start()
    await connection.value.invoke('JoinLeague', leagueId)
  }

  async function disconnect() {
    if (connection.value) {
      await connection.value.invoke('LeaveLeague', leagueId)
      await connection.value.stop()
    }
  }

  onUnmounted(disconnect)

  return { connect, disconnect }
}
