import { computed } from 'vue';
import { useDocumentStore } from '@/stores/documentStore';
import type {
  PassportEditConfig,
  PassportQualityParameter,
  MethodOption,
  HalValueUpdateEvent,
  MethodUpdateEvent,
  PrintValueUpdateEvent
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
   * Пересчитать значение для печати (PrintValue) на основе метода и значения ХАЛ
   */
  function recalculatePrintValue(param: PassportQualityParameter): string {
    // Найти выбранный метод
    const selectedMethod = param.method.options.find(
      (m: MethodOption) => m.name === param.method.selected
    );

    // Если есть ValueString из ELIS и ХАЛ заполнено из ELIS, используем его
    if (param.elisFlags.printValue && param.elisFlags.hal) {
      return param.values.printValue;
    }

    // Парсим значение ХАЛ
    const halValue = parseFloat(param.values.hal.replace(',', '.'));

    // Если значение невалидное, возвращаем прочерк
    if (isNaN(halValue)) {
      return '-';
    }

    // Если у метода активирован лимит и значение ниже порога
    if (
      selectedMethod?.limitValueActivate &&
      selectedMethod.limitValue !== undefined &&
      halValue < selectedMethod.limitValue
    ) {
      return selectedMethod.limitValueString || '-';
    }

    // Иначе возвращаем само значение ХАЛ
    return param.values.hal;
  }

  /**
   * Определить, редактируема ли ячейка печати
   * Ячейка редактируема, если limitValueActivate === true И halValue < limitValue
   */
  function isPrintCellEditable(param: PassportQualityParameter): boolean {
    const selectedMethod = param.method.options.find(
      (m: MethodOption) => m.name === param.method.selected
    );

    if (!selectedMethod || !selectedMethod.limitValueActivate) {
      return false;
    }

    const halValue = parseFloat(param.values.hal.replace(',', '.'));
    if (isNaN(halValue)) {
      return false;
    }

    return selectedMethod.limitValue !== undefined && halValue < selectedMethod.limitValue;
  }

  /**
   * Обработчик обновления значения ХАЛ
   */
  function handleHalValueUpdate(event: HalValueUpdateEvent) {
    const param = findParameter(event.paramKey);
    if (!param) {
      console.warn(`Параметр с ключом ${event.paramKey} не найден`);
      return;
    }

    // Обновляем значение ХАЛ
    param.values.hal = event.value;

    // Пересчитываем значение для печати
    param.values.printValue = recalculatePrintValue(param);

    // Отмечаем, что документ изменён
    store.isDirty = true;

    console.log(`[usePassportEditor] ХАЛ обновлено: ${event.paramKey} = ${event.value}, PrintValue = ${param.values.printValue}`);
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

    // Обновляем выбранный метод
    param.method.selected = event.methodName;

    // Пересчитываем значение для печати (может измениться редактируемость)
    param.values.printValue = recalculatePrintValue(param);

    // Отмечаем, что документ изменён
    store.isDirty = true;

    console.log(`[usePassportEditor] Метод обновлен: ${event.paramKey} = ${event.methodName}, PrintValue = ${param.values.printValue}`);
  }

  /**
   * Обработчик обновления значения для печати (ручное редактирование)
   */
  function handlePrintValueUpdate(event: PrintValueUpdateEvent) {
    const param = findParameter(event.paramKey);
    if (!param) {
      console.warn(`Параметр с ключом ${event.paramKey} не найден`);
      return;
    }

    // Обновляем значение для печати
    param.values.printValue = event.value;

    // Отмечаем, что документ изменён
    store.isDirty = true;

    // Сбрасываем флаг ELIS заполнения (пользователь ввел вручную)
    param.elisFlags.printValue = false;

    console.log(`[usePassportEditor] PrintValue обновлено вручную: ${event.paramKey} = ${event.value}`);
  }

  return {
    // Computed
    passportConfig,
    qualityParameters,
    isElisUsed,
    hasQualityParameters,

    // Methods
    findParameter,
    recalculatePrintValue,
    isPrintCellEditable,

    // Event handlers
    handleHalValueUpdate,
    handleMethodUpdate,
    handlePrintValueUpdate
  };
}
