import axios, { type AxiosInstance } from 'axios';
import type { CfgApp, ValidationResult } from '../types/config.types';

/**
 * API сервис для работы с конфигурацией
 */
class ApiService {
  private api: AxiosInstance;

  constructor() {
    this.api = axios.create({
      baseURL: '/api',
      headers: {
        'Content-Type': 'application/json'
      }
    });
  }

  /**
   * Получить конфигурацию приложения
   */
  async getConfig(): Promise<CfgApp> {
    const response = await this.api.get<CfgApp>('/configurator/config');
    return response.data;
  }

  /**
   * Сохранить конфигурацию приложения
   */
  async saveConfig(config: CfgApp): Promise<void> {
    await this.api.post('/configurator/config', config);
  }

  /**
   * Валидировать конфигурацию
   */
  async validateConfig(config: CfgApp): Promise<ValidationResult> {
    const response = await this.api.post<ValidationResult>('/configurator/validate', config);
    return response.data;
  }
}

export default new ApiService();
