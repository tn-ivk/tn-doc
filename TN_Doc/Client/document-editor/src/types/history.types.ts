/**
 * Источник данных поля
 */
export enum DataSource {
  /** Неизвестный источник (для старых данных) */
  Unknown = 'Unknown',
  /** Загружено из протокола ЕЛИС */
  ELIS = 'ELIS',
  /** Отредактировано вручную пользователем */
  Manual = 'Manual',
  /** Изменено системой ИВК (округление) */
  IVK = 'IVK'
}

/**
 * Запись истории изменения поля
 */
export interface FieldHistoryEntry {
  /** Источник данных */
  source: DataSource;
  /** Дата и время изменения (ISO 8601) */
  modifiedAt: string;
  /** Автор изменения ("Пользователь" для ручных правок) */
  modifiedBy: string;
  /** Значение после изменения */
  value: string;
  /** Предыдущее значение */
  previousValue?: string;
  /** Комментарий */
  comment?: string;
}

/**
 * Конфигурация отображения источника
 */
export interface SourceDisplayConfig {
  /** Иконка PrimeVue (например, 'pi-user-edit') */
  icon: string;
  /** Цвет иконки */
  color: string;
  /** Текст для отображения вместо иконки (опционально) */
  text?: string;
  /** Описание для tooltip */
  description: string;
}

/**
 * Маппинг источников на конфигурацию отображения
 */
export const SOURCE_DISPLAY_CONFIG: Record<DataSource, SourceDisplayConfig> = {
  [DataSource.Unknown]: {
    icon: 'pi-question-circle',
    color: '#9E9E9E',
    description: 'Неизвестный источник'
  },
  [DataSource.ELIS]: {
    icon: 'pi-database',
    color: '#4CAF50',
    text: 'ЕЛИС',
    description: 'Загружено из протокола ЕЛИС'
  },
  [DataSource.Manual]: {
    icon: 'pi-user-edit',
    color: '#2196F3',
    description: 'Отредактировано вручную'
  },
  [DataSource.IVK]: {
    icon: 'pi-cog',
    color: '#FF9800',
    text: 'ИВК',
    description: 'Округлено системой ИВК'
  }
};

/**
 * Максимальное количество записей истории
 */
export const MAX_HISTORY_ENTRIES = 10;
