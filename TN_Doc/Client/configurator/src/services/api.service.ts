import axios, { type AxiosInstance } from 'axios';
import type { CfgApp, ValidationResult } from '../types/config.types';

/**
 * Преобразовать camelCase в PascalCase для ключей объекта
 */
function toPascalCase(obj: any): any {
  if (obj === null || obj === undefined) return obj;
  if (Array.isArray(obj)) return obj.map(toPascalCase);
  if (typeof obj !== 'object') return obj;

  const result: any = {};
  for (const key in obj) {
    if (Object.prototype.hasOwnProperty.call(obj, key)) {
      const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
      result[pascalKey] = toPascalCase(obj[key]);
    }
  }
  return result;
}

/**
 * Преобразовать PascalCase в camelCase для ключей объекта
 */
function toCamelCase(obj: any): any {
  if (obj === null || obj === undefined) return obj;
  if (Array.isArray(obj)) return obj.map(toCamelCase);
  if (typeof obj !== 'object') return obj;

  const result: any = {};
  for (const key in obj) {
    if (Object.prototype.hasOwnProperty.call(obj, key)) {
      const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
      result[camelKey] = toCamelCase(obj[key]);
    }
  }
  return result;
}

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
    const response = await this.api.get('/configurator/config');
    // Преобразуем camelCase от API в PascalCase для TypeScript типов
    return toPascalCase(response.data) as CfgApp;
  }

  /**
   * Сохранить конфигурацию приложения
   */
  async saveConfig(config: CfgApp): Promise<void> {
    // Преобразуем PascalCase в camelCase для API
    await this.api.post('/configurator/config', toCamelCase(config));
  }

  /**
   * Валидировать конфигурацию
   */
  async validateConfig(config: CfgApp): Promise<ValidationResult> {
    // Преобразуем PascalCase в camelCase для API
    const response = await this.api.post('/configurator/validate', toCamelCase(config));
    // Преобразуем camelCase от API в PascalCase для TypeScript типов
    return toPascalCase(response.data) as ValidationResult;
  }
}

export default new ApiService();
