/**
 * API клиент для взаимодействия с TN_Doc backend
 */

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
  }

  /**
   * GET запрос с опциональным кэшированием
   */
  async get<T>(endpoint: string, useCache = false): Promise<T> {
    if (useCache) {
      const cached = this.cache.get(endpoint);
      if (cached && Date.now() - cached.timestamp < this.cacheTimeout) {
        return cached.data as T;
      }
    }

    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      credentials: 'same-origin',
      headers: {
        'Accept': 'application/json',
        'X-Requested-With': 'XMLHttpRequest'
      }
    });

    if (!response.ok) {
      throw new Error(
        `API Error: ${response.status} ${response.statusText}`
      );
    }

    const data = await response.json();

    if (useCache) {
      this.cache.set(endpoint, { data, timestamp: Date.now() });
    }

    return data as T;
  }

  /**
   * POST запрос
   */
  async post<T, D = any>(
    endpoint: string,
    data?: D
  ): Promise<T> {
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
      throw new Error(
        `API Error: ${response.status} ${response.statusText}`
      );
    }

    return response.json() as Promise<T>;
  }

  /**
   * Очистка кэша
   */
  clearCache(endpoint?: string): void {
    if (endpoint) {
      this.cache.delete(endpoint);
    } else {
      this.cache.clear();
    }
  }
}

export const apiClient = new ApiClient();