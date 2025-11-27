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
    const response = await this.api.get<DocumentEditConfig>(url);
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

  /**
   * Добавить метод испытаний в справочник
   * @param editConfigFilePath Относительный путь к файлу конфигурации
   * @param parameterId ID параметра качества
   * @param methodName Название метода
   * @param isDefault Метод по умолчанию
   * @param limitValueActivate Активирован ли лимит
   * @param limitValue Пороговое значение
   * @param limitValueString Строка лимита
   * @returns ID добавленного метода
   */
  async addMethodToDictionary(
    editConfigFilePath: string,
    parameterId: number,
    methodName: string,
    isDefault = false,
    limitValueActivate = false,
    limitValue?: number,
    limitValueString?: string
  ): Promise<{ id: number }> {
    logger.debug('API: addMethodToDictionary запрос', { editConfigFilePath, parameterId, methodName });
    const response = await axios.post<{ id: number }>(
      '/direditor/AddMethod',
      {
        editConfigFilePath,
        parameterId,
        methodName,
        isDefault,
        limitValueActivate,
        limitValue,
        limitValueString
      }
    );
    logger.info('API: addMethodToDictionary успешно', { methodId: response.data.id });
    return response.data;
  }

}

// Экспортируем синглтон
export const documentApi = new DocumentApiService();
