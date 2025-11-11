/**
 * Типы для работы с данными ELIS (Единая Лабораторная Информационная Система)
 */

/**
 * Полные данные протокола ELIS для паспорта качества
 *
 * Структура данных ELIS включает:
 * 1. Корневой уровень: основные поля (labName, protocolNumber, pointDeliveryName, даты)
 * 2. labInfo: дополнительная информация о лаборатории (accreditationNumber, ownerName)
 * 3. parameters: качественные показатели (русские полные названия)
 * 4. signers: подписанты (представители лаборатории)
 */
export interface ElisPassportData {
  /** Название лаборатории (на корневом уровне!) */
  labName?: string;
  /** Номер протокола испытаний */
  protocolNumber?: string;
  /** Точка поставки */
  pointDeliveryName?: string;
  /** Место отбора пробы */
  samplingLocation?: string;
  /** Дата протокола */
  protocolDate?: string;
  /** Период отбора пробы */
  startPeriodTime?: string;
  endPeriodTime?: string;

  /** Дополнительная информация о лаборатории (accreditationNumber, ownerName) */
  labInfo?: ElisLabInfo;
  /** Параметры качества */
  parameters?: Record<string, ElisParameter>;
  /** Подписанты (представители) */
  signers?: ElisSigners;

  /** Автоматически добавленные поля (через enrichElisData) */
  chiefLabShortSign?: string;
  chiefLabPosition?: string;
  chiefLabOrganization?: string;
}

/**
 * Информация о лаборатории (отдельный объект, передаваемый из localStorage.labInfo)
 */
export interface ElisLabInfo {
  /** Владелец (организация) */
  ownerName?: string;
  /** Номер аттестата аккредитации */
  accreditationNumber?: string;
  /** Адрес лаборатории */
  labAddress?: string;
  /** Автоматически добавленные поля (через enrichElisData) */
  chiefLabShortSign?: string;
  chiefLabPosition?: string;
  chiefLabOrganization?: string;
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
  /** Погрешность измерения */
  measurementError?: string;
  /** Тип документа (например, "ПротоколИспытаний") */
  documentType?: string;
  /** Номер документа */
  documentNumber?: string;
  /** Дата документа */
  documentDate?: string;
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
  /** ФИО в формате "Фамилия И. О." (legacy поле) */
  iof?: string;
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
