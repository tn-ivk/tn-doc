import { logger } from '@tn-doc/shared';
import { computed, watch } from 'vue';
import { useDocumentStore } from '@/stores/documentStore';
import type {
  PassportEditConfig,
  PassportQualityParameter,
  MethodOption,
  MeasurementUpdateEvent,
  MethodUpdateEvent,
  ResultUpdateEvent
} from '@/types/passport.types';

interface SerializedMethodPayload {
  Id: number;
  Use: boolean;
  IdParameter: number;
  Name: string;
  LimitValueActivate: boolean;
  LimitValue: number;
  LimitValueString: string;
  IsDefault: boolean;
}

interface SerializedMethodPayload {
  Id: number;
  Use: boolean;
  IdParameter: number;
  Name: string;
  LimitValueActivate: boolean;
  LimitValue: number;
  LimitValueString: string;
  IsDefault: boolean;
}

const serializeMethodOption = (option: MethodOption | null): string => {
  if (!option || !option.name || option.name.trim() === '') {
    return '';
  }

  const payload: SerializedMethodPayload = {
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
};

const tryParseSerializedMethod = (value: unknown): MethodOption | null => {
  if (typeof value !== 'string' || value.trim() == '') {
    return null;
  }

  try {
    const parsed = JSON.parse(value) as Partial<SerializedMethodPayload>;
    if (!parsed || typeof parsed !== 'object' || typeof parsed.Name !== 'string') {
      return null;
    }

    const payload: SerializedMethodPayload = {
      Id: typeof parsed.Id === 'number' ? parsed.Id : 0,
      Use: Boolean(parsed.Use),
      IdParameter: typeof parsed.IdParameter === 'number' ? parsed.IdParameter : 0,
      Name: parsed.Name,
      LimitValueActivate: Boolean(parsed.LimitValueActivate),
      LimitValue: typeof parsed.LimitValue === 'number' ? parsed.LimitValue : 0,
      LimitValueString: typeof parsed.LimitValueString === 'string' ? parsed.LimitValueString : '',
      IsDefault: Boolean(parsed.IsDefault)
    };

    return {
      id: payload.Id,
      use: payload.Use,
      idParameter: payload.IdParameter,
      name: payload.Name,
      isDefault: payload.IsDefault,
      limitValueActivate: payload.LimitValueActivate,
      limitValue: payload.LimitValue,
      limitValueString: payload.LimitValueString
    };
  } catch (error) {
    logger.warn('usePassportEditor: parse method failed', {
      value,
      error: error instanceof Error ? error.message : String(error)
    });
    return null;
  }
};

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
   * Качественные параметры из конфигурации
   */
  const qualityParameters = computed<PassportQualityParameter[]>(() => {
    return passportConfig.value?.qualityParameters || [];
  });

let methodsHydrated = false;

watch(
  () => qualityParameters.value,
  (params) => {
    if (methodsHydrated || !params || params.length === 0) {
      return;
    }

    params.forEach((param) => {
      const storedValue = store.formData[`method.${param.key}`];
      const parsedOption = tryParseSerializedMethod(storedValue);
      if (!parsedOption) {
        return;
      }

      const existingIndex = param.method.options.findIndex(
        (option) => option.name === parsedOption.name
      );

      if (existingIndex >= 0) {
        param.method.options[existingIndex] = {
          ...param.method.options[existingIndex],
          ...parsedOption
        };
      } else {
        param.method.options.push(parsedOption);
      }

      if (!param.method.selected || param.method.selected.trim() === '') {
        param.method.selected = parsedOption.name;
      }
    });

    methodsHydrated = true;
  },
  { immediate: true }
);

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

    param.values.measurement = event.value;
    param.values.result = recalculateResult(param);
    store.bulkUpdateFields({
      [`value.${event.paramKey}`]: event.value,
      [`value.${event.paramKey}__elisFilled`]: false,
      [`result.${event.paramKey}`]: param.values.result,
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
    param.method.selected = methodOption?.name || '';
    if (methodOption) {
      const existing = param.method.options.find(opt => opt.name === methodOption.name);
      if (!existing) {
        param.method.options.push(methodOption);
      }
    }
    param.values.result = recalculateResult(param);
    store.bulkUpdateFields({
      [`method.${event.paramKey}`]: serializeMethodOption(methodOption),
      [`method.${event.paramKey}__elisFilled`]: false,
      [`result.${event.paramKey}`]: param.values.result,
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

    param.values.result = event.value;
    param.elisFlags.result = false;
    store.bulkUpdateFields({
      [`result.${event.paramKey}`]: event.value,
      [`result.${event.paramKey}__elisFilled`]: false
    });
  }

  return {
    passportConfig,
    qualityParameters,
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
