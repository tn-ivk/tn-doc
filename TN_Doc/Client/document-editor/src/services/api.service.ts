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
      console.error('Health check failed:', error);
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
    const response = await this.api.get<DocumentEditConfig>(
      `/${deviceId}/${docType}/edit/${id}`
    );
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
   * Получить список некорректных символов для устройства
   * @param deviceId ID устройства (целое число)
   */
  async getInvalidChars(deviceId: number): Promise<string[]> {
    try {
      const response = await axios.get<string>('/Home/GetInvalideChars', {
        params: { IdDevice: deviceId }
      });

      // Парсим JSON строку в массив
      if (response.data) {
        return JSON.parse(response.data);
      }
      return [];
    } catch (error) {
      console.error('Ошибка при получении списка некорректных символов:', error);
      return [];
    }
  }
}

// Экспортируем синглтон
export const documentApi = new DocumentApiService();
