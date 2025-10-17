import axios, { AxiosInstance } from 'axios'
import type { DocumentConfig, SaveDocumentData } from '@/types/document.types'

/**
 * API клиент для работы с документами
 */
class ApiService {
  private client: AxiosInstance

  constructor() {
    this.client = axios.create({
      baseURL: '/api/documents',
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json'
      }
    })

    // Интерцептор для логирования запросов
    this.client.interceptors.request.use(
      (config) => {
        console.log(`API Request: ${config.method?.toUpperCase()} ${config.url}`)
        return config
      },
      (error) => {
        console.error('API Request Error:', error)
        return Promise.reject(error)
      }
    )

    // Интерцептор для логирования ответов
    this.client.interceptors.response.use(
      (response) => {
        console.log(`API Response: ${response.config.url} - ${response.status}`)
        return response
      },
      (error) => {
        console.error('API Response Error:', error.response?.data || error.message)
        return Promise.reject(error)
      }
    )
  }

  /**
   * Получает конфигурацию формы редактирования документа
   */
  async getEditConfig(
    deviceId: string,
    docType: string,
    docId: number
  ): Promise<DocumentConfig> {
    try {
      const response = await this.client.get<DocumentConfig>(
        `/${deviceId}/${docType}/edit/${docId}`
      )
      return response.data
    } catch (error) {
      console.error('Ошибка при получении конфигурации документа:', error)
      throw error
    }
  }

  /**
   * Сохраняет документ
   */
  async saveDocument(
    deviceId: string,
    docType: string,
    docId: number,
    data: SaveDocumentData
  ): Promise<{ success: boolean; message?: string }> {
    try {
      const response = await this.client.post<{ success: boolean; message?: string }>(
        `/${deviceId}/${docType}/save/${docId}`,
        { data }
      )
      return response.data
    } catch (error) {
      console.error('Ошибка при сохранении документа:', error)
      throw error
    }
  }

  /**
   * Проверка доступности API
   */
  async healthCheck(): Promise<boolean> {
    try {
      const response = await this.client.get('/health')
      return response.status === 200
    } catch (error) {
      console.error('API недоступен:', error)
      return false
    }
  }
}

export const apiService = new ApiService()
