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
  icon?: string;
  /** SVG path для кастомной иконки */
  svgPath?: string;
  /** SVG viewBox для кастомной иконки */
  svgViewBox?: string;
  /** Цвет иконки */
  color: string;
  /** Текст для отображения вместо иконки (опционально) */
  text?: string;
  /** Описание для tooltip */
  description: string;
}

const FLASK_ICON = {
  path:
    'M384 64L224 64C206.3 64 192 78.3 192 96C192 113.7 206.3 128 224 128L224 279.5L103.5 490.3C98.6 499 96 508.7 96 518.7C96 550.4 121.6 576 153.3 576L486.7 576C518.3 576 544 550.4 544 518.7C544 508.7 541.4 498.9 536.5 490.3L416 279.5L416 128C433.7 128 448 113.7 448 96C448 78.3 433.7 64 416 64L384 64zM288 279.5L288 128L352 128L352 279.5C352 290.6 354.9 301.6 360.4 311.3L402 384L238 384L279.6 311.3C285.1 301.6 288 290.7 288 279.5z',
  viewBox: '0 0 640 640'
} as const;

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
    svgPath: FLASK_ICON.path,
    svgViewBox: FLASK_ICON.viewBox,
    color: '#4CAF50',
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
    svgPath: FLASK_ICON.path,
    svgViewBox: FLASK_ICON.viewBox,
    color: '#4CAF50',
    description: 'Из протокола ЕЛИС'
  },
  [DataSource.DefaultMethod]: {
    icon: 'pi-user-edit',
    color: '#2196F3',
    description: 'Метод по умолчанию'
  }
};
