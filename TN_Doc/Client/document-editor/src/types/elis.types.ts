/**
 * Типы для работы с данными ELIS (Единая Лабораторная Информационная Система)
 */

/**
 * Полные данные протокола ELIS для паспорта качества
 */
export interface ElisPassportData {
  /** Информация о лаборатории */
  labInfo?: ElisLabInfo;
  /** Параметры качества */
  parameters?: Record<string, ElisParameter>;
  /** Подписанты (представители) */
  signers?: ElisSigners;
  /** Период отбора пробы */
  startPeriodTime?: string;
  endPeriodTime?: string;
  /** Номер протокола испытаний */
  protocolNumber?: string;
  /** Точка поставки */
  pointDeliveryName?: string;
}

/**
 * Информация о лаборатории
 */
export interface ElisLabInfo {
  /** Название лаборатории */
  labName?: string;
  /** Адрес лаборатории */
  labAddress?: string;
  /** Номер аттестата аккредитации */
  accreditationNumber?: string;
  /** Альтернативные названия полей */
  [key: string]: any;
}

/**
 * Параметр качества из ELIS
 * Ключ - русское полное название параметра (например, "Массовая доля воды(%)")
 */
export interface ElisParameter {
  /** Числовое значение измерения */
  value?: number;
  /** Текстовое представление результата (может содержать "Менее 4,0", "Более 10", и т.д.) */
  valueString?: string;
  /** Название метода испытаний (ГОСТ, ТУ и т.д.) */
  testMethodName?: string;
}

/**
 * Подписанты документа
 */
export interface ElisSigners {
  /** Представитель лаборатории */
  laboratory?: ElisLaboratoryRepresentative;
}

/**
 * Представитель лаборатории
 */
export interface ElisLaboratoryRepresentative {
  /** Имя */
  givenName?: string;
  /** Отчество */
  middleName?: string;
  /** Фамилия */
  familyName?: string;
  /** Должность */
  post?: string;
  /** Организация */
  company?: string;
}

/**
 * Метод испытаний, созданный из ELIS данных
 */
export interface ElisMethodData {
  /** Название метода (из testMethodName) */
  name: string;
  /** Пороговое значение (парсится из valueString) */
  limitValue?: number;
  /** Оператор сравнения */
  operator?: 'less' | 'more' | 'less_equal' | 'more_equal';
  /** Строка для отображения при превышении порога */
  limitValueString?: string;
}
