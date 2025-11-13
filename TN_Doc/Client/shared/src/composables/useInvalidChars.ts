/**
 * Composable для работы с недопустимыми символами устройства
 *
 * Предоставляет кэшированный доступ к списку InvalidChars для каждого устройства.
 * Кэш на уровне модуля - переживает пересоздание компонентов и store.
 */

import { logger } from '../logger';

// Глобальный кэш на уровне модуля (shared между всеми вызовами useInvalidChars)
const cache = new Map<number, string[]>();

/**
 * Composable для работы с InvalidChars
 */
export function useInvalidChars() {
  /**
   * Получить список недопустимых символов для устройства
   *
   * @param deviceId ID устройства
   * @returns Массив недопустимых символов
   *
   * @example
   * const { getInvalidChars } = useInvalidChars();
   * const chars = await getInvalidChars(1); // ['"', "'", "\\"]
   */
  async function getInvalidChars(deviceId: number): Promise<string[]> {
    // Проверяем кэш
    if (cache.has(deviceId)) {
      return cache.get(deviceId)!;
    }

    // Загружаем с сервера
    try {
      const response = await fetch(`/Home/GetInvalideChars?IdDevice=${deviceId}`, {
        credentials: 'same-origin',
        headers: {
          'Accept': 'application/json',
          'X-Requested-With': 'XMLHttpRequest'
        }
      });

      if (!response.ok) {
        logger.error('[useInvalidChars] Ошибка HTTP', {
          deviceId,
          status: response.status,
          statusText: response.statusText
        });
        return [];
      }

      const data = await response.json();

      // Сервер может вернуть массив напрямую или JSON строку
      const chars: string[] = Array.isArray(data)
        ? data
        : (typeof data === 'string' && data ? JSON.parse(data) : []);

      // Кэшируем результат
      cache.set(deviceId, chars);

      return chars;
    } catch (error) {
      logger.error('[useInvalidChars] Ошибка загрузки', {
        deviceId,
        error: error instanceof Error ? error.message : String(error)
      });
      return [];
    }
  }

  /**
   * Очистить кэш InvalidChars
   *
   * Используется когда InvalidChars изменяются в конфигураторе,
   * чтобы другие приложения получили актуальные данные.
   *
   * @param deviceId ID устройства (если не указан - очистится весь кэш)
   *
   * @example
   * const { clearCache } = useInvalidChars();
   *
   * // Очистить кэш для конкретного устройства
   * clearCache(1);
   *
   * // Очистить весь кэш
   * clearCache();
   */
  function clearCache(deviceId?: number): void {
    if (deviceId !== undefined) {
      cache.delete(deviceId);
    } else {
      cache.clear();
    }
  }

  /**
   * Получить размер кэша (для отладки)
   */
  function getCacheSize(): number {
    return cache.size;
  }

  return {
    getInvalidChars,
    clearCache,
    getCacheSize
  };
}
