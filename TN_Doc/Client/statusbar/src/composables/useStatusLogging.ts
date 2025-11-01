/**
 * @deprecated Используйте logger из @tn-doc/shared
 *
 * Этот composable устарел и оставлен для обратной совместимости.
 * Вместо него используйте:
 *
 * import { logger } from '@tn-doc/shared';
 * logger.info('сообщение', { context });
 */

export type LogLevel = 'debug' | 'info' | 'warn' | 'error';

interface LogEntry {
  level: LogLevel;
  message: string;
  data?: any;
  timestamp?: string;
}

export function useStatusLogging() {
  /**
   * Отправка лога на сервер TN_Doc
   */
  async function logToServer(entry: LogEntry) {
    try {
      const logData = {
        Level: entry.level.charAt(0).toUpperCase() + entry.level.slice(1),
        Message: `[StatusBar] ${entry.message}${entry.data ? ` | Data: ${JSON.stringify(entry.data)}` : ''}`
      };

      // Интеграция с существующим ClientLogController TN_Doc
      await fetch('/api/ClientLog/logging', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(logData)
      });
    } catch (error) {
      // Не логируем ошибки логирования, чтобы избежать циклов
      console.error('Failed to send log to server:', error);
    }
  }

  /**
   * Логирование в консоль и на сервер
   */
  function log(level: LogLevel, message: string, data?: any) {
    const entry: LogEntry = { level, message, data };

    // Логирование в консоль
    switch (level) {
      case 'debug':
        console.debug(`[StatusBar] ${message}`, data);
        break;
      case 'info':
        console.info(`[StatusBar] ${message}`, data);
        break;
      case 'warn':
        console.warn(`[StatusBar] ${message}`, data);
        break;
      case 'error':
        console.error(`[StatusBar] ${message}`, data);
        break;
    }

    // Отправка на сервер (только для info, warn, error)
    if (level !== 'debug') {
      logToServer(entry).catch(() => {
        // Игнорируем ошибки отправки логов
      });
    }
  }

  return {
    log,
    logToServer
  };
}