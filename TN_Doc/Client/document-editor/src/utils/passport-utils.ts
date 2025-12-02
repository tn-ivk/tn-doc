/**
 * Утилиты для работы с паспортами качества
 */

/**
 * Нормализовать значение для сравнения
 *
 * Приводит значение к строке, заменяет запятую на точку,
 * и преобразует числа к единому формату
 *
 * @param value - значение для нормализации
 * @returns нормализованная строка
 */
export const normalizeValue = (value: any): string => {
  if (value === null || value === undefined || value === '') {
    return '';
  }

  const strValue = String(value).trim();
  const normalized = strValue.replace(',', '.');

  // Если это число, проверяем что преобразование корректно
  const numValue = parseFloat(normalized);
  if (!isNaN(numValue)) {
    return numValue.toString();
  }

  return strValue;
};
