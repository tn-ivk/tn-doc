import { useDocumentStore } from '@/stores/documentStore';
import { DataSource, type FieldHistoryEntry } from '@/types/history.types';
import { logger } from '@tn-doc/shared';

/**
 * Нормализовать значение для сравнения
 * Приводит числа к единому формату (точка вместо запятой, удаляет лишние пробелы)
 */
const normalizeValue = (value: any): string => {
  // ДИАГНОСТИКА: Логируем входное значение
  logger.debug('[normalizeValue] Входное значение', {
    value,
    type: typeof value,
    isNull: value === null,
    isUndefined: value === undefined,
    isEmpty: value === ''
  });

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
    logger.debug('[normalizeValue] Нормализовано как число', {
      original: value,
      normalized
    });
    return normalized;
  }

  // Для нечисловых значений возвращаем оригинальную строку (без замены запятой)
  logger.debug('[normalizeValue] Нормализовано как строка', {
    original: value,
    normalized: strValue
  });
  return strValue;
};

/**
 * Композабл для работы с историей изменений полей
 */
export function useFieldHistory() {
  const store = useDocumentStore();

  /**
   * Константа автора вручную внесённых изменений
   * (ФИО недоступно на момент редактирования)
   */
  const MANUAL_AUTHOR = 'Пользователь';

  /**
   * Создать запись истории
   */
  const createHistoryEntry = (
    source: DataSource,
    value: string,
    previousValue?: string,
    comment?: string
  ): FieldHistoryEntry => {
    const modifiedBy = source === DataSource.ELIS
      ? 'ELIS'
      : source === DataSource.IVK
      ? 'IVK'
      : MANUAL_AUTHOR;

    const entry = {
      source,
      modifiedAt: new Date().toISOString(),
      modifiedBy,
      value,
      previousValue,
      comment
    };


    return entry;
  };

  /**
   * Добавить запись в историю поля
   */
  const addHistoryEntry = (fieldKey: string, entry: FieldHistoryEntry) => {
    if (!store.formHistory[fieldKey]) {
      store.formHistory[fieldKey] = [];
    }

    store.formHistory[fieldKey].push(entry);
  };

  /**
   * Отследить ручное изменение поля
   */
  const trackManualChange = (fieldKey: string, newValue: any, previousValue?: any) => {
    // ДИАГНОСТИКА: Логируем входные параметры
    logger.info('[trackManualChange] Начало обработки', {
      fieldKey,
      newValue,
      previousValue,
      newValueType: typeof newValue,
      previousValueType: typeof previousValue
    });

    // Нормализуем значения для корректного сравнения (точка/запятая в числах)
    const newValueNormalized = normalizeValue(newValue);
    const previousValueNormalized = normalizeValue(previousValue);

    // ДИАГНОСТИКА: Логируем нормализованные значения и результат сравнения
    logger.info('[trackManualChange] После нормализации', {
      fieldKey,
      newValueNormalized,
      previousValueNormalized,
      areEqual: newValueNormalized === previousValueNormalized,
      stringComparison: `"${newValueNormalized}" === "${previousValueNormalized}"`
    });

    // Если нормализованные значения совпадают, не создаем запись в истории
    if (newValueNormalized === previousValueNormalized) {
      logger.warn('[trackManualChange] ПРОПУСК: значения идентичны после нормализации', {
        fieldKey,
        reason: 'normalized values are equal'
      });
      return;
    }

    const entry = createHistoryEntry(
      DataSource.Manual,
      newValueNormalized,
      previousValueNormalized || undefined,
      'Отредактировано вручную'
    );

    logger.info('[trackManualChange] Создана запись истории', {
      fieldKey,
      entry
    });

    addHistoryEntry(fieldKey, entry);
  };

  /**
   * Отследить загрузку из ELIS
   */
  const trackElisLoad = (fieldKey: string, value: any, protocolNumber?: string) => {

    const comment = protocolNumber
      ? `Загружено из протокола ${protocolNumber}`
      : 'Загружено из протокола ЕЛИС';

    const entry = createHistoryEntry(
      DataSource.ELIS,
      String(value),
      undefined,
      comment
    );

    addHistoryEntry(fieldKey, entry);
  };

  /**
   * Отследить округление ИВК
   */
  const trackIVKRounding = (
    fieldKey: string,
    originalValue: any,
    roundedValue: any,
    roundDigits: number
  ) => {

    const comment = `Округлено: ${originalValue} → ${roundedValue} (${roundDigits} знаков)`;

    const entry = createHistoryEntry(
      DataSource.IVK,
      String(roundedValue),
      String(originalValue),
      comment
    );

    addHistoryEntry(fieldKey, entry);
  };

  /**
   * Получить историю поля
   */
  const getFieldHistory = (fieldKey: string): FieldHistoryEntry[] => {
    return store.formHistory[fieldKey] || [];
  };

  /**
   * Получить последний источник изменений
   */
  const getLastSource = (fieldKey: string): DataSource => {
    const history = getFieldHistory(fieldKey);
    if (history.length === 0) {
      return DataSource.Unknown;
    }
    return history[history.length - 1].source;
  };

  /**
   * Получить последнего автора изменений
   */
  const getLastModifiedBy = (fieldKey: string): string | undefined => {
    const history = getFieldHistory(fieldKey);
    if (history.length === 0) {
      return undefined;
    }
    return history[history.length - 1].modifiedBy;
  };

  /**
   * Очистить историю поля
   */
  const clearFieldHistory = (fieldKey: string) => {
    delete store.formHistory[fieldKey];
    logger.debug(`[useFieldHistory] История поля очищена: поле="${fieldKey}"`);
  };

  return {
    createHistoryEntry,
    addHistoryEntry,
    trackManualChange,
    trackElisLoad,
    trackIVKRounding,
    getFieldHistory,
    getLastSource,
    getLastModifiedBy,
    clearFieldHistory
  };
}
