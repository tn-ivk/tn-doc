export class ApiClient {
  private baseUrl: string;
  private cache = new Map<string, { data: any; timestamp: number }>();
  private cacheTimeout = 5000;

  constructor(baseUrl = '') {
    this.baseUrl = baseUrl;
  }

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
        'Accept': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error(`API Error: ${response.statusText}`);
    }

    const data = await response.json();

    if (useCache) {
      this.cache.set(endpoint, { data, timestamp: Date.now() });
    }

    return data as T;
  }
}

export const apiClient = new ApiClient();


