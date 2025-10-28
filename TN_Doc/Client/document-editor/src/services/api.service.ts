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
    const url = `/${deviceId}/${docType}/edit/${id}`;
    console.log('[API] getEditConfig - запрос:', { deviceId, docType, id, url });

    const response = await this.api.get<DocumentEditConfig>(url);

    console.log('[API] getEditConfig - ответ получен');
    console.log('[API] getEditConfig - статус:', response.status);
    console.log('[API] getEditConfig - данные (сырой JSON):', JSON.stringify(response.data, null, 2));

    // Детальная проверка fields и initialValues
    if (response.data) {
      console.log('[API] getEditConfig - количество полей:', response.data.fields?.length);
      console.log('[API] getEditConfig - количество initialValues:', Object.keys(response.data.initialValues || {}).length);

      // Ищем поля, связанные с датой/временем
      const dateFields = response.data.fields?.filter(f =>
        f.label?.toLowerCase().includes('дата') ||
        f.label?.toLowerCase().includes('время') ||
        f.key?.toLowerCase().includes('date') ||
        f.key?.toLowerCase().includes('time')
      );

      if (dateFields && dateFields.length > 0) {
        console.log('[API] getEditConfig - 🔍 Найдены поля даты/времени:', dateFields.map(f => ({
          key: f.key,
          label: f.label,
          type: f.type,
          initialValue: response.data.initialValues?.[f.key]
        })));
      } else {
        console.warn('[API] getEditConfig - ⚠️ Не найдены поля даты/времени');
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
    console.log('[API] updateDocument - запрос:', { deviceId, docType, id });
    const response = await this.api.post<SaveDocumentResponse>(
      `/${deviceId}/${docType}/update/${id}`,
      data
    );
    console.log('[API] updateDocument - документ успешно обновлен');
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
