import { logger } from '@tn-doc/shared';
import axios, { type AxiosInstance } from 'axios';
import type { DocumentEditConfig, SaveDocumentRequest, SaveDocumentResponse } from '@/types/document.types';

/**
 * API клиент для взаимодействия с DocumentEditController
 */
class DocumentApiService {
  private api: AxiosInstance;

  constructor() {
    this.api = axios.create({
      baseURL: '/api/documents',
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json'
      }
    });

    // Интерсептор для обработки ошибок запросов
    this.api.interceptors.request.use(
      (config) => config,
      (error) => Promise.reject(error)
    );

    // Интерсептор для обработки ошибок ответов
    this.api.interceptors.response.use(
      (response) => response,
      (error) => Promise.reject(error)
    );
  }

  /**
   * Проверка доступности API
   */
  async healthCheck(): Promise<boolean> {
    try {
      const response = await this.api.get('/health');
      return response.data.status === 'healthy';
    } catch (error) {
      logger.error('API: health check failed', {
        error: error instanceof Error ? error.message : String(error)
      });
      return false;
    }
  }

  /**
   * Получить конфигурацию формы редактирования документа
   * @param deviceId ID устройства (целое число)
   * @param docType Тип документа (Report, Act, Passport и т.д.)
   * @param id ID документа
   */
  async getEditConfig(
    deviceId: number,
    docType: string,
    id: number
  ): Promise<DocumentEditConfig> {
    const url = `/${deviceId}/${docType}/edit/${id}`;
    logger.debug('API: getEditConfig запрос', { deviceId, docType, id, url });

    const response = await this.api.get<DocumentEditConfig>(url);

    logger.debug('API: getEditConfig ответ получен');
    logger.debug('API: getEditConfig статус', { status: response.status });
    logger.trace('API: getEditConfig данные', { data: response.data });

    // Детальная проверка fields и initialValues
    if (response.data) {
      logger.debug('API: getEditConfig количество полей', { count: response.data.fields?.length });
      logger.debug('API: getEditConfig количество initialValues', { count: Object.keys(response.data.initialValues || {}).length });

      // Ищем поля, связанные с датой/временем
      const dateFields = response.data.fields?.filter(f =>
        f.label?.toLowerCase().includes('дата') ||
        f.label?.toLowerCase().includes('время') ||
        f.key?.toLowerCase().includes('date') ||
        f.key?.toLowerCase().includes('time')
      );

      if (dateFields && dateFields.length > 0) {
        logger.debug('API: getEditConfig найдены поля даты/времени', {
          fields: dateFields.map(f => ({
            key: f.key,
            label: f.label,
            type: f.type,
            initialValue: response.data.initialValues?.[f.key]
          }))
        });
      } else {
        logger.warn('API: getEditConfig не найдены поля даты/времени');
      }
    }

    return response.data;
  }

  /**
   * Сохранить изменения документа
   * @param deviceId ID устройства (целое число)
   * @param docType Тип документа
   * @param id ID документа
   * @param data JSON данные документа
   */
  async saveDocument(
    deviceId: number,
    docType: string,
    id: number,
    data: SaveDocumentRequest
  ): Promise<SaveDocumentResponse> {
    const response = await this.api.post<SaveDocumentResponse>(
      `/${deviceId}/${docType}/save/${id}`,
      data
    );
    return response.data;
  }

  /**
   * Обновить документ (используется после успешной записи в OPC тег)
   * @param deviceId ID устройства (целое число)
   * @param docType Тип документа
   * @param id ID документа
   * @param data JSON данные документа
   */
  async updateDocument(
    deviceId: number,
    docType: string,
    id: number,
    data: SaveDocumentRequest
  ): Promise<SaveDocumentResponse> {
    logger.debug('API: updateDocument запрос', { deviceId, docType, id });
    const response = await this.api.post<SaveDocumentResponse>(
      `/${deviceId}/${docType}/update/${id}`,
      data
    );
    logger.info('API: updateDocument успешно', { deviceId, docType, id });
    return response.data;
  }

}

// Экспортируем синглтон
export const documentApi = new DocumentApiService();
