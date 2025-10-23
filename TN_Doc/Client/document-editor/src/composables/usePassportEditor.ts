import { computed } from 'vue';
import { useDocumentStore } from '@/stores/documentStore';
import type {
  PassportEditConfig,
  PassportQualityParameter,
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
   * Качественные параметры из конфигурации
   */
  const qualityParameters = computed<PassportQualityParameter[]>(() => {
    return passportConfig.value?.qualityParameters || [];
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
      console.warn(`Параметр с ключом ${event.paramKey} не найден`);
      return;
    }

    param.values.measurement = event.value;
    param.values.result = recalculateResult(param);
    store.isDirty = true;
  }

  /**
   * Обработчик обновления метода испытаний
   */
  function handleMethodUpdate(event: MethodUpdateEvent) {
    const param = findParameter(event.paramKey);
    if (!param) {
      console.warn(`Параметр с ключом ${event.paramKey} не найден`);
      return;
    }

    param.method.selected = event.methodName;
    param.values.result = recalculateResult(param);
    store.isDirty = true;
  }

  /**
   * Обработчик обновления результата (ручное редактирование)
   */
  function handleResultUpdate(event: ResultUpdateEvent) {
    const param = findParameter(event.paramKey);
    if (!param) {
      console.warn(`Параметр с ключом ${event.paramKey} не найден`);
      return;
    }

    param.values.result = event.value;
    store.isDirty = true;
    param.elisFlags.result = false;
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
