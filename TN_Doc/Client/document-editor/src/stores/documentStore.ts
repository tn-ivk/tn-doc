import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { documentApi } from '@/services/api.service';
import type { DocumentEditConfig, FormField } from '@/types/document.types';

/**
 * Store для управления состоянием документа
 */
export const useDocumentStore = defineStore('document', () => {
  // State
  const config = ref<DocumentEditConfig | null>(null);
  const formData = ref<Record<string, any>>({});
  const isDirty = ref(false);
  const isLoading = ref(false);
  const isSaving = ref(false);
  const error = ref<string | null>(null);

  // Getters
  const isReady = computed(() => config.value !== null && !isLoading.value);
  const hasUnsavedChanges = computed(() => isDirty.value);
  const documentTitle = computed(() => config.value?.title || 'Загрузка...');
  const fields = computed(() => config.value?.fields || []);

  /**
   * Проверка валидации: есть ли незаполненные обязательные поля или некорректные символы
   */
  const hasValidationErrors = computed(() => {
    if (!config.value) return false;

    const invalidChars = config.value.invalidChars || [];

    for (const field of config.value.fields) {
      const value = formData.value[field.key];

      // Проверка обязательных полей
      if (field.required) {
        if (value === null || value === undefined || value === '') {
          return true;
        }
      }

      // Проверка некорректных символов для текстовых полей
      if (field.type === 'text' && value && typeof value === 'string' && invalidChars.length > 0) {
        for (const char of invalidChars) {
          if (value.includes(char)) {
            return true;
          }
        }
      }
    }

    return false;
  });

  /**
   * Можно ли сохранить документ (нет ошибок валидации)
   */
  const canSave = computed(() => !hasValidationErrors.value);

  /**
   * Загрузить конфигурацию документа
   */
  async function loadConfig(deviceId: number, docType: string, id: number) {
    console.log('[DocumentStore] loadConfig - начало', { deviceId, docType, id });
    isLoading.value = true;
    error.value = null;

    try {
      console.log('[DocumentStore] Запрос конфигурации редактирования...');
      const loadedConfig = await documentApi.getEditConfig(deviceId, docType, id);
      console.log('[DocumentStore] Конфигурация получена:', loadedConfig);
      console.log('[DocumentStore] Количество полей:', loadedConfig.fields?.length);
      console.log('[DocumentStore] Начальные значения:', loadedConfig.initialValues);

      // Загружаем список некорректных символов для данного устройства
      console.log('[DocumentStore] Запрос некорректных символов...');
      const invalidChars = await documentApi.getInvalidChars(deviceId);
      console.log('[DocumentStore] Загружены некорректные символы для устройства', deviceId, ':', invalidChars);
      loadedConfig.invalidChars = invalidChars;

      console.log('[DocumentStore] Установка конфигурации в state...');
      config.value = loadedConfig;

      // Инициализируем formData начальными значениями
      console.log('[DocumentStore] Инициализация formData...');
      console.log('[DocumentStore] initialValues полный дамп:', JSON.stringify(loadedConfig.initialValues, null, 2));
      formData.value = { ...loadedConfig.initialValues };
      console.log('[DocumentStore] formData инициализирован:', Object.keys(formData.value).length, 'полей');
      console.log('[DocumentStore] formData полный дамп:', JSON.stringify(formData.value, null, 2));

      // Проверка поля даты и времени отбора
      const dateKeys = Object.keys(formData.value).filter(key =>
        key.toLowerCase().includes('date') || key.toLowerCase().includes('дата') || key.toLowerCase().includes('время')
      );
      if (dateKeys.length > 0) {
        console.log('[DocumentStore] 🔍 Найдены ключи, связанные с датой/временем:', dateKeys);
        dateKeys.forEach(key => {
          console.log(`[DocumentStore] 🔍 ${key} = ${formData.value[key]} (тип: ${typeof formData.value[key]})`);
        });
      } else {
        console.warn('[DocumentStore] ⚠️ Не найдены ключи, связанные с датой/временем в formData');
      }

      isDirty.value = false;
      console.log('[DocumentStore] loadConfig - успешно завершено');
    } catch (err: any) {
      error.value = err.response?.data?.error || err.message || 'Не удалось загрузить конфигурацию документа';
      console.error('[DocumentStore] Ошибка загрузки конфигурации:', err);
      throw err;
    } finally {
      isLoading.value = false;
    }
  }

  /**
   * Обновить значение поля формы
   */
  function updateField(key: string, value: any) {
    formData.value[key] = value;
    isDirty.value = true;
  }

  /**
   * Сохранить документ
   */
  async function saveDocument() {
    if (!config.value) {
      throw new Error('Конфигурация документа не загружена');
    }

    isSaving.value = true;

    try {
      const response = await documentApi.saveDocument(
        config.value.deviceId,
        config.value.docType,
        config.value.docId,
        formData.value
      );

      if (response.success) {
        isDirty.value = false;
        return response;
      } else {
        throw new Error(response.error || 'Не удалось сохранить документ');
      }
    } catch (err: any) {
      // НЕ устанавливаем error.value, чтобы форма продолжала отображаться
      // Ошибка будет показана через Toast в DocumentEditor.vue
      console.error('[DocumentStore] Ошибка сохранения:', err);
      throw err;
    } finally {
      isSaving.value = false;
    }
  }

  /**
   * Сбросить состояние store
   */
  function reset() {
    config.value = null;
    formData.value = {};
    isDirty.value = false;
    isLoading.value = false;
    isSaving.value = false;
    error.value = null;
  }

  return {
    // State
    config,
    formData,
    isDirty,
    isLoading,
    isSaving,
    error,

    // Getters
    isReady,
    hasUnsavedChanges,
    documentTitle,
    fields,
    hasValidationErrors,
    canSave,

    // Actions
    loadConfig,
    updateField,
    saveDocument,
    reset
  };
});
