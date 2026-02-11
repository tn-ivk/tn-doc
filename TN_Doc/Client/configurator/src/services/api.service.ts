import axios, { type AxiosInstance, type AxiosError } from 'axios';
import { logger } from '@tn-doc/shared';
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
      
      // Специальная обработка для dbConnectionStrings
      if (key === 'dbConnectionStrings') {
        result['DBConnectionStrings'] = toPascalCase(obj[key]);
      }
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
      
      // Специальная обработка для DBConnectionStrings
      if (key === 'DBConnectionStrings') {
        result['dbConnectionStrings'] = toCamelCase(obj[key]);
      }
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

    // Логирование HTTP-ошибок через interceptor
    this.api.interceptors.response.use(
      (response) => response,
      (error: AxiosError) => {
        const url = error.config?.url || 'unknown';
        const method = error.config?.method?.toUpperCase() || 'unknown';

        if (error.response) {
          // Сервер ответил с ошибочным статусом
          logger.error(`ApiService: HTTP ${error.response.status} на ${method} ${url}`, {
            status: error.response.status,
            statusText: error.response.statusText,
            data: typeof error.response.data === 'string'
              ? error.response.data.substring(0, 500)
              : JSON.stringify(error.response.data)?.substring(0, 500)
          });
        } else if (error.request) {
          // Запрос отправлен, но ответ не получен
          logger.error(`ApiService: нет ответа на ${method} ${url}`, {
            message: error.message
          });
        } else {
          // Ошибка при формировании запроса
          logger.error(`ApiService: ошибка запроса ${method} ${url}`, {
            message: error.message
          });
        }

        return Promise.reject(error);
      }
    );
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

  /**
   * Загрузить конфигурационный файл документа
   */
  async loadDocumentConfig(configPath: string): Promise<string> {
    const response = await this.api.get('/configurator/document-config', {
      params: { path: configPath },
      responseType: 'text' // Получаем сырой JSON-текст, без автопарсинга Axios
    });
    return response.data;
  }

  /**
   * Сохранить конфигурационный файл документа
   */
  async saveDocumentConfig(configPath: string, content: string): Promise<void> {
    await this.api.post('/configurator/document-config', {
      path: configPath,
      content: content
    });
  }
}

export default new ApiService();
