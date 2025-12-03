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
import { DataSource } from '@/types/history.types';
import { normalizeValue } from '@/utils/passport-utils';

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

/**
 * Композабл для работы с редактором паспорта качества
 * Содержит логику для работы с качественными параметрами, методами испытаний и ELIS
 */
export function usePassportEditor() {
  const store = useDocumentStore();
  const { trackManualChange, trackAutoFill, trackReturnToElis } = useFieldHistory();

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
   * Примечание: Slave-параметры исключены из списка (не отображаются в UI)
   */
  const qualityParameters = computed<PassportQualityParameter[]>(() => {
    const schema = qualityParametersSchema.value;
    if (schema.length === 0) {
      return [];
    }

    // Фильтруем Slave-параметры - они не отображаются в таблице редактирования
    // (значение Slave вычисляется автоматически в ИВК от Master-параметра)
    return schema.filter(paramSchema => paramSchema.role !== 'Slave').map(paramSchema => {
      const isBallast = resolveIsBallastFlag(paramSchema);
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

      // Объединяем методы из схемы + ELIS-метод (если есть) + выбранный метод
      const methodOptions = [...paramSchema.methodOptions];

      // Добавляем ELIS-метод (сохранённый при загрузке или из handleElisData), если его нет в списке
      // Это позволяет вернуться к методу из ELIS после выбора другого метода из справочника
      const elisOptionKey = `method.${paramSchema.key}__elisOption`;
      const elisOption = store.formData[elisOptionKey];

      if (elisOption) {
        const elisMethodName = elisOption.Name || elisOption.name;
        const alreadyInList = methodOptions.find(m => m.name === elisMethodName);
        if (elisMethodName && !alreadyInList) {
          const elisMethodOption = {
            id: elisOption.Id || elisOption.id || 0,
            use: Boolean(elisOption.Use ?? elisOption.use),
            idParameter: elisOption.IdParameter || elisOption.idParameter || 0,
            name: elisMethodName,
            isDefault: Boolean(elisOption.IsDefault ?? elisOption.isDefault),
            limitValueActivate: Boolean(elisOption.LimitValueActivate ?? elisOption.limitValueActivate),
            limitValue: elisOption.LimitValue || elisOption.limitValue || 0,
            limitValueString: elisOption.LimitValueString || elisOption.limitValueString || ''
          };
          methodOptions.push(elisMethodOption);
        }
      }

      // Добавляем текущий выбранный метод, если его нет в списке
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
    // Нередактируемый параметр - не пересчитывать
    if (!param.editable) {
      return param.values.result || param.values.measurement;
    }
    
    if (param.isBallast) {
      return param.values.measurement;
    }
    
    // Если есть результат из ELIS и measurement заполнено из ELIS, используем его
    if (param.elisFlags.result && param.elisFlags.measurement) {
      return param.values.result;
    }

    const measurementValue = parseFloat(param.values.measurement.replace(',', '.'));
    if (isNaN(measurementValue)) {
      return '-';
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
    console.log('handleMeasurementFieldChange');
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

    const resultField = `result.${paramKey}`;

    // Если result загружен из ЕЛИС - не синхронизировать балластный параметр
    // Это предотвращает перезапись result из ЕЛИС при программном изменении measurement
    const isResultFromElis = store.formData[`${resultField}__elisFilled`] === true;
    if (isResultFromElis) {
      logger.debug('[handleMeasurementFieldChange] result из ЕЛИС, пропускаем синхронизацию балластного параметра', {
        paramKey
      });
      return;
    }

    // Проверяем, является ли новое значение оригинальным из ELIS
    const elisOriginal = store.formData[`${measurementField}__elisOriginal`];
    const isBackToElisValue = elisOriginal !== undefined &&
      normalizedNew === normalizeValue(elisOriginal);

    // Определяем source: ELIS если текущий флаг, ReturnToELIS если вернулись к оригиналу
    const currentElisFilled = store.formData[`${measurementField}__elisFilled`] === true;
    const source = currentElisFilled
      ? DataSource.ELIS
      : isBackToElisValue
        ? DataSource.ReturnToELIS
        : DataSource.Manual;

    measurementGuard.add(measurementField);
    resultGuard.add(resultField);
    store.syncBallastParameter(paramKey, newValue?.toString() ?? '', {
      source,
      comment: source === DataSource.ELIS
        ? 'Синхронизация с ELIS'
        : source === DataSource.ReturnToELIS
          ? 'Возврат к значению ELIS'
          : 'Синхронизация балластного параметра'
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

    // Modal поведение: только при ELIS и редактируемом параметре
    const isModalMode = isElisUsed.value && schema.editable;
    if (!isModalMode) {
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
    console.log('handleMeasurementUpdate');
    const param = findParameter(event.paramKey);
    if (!param) {
      logger.warn(`Параметр с ключом ${event.paramKey} не найден`);
      return;
    }

    const measurementKey = `value.${event.paramKey}`;
    const resultKey = `result.${event.paramKey}`;
    const currentMeasurement = store.formData[measurementKey] ?? '';
    // Нормализуем значения для корректного сравнения
    const normalizedCurrent = normalizeValue(currentMeasurement);
    const normalizedNew = normalizeValue(event.value);
    const measurementChanged = normalizedCurrent !== normalizedNew;

    if(!measurementChanged) {
      console.log(`Значение параметра с ключом ${event.paramKey} не было изменено`);
      return;
    }
    else {
      console.log(`Значение параметра с ключом ${event.paramKey} было изменено: ${normalizedCurrent} -> ${normalizedNew}`);
    }
      
      
    const isBallast = param.isBallast === true;
    if (isBallast) {
      // Проверяем, вернулось ли значение к оригинальному из ELIS
      const elisOriginal = store.formData[`${measurementKey}__elisOriginal`];
      const normalizedElisOriginal = elisOriginal !== undefined ? normalizeValue(elisOriginal) : undefined;
      const isBackToElisValue = elisOriginal !== undefined &&
        normalizedNew === normalizedElisOriginal;

      console.log(`[handleMeasurementUpdate] isBallast ${measurementKey}:`, {
        newValue: event.value,
        normalizedNew,
        elisOriginal,
        normalizedElisOriginal,
        isBackToElisValue
      });

      // handleMeasurementUpdate вызывается ТОЛЬКО при ручном изменении через UI
      // Поэтому результат всегда должен пересчитываться (флаги ELIS сбрасываются)
      if (!measurementChanged && store.formData[resultKey] === event.value) {
        return;
      }
      measurementGuard.add(measurementKey);
      resultGuard.add(resultKey);
      // syncBallastParameter запишет историю для measurement и result
      // Если вернулись к оригиналу ELIS, передаем source: ReturnToELIS
      const source = isBackToElisValue ? DataSource.ReturnToELIS : DataSource.Manual;
      store.syncBallastParameter(event.paramKey, event.value, {
        source,
        comment: isBackToElisValue ? 'Возврат к значению ELIS' : 'Изменено оператором'
      });
      return;
    }

    if (measurementChanged) {
      measurementGuard.add(measurementKey);

      // Проверяем, вернулось ли значение к оригинальному из ELIS
      const elisOriginal = store.formData[`${measurementKey}__elisOriginal`];
      const normalizedElisOriginal = elisOriginal !== undefined ? normalizeValue(elisOriginal) : undefined;
      const isBackToElisValue = elisOriginal !== undefined &&
        normalizedNew === normalizedElisOriginal;

      console.log(`[handleMeasurementUpdate] ${measurementKey}:`, {
        newValue: event.value,
        normalizedNew,
        elisOriginal,
        normalizedElisOriginal,
        isBackToElisValue
      });

      // Записываем историю изменения measurement (для обычных параметров)
      if (isBackToElisValue) {
        trackReturnToElis(measurementKey, event.value, currentMeasurement);
      } else {
        trackManualChange(measurementKey, event.value, currentMeasurement);
      }

      store.bulkUpdateFields({
        [measurementKey]: event.value,
        [`${measurementKey}__elisFilled`]: isBackToElisValue // Восстанавливаем флаг если вернулись к оригиналу
      });
    }

    // Результат пересчитывается только для редактируемых параметров
    // При изменении measurement флаг manualOverride сбрасывается
    if (!param.editable) {
      return;
    }

    const tempParam = {
      ...param,
      values: { ...param.values, measurement: event.value },
      // Сбрасываем флаг ELIS для measurement, т.к. значение изменено вручную
      elisFlags: {
        ...param.elisFlags,
        measurement: false
      }
    };
    const newResult = recalculateResult(tempParam);
    const previousResult = store.formData[resultKey] ?? '';
    if (previousResult !== newResult) {
      resultGuard.add(resultKey);

      // Проверяем, вернулось ли значение result к оригинальному из ELIS
      const resultElisOriginal = store.formData[`${resultKey}__elisOriginal`];
      const isResultBackToElis = resultElisOriginal !== undefined &&
        normalizeValue(newResult) === normalizeValue(resultElisOriginal);

      store.bulkUpdateFields({
        [resultKey]: newResult,
        [`${resultKey}__elisFilled`]: isResultBackToElis, // Восстанавливаем флаг если вернулись к оригиналу
        [`${resultKey}__manualOverride`]: false  // Сбрасываем manualOverride при пересчёте
      });

      // Записываем историю для результата (пересчитан из измерения)
      trackAutoFill(resultKey, newResult, previousResult);
      console.log('trackAutoFill');
    }
  }

  /**
   * Обработчик обновления метода испытаний
   * Примечание: пересчёт результата при изменении метода отключён
   */
  function handleMethodUpdate(event: MethodUpdateEvent) {
    console.log('handleMethodUpdate');
    const param = findParameter(event.paramKey);
    if (!param) {
      logger.warn(`Параметр с ключом ${event.paramKey} не найден`);
      return;
    }

    const methodOption = event.method;
    const methodJson = methodOption ? serializeMethodOption(methodOption) : '';

    logger.debug('[usePassportEditor] handleMethodUpdate', {
      paramKey: event.paramKey,
      methodName: methodOption?.name || null
    });

    // Сохраняем предыдущее значение для истории
    const methodKey = `method.${event.paramKey}`;
    const previousMethodJson = store.formData[methodKey] || '';
    const previousMethod = tryParseMethod(previousMethodJson);
    const previousMethodName = previousMethod?.name || '';
    const newMethodName = methodOption?.name || '';

    // Определяем, изменилось ли название метода (для elisFlags)
    const methodNameChanged = newMethodName !== previousMethodName;

    // Проверяем, вернулся ли метод к оригинальному значению из ELIS
    // Используем __elisOption (полный объект) или __elisOriginal (имя) для определения
    const elisOption = store.formData[`${methodKey}__elisOption`];
    const elisMethodName = elisOption?.Name || elisOption?.name;
    const methodElisOriginal = store.formData[`${methodKey}__elisOriginal`];
    const isBackToElisMethod = (elisMethodName !== undefined && newMethodName === elisMethodName) ||
      (methodElisOriginal !== undefined && newMethodName === methodElisOriginal);

    // Если название метода не изменилось - сохраняем текущий флаг ELIS
    // Если название изменилось - проверяем, вернулись ли к оригиналу ELIS
    const currentMethodElisFlag = store.formData[`${methodKey}__elisFilled`] === true;
    const newMethodElisFlag = methodNameChanged ? isBackToElisMethod : currentMethodElisFlag;

    const updates: Record<string, any> = {
      [methodKey]: methodJson,
      [`${methodKey}__elisFilled`]: newMethodElisFlag
    };

    store.bulkUpdateFields(updates);

    // Записать историю изменения метода (только name, а не весь объект)
    if (methodNameChanged) {
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
    console.log('handleResultUpdate');
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
