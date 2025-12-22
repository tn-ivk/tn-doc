import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { logger, useInvalidChars } from '@tn-doc/shared';
import { documentApi } from '@/services/api.service';
import type { DocumentEditConfig, FormField } from '@/types/document.types';
import type { PassportEditConfig } from '@/types/passport.types';
import type { FieldHistoryEntry } from '@/types/history.types';
import { DataSource } from '@/types/history.types';
import { normalizeValue } from '@/utils/passport-utils';
import { normalizeForComparison, resolveFieldTypeForComparison } from '@/utils/field-compare-utils';
import { normalizeDecimalValue } from '@/composables/usePassportNormalization';
import { compactAllFieldsHistory } from '@/utils/history-utils';

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
   * Флаг загрузки данных из ELIS
   * Используется для пропуска создания записей истории при автозаполнении связанных полей
   */
  const isLoadingFromElis = ref(false);

  /**
   * История изменений полей
   * Ключ - название поля (controlId), значение - массив записей истории
   */
  const formHistory = ref<Record<string, FieldHistoryEntry[]>>({});

  /**
   * Поля, отсутствующие в текущем протоколе ELIS (только для сессии)
   */
  const elisMissingFields = ref<Record<string, true>>({});

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

      if (loadedConfig.docType === 'Passport') {
        const passportConfig = loadedConfig as PassportEditConfig;
        const parametersSchema = passportConfig.qualityParametersSchema || [];

        for (const paramSchema of parametersSchema) {
          const roundValue = paramSchema.roundValue ?? (paramSchema as any).RoundValue ?? 0;
          const resultKey = `result.${paramSchema.key}`;
          const resultValue = formData.value[resultKey];

          if (resultValue === undefined || resultValue === null || resultValue === '') {
            continue;
          }

          const normalizedResult = normalizeDecimalValue(resultValue, roundValue);
          if (normalizedResult !== resultValue) {
            formData.value[resultKey] = normalizedResult;
          }
        }
      }

      // Загружаем историю изменений полей
      formHistory.value = {};
      elisMissingFields.value = {};

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
          if (isMethodFromElis) {
            const methodJson = formData.value[methodKey];
            if (methodJson && typeof methodJson === 'string' && methodJson.trim() !== '') {
              try {
                const methodObject = JSON.parse(methodJson);
                // Сохраняем полный объект метода из ELIS для возможности выбора его позже
                formData.value[`${methodKey}__elisOption`] = methodObject;
                logger.debug('[documentStore] Сохранён ELIS-метод для параметра', {
                  paramKey: paramSchema.key,
                  methodName: methodObject.Name || methodObject.name
                });
              } catch (e) {
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
   * Для полей AdditionalInfo проверяет возврат к оригинальному значению ELIS
   *
   * @returns объект с информацией о типе изменения:
   *   - isReturnToElis: true если значение вернулось к оригинальному ELIS
   *   - wasElisField: true если поле имело elisOriginal (было заполнено из ELIS)
   */
  function updateField(key: string, value: any): { isReturnToElis: boolean; wasElisField: boolean } {
    const previousValue = formData.value[key];

    formData.value[key] = value;
    isDirty.value = true;

    // Проверяем возврат к оригинальному значению ELIS
    const elisOriginal = formData.value[`${key}__elisOriginal`];
    const wasElisField = elisOriginal !== undefined;

    if (wasElisField) {
      const fieldType = resolveFieldTypeForComparison(key, config.value?.fields ?? [], config.value?.docType);
      const normalizedNew = normalizeForComparison(fieldType, value);
      const normalizedOriginal = normalizeForComparison(fieldType, elisOriginal);
      const isReturnToElis = normalizedNew === normalizedOriginal;

      // Обновляем флаг elisFilled
      formData.value[`${key}__elisFilled`] = isReturnToElis;

      logger.debug('[updateField] Проверка возврата к ELIS', {
        key,
        value,
        elisOriginal,
        normalizedNew,
        normalizedOriginal,
        isReturnToElis
      });

      return { isReturnToElis, wasElisField };
    }

    return { isReturnToElis: false, wasElisField: false };
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

  const pushHistoryEntry = (fieldKey: string, entry: FieldHistoryEntry) => {
    const currentHistory = ensureArray(formHistory.value[fieldKey]);
    currentHistory.push(entry);
    formHistory.value[fieldKey] = currentHistory;
  };

  /**
   * Получить roundValue из схемы параметра
   */
  const getRoundValue = (paramKey: string): number => {
    if (config.value?.docType !== 'Passport') {
      return 0;
    }
    const passportConfig = config.value as PassportEditConfig;
    const schema = passportConfig.qualityParametersSchema?.find(p => p.key === paramKey);
    return schema?.roundValue ?? (schema as any)?.RoundValue ?? 0;
  };

  function syncBallastParameter(paramKey: string, value: string, options?: SyncBallastOptions) {
    const measurementKey = `value.${paramKey}`;
    const resultKey = `result.${paramKey}`;
    const normalizedNewValue = value ?? '';

    // Получаем roundValue из схемы параметра
    const roundValue = getRoundValue(paramKey);

    // Нормализуем результат: пустое значение → "0", иначе нормализуем с учётом roundValue
    const valueToNormalize = normalizedNewValue.trim() || '0';
    const normalizedResultValue = normalizeDecimalValue(valueToNormalize, roundValue);

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

    formData.value[measurementKey] = normalizedNewValue;
    formData.value[resultKey] = normalizedResultValue;
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

    if (options?.trackResultHistory !== false && previousResult !== normalizedResultValue) {
      pushHistoryEntry(
        resultKey,
        createHistoryEntry(DataSource.Auto, normalizedResultValue, previousResult, options?.comment ?? 'Результат синхронизирован с измерением')
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
      // Схлопываем историю: оставляем только последнюю запись для каждого поля
      const compactedHistory = compactAllFieldsHistory(formHistory.value);
      const payload = {
        ...formData.value,
        __history: compactedHistory
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
    elisMissingFields.value = {};
    isDirty.value = false;
    isLoading.value = false;
    isSaving.value = false;
    isLoadingFromElis.value = false;
    error.value = null;
  }

  return {
    // State
    config,
    formData,
    formHistory,
    elisMissingFields,
    isDirty,
    isLoading,
    isSaving,
    isLoadingFromElis,
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
