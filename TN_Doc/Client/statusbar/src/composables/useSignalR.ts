import { ref, onMounted, onUnmounted } from 'vue';
import * as signalR from '@microsoft/signalr';
import { logger } from '@tn-doc/shared';
import type { ConnectionState } from '../types/status.types';

export function useSignalR(hubUrl: string) {
  const connection = ref<signalR.HubConnection | null>(null);
  const connectionState = ref<ConnectionState>('disconnected');
  const error = ref<string | null>(null);

  // Очередь отложенных подписок, которые будут применены после подключения
  const pendingSubscriptions = new Map<string, ((...args: any[]) => void)[]>();

  async function connect() {
    try {
      connectionState.value = 'connecting';
      logger.info(`SignalR: попытка подключения к хабу`, { hubUrl });

      connection.value = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          withCredentials: true,
          skipNegotiation: false
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            const delay = retryContext.elapsedMilliseconds < 60000 ? 5000 : 30000;
            logger.warn(`SignalR: попытка переподключения #${retryContext.previousRetryCount + 1} через ${delay}ms`);
            return delay;
          }
        })
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      connection.value.onreconnecting((err) => {
        connectionState.value = 'connecting';
        logger.warn('SignalR: соединение потеряно, попытка переподключения', {
          error: err?.message
        });
      });

      connection.value.onreconnected((connectionId) => {
        connectionState.value = 'connected';
        logger.info('SignalR: успешно переподключен', { connectionId });
      });

      connection.value.onclose((err) => {
        connectionState.value = 'disconnected';
        if (err) {
          logger.error('SignalR: соединение закрыто с ошибкой', {
            error: err.message
          });
        } else {
          logger.info('SignalR: соединение закрыто штатно');
        }
      });

      await connection.value.start();
      connectionState.value = 'connected';
      error.value = null;
      logger.info('SignalR: успешно подключен', {
        hubUrl,
        connectionId: connection.value.connectionId
      });

      // Применяем все отложенные подписки после успешного подключения
      if (pendingSubscriptions.size > 0) {
        logger.debug(`SignalR: применение ${pendingSubscriptions.size} отложенных подписок`);
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
      logger.error('SignalR: ошибка подключения', {
        hubUrl,
        error: err instanceof Error ? err.message : String(err)
      });
    }
  }

  function on(eventName: string, callback: (...args: any[]) => void) {
    if (!connection.value || connectionState.value !== 'connected') {
      // Добавляем подписку в очередь, если подключение еще не установлено
      if (!pendingSubscriptions.has(eventName)) {
        pendingSubscriptions.set(eventName, []);
      }
      pendingSubscriptions.get(eventName)!.push(callback);
      logger.debug(`SignalR: подписка на событие ${eventName} отложена (соединение не готово)`);
      return;
    }

    // Подключение установлено - подписываемся сразу
    connection.value.on(eventName, callback);
    logger.debug(`SignalR: подписка на событие ${eventName} активна`);
  }

  function off(eventName: string, callback?: (...args: any[]) => void) {
    if (!connection.value) return;

    if (callback) {
      connection.value.off(eventName, callback);
    } else {
      connection.value.off(eventName);
    }
    logger.debug(`SignalR: отписка от события ${eventName}`);
  }

  async function disconnect() {
    if (connection.value) {
      await connection.value.stop();
      connection.value = null;
      connectionState.value = 'disconnected';
      logger.info('SignalR: соединение вручную закрыто');
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