import { useDocumentStore } from '@/stores/documentStore';
import { DataSource, type FieldHistoryEntry } from '@/types/history.types';

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
      : source === DataSource.ElisMissing
      ? 'ELIS'
      : source === DataSource.ReturnToELIS
      ? MANUAL_AUTHOR  // Возврат к ELIS - это действие пользователя
      : source === DataSource.DefaultMethod
      ? 'Система'  // Метод по умолчанию - системное действие
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
   * Снять флаг ElisMissing для поля (только текущая сессия)
   */
  const clearElisMissing = (fieldKey: string) => {
    if (!store.elisMissingFields[fieldKey]) {
      return;
    }

    const next = { ...store.elisMissingFields };
    delete next[fieldKey];
    store.elisMissingFields = next;
  };

  /**
   * Сбросить все флаги ElisMissing (только текущая сессия)
   */
  const resetElisMissing = () => {
    if (Object.keys(store.elisMissingFields).length === 0) {
      return;
    }
    store.elisMissingFields = {};
  };

  /**
   * Отследить ручное изменение поля
   */
  const trackManualChange = (fieldKey: string, newValue: any, previousValue?: any) => {
    // Нормализуем значения для корректного сравнения (точка/запятая в числах)
    const newValueNormalized = normalizeValue(newValue);
    const previousValueNormalized = normalizeValue(previousValue);

    // Если нормализованные значения совпадают, не создаем запись в истории
    if (newValueNormalized === previousValueNormalized) {
      return;
    }

    const entry = createHistoryEntry(
      DataSource.Manual,
      newValueNormalized,
      previousValueNormalized || undefined,
      'Отредактировано вручную'
    );

    addHistoryEntry(fieldKey, entry);
  };

  /**
   * Отследить загрузку из ELIS
   */
  const trackElisLoad = (fieldKey: string, value: any, protocolNumber?: string) => {
    clearElisMissing(fieldKey);

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
   * Отследить поле, которое ожидалось из ELIS, но не было загружено
   */
  const trackElisMissing = (fieldKey: string, _protocolNumber?: string) => {
    if (store.elisMissingFields[fieldKey]) {
      return;
    }

    store.elisMissingFields = {
      ...store.elisMissingFields,
      [fieldKey]: true
    };
  };

  /**
   * Отследить возврат к оригинальному значению ELIS
   */
  const trackReturnToElis = (fieldKey: string, newValue: any, previousValue?: any) => {
    const newValueNormalized = normalizeValue(newValue);
    const previousValueNormalized = normalizeValue(previousValue);

    if (newValueNormalized === previousValueNormalized) {
      return;
    }

    const entry = createHistoryEntry(
      DataSource.ReturnToELIS,
      newValueNormalized,
      previousValueNormalized || undefined,
      'Возврат к значению ELIS'
    );

    addHistoryEntry(fieldKey, entry);
  };

  /**
   * Отследить автоматическое заполнение поля
   * Используется когда значение установлено системой, но не из внешнего источника
   */
  const trackAutoFill = (fieldKey: string, newValue: any, previousValue?: any) => {
    const entry: FieldHistoryEntry = {
      source: DataSource.Auto,
      modifiedAt: new Date().toISOString(),
      modifiedBy: 'Система',
      value: String(newValue ?? ''),
      previousValue: String(previousValue ?? ''),
      comment: 'Заполнено автоматически'
    };
    addHistoryEntry(fieldKey, entry);
  };

  /**
   * Отследить подстановку метода по умолчанию
   * Используется при загрузке документа, если метод был автоматически подставлен из конфигурации
   */
  const trackDefaultMethod = (fieldKey: string, methodName: string) => {
    const entry = createHistoryEntry(
      DataSource.DefaultMethod,
      methodName,
      undefined,
      'Метод по умолчанию'
    );
    addHistoryEntry(fieldKey, entry);
  };

  /**
   * Проверить наличие ElisMissing для поля
   */
  const hasElisMissing = (fieldKey: string): boolean => {
    return store.elisMissingFields[fieldKey] === true;
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

  return {
    createHistoryEntry,
    addHistoryEntry,
    trackManualChange,
    trackElisLoad,
    trackElisMissing,
    trackIVKRounding,
    trackReturnToElis,
    trackAutoFill,
    trackDefaultMethod,
    resetElisMissing,
    getFieldHistory,
    getLastSource,
    getLastModifiedBy,
    hasElisMissing
  };
}
