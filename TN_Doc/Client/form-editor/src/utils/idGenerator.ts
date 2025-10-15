// Генератор уникальных ID для элементов формы

let idCounter = 0

/**
 * Генерирует уникальный ID с заданным префиксом
 * @param prefix Префикс для ID
 * @returns Уникальный ID в формате prefix_timestamp_counter_random
 */
export function generateUniqueId(prefix: string): string {
  idCounter++
  const timestamp = Date.now()
  const random = Math.random().toString(36).substr(2, 9)
  return `${prefix}_${timestamp}_${idCounter}_${random}`
}

/**
 * Сбрасывает счетчик ID (используется для тестирования)
 */
export function resetIdCounter(): void {
  idCounter = 0
}
