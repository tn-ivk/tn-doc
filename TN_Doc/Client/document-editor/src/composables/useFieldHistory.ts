import { useDocumentStore } from '@/stores/documentStore';
import { DataSource, type FieldHistoryEntry, MAX_HISTORY_ENTRIES } from '@/types/history.types';
import { logger } from '@tn-doc/shared';

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

    return {
      source,
      modifiedAt: new Date().toISOString(),
      modifiedBy,
      value,
      previousValue,
      comment
    };
  };

  /**
   * Добавить запись в историю поля
   */
  const addHistoryEntry = (fieldKey: string, entry: FieldHistoryEntry) => {
    if (!store.formHistory[fieldKey]) {
      store.formHistory[fieldKey] = [];
    }

    store.formHistory[fieldKey].push(entry);

    // Ограничиваем размер истории (FIFO - First In First Out)
    if (store.formHistory[fieldKey].length > MAX_HISTORY_ENTRIES) {
      store.formHistory[fieldKey].shift(); // Удаляем самую старую запись
    }

    logger.debug(`[useFieldHistory] Добавлена запись в историю: поле="${fieldKey}", источник=${entry.source}, автор=${entry.modifiedBy}`);
  };

  /**
   * Отследить ручное изменение поля
   */
  const trackManualChange = (fieldKey: string, newValue: any, previousValue?: any) => {
    // Если значение не изменилось, не создаем запись в истории
    const newValueStr = String(newValue);
    const previousValueStr = previousValue !== undefined ? String(previousValue) : '';

    if (newValueStr === previousValueStr) {
      logger.debug(`[useFieldHistory] Значение не изменилось, запись в историю не создана: поле="${fieldKey}", значение="${newValueStr}"`);
      return;
    }

    logger.debug(`[useFieldHistory] Создана запись о ручном изменении: поле="${fieldKey}", старое="${previousValueStr}", новое="${newValueStr}"`);

    const entry = createHistoryEntry(
      DataSource.Manual,
      newValueStr,
      previousValueStr || undefined,
      'Отредактировано вручную'
    );

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
