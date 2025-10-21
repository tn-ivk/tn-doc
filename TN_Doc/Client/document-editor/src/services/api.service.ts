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
      console.log('[API] Запрос некорректных символов для устройства:', deviceId);
      const response = await axios.get<any>('/Home/GetInvalideChars', {
        params: { IdDevice: deviceId }
      });

      console.log('[API] Ответ сервера (сырой):', response.data);
      console.log('[API] Тип ответа:', typeof response.data, Array.isArray(response.data) ? '(массив)' : '');

      // Axios автоматически парсит JSON, проверяем тип
      if (Array.isArray(response.data)) {
        console.log('[API] Возвращаем массив напрямую:', response.data);
        return response.data;
      } else if (typeof response.data === 'string' && response.data) {
        // Если вернулась строка, парсим её
        const parsed = JSON.parse(response.data);
        console.log('[API] Распарсенный массив из строки:', parsed);
        return parsed;
      }

      console.log('[API] Пустой или некорректный ответ от сервера');
      return [];
    } catch (error) {
      console.error('[API] Ошибка при получении списка некорректных символов:', error);
      return [];
    }
  }
}

// Экспортируем синглтон
export const documentApi = new DocumentApiService();
