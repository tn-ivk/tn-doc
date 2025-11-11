import { logger } from '@tn-doc/shared';
import { computed } from 'vue';
import { useDocumentStore } from '@/stores/documentStore';
import type {
  PassportEditConfig,
  PassportQualityParameter,
  PassportQualityParameterSchema,
  MethodOption,
  MeasurementUpdateEvent,
  MethodUpdateEvent,
  ResultUpdateEvent
} from '@/types/passport.types';

/**
 * Композабл для работы с редактором паспорта качества
 * Содержит логику для работы с качественными параметрами, методами испытаний и ELIS
 */
export function usePassportEditor() {
  const store = useDocumentStore();

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
   * Вычисляемое свойство для объединения схемы + данных из formData
   * Создает полные объекты PassportQualityParameter динамически
   */
  const qualityParameters = computed<PassportQualityParameter[]>(() => {
    const schema = qualityParametersSchema.value;
    if (schema.length === 0) {
      return [];
    }

    return schema.map(paramSchema => {
      // Извлекаем данные из formData
      const measurementValue = store.formData[`value.${paramSchema.key}`] || '';
      const resultValue = store.formData[`result.${paramSchema.key}`] || '';
      const methodJson = store.formData[`method.${paramSchema.key}`] || '';
      const documentNumber = store.formData[`document.${paramSchema.key}`] || '';

      // Парсим выбранный метод
      const selectedMethod = tryParseMethod(methodJson);
      const selectedMethodName = selectedMethod?.name || '';

      // Определяем флаги ELIS
      const elisFlags = {
        measurement: store.formData[`value.${paramSchema.key}__elisFilled`] === true,
        method: store.formData[`method.${paramSchema.key}__elisFilled`] === true,
        result: store.formData[`result.${paramSchema.key}__elisFilled`] === true,
        document: store.formData[`document.${paramSchema.key}__elisFilled`] === true
      };

      // Объединяем методы из схемы + выбранный метод (если есть и его нет в списке)
      const methodOptions = [...paramSchema.methodOptions];
      if (selectedMethod && !methodOptions.find(m => m.name === selectedMethod.name)) {
        methodOptions.push(selectedMethod);
      }

      // Создаем полный объект параметра
      return {
        ...paramSchema,
        values: {
          measurement: measurementValue,
          result: resultValue
        },
        method: {
          selected: selectedMethodName,
          options: methodOptions,
          requiredFill: paramSchema.methodRequiredFill
        },
        document: documentNumber ? {
          number: documentNumber,
          elisFilled: elisFlags.document
        } : undefined,
        elisFlags
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

  /**
   * Пересчитать результат на основе метода и значения measurement
   */
  function recalculateResult(param: PassportQualityParameter): string {
    const selectedMethod = param.method.options.find(
      (m: MethodOption) => m.name === param.method.selected
    );

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
   * Обработчик обновления measurement
   */
  function handleMeasurementUpdate(event: MeasurementUpdateEvent) {
    const param = findParameter(event.paramKey);
    if (!param) {
      logger.warn(`Параметр с ключом ${event.paramKey} не найден`);
      return;
    }

    // Пересчитываем результат с новым measurement
    const tempParam = {
      ...param,
      values: { ...param.values, measurement: event.value }
    };
    const newResult = recalculateResult(tempParam);

    // Обновляем formData
    store.bulkUpdateFields({
      [`value.${event.paramKey}`]: event.value,
      [`value.${event.paramKey}__elisFilled`]: false,
      [`result.${event.paramKey}`]: newResult,
      [`result.${event.paramKey}__elisFilled`]: false
    });
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

    // Пересчитываем результат с новым методом
    const tempParam = {
      ...param,
      method: {
        selected: methodOption?.name || '',
        options: param.method.options
      }
    };
    const newResult = recalculateResult(tempParam);

    store.bulkUpdateFields({
      [`method.${event.paramKey}`]: methodJson,
      [`method.${event.paramKey}__elisFilled`]: false,
      [`result.${event.paramKey}`]: newResult,
      [`result.${event.paramKey}__elisFilled`]: false
    });
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

    store.bulkUpdateFields({
      [`result.${event.paramKey}`]: event.value,
      [`result.${event.paramKey}__elisFilled`]: false
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

  return {
    passportConfig,
    qualityParameters, // Теперь вычисляется динамически из схемы + formData!
    isElisUsed,
    hasQualityParameters,
    findParameter,
    recalculateResult,
    isResultEditable,
    handleMeasurementUpdate,
    handleMethodUpdate,
    handleResultUpdate
  };
}
