/**
 * API клиент для взаимодействия с TN_Doc backend
 */

import { logger } from '../logger';

interface CacheEntry<T> {
  data: T;
  timestamp: number;
}

export class ApiClient {
  private baseUrl: string;
  private cache = new Map<string, CacheEntry<any>>();
  private cacheTimeout: number;

  constructor(baseUrl = '', cacheTimeout = 5000) {
    this.baseUrl = baseUrl;
    this.cacheTimeout = cacheTimeout;
    logger.debug('ApiClient: инициализирован', { baseUrl, cacheTimeout });
  }

  /**
   * GET запрос с опциональным кэшированием
   */
  async get<T>(endpoint: string, useCache = false): Promise<T> {
    logger.debug('ApiClient: GET запрос', { endpoint, useCache });

    if (useCache) {
      const cached = this.cache.get(endpoint);
      if (cached && Date.now() - cached.timestamp < this.cacheTimeout) {
        logger.trace('ApiClient: GET из кэша', { endpoint });
        return cached.data as T;
      }
    }

    try {
      const response = await fetch(`${this.baseUrl}${endpoint}`, {
        credentials: 'same-origin',
        headers: {
          'Accept': 'application/json',
          'X-Requested-With': 'XMLHttpRequest'
        }
      });

      if (!response.ok) {
        logger.error('ApiClient: GET ошибка HTTP', {
          endpoint,
          status: response.status,
          statusText: response.statusText
        });
        throw new Error(
          `API Error: ${response.status} ${response.statusText}`
        );
      }

      const data = await response.json();

      logger.debug('ApiClient: GET успешно', {
        endpoint,
        dataSize: JSON.stringify(data).length
      });

      if (useCache) {
        this.cache.set(endpoint, { data, timestamp: Date.now() });
        logger.trace('ApiClient: результат сохранен в кэш', { endpoint });
      }

      return data as T;
    } catch (error) {
      logger.error('ApiClient: GET исключение', {
        endpoint,
        error: error instanceof Error ? error.message : String(error)
      });
      throw error;
    }
  }

  /**
   * POST запрос
   */
  async post<T, D = any>(
    endpoint: string,
    data?: D
  ): Promise<T> {
    logger.debug('ApiClient: POST запрос', {
      endpoint,
      hasData: !!data,
      dataSize: data ? JSON.stringify(data).length : 0
    });

    try {
      const response = await fetch(`${this.baseUrl}${endpoint}`, {
        method: 'POST',
        credentials: 'same-origin',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
          'X-Requested-With': 'XMLHttpRequest'
        },
        body: data ? JSON.stringify(data) : undefined
      });

      if (!response.ok) {
        logger.error('ApiClient: POST ошибка HTTP', {
          endpoint,
          status: response.status,
          statusText: response.statusText
        });
        throw new Error(
          `API Error: ${response.status} ${response.statusText}`
        );
      }

      const result = await response.json();

      logger.debug('ApiClient: POST успешно', {
        endpoint,
        responseSize: JSON.stringify(result).length
      });

      return result as T;
    } catch (error) {
      logger.error('ApiClient: POST исключение', {
        endpoint,
        error: error instanceof Error ? error.message : String(error)
      });
      throw error;
    }
  }

  /**
   * Очистка кэша
   */
  clearCache(endpoint?: string): void {
    if (endpoint) {
      this.cache.delete(endpoint);
      logger.debug('ApiClient: очищен кэш для endpoint', { endpoint });
    } else {
      const cacheSize = this.cache.size;
      this.cache.clear();
      logger.debug('ApiClient: очищен весь кэш', { entriesCleared: cacheSize });
    }
  }
}

export const apiClient = new ApiClient();