import { DataSource, type FieldHistoryEntry } from '@/types/history.types';

/**
 * Возвращает только последнюю запись истории поля.
 * Нас интересует только финальное состояние, а не промежуточные шаги ввода.
 *
 * @param history - массив записей истории поля
 * @returns массив с одной записью (последней) или пустой массив
 */
export function compactFieldHistory(history: FieldHistoryEntry[]): FieldHistoryEntry[] {
  if (!history || history.length === 0) return [];

  // ElisMissing не сохраняем на бэкенд
  const filteredHistory = history.filter(entry => entry.source !== DataSource.ElisMissing);

  if (filteredHistory.length === 0) {
    return [];
  }

  // Возвращаем только последнюю запись
  return [filteredHistory[filteredHistory.length - 1]];
}

/**
 * Оставляет только последнюю запись истории для каждого поля.
 * Используется перед сохранением документа на бэкенд.
 *
 * @param formHistory - объект с историей всех полей
 * @returns новый объект с последней записью для каждого поля
 */
export function compactAllFieldsHistory(
  formHistory: Record<string, FieldHistoryEntry[]>
): Record<string, FieldHistoryEntry[]> {
  const result: Record<string, FieldHistoryEntry[]> = {};

  for (const [fieldKey, history] of Object.entries(formHistory)) {
    result[fieldKey] = compactFieldHistory(history);
  }

  return result;
}
