import { logger } from '@tn-doc/shared';
import { computed, ref, watch, type WatchStopHandle } from 'vue';
import { useDocumentStore } from '@/stores/documentStore';
import { useFieldHistory } from '@/composables/useFieldHistory';
import type {
  PassportEditConfig,
  PassportQualityParameter,
  PassportQualityParameterSchema,
  MethodOption,
  MeasurementUpdateEvent,
  MethodUpdateEvent,
  ParameterDocument,
  ResultUpdateEvent
} from '@/types/passport.types';
import type { ResultEditMode } from '@/types/passport.types';
import { DataSource } from '@/types/history.types';

const DEFAULT_RESULT_MODE: ResultEditMode = 'auto';

const measurementWatchers = new Map<string, WatchStopHandle>();
const resultWatchers = new Map<string, WatchStopHandle>();
const measurementGuard = new Set<string>();
const resultGuard = new Set<string>();

const resolveIsBallastFlag = (schema: PassportQualityParameterSchema): boolean => {
  if (typeof schema.isBallast === 'boolean') {
    return schema.isBallast;
  }

  const legacy = (schema as unknown as { isBalast?: boolean }).isBalast;
  if (typeof legacy === 'boolean') {
    return legacy;
  }

  return false;
};

const resolveResultEditMode = (schema: PassportQualityParameterSchema): ResultEditMode => {
  const mode = schema.resultEditMode as ResultEditMode | undefined;
  if (mode) {
    return mode;
  }

  const legacy = (schema as unknown as { ResultEditMode?: ResultEditMode }).ResultEditMode;
  if (legacy) {
    return legacy;
  }

  return DEFAULT_RESULT_MODE;
};

/**
 * Нормализовать значение для сравнения
 * Приводит числа к единому формату (точка вместо запятой, удаляет лишние пробелы)
 */
const normalizeValue = (value: any): string => {
  if (value === null || value === undefined || value === '') {
    return '';
  }

  const strValue = String(value).trim();

  // Заменяем запятую на точку для чисел
  const normalized = strValue.replace(',', '.');

  // Если это число, проверяем что преобразование корректно
  const numValue = parseFloat(normalized);
  if (!isNaN(numValue)) {
    // Возвращаем нормализованную строку с точкой
    return normalized;
  }

  // Для нечисловых значений возвращаем оригинальную строку (без замены запятой)
  return strValue;
};

/**
 * Композабл для работы с редактором паспорта качества
 * Содержит логику для работы с качественными параметрами, методами испытаний и ELIS
 */
export function usePassportEditor() {
  const store = useDocumentStore();
  const { trackManualChange } = useFieldHistory();

  /**
   * Приведение конфигурации к типу PassportEditConfig
   */
  const passportConfig = computed<PassportEditConfig | null>(() => {
    if (!store.config || store.config.docType !== 'Passport') {
      return null;
    }
    return store.config as PassportEditConfig;
  });

  /**
   * Схема параметров качества из конфигурации (только метаданные)
   */
  const qualityParametersSchema = computed<PassportQualityParameterSchema[]>(() => {
    return passportConfig.value?.qualityParametersSchema || [];
  });

  /**
   * Путь к файлу конфигурации (для добавления методов в справочник)
   */
  const editConfigFilePath = computed<string>(() => {
    return passportConfig.value?.editConfigFilePath || '';
  });

  /**
   * Вычисляемое свойство для объединения схемы + данных из formData
   * Создает полные объекты PassportQualityParameter динамически
   */
  const qualityParameters = computed<PassportQualityParameter[]>(() => {
    const schema = qualityParametersSchema.value;
    if (schema.length === 0) {
      return [];
    }

    return schema.map(paramSchema => {
      const isBallast = resolveIsBallastFlag(paramSchema);
      const resultEditMode = resolveResultEditMode(paramSchema);
      // Извлекаем данные из formData
      const measurementValue = store.formData[`value.${paramSchema.key}`] || '';
      const resultValue = store.formData[`result.${paramSchema.key}`] || '';
      const methodJson = store.formData[`method.${paramSchema.key}`] || '';
      const documentRaw = store.formData[`document.${paramSchema.key}`];
      const documentElisFilled = store.formData[`document.${paramSchema.key}__elisFilled`] === true;
      const documentInfo = tryParseDocument(documentRaw, documentElisFilled);
      const manualOverride = store.formData[`result.${paramSchema.key}__manualOverride`] === true;

      // Парсим выбранный метод
      const selectedMethod = tryParseMethod(methodJson);
      const selectedMethodName = selectedMethod?.name || '';

      // Определяем флаги ELIS
      const elisFlags = {
        measurement: store.formData[`value.${paramSchema.key}__elisFilled`] === true,
        method: store.formData[`method.${paramSchema.key}__elisFilled`] === true,
        result: store.formData[`result.${paramSchema.key}__elisFilled`] === true,
        document: documentElisFilled
      };

      // Объединяем методы из схемы + выбранный метод (если есть и его нет в списке)
      const methodOptions = [...paramSchema.methodOptions];
      if (selectedMethod && !methodOptions.find(m => m.name === selectedMethod.name)) {
        methodOptions.push(selectedMethod);
      }

      const methodNameTrimmed = selectedMethodName.trim();
      const normalizedMethodName = methodNameTrimmed.toLowerCase();
      // Проверяем наличие метода в локальном справочнике (CfgEditPassport*.json)
      // Используем отдельный список localMethodNames, который содержит только методы из конфигурации
      const localMethods = paramSchema.localMethodNames || [];
      const isMethodInDictionary = normalizedMethodName
        ? localMethods.some(name => name.trim().toLowerCase() === normalizedMethodName)
        : true;

      // Создаем полный объект параметра
      return {
        ...paramSchema,
        isBallast,
        resultEditMode,
        values: {
          measurement: measurementValue,
          result: resultValue
        },
        method: {
          selected: selectedMethodName,
          options: methodOptions,
          requiredFill: paramSchema.methodRequiredFill,
          isInDictionary: isMethodInDictionary
        },
        document: documentInfo,
        elisFlags,
        manualOverride
      };
    });
  });

  /**
   * Используется ли ELIS для данного устройства
   */
  const isElisUsed = computed<boolean>(() => {
    return passportConfig.value?.isElisUsed || false;
  });

  /**
   * Есть ли качественные параметры в конфигурации
   */
  const hasQualityParameters = computed<boolean>(() => {
    return qualityParameters.value.length > 0;
  });

  /**
   * Найти параметр по ключу
   */
  function findParameter(paramKey: string): PassportQualityParameter | undefined {
    return qualityParameters.value.find(p => p.key === paramKey);
  }

  function findParameterSchema(paramKey: string): PassportQualityParameterSchema | undefined {
    return qualityParametersSchema.value.find(p => p.key === paramKey);
  }

  /**
   * Пересчитать результат на основе метода и значения measurement
   */
  function recalculateResult(param: PassportQualityParameter): string {
    const selectedMethod = param.method.options.find(
      (m: MethodOption) => m.name === param.method.selected
    );

    if (param.isBallast) {
      return param.values.measurement;
    }

    const resultMode = param.resultEditMode ?? DEFAULT_RESULT_MODE;
    if (resultMode === 'modal' && param.manualOverride) {
      return param.values.result;
    }

    if (resultMode === 'readonly') {
      return param.values.result || param.values.measurement;
    }

    // Если есть результат из ELIS и measurement заполнено из ELIS, используем его
    if (param.elisFlags.result && param.elisFlags.measurement) {
      return param.values.result;
    }

    const measurementValue = parseFloat(param.values.measurement.replace(',', '.'));

    if (isNaN(measurementValue)) {
      return '-';
    }

    // Если у метода активирован лимит и значение ниже порога
    if (
      selectedMethod?.limitValueActivate &&
      selectedMethod.limitValue !== undefined &&
      measurementValue < selectedMethod.limitValue
    ) {
      return selectedMethod.limitValueString || '-';
    }

    return param.values.measurement;
  }

  /**
   * Определить, редактируема ли ячейка результата
   */
  function isResultEditable(param: PassportQualityParameter): boolean {
    const selectedMethod = param.method.options.find(
      (m: MethodOption) => m.name === param.method.selected
    );

    if (!selectedMethod || !selectedMethod.limitValueActivate) {
      return false;
    }

    const measurementValue = parseFloat(param.values.measurement.replace(',', '.'));
    if (isNaN(measurementValue)) {
      return false;
    }

    return selectedMethod.limitValue !== undefined && measurementValue < selectedMethod.limitValue;
  }

  /**
   * Регистрация реактивных наблюдателей за value/result
   */
  const registerFieldWatchers = (params: PassportQualityParameter[]) => {
    const keys = params.map(param => param.key);

    keys.forEach((paramKey) => {
      if (!measurementWatchers.has(paramKey)) {
        const stop = watch(
          () => store.formData[`value.${paramKey}`],
          (newValue, oldValue) => handleMeasurementFieldChange(paramKey, newValue, oldValue)
        );
        measurementWatchers.set(paramKey, stop);
      }

      if (!resultWatchers.has(paramKey)) {
        const stop = watch(
          () => store.formData[`result.${paramKey}`],
          (newValue, oldValue) => handleResultFieldChange(paramKey, newValue, oldValue)
        );
        resultWatchers.set(paramKey, stop);
      }
    });

    for (const [key, stop] of measurementWatchers.entries()) {
      if (!keys.includes(key)) {
        stop();
        measurementWatchers.delete(key);
      }
    }

    for (const [key, stop] of resultWatchers.entries()) {
      if (!keys.includes(key)) {
        stop();
        resultWatchers.delete(key);
      }
    }
  };

  const handleMeasurementFieldChange = (paramKey: string, newValue: unknown, oldValue: unknown) => {
    const measurementField = `value.${paramKey}`;
    if (measurementGuard.has(measurementField)) {
      measurementGuard.delete(measurementField);
      return;
    }

    const schema = findParameterSchema(paramKey);
    if (!schema) {
      return;
    }

    if (!resolveIsBallastFlag(schema)) {
      return;
    }

    const normalizedNew = normalizeValue(newValue ?? '');
    const normalizedOld = normalizeValue(oldValue ?? '');

    if (normalizedNew === normalizedOld) {
      return;
    }

    const source = store.formData[`${measurementField}__elisFilled`] === true
      ? DataSource.ELIS
      : DataSource.Manual;

    measurementGuard.add(measurementField);
    const resultField = `result.${paramKey}`;
    resultGuard.add(resultField);
    store.syncBallastParameter(paramKey, newValue?.toString() ?? '', {
      source,
      comment: source === DataSource.ELIS ? 'Синхронизация с ELIS' : 'Синхронизация балластного параметра'
    });
  };

  const handleResultFieldChange = (paramKey: string, newValue: unknown, oldValue: unknown) => {
    const resultField = `result.${paramKey}`;
    if (resultGuard.has(resultField)) {
      resultGuard.delete(resultField);
      return;
    }

    const schema = findParameterSchema(paramKey);
    if (!schema) {
      return;
    }

    const isBallast = resolveIsBallastFlag(schema);
    if (isBallast) {
      const measurementValue = store.formData[`value.${paramKey}`] ?? '';
      measurementGuard.add(`value.${paramKey}`);
      resultGuard.add(resultField);
      store.syncBallastParameter(paramKey, measurementValue?.toString() ?? '', {
        source: DataSource.Manual,
        trackMeasurementHistory: false
      });
      return;
    }

    const resultMode = resolveResultEditMode(schema);
    if (resultMode !== 'modal') {
      return;
    }

    const normalizedNew = normalizeValue(newValue ?? '');
    const normalizedOld = normalizeValue(oldValue ?? '');
    if (normalizedNew === normalizedOld) {
      return;
    }

    const isElisFilled = store.formData[`${resultField}__elisFilled`] === true;
    if (isElisFilled) {
      return;
    }

    resultGuard.add(resultField);
    store.markManualOverride(paramKey, newValue?.toString() ?? '', {
      source: DataSource.Manual
    });
  };

  watch(qualityParameters, (params) => {
    registerFieldWatchers(params);
  }, { immediate: true });

  /**
   * Обработчик обновления measurement
   */
  function handleMeasurementUpdate(event: MeasurementUpdateEvent) {
    const param = findParameter(event.paramKey);
    if (!param) {
      logger.warn(`Параметр с ключом ${event.paramKey} не найден`);
      return;
    }

    const measurementKey = `value.${event.paramKey}`;
    const currentMeasurement = store.formData[measurementKey] ?? '';
    const measurementChanged = currentMeasurement !== event.value;
    const isBallast = param.isBallast === true;
    if (isBallast) {
      if (!measurementChanged && store.formData[`result.${event.paramKey}`] === event.value) {
        return;
      }
      measurementGuard.add(measurementKey);
      const resultKey = `result.${event.paramKey}`;
      resultGuard.add(resultKey);
      store.syncBallastParameter(event.paramKey, event.value, {
        source: DataSource.Manual,
        trackMeasurementHistory: false,
        comment: 'Изменено оператором'
      });
      return;
    }

    if (measurementChanged) {
      measurementGuard.add(measurementKey);
      store.bulkUpdateFields({
        [measurementKey]: event.value,
        [`${measurementKey}__elisFilled`]: false
      });
    }

    const shouldUpdateResult =
      (param.resultEditMode === 'auto' || param.resultEditMode === DEFAULT_RESULT_MODE) ||
      (param.resultEditMode === 'modal' && !param.manualOverride);

    if (!shouldUpdateResult) {
      return;
    }

    const tempParam = {
      ...param,
      values: { ...param.values, measurement: event.value }
    };
    const newResult = recalculateResult(tempParam);
    const resultKey = `result.${event.paramKey}`;
    const previousResult = store.formData[resultKey] ?? '';
    if (previousResult !== newResult) {
      resultGuard.add(resultKey);
      store.bulkUpdateFields({
        [resultKey]: newResult,
        [`${resultKey}__elisFilled`]: false,
        [`${resultKey}__manualOverride`]: param.manualOverride === true
      });

      // Записываем историю для результата (пересчитан из измерения)
      trackManualChange(resultKey, newResult, previousResult);
    }
  }

  /**
   * Обработчик обновления метода испытаний
   */
  function handleMethodUpdate(event: MethodUpdateEvent) {
    const param = findParameter(event.paramKey);
    if (!param) {
      logger.warn(`Параметр с ключом ${event.paramKey} не найден`);
      return;
    }

    const methodOption = event.method;
    const methodJson = methodOption ? serializeMethodOption(methodOption) : '';

    logger.debug('[usePassportEditor] handleMethodUpdate', {
      paramKey: event.paramKey,
      methodOption: methodOption ? {
        id: methodOption.id,
        name: methodOption.name,
        limitValueActivate: methodOption.limitValueActivate,
        limitValue: methodOption.limitValue,
        limitValueString: methodOption.limitValueString
      } : null,
      methodJson: methodJson.substring(0, 200) // первые 200 символов
    });

    // Сохраняем предыдущее значение для истории
    const methodKey = `method.${event.paramKey}`;
    const previousMethodJson = store.formData[methodKey] || '';
    const previousMethod = tryParseMethod(previousMethodJson);
    const previousMethodName = previousMethod?.name || '';
    const newMethodName = methodOption?.name || '';

    // Пересчитываем результат с новым методом
    // ВАЖНО: добавляем обновлённый метод в options, чтобы recalculateResult
    // использовал актуальные значения limitValue/limitValueActivate/limitValueString
    const updatedOptions = methodOption
      ? [
          ...param.method.options.filter(m => m.name !== methodOption.name),
          methodOption
        ]
      : param.method.options;

    const tempParam = {
      ...param,
      method: {
        selected: methodOption?.name || '',
        options: updatedOptions
      }
    };
    const newResult = recalculateResult(tempParam);

    // Определяем, изменилось ли название метода
    const methodNameChanged = newMethodName !== previousMethodName;

    // Если название метода не изменилось - сохраняем текущий флаг ELIS
    // Если название изменилось - метод становится "ручным" (флаг сбрасывается)
    const currentMethodElisFlag = store.formData[`method.${event.paramKey}__elisFilled`] === true;
    const newMethodElisFlag = methodNameChanged ? false : currentMethodElisFlag;

    const updates: Record<string, any> = {
      [`method.${event.paramKey}`]: methodJson,
      [`method.${event.paramKey}__elisFilled`]: newMethodElisFlag
    };

    const shouldUpdateResult =
      (param.resultEditMode === 'auto' || param.resultEditMode === DEFAULT_RESULT_MODE) ||
      (param.resultEditMode === 'modal' && !param.manualOverride);

    if (shouldUpdateResult) {
      const resultKey = `result.${event.paramKey}`;
      const currentResult = store.formData[resultKey] || '';
      const resultChanged = currentResult !== newResult;

      // Если результат не изменился - сохраняем текущий флаг ELIS
      // Если результат изменился (из-за пересчёта) - сбрасываем флаг
      const currentResultElisFlag = store.formData[`${resultKey}__elisFilled`] === true;
      const newResultElisFlag = resultChanged ? false : currentResultElisFlag;

      resultGuard.add(resultKey);
      updates[resultKey] = newResult;
      updates[`${resultKey}__elisFilled`] = newResultElisFlag;
      updates[`${resultKey}__manualOverride`] = param.manualOverride === true;
    }

    logger.debug('[usePassportEditor] bulkUpdateFields для метода', {
      methodKey: `method.${event.paramKey}`,
      methodJsonLength: updates[`method.${event.paramKey}`]?.length,
      resultKey: `result.${event.paramKey}`,
      newResult: updates[`result.${event.paramKey}`]
    });

    store.bulkUpdateFields(updates);

    // Записать историю изменения метода (только name, а не весь объект)
    if (newMethodName !== previousMethodName) {
      trackManualChange(
        methodKey,
        newMethodName,
        previousMethodName || undefined
      );
    }
  }

  /**
   * Обработчик обновления результата (ручное редактирование)
   */
  function handleResultUpdate(event: ResultUpdateEvent) {
    const param = findParameter(event.paramKey);
    if (!param) {
      logger.warn(`Параметр с ключом ${event.paramKey} не найден`);
      return;
    }

    const resultKey = `result.${event.paramKey}`;
    if (store.formData[resultKey] === event.value) {
      return;
    }
    resultGuard.add(resultKey);
    store.markManualOverride(event.paramKey, event.value, {
      source: DataSource.Manual,
      payloadType: 'ResultModal'
    });
  }

  /**
   * Вспомогательная функция сериализации метода в JSON строку
   */
  function serializeMethodOption(option: MethodOption | null): string {
    if (!option || !option.name || option.name.trim() === '') {
      return '';
    }

    const payload = {
      Id: option.id,
      Use: option.use,
      IdParameter: option.idParameter,
      Name: option.name,
      LimitValueActivate: option.limitValueActivate,
      LimitValue: option.limitValue ?? 0,
      LimitValueString: option.limitValueString ?? '',
      IsDefault: option.isDefault
    };

    return JSON.stringify(payload);
  }

  /**
   * Вспомогательная функция парсинга метода из JSON строки
   */
  function tryParseMethod(methodJson: string): MethodOption | null {
    if (!methodJson || methodJson.trim() === '') {
      return null;
    }

    try {
      const parsed = JSON.parse(methodJson);
      if (!parsed || typeof parsed !== 'object') {
        return null;
      }

      // Поддерживаем как PascalCase (из бэкенда), так и camelCase (из ELIS интеграции)
      const name = parsed.Name || parsed.name;
      if (typeof name !== 'string') {
        return null;
      }

      return {
        id: parsed.Id || parsed.id || 0,
        use: Boolean(parsed.Use ?? parsed.use),
        idParameter: parsed.IdParameter || parsed.idParameter || 0,
        name: name,
        isDefault: Boolean(parsed.IsDefault ?? parsed.isDefault),
        limitValueActivate: Boolean(parsed.LimitValueActivate ?? parsed.limitValueActivate),
        limitValue: parsed.LimitValue || parsed.limitValue || 0,
        limitValueString: parsed.LimitValueString || parsed.limitValueString || ''
      };
    } catch (error) {
      logger.warn('usePassportEditor: parse method failed', {
        methodJson,
        error: error instanceof Error ? error.message : String(error)
      });
      return null;
    }
  }

  /**
   * Добавить метод в локальный список методов параметра
   * Вызывается после успешного добавления метода в справочник через API
   * Это обновляет localMethodNames, что убирает предупреждение "отсутствует в справочнике"
   */
  function addMethodToLocalDictionary(paramKey: string, methodName: string): void {
    if (!passportConfig.value?.qualityParametersSchema) {
      logger.warn('[usePassportEditor] addMethodToLocalDictionary: схема не найдена');
      return;
    }

    const schema = passportConfig.value.qualityParametersSchema.find(p => p.key === paramKey);
    if (!schema) {
      logger.warn('[usePassportEditor] addMethodToLocalDictionary: параметр не найден', { paramKey });
      return;
    }

    // Проверяем, что метод ещё не в списке
    const normalizedName = methodName.trim().toLowerCase();
    const alreadyExists = schema.localMethodNames?.some(
      name => name.trim().toLowerCase() === normalizedName
    );

    if (!alreadyExists) {
      if (!schema.localMethodNames) {
        schema.localMethodNames = [];
      }
      schema.localMethodNames.push(methodName);
      logger.info('[usePassportEditor] Метод добавлен в локальный справочник', {
        paramKey,
        methodName,
        localMethodNamesCount: schema.localMethodNames.length
      });
    } else {
      logger.debug('[usePassportEditor] Метод уже в локальном справочнике', { paramKey, methodName });
    }
  }

  function tryParseDocument(value: unknown, elisFilled: boolean): ParameterDocument | undefined {
    if (value === null || value === undefined) {
      return undefined;
    }

    if (typeof value === 'object' && !Array.isArray(value)) {
      const payload = value as Record<string, any>;
      const numberValue = payload.Number ?? payload.number;
      const typeValue = payload.Type ?? payload.type;
      const dateValue = payload.Date ?? payload.date;

      if (!numberValue && !typeValue && !dateValue) {
        return undefined;
      }

      return {
        number: numberValue?.toString() ?? '',
        type: typeValue ? typeValue.toString() : undefined,
        date: dateValue ? dateValue.toString() : undefined,
        elisFilled
      };
    }

    const raw = value.toString().trim();
    if (!raw) {
      return undefined;
    }

    if (raw.startsWith('{') && raw.endsWith('}')) {
      try {
        const parsed = JSON.parse(raw);
        return tryParseDocument(parsed, elisFilled);
      } catch (error) {
        logger.warn('usePassportEditor: parse document failed', {
          raw,
          error: error instanceof Error ? error.message : String(error)
        });
        return undefined;
      }
    }

    return {
      number: raw.replace(/^"|"$/g, ''),
      elisFilled
    };
  }

  return {
    passportConfig,
    qualityParameters, // Теперь вычисляется динамически из схемы + formData!
    isElisUsed,
    hasQualityParameters,
    editConfigFilePath, // Путь к файлу конфигурации для добавления методов
    findParameter,
    recalculateResult,
    isResultEditable,
    handleMeasurementUpdate,
    handleMethodUpdate,
    handleResultUpdate,
    addMethodToLocalDictionary // Добавить метод в локальный справочник (убирает предупреждение)
  };
}
