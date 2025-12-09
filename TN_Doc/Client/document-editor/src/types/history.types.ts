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
  IVK = 'IVK',
  /** Ожидалось из ЕЛИС, но не было загружено */
  ElisMissing = 'ElisMissing',
  /** Автоматическое заполнение (без визуального индикатора) */
  Auto = 'Auto',
  /** Возврат к оригинальному значению ЕЛИС */
  ReturnToELIS = 'ReturnToELIS',
  /** Метод по умолчанию (автоматически подставлен из конфигурации) */
  DefaultMethod = 'DefaultMethod'
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
    description: 'Источник неизвестен'
  },
  [DataSource.ELIS]: {
    icon: 'pi-database',
    color: '#4CAF50',
    text: 'ЕЛИС',
    description: 'Из протокола ЕЛИС'
  },
  [DataSource.Manual]: {
    icon: 'pi-user-edit',
    color: '#2196F3',
    description: 'Изменено вручную'
  },
  [DataSource.IVK]: {
    icon: 'pi-cog',
    color: '#FF9800',
    text: 'ИВК',
    description: 'Рассчитано ИВК'
  },
  [DataSource.ElisMissing]: {
    icon: 'pi-exclamation-triangle',
    color: '#f5c24c',
    description: 'Нет в протоколе ЕЛИС'
  },
  [DataSource.Auto]: {
    icon: '',
    color: 'transparent',
    description: 'Заполнено автоматически'
  },
  [DataSource.ReturnToELIS]: {
    icon: 'pi-database',
    color: '#4CAF50',
    text: 'ЕЛИС',
    description: 'Из протокола ЕЛИС'
  },
  [DataSource.DefaultMethod]: {
    icon: 'pi-user-edit',
    color: '#2196F3',
    description: 'Метод по умолчанию'
  }
};
