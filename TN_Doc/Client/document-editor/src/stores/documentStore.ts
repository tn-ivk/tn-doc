import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { logger, useInvalidChars } from '@tn-doc/shared';
import { documentApi } from '@/services/api.service';
import type { DocumentEditConfig, FormField } from '@/types/document.types';
import type { PassportEditConfig } from '@/types/passport.types';
import type { FieldHistoryEntry } from '@/types/history.types';
import { DataSource } from '@/types/history.types';
import { normalizeValue } from '@/utils/passport-utils';

const MANUAL_AUTHOR = 'Пользователь';

type SyncBallastOptions = {
  source?: DataSource;
  comment?: string;
  trackMeasurementHistory?: boolean;
  trackResultHistory?: boolean;
};

type ManualOverrideOptions = {
  source?: DataSource;
  comment?: string;
  payloadType?: string;
};

const resolveAuthor = (source: DataSource) => {
  switch (source) {
    case DataSource.ELIS:
      return 'ELIS';
    case DataSource.IVK:
      return 'IVK';
    case DataSource.ReturnToELIS:
      return MANUAL_AUTHOR; // Возврат к ELIS - это действие пользователя
    default:
      return MANUAL_AUTHOR;
  }
};

const createHistoryEntry = (
  source: DataSource,
  value: string,
  previousValue?: string,
  comment?: string
): FieldHistoryEntry => ({
  source,
  modifiedAt: new Date().toISOString(),
  modifiedBy: resolveAuthor(source),
  value,
  previousValue,
  comment
});

const ensureArray = <T>(value: T[] | undefined): T[] => {
  if (value) {
    return value;
  }
  return [];
};

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
        // Slave-параметры не валидируются (не отображаются в UI, значение вычисляется в ИВК)
        if (paramSchema.role === 'Slave') {
          continue;
        }

        // Если поле заблокировано для ввода, валидация requiredFill не применяется
        if (!paramSchema.editable) {
          continue;
        }

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

      // НОВЫЙ ФОРМАТ: Извлекаем историю из initialValues
      // Бэкенд передает историю под ключами с постфиксом __history
      const historyKeysInInitialValues = Object.keys(loadedConfig.initialValues).filter(k => k.endsWith('__history'));

      for (const historyKey of historyKeysInInitialValues) {
        // Убираем постфикс __history, чтобы получить имя поля
        const fieldKey = historyKey.replace('__history', '');
        const historyData = loadedConfig.initialValues[historyKey];

        if (Array.isArray(historyData) && historyData.length > 0) {
          formHistory.value[fieldKey] = historyData;
        }
      }

      // СТАРЫЙ ФОРМАТ (для обратной совместимости): Загрузить историю из конфигурации (если есть)
      if ((loadedConfig as any).fieldHistory) {
        const fieldHistory = (loadedConfig as any).fieldHistory;
        // Объединяем со старым форматом (новый формат имеет приоритет)
        for (const [key, value] of Object.entries(fieldHistory)) {
          if (!formHistory.value[key]) {
            formHistory.value[key] = value as FieldHistoryEntry[];
          }
        }
      }

      // СТАРЫЙ ФОРМАТ (для обратной совместимости): Загрузить историю параметров качества
      if (loadedConfig.docType === 'Passport') {
        const passportConfig = loadedConfig as PassportEditConfig;
        const parametersSchema = passportConfig.qualityParametersSchema || [];

        for (const paramSchema of parametersSchema) {
          if ((paramSchema as any).history && Array.isArray((paramSchema as any).history)) {
            // Для параметров качества используем составные ключи
            const historyEntries = (paramSchema as any).history as FieldHistoryEntry[];
            const valueKey = `value.${paramSchema.key}`;
            const resultKey = `result.${paramSchema.key}`;
            const methodKey = `method.${paramSchema.key}`;

            // Только если история еще не загружена из нового формата
            if (!formHistory.value[valueKey]) {
              formHistory.value[valueKey] = [...historyEntries];
            }
            if (!formHistory.value[resultKey]) {
              formHistory.value[resultKey] = [...historyEntries];
            }
            if (!formHistory.value[methodKey]) {
              formHistory.value[methodKey] = [...historyEntries];
            }
          }

          // Сохранение ELIS-метода для возможности возврата к нему после выбора другого метода
          const methodKey = `method.${paramSchema.key}`;
          const isMethodFromElis = formData.value[`${methodKey}__elisFilled`] === true;
          console.log(`[documentStore] Проверка ELIS-метода для ${paramSchema.key}:`, {
            methodKey,
            isMethodFromElis,
            methodValue: formData.value[methodKey]
          });
          if (isMethodFromElis) {
            const methodJson = formData.value[methodKey];
            if (methodJson && typeof methodJson === 'string' && methodJson.trim() !== '') {
              try {
                const methodObject = JSON.parse(methodJson);
                // Сохраняем полный объект метода из ELIS для возможности выбора его позже
                formData.value[`${methodKey}__elisOption`] = methodObject;
                console.log(`[documentStore] ✅ Сохранён ELIS-метод для ${paramSchema.key}:`, {
                  methodName: methodObject.Name || methodObject.name,
                  methodObject
                });
                logger.debug('[documentStore] Сохранён ELIS-метод для параметра', {
                  paramKey: paramSchema.key,
                  methodName: methodObject.Name || methodObject.name
                });
              } catch (e) {
                console.error(`[documentStore] ❌ Ошибка парсинга ELIS-метода для ${paramSchema.key}:`, e);
                logger.warn('[documentStore] Не удалось распарсить ELIS-метод', {
                  paramKey: paramSchema.key,
                  methodJson
                });
              }
            }
          }
        }
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

    // Диагностика: логируем все __elisOption ключи
    const elisOptionKeys = Object.keys(payload).filter(k => k.includes('__elisOption'));
    if (elisOptionKeys.length > 0) {
      console.log('[documentStore] bulkUpdateFields - elisOption ключи:', elisOptionKeys.map(k => ({
        key: k,
        value: payload[k]
      })));
    }

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

  const pushHistoryEntry = (fieldKey: string, entry: FieldHistoryEntry) => {
    const currentHistory = ensureArray(formHistory.value[fieldKey]);
    currentHistory.push(entry);
    formHistory.value[fieldKey] = currentHistory;
  };

  function syncBallastParameter(paramKey: string, value: string, options?: SyncBallastOptions) {
    const measurementKey = `value.${paramKey}`;
    const resultKey = `result.${paramKey}`;
    const normalizedNewValue = value ?? '';
    const previousMeasurement = formData.value[measurementKey] ?? '';
    const previousResult = formData.value[resultKey] ?? '';
    const source = options?.source ?? DataSource.Unknown;

    // Проверяем, вернулось ли значение к оригинальному из ELIS
    const measurementElisOriginal = formData.value[`${measurementKey}__elisOriginal`];
    const normalizedElisOriginal = measurementElisOriginal !== undefined ? normalizeValue(measurementElisOriginal) : undefined;
    const normalizedNew = normalizeValue(normalizedNewValue);
    const isBackToElisMeasurement = measurementElisOriginal !== undefined &&
      normalizedNew === normalizedElisOriginal;

    // Для measurement: если source=ELIS, ReturnToELIS или вернулись к оригиналу
    const isMeasurementElisFilled = source === DataSource.ELIS || source === DataSource.ReturnToELIS || isBackToElisMeasurement;

    console.log(`[syncBallastParameter] ${measurementKey}:`, {
      value: normalizedNewValue,
      normalizedNew,
      source,
      measurementElisOriginal,
      normalizedElisOriginal,
      isBackToElisMeasurement,
      isMeasurementElisFilled
    });

    formData.value[measurementKey] = normalizedNewValue;
    formData.value[resultKey] = normalizedNewValue;
    formData.value[`${measurementKey}__elisFilled`] = isMeasurementElisFilled;
    formData.value[`${resultKey}__elisFilled`] = false; // result балластного параметра не помечается как ELIS
    formData.value[`${resultKey}__manualOverride`] = false;
    isDirty.value = true;

    if (options?.trackMeasurementHistory !== false && previousMeasurement !== normalizedNewValue) {
      pushHistoryEntry(
        measurementKey,
        createHistoryEntry(source, normalizedNewValue, previousMeasurement, options?.comment)
      );
    }

    if (options?.trackResultHistory !== false && previousResult !== normalizedNewValue) {
      pushHistoryEntry(
        resultKey,
        createHistoryEntry(DataSource.Auto, normalizedNewValue, previousResult, options?.comment ?? 'Результат синхронизирован с измерением')
      );
    }
  }

  function markManualOverride(paramKey: string, resultValue: string, options?: ManualOverrideOptions) {
    const resultKey = `result.${paramKey}`;
    const normalizedNewValue = resultValue ?? '';
    const previousResult = formData.value[resultKey] ?? '';
    const inputSource = options?.source ?? DataSource.Manual;

    // Проверяем, вернулось ли значение к оригинальному из ELIS
    const resultElisOriginal = formData.value[`${resultKey}__elisOriginal`];
    const isBackToElisResult = resultElisOriginal !== undefined &&
      normalizeValue(normalizedNewValue) === normalizeValue(resultElisOriginal);

    // Определяем финальный source: если вернулись к ELIS - используем ReturnToELIS
    const historySource = isBackToElisResult ? DataSource.ReturnToELIS : inputSource;

    // Если source=ELIS, ReturnToELIS или вернулись к оригиналу - устанавливаем флаг
    const isElisFilled = inputSource === DataSource.ELIS || inputSource === DataSource.ReturnToELIS || isBackToElisResult;

    formData.value[resultKey] = normalizedNewValue;
    formData.value[`${resultKey}__elisFilled`] = isElisFilled;
    // manualOverride: true только если ручное изменение И НЕ вернулись к ELIS
    formData.value[`${resultKey}__manualOverride`] = inputSource === DataSource.Manual && !isBackToElisResult;
    isDirty.value = true;

    if (previousResult !== normalizedNewValue) {
      pushHistoryEntry(
        resultKey,
        createHistoryEntry(
          historySource,
          normalizedNewValue,
          previousResult,
          isBackToElisResult
            ? 'Возврат к значению ELIS'
            : (options?.comment ?? (options?.payloadType === 'ResultModal' ? 'Изменено через модалку результата' : undefined))
        )
      );
    }
  }

  async function saveDocument() {
    if (!config.value) {
      throw new Error('Конфигурация документа не загружена');
    }

    isSaving.value = true;

    try {
      // Подготовить данные для сохранения (включая историю)

      const payload = {
        ...formData.value,
        __history: formHistory.value // Передаем историю
      };


      const response = await documentApi.saveDocument(
        config.value.deviceId,
        config.value.docType,
        config.value.docId,
        payload
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
    syncBallastParameter,
    markManualOverride,
    saveDocument,
    reset
  };
});
