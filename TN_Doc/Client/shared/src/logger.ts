/**
 * Модуль логирования для Vue-компонентов TN_Doc
 * Отправляет логи на сервер через API /api/ClientLog/logging
 *
 * @example
 * import { logger } from '@tn-doc/shared';
 * logger.info('Компонент загружен');
 * logger.error('Ошибка загрузки данных', { deviceId: 123 });
 */

type LogLevel = 'Trace' | 'Debug' | 'Info' | 'Warn' | 'Error';

interface LogEntry {
  level: LogLevel;
  message: string;
  context?: Record<string, any>;
}

interface LoggerConfig {
  /** Базовый URL API (по умолчанию '') */
  endpoint?: string;
  /** Логировать в консоль дополнительно к серверу (по умолчанию true в dev) */
  consoleLog?: boolean;
  /** Отключить отправку на сервер (полезно для тестов) */
  disabled?: boolean;
  /** Добавлять глобальный контекст ко всем логам (например, userId, app version) */
  globalContext?: Record<string, any>;
}

class Logger {
  private readonly endpoint: string;
  private consoleLog: boolean;
  private disabled: boolean;
  private globalContext: Record<string, any>;

  constructor(config: LoggerConfig = {}) {
    this.endpoint = config.endpoint || '/api/ClientLog/logging';
    // По умолчанию включаем консольное логирование в dev режиме
    const isDev = typeof import.meta !== 'undefined' && (import.meta as any).env?.DEV;
    this.consoleLog = config.consoleLog ?? isDev ?? true;
    this.disabled = config.disabled ?? false;
    this.globalContext = config.globalContext || {};
  }

  /**
   * Установить глобальный контекст (например, после входа пользователя)
   */
  setGlobalContext(context: Record<string, any>): void {
    this.globalContext = { ...this.globalContext, ...context };
  }

  /**
   * Очистить глобальный контекст
   */
  clearGlobalContext(): void {
    this.globalContext = {};
  }

  /**
   * Получить текущий глобальный контекст
   */
  getGlobalContext(): Record<string, any> {
    return { ...this.globalContext };
  }

  private async sendLog(level: LogLevel, message: string, context?: Record<string, any>): Promise<void> {
    // Логирование в консоль
    if (this.consoleLog) {
      const consoleMethod = level === 'Error' ? 'error' : level === 'Warn' ? 'warn' : 'info';
      console[consoleMethod](`[${level}] ${message}`, context || '');
    }

    // Если логирование отключено, не отправляем на сервер
    if (this.disabled) {
      return;
    }

    try {
      // Объединяем контекст с глобальным
      const fullContext = { ...this.globalContext, ...context };

      // Формируем сообщение с контекстом для сервера
      // Если есть контекст, добавляем его в сообщение как JSON
      let fullMessage = message;
      if (Object.keys(fullContext).length > 0) {
        fullMessage = `${message} | ${JSON.stringify(fullContext)}`;
      }

      // Отправляем только level и message (без context), так как сервер не поддерживает context
      const payload = {
        level,
        message: fullMessage
      };

      await fetch(this.endpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Requested-With': 'XMLHttpRequest'
        },
        credentials: 'same-origin',
        body: JSON.stringify(payload),
      });
    } catch (error) {
      // При закрытии страницы fetch обрывается с "Failed to fetch" или AbortError
      // Это нормальное поведение, не засоряем консоль предупреждениями
      // Fallback на консоль только если consoleLog отключен и это не обрыв соединения
      const isAbortedOrNetworkError =
        error instanceof TypeError ||
        (error instanceof DOMException && error.name === 'AbortError');

      if (!this.consoleLog && !isAbortedOrNetworkError) {
        console.warn('Не удалось отправить лог на сервер:', message, error);
      }
    }
  }

  /**
   * Трассировка - наиболее детальные логи
   */
  trace(message: string, context?: Record<string, any>): void {
    this.sendLog('Trace', message, context);
  }

  /**
   * Отладочная информация
   */
  debug(message: string, context?: Record<string, any>): void {
    this.sendLog('Debug', message, context);
  }

  /**
   * Информационное сообщение
   */
  info(message: string, context?: Record<string, any>): void {
    this.sendLog('Info', message, context);
  }

  /**
   * Предупреждение
   */
  warn(message: string, context?: Record<string, any>): void {
    this.sendLog('Warn', message, context);
  }

  /**
   * Ошибка
   */
  error(message: string, context?: Record<string, any>): void {
    this.sendLog('Error', message, context);
  }

  /**
   * Логирование исключения с полным stack trace
   */
  exception(error: Error, message?: string, context?: Record<string, any>): void {
    this.sendLog('Error', message || error.message, {
      ...context,
      errorName: error.name,
      errorMessage: error.message,
      stack: error.stack
    });
  }
}

// Экспортируем singleton instance
export const logger = new Logger();

// Экспортируем класс для создания дополнительных инстансов с кастомной конфигурацией
export { Logger, type LogLevel, type LogEntry, type LoggerConfig };
