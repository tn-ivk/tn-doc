/**
 * Простой EventBus для координации popup-ов истории изменений.
 * Позволяет закрывать все открытые popup-ы перед открытием нового.
 */

type Listener = () => void;

const listeners: Set<Listener> = new Set();

/**
 * Подписаться на событие закрытия всех popup-ов
 */
export function onCloseAllHistoryPopups(callback: Listener): () => void {
  listeners.add(callback);
  // Возвращаем функцию отписки
  return () => {
    listeners.delete(callback);
  };
}

/**
 * Закрыть все открытые popup-ы истории
 */
export function closeAllHistoryPopups(): void {
  listeners.forEach(callback => callback());
}
