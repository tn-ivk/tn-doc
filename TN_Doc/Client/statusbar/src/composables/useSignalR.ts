import { ref, onMounted, onUnmounted } from 'vue';
import * as signalR from '@microsoft/signalr';
import type { ConnectionState } from '../types/status.types';
import { useStatusLogging } from './useStatusLogging';

export function useSignalR(hubUrl: string) {
  const connection = ref<signalR.HubConnection | null>(null);
  const connectionState = ref<ConnectionState>('disconnected');
  const error = ref<string | null>(null);
  const { log } = useStatusLogging();

  // Очередь отложенных подписок, которые будут применены после подключения
  const pendingSubscriptions = new Map<string, ((...args: any[]) => void)[]>();

  async function connect() {
    try {
      connectionState.value = 'connecting';
      log('info', `Attempting to connect to SignalR hub: ${hubUrl}`);

      connection.value = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          withCredentials: true,
          skipNegotiation: false
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            const delay = retryContext.elapsedMilliseconds < 60000 ? 5000 : 30000;
            log('warn', `SignalR reconnection attempt #${retryContext.previousRetryCount + 1} in ${delay}ms`);
            return delay;
          }
        })
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      connection.value.onreconnecting((err) => {
        connectionState.value = 'connecting';
        log('warn', 'SignalR connection lost, attempting to reconnect', err?.message);
      });

      connection.value.onreconnected((connectionId) => {
        connectionState.value = 'connected';
        log('info', `SignalR reconnected successfully with ID: ${connectionId}`);
      });

      connection.value.onclose((err) => {
        connectionState.value = 'disconnected';
        if (err) {
          log('error', 'SignalR connection closed with error', err.message);
        } else {
          log('info', 'SignalR connection closed normally');
        }
      });

      await connection.value.start();
      connectionState.value = 'connected';
      error.value = null;
      log('info', `SignalR connected successfully to ${hubUrl} with ID: ${connection.value.connectionId}`);

      // Применяем все отложенные подписки после успешного подключения
      if (pendingSubscriptions.size > 0) {
        log('debug', `Applying ${pendingSubscriptions.size} pending subscription(s)`);
        pendingSubscriptions.forEach((callbacks, eventName) => {
          callbacks.forEach(callback => {
            connection.value!.on(eventName, callback);
          });
        });
        pendingSubscriptions.clear();
      }
    } catch (err) {
      connectionState.value = 'disconnected';
      error.value = err instanceof Error ? err.message : 'Connection failed';
      log('error', 'SignalR connection failed', err);
    }
  }

  function on(eventName: string, callback: (...args: any[]) => void) {
    if (!connection.value || connectionState.value !== 'connected') {
      // Добавляем подписку в очередь, если подключение еще не установлено
      if (!pendingSubscriptions.has(eventName)) {
        pendingSubscriptions.set(eventName, []);
      }
      pendingSubscriptions.get(eventName)!.push(callback);
      log('debug', `Queued subscription for event ${eventName} (connection not ready)`);
      return;
    }

    // Подключение установлено - подписываемся сразу
    connection.value.on(eventName, callback);
    log('debug', `Subscribed to SignalR event: ${eventName}`);
  }

  function off(eventName: string, callback?: (...args: any[]) => void) {
    if (!connection.value) return;

    if (callback) {
      connection.value.off(eventName, callback);
    } else {
      connection.value.off(eventName);
    }
    log('debug', `Unsubscribed from SignalR event: ${eventName}`);
  }

  async function disconnect() {
    if (connection.value) {
      await connection.value.stop();
      connection.value = null;
      connectionState.value = 'disconnected';
      log('info', 'SignalR connection manually disconnected');
    }
  }

  onMounted(() => {
    connect();
  });

  onUnmounted(() => {
    disconnect();
  });

  return {
    connection,
    connectionState,
    error,
    connect,
    disconnect,
    on,
    off
  };
}