import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { DocumentConfig, FormField, SaveDocumentData, FormFieldValue } from '@/types/document.types'
import { apiService } from '@/services/api.service'

export const useDocumentStore = defineStore('document', () => {
  // State
  const config = ref<DocumentConfig | null>(null)
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  // Computed
  const fields = computed(() => config.value?.fields || [])
  const title = computed(() => config.value?.title || 'Редактирование документа')
  const invalidChars = computed(() => config.value?.invalidChars || [])

  /**
   * Проверка валидности всех полей
   */
  const isValid = computed(() => {
    if (!config.value) return false

    return config.value.fields.every(field => {
      // Проверка обязательных полей
      if (field.required && !field.value) {
        return false
      }

      // Проверка недопустимых символов
      if (field.value) {
        for (const char of invalidChars.value) {
          if (field.value.includes(char)) {
            return false
          }
        }
      }

      return true
    })
  })

  // Actions
  /**
   * Загружает конфигурацию документа
   */
  async function loadDocument(deviceId: string, docType: string, docId: number) {
    loading.value = true
    error.value = null

    try {
      console.log(`Загрузка документа: ${docType} (ID: ${docId}) для устройства ${deviceId}`)
      config.value = await apiService.getEditConfig(deviceId, docType, docId)
      console.log('Документ успешно загружен:', config.value)
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Ошибка при загрузке документа'
      console.error('Ошибка загрузки документа:', err)
      throw err
    } finally {
      loading.value = false
    }
  }

  /**
   * Обновляет значение поля
   */
  function updateField(fieldName: string, value: string) {
    if (!config.value) return

    const field = config.value.fields.find(f => f.name === fieldName)
    if (field) {
      field.value = value
      console.log(`Поле ${fieldName} обновлено:`, value)
    }
  }

  /**
   * Сохраняет документ
   */
  async function saveDocument() {
    if (!config.value) {
      throw new Error('Конфигурация документа не загружена')
    }

    if (!isValid.value) {
      throw new Error('Форма содержит ошибки валидации')
    }

    saving.value = true
    error.value = null

    try {
      const saveData: SaveDocumentData = {
        docID: config.value.docId,
        values: config.value.fields.map(field => ({
          key: field.name,
          value: field.value || '',
          tag: 'AdditionalInfo'
        }))
      }

      console.log('Сохранение документа:', saveData)

      const result = await apiService.saveDocument(
        config.value.deviceId,
        config.value.docType,
        config.value.docId,
        saveData
      )

      if (result.success) {
        console.log('Документ успешно сохранен')
        // Отправляем событие в родительское окно (для iframe)
        window.parent?.postMessage('DocumentSaved', '*')
        return true
      } else {
        throw new Error(result.message || 'Не удалось сохранить документ')
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || err.message || 'Ошибка при сохранении документа'
      console.error('Ошибка сохранения документа:', err)
      throw err
    } finally {
      saving.value = false
    }
  }

  /**
   * Сбрасывает состояние
   */
  function reset() {
    config.value = null
    loading.value = false
    saving.value = false
    error.value = null
  }

  return {
    // State
    config,
    loading,
    saving,
    error,

    // Computed
    fields,
    title,
    invalidChars,
    isValid,

    // Actions
    loadDocument,
    updateField,
    saveDocument,
    reset
  }
})
