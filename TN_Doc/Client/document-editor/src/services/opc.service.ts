import axios, { type AxiosInstance } from 'axios';

/**
 * Интерфейс запроса на запись тега
 */
interface WriteTagRequest {
  DeviceName: string;
  NameTag: string;
  ValueTag: any;
  NamespaceIndex: number;
  IndexArray: number;
}

/**
 * API клиент для взаимодействия с TN_MessagingService (OPC UA/DA клиент)
 * Порт по умолчанию: 5010
 */
class OpcApiService {
  private api: AxiosInstance;

  constructor(baseURL: string = 'http://localhost:5010') {
    this.api = axios.create({
      baseURL: `${baseURL}/api/Values`,
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json'
      }
    });

    // Интерсептор для логирования запросов
    this.api.interceptors.request.use(
      (config) => {
        console.log('[OPC API] Запрос:', config.method?.toUpperCase(), config.url, config.data);
        return config;
      },
      (error) => {
        console.error('[OPC API] Ошибка запроса:', error);
        return Promise.reject(error);
      }
    );

    // Интерсептор для логирования ответов
    this.api.interceptors.response.use(
      (response) => {
        console.log('[OPC API] Ответ:', response.status, response.data);
        return response;
      },
      (error) => {
        console.error('[OPC API] Ошибка ответа:', error.response?.status, error.response?.data);
        return Promise.reject(error);
      }
    );
  }

  /**
   * Прочитать значение тега
   * @param deviceName GUID устройства или имя устройства
   * @param tagName Имя тега (например: "ARM.ARM_FillActAndPassportResult")
   * @param namespaceIndex Индекс namespace (по умолчанию 2)
   * @param indexArray Индекс массива (по умолчанию 0)
   */
  async readTag(
    deviceName: string,
    tagName: string,
    namespaceIndex: number = 2,
    indexArray: number = 0
  ): Promise<any> {
    try {
      const url = `/${deviceName}/${tagName}/${namespaceIndex}/${indexArray}`;
      const response = await this.api.get(url);
      return response.data;
    } catch (error: any) {
      console.error('[OPC API] Ошибка чтения тега:', { deviceName, tagName, error: error.message });
      throw new Error(`Не удалось прочитать тег ${tagName}: ${error.message}`);
    }
  }

  /**
   * Записать значение в тег
   * @param deviceName GUID устройства или имя устройства
   * @param tagName Имя тега (например: "ARM.ARM_FillActAndPassport")
   * @param value Значение для записи
   * @param namespaceIndex Индекс namespace (по умолчанию 2)
   * @param indexArray Индекс массива (по умолчанию 3)
   */
  async writeTag(
    deviceName: string,
    tagName: string,
    value: any,
    namespaceIndex: number = 2,
    indexArray: number = 3
  ): Promise<void> {
    try {
      const payload: WriteTagRequest = {
        DeviceName: deviceName,
        NameTag: tagName,
        ValueTag: value,
        NamespaceIndex: namespaceIndex,
        IndexArray: indexArray
      };

      await this.api.put('/', payload);
      console.log('[OPC API] Тег успешно записан:', { deviceName, tagName, value });
    } catch (error: any) {
      console.error('[OPC API] Ошибка записи тега:', { deviceName, tagName, value, error: error.message });
      throw new Error(`Не удалось записать тег ${tagName}: ${error.message}`);
    }
  }

  /**
   * Сформировать полное имя тега с префиксом
   * @param tagName Короткое имя тега (например: "ARM.ARM_FillActAndPassport")
   * @param prefix Префикс устройства (например: "IVK")
   */
  getFullTagName(tagName: string, prefix: string): string {
    return `${prefix}.${tagName}`;
  }

  /**
   * Polling тега с ожиданием изменения значения
   * @param deviceName GUID устройства
   * @param tagName Имя тега для опроса
   * @param expectedChange Функция проверки условия (например: (current, initial) => current > initial)
   * @param maxDuration Максимальное время ожидания в миллисекундах (по умолчанию 5000)
   * @param pollInterval Интервал опроса в миллисекундах (по умолчанию 500)
   * @param namespaceIndex Индекс namespace (по умолчанию 2)
   * @param indexArray Индекс массива (по умолчанию 0)
   * @returns true если условие выполнено, false если таймаут
   */
  async pollTag(
    deviceName: string,
    tagName: string,
    expectedChange: (currentValue: any, initialValue: any) => boolean,
    maxDuration: number = 5000,
    pollInterval: number = 500,
    namespaceIndex: number = 2,
    indexArray: number = 0
  ): Promise<boolean> {
    const startTime = Date.now();

    try {
      // Читаем начальное значение тега
      const initialValue = await this.readTag(deviceName, tagName, namespaceIndex, indexArray);
      console.log('[OPC API] Polling начат. Начальное значение тега:', initialValue);

      return new Promise<boolean>((resolve) => {
        const intervalId = setInterval(async () => {
          const currentTime = Date.now();
          const elapsed = currentTime - startTime;

          try {
            // Читаем текущее значение тега
            const currentValue = await this.readTag(deviceName, tagName, namespaceIndex, indexArray);

            // Проверяем условие
            if (expectedChange(currentValue, initialValue)) {
              console.log('[OPC API] Polling завершен успешно. Текущее значение:', currentValue);
              clearInterval(intervalId);
              resolve(true);
              return;
            }

            // Проверяем таймаут
            if (elapsed >= maxDuration) {
              console.warn('[OPC API] Polling завершен по таймауту. Текущее значение:', currentValue);
              clearInterval(intervalId);
              resolve(false);
              return;
            }
          } catch (error) {
            console.error('[OPC API] Ошибка при чтении тега в polling:', error);
            // Продолжаем polling даже при ошибке чтения
          }
        }, pollInterval);
      });
    } catch (error) {
      console.error('[OPC API] Ошибка при инициализации polling:', error);
      return false;
    }
  }
}

// Экспортируем синглтон
export const opcApi = new OpcApiService();
