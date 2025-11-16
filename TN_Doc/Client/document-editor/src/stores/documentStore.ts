import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { logger, useInvalidChars } from '@tn-doc/shared';
import { documentApi } from '@/services/api.service';
import type { DocumentEditConfig, FormField } from '@/types/document.types';
import type { PassportEditConfig } from '@/types/passport.types';
import type { FieldHistoryEntry } from '@/types/history.types';

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

  /**
   * История изменений полей
   * Ключ - название поля (controlId), значение - массив записей истории
   */
  const formHistory = ref<Record<string, FieldHistoryEntry[]>>({});

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

    // Дополнительная валидация для паспорта качества (таблица Edit)
    if (config.value.docType === 'Passport') {
      const passportConfig = config.value as PassportEditConfig;
      const parametersSchema = passportConfig.qualityParametersSchema || [];

      for (const paramSchema of parametersSchema) {
        // Берем значения из formData вместо param.values
        const measurement = (formData.value[`value.${paramSchema.key}`] ?? '').toString();

        // Проверка обязательных полей измерения
        if (paramSchema.requiredFill) {
          if (!measurement || measurement === '') {
            return true;
          }
        }

        // Проверка количества знаков после запятой
        if (paramSchema.roundValue && measurement) {
          const normalized = measurement.replace(',', '.');
          const parts = normalized.split('.');
          if (parts.length > 1 && parts[1].length > paramSchema.roundValue) {
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
    isLoading.value = true;
    error.value = null;

    try {
      const loadedConfig = await documentApi.getEditConfig(deviceId, docType, id);

      // Загружаем список некорректных символов для данного устройства с кэшированием
      const { getInvalidChars } = useInvalidChars();
      const invalidChars = await getInvalidChars(deviceId);
      loadedConfig.invalidChars = invalidChars;

      config.value = loadedConfig;

      // Инициализируем formData начальными значениями
      formData.value = { ...loadedConfig.initialValues };

      // Загружаем историю изменений полей
      formHistory.value = {};
      logger.debug('[FieldHistoryMap] Начало загрузки истории изменений из конфигурации');

      // Загрузить историю из конфигурации (если есть)
      if ((loadedConfig as any).fieldHistory) {
        const fieldHistory = (loadedConfig as any).fieldHistory;
        const fieldCount = Object.keys(fieldHistory).length;
        logger.debug(`[FieldHistoryMap] Загрузка истории из loadedConfig.fieldHistory: найдено ${fieldCount} полей`);

        formHistory.value = { ...fieldHistory };

        // Детальная информация о загруженных полях
        for (const [key, entries] of Object.entries(fieldHistory)) {
          const entriesArray = entries as FieldHistoryEntry[];
          logger.debug(`[FieldHistoryMap] Поле "${key}": загружено ${entriesArray.length} записей истории`);
        }
      } else {
        logger.debug('[FieldHistoryMap] loadedConfig.fieldHistory отсутствует');
      }

      // Загрузить историю параметров качества
      if (loadedConfig.docType === 'Passport') {
        const passportConfig = loadedConfig as PassportEditConfig;
        const parametersSchema = passportConfig.qualityParametersSchema || [];

        logger.debug(`[FieldHistoryMap] Загрузка истории параметров качества: найдено ${parametersSchema.length} параметров`);

        for (const paramSchema of parametersSchema) {
          if ((paramSchema as any).history && Array.isArray((paramSchema as any).history)) {
            // Для параметров качества используем составные ключи
            const historyEntries = (paramSchema as any).history as FieldHistoryEntry[];
            formHistory.value[`value.${paramSchema.key}`] = [...historyEntries];
            formHistory.value[`result.${paramSchema.key}`] = [...historyEntries];
            formHistory.value[`method.${paramSchema.key}`] = [...historyEntries];

            logger.debug(`[FieldHistoryMap] Параметр "${paramSchema.key}": создано 3 ключа истории (value/result/method) с ${historyEntries.length} записями каждый`);
          }
        }
      }

      const totalFields = Object.keys(formHistory.value).length;
      logger.info(`[FieldHistoryMap] История изменений загружена: всего полей с историей = ${totalFields}`);

      // Вывод всех ключей для диагностики
      if (totalFields > 0) {
        logger.debug(`[FieldHistoryMap] Ключи истории: ${Object.keys(formHistory.value).join(', ')}`);
      }

      isDirty.value = false;
    } catch (err: any) {
      error.value = err.response?.data?.error || err.message || 'Не удалось загрузить конфигурацию документа';
      logger.error('DocumentStore: ошибка загрузки конфигурации', {
        error: err.message,
        response: err.response?.data
      });
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

  function bulkUpdateFields(payload: Record<string, any>) {
    if (!payload) return;
    let changed = false;
    for (const [key, value] of Object.entries(payload)) {
      if (formData.value[key] !== value) {
        formData.value[key] = value;
        changed = true;
      }
    }
    if (changed) {
      isDirty.value = true;
    }
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
      // Подготовить данные для сохранения (включая историю)
      logger.debug('[FieldHistoryMap] Подготовка payload для сохранения документа');
      logger.debug(`[FieldHistoryMap] formHistory содержит ${Object.keys(formHistory.value).length} полей с историей`);

      // Детальная информация о структуре истории
      for (const [key, entries] of Object.entries(formHistory.value)) {
        logger.debug(`[FieldHistoryMap] Поле "${key}": отправка ${entries.length} записей истории`);
      }

      const payload = {
        ...formData.value,
        __history: formHistory.value // Передаем историю
      };

      logger.debug(`[FieldHistoryMap] Payload содержит ${Object.keys(payload).length} ключей (включая __history)`);
      logger.debug(`[FieldHistoryMap] Структура __history: ${JSON.stringify(Object.keys(formHistory.value))}`);

      const response = await documentApi.saveDocument(
        config.value.deviceId,
        config.value.docType,
        config.value.docId,
        payload
      );

      if (response.success) {
        isDirty.value = false;
        logger.info(`[FieldHistoryMap] Документ сохранен с историей: ${Object.keys(formHistory.value).length} полей с историей`);
        return response;
      } else {
        throw new Error(response.error || 'Не удалось сохранить документ');
      }
    } catch (err: any) {
      // НЕ устанавливаем error.value, чтобы форма продолжала отображаться
      // Ошибка будет показана через Toast в DocumentEditor.vue
      logger.error('DocumentStore: ошибка сохранения документа', {
        deviceId: config.value.deviceId,
        docType: config.value.docType,
        docId: config.value.docId,
        error: err.message,
        response: err.response?.data
      });
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
    formHistory.value = {};
    isDirty.value = false;
    isLoading.value = false;
    isSaving.value = false;
    error.value = null;
  }

  return {
    // State
    config,
    formData,
    formHistory,
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
    bulkUpdateFields,
    saveDocument,
    reset
  };
});
