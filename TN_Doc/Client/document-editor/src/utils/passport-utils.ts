/**
 * Утилиты для работы с паспортами качества
 */

/**
 * Проверяет, является ли строка датой в формате ISO 8601 или datetime-local
 *
 * @param value - значение для проверки
 * @returns true если это дата
 */
function isDateTimeString(value: string): boolean {
  // Паттерн для ISO 8601: 2025-12-02T00:00:00Z, 2025-12-02T03:01:00, 2025-12-02
  return /^\d{4}-\d{2}-\d{2}(T\d{2}:\d{2}(:\d{2})?(\.\d+)?(Z|[+-]\d{2}:\d{2})?)?$/.test(value);
}

/**
 * Нормализовать дату/время для сравнения
 *
 * Сравнивает даты по timestamp (миллисекунды с эпохи Unix),
 * игнорируя разницу в форматировании (UTC vs local time)
 *
 * ВАЖНО: Эта функция НЕ конвертирует исходные данные из ELIS/ИВК,
 * а только обеспечивает корректное сравнение значений
 *
 * @param value - значение даты (ISO 8601 с 'Z', без 'Z', или datetime-local)
 * @returns timestamp в миллисекундах или пустая строка
 *
 * @example
 * // Эти значения будут считаться РАВНЫМИ (один момент времени):
 * normalizeDateTimeForComparison("2025-12-02T00:00:00Z")    // UTC: 1733097600000
 * normalizeDateTimeForComparison("2025-12-02T03:00:00")     // UTC+3: 1733097600000
 *
 * @example
 * // Эти значения будут РАЗНЫМИ:
 * normalizeDateTimeForComparison("2025-12-02T00:00:00Z")    // 1733097600000
 * normalizeDateTimeForComparison("2025-12-02T03:01:00")     // 1733097660000 (+1 мин)
 */
export const normalizeDateTimeForComparison = (value: any): string => {
  if (value === null || value === undefined || value === '') {
    return '';
  }

  const strValue = String(value).trim();

  // Проверяем, является ли это датой
  if (!isDateTimeString(strValue)) {
    return strValue;
  }

  // Конвертируем в Date объект
  const date = new Date(strValue);

  // Проверяем валидность
  if (isNaN(date.getTime())) {
    return strValue; // Не удалось распарсить - возвращаем как есть
  }

  // Возвращаем timestamp для единообразного сравнения
  // Timestamp не зависит от временной зоны
  return date.getTime().toString();
};

/**
 * Нормализовать значение для сравнения
 *
 * Приводит значение к строке, заменяет запятую на точку,
 * и преобразует числа к единому формату
 *
 * ВАЖНО: Для дат/времени НЕ использует parseFloat (т.к. "2025-12-02..." парсится как 2025)
 * Вместо этого возвращает строку как есть - для дат используйте normalizeDateTimeForComparison
 *
 * @param value - значение для нормализации
 * @returns нормализованная строка
 */
export const normalizeValue = (value: any): string => {
  if (value === null || value === undefined || value === '') {
    return '';
  }

  const strValue = String(value).trim();

  // ВАЖНО: НЕ парсим даты как числа!
  // parseFloat("2025-12-02T00:00:00Z") вернёт 2025, что неправильно
  if (isDateTimeString(strValue)) {
    return strValue; // Для дат используйте normalizeDateTimeForComparison
  }

  const normalized = strValue.replace(',', '.');

  // Если это число, проверяем что преобразование корректно
  const numValue = parseFloat(normalized);
  if (!isNaN(numValue)) {
    return numValue.toString();
  }

  return strValue;
};
