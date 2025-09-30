import { ref, onMounted, onUnmounted } from 'vue'
import * as signalR from '@microsoft/signalr'

export function useSignalR(hubUrl: string) {
  const connection = ref<signalR.HubConnection | null>(null)
  const connectionState = ref<'disconnected' | 'connecting' | 'connected'>('disconnected')
  const error = ref<string | null>(null)

  async function connect() {
    try {
      connectionState.value = 'connecting'

      connection.value = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, { withCredentials: true })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            const delay = retryContext.elapsedMilliseconds < 60000 ? 5000 : 30000
            return delay
          }
        })
        .configureLogging(signalR.LogLevel.Warning)
        .build()

      connection.value.onreconnecting(() => {
        connectionState.value = 'connecting'
      })

      connection.value.onreconnected(() => {
        connectionState.value = 'connected'
      })

      connection.value.onclose(() => {
        connectionState.value = 'disconnected'
      })

      await connection.value.start()
      connectionState.value = 'connected'
      error.value = null
    } catch (err) {
      connectionState.value = 'disconnected'
      error.value = err instanceof Error ? err.message : 'Connection failed'
    }
  }

  function on(eventName: string, callback: (...args: any[]) => void) {
    connection.value?.on(eventName, callback)
  }

  function off(eventName: string, callback?: (...args: any[]) => void) {
    if (callback) {
      connection.value?.off(eventName, callback)
    } else {
      connection.value?.off(eventName)
    }
  }

  async function disconnect() {
    if (connection.value) {
      await connection.value.stop()
      connection.value = null
      connectionState.value = 'disconnected'
    }
  }

  onMounted(() => { void connect() })
  onUnmounted(() => { void disconnect() })

  return {
    connection,
    connectionState,
    error,
    connect,
    disconnect,
    on,
    off
  } as const
}


