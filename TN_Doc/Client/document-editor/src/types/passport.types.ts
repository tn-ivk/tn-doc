/**
 * Типы для работы с паспортами качества (Passport)
 */

import type { DocumentEditConfig } from './document.types';

/**
 * Расширенная конфигурация для паспорта качества
 */
export interface PassportEditConfig extends DocumentEditConfig {
  /** Используется ли ELIS для данного устройства */
  isElisUsed: boolean;
  /** Схема параметров качества (только метаданные, без данных) */
  qualityParametersSchema?: PassportQualityParameterSchema[];
}

/**
 * Схема параметра качества нефти (только метаданные, без данных)
 */
export interface PassportQualityParameterSchema {
  /** ID параметра */
  id: number;
  /** Ключ параметра (например, "TempCorrection") */
  key: string;
  /** Название параметра (отображаемое) */
  name: string;
  /** Редактируемо ли поле ХАЛ */
  editable: boolean;
  /** Обязательно ли заполнение */
  requiredFill?: boolean;
  /** Обязательно ли заполнение метода испытаний */
  methodRequiredFill?: boolean;
  /** Количество знаков после запятой для округления */
  roundValue?: number;
  /** ELIS метаданные */
  elisData?: ElisData;
  /** Доступные методы испытаний (из конфигурации) */
  methodOptions: MethodOption[];
}

/**
 * Полный параметр качества нефти (схема + данные)
 * Создается динамически из PassportQualityParameterSchema + formData
 */
export interface PassportQualityParameter extends PassportQualityParameterSchema {
  /** Значения параметра (вычисляются из formData) */
  values: ParameterValues;
  /** Метод испытаний (вычисляется из formData) */
  method: ParameterMethod;
  /** Документ (только если ELIS используется, вычисляется из formData) */
  document?: ParameterDocument;
  /** Флаги заполнения из ELIS (вычисляются из formData) */
  elisFlags: ParameterElisFlags;
}

/**
 * Значения параметра (новая структура с объединенным измерением)
 */
export interface ParameterValues {
  /** Измерение (объединенное значение: ELIS → HAL → IVK → пусто) */
  measurement: string;
  /** Результат (ранее printValue) */
  result: string;
}

/**
 * Метод испытаний
 */
export interface ParameterMethod {
  /** Выбранный метод */
  selected: string;
  /** Доступные методы */
  options: MethodOption[];
  /** Обязательно ли заполнение метода */
  requiredFill?: boolean;
}

/**
 * Опция метода испытаний
 */
export interface MethodOption {
  /** Уникальный идентификатор метода */
  id: number;
  /** Используется ли метод в конфигурации */
  use: boolean;
  /** Идентификатор параметра, к которому относится метод */
  idParameter: number;
  /** Название метода (например, "ГОСТ 2177-99") */
  name: string;
  /** Используется ли по умолчанию */
  isDefault: boolean;
  /** Активирован ли лимит значений */
  limitValueActivate: boolean;
  /** Пороговое значение */
  limitValue?: number;
  /** Строка для отображения при значении ниже порога */
  limitValueString?: string;
}

/**
 * Документ ELIS для параметра
 */
export interface ParameterDocument {
  /** Номер документа */
  number: string;
  /** Заполнено ли из ELIS */
  elisFilled: boolean;
}

/**
 * Флаги заполнения из ELIS для параметра (новая структура)
 */
export interface ParameterElisFlags {
  /** Измерение заполнено из ELIS (вместо hal) */
  measurement: boolean;
  /** Метод испытаний заполнен из ELIS */
  method: boolean;
  /** Результат заполнен из ELIS (вместо printValue) */
  result: boolean;
  /** Документ заполнен из ELIS */
  document: boolean;
}

/**
 * ELIS метаданные
 */
export interface ElisData {
  /** Ключ ELIS */
  keyELIS: string;
  /** Альтернативные имена (алиасы) */
  elisAlias?: string[];
}

/**
 * Событие обновления значения measurement
 */
export interface MeasurementUpdateEvent {
  /** Ключ параметра */
  paramKey: string;
  /** Новое значение */
  value: string;
}

/**
 * Событие обновления метода
 */
export interface MethodUpdateEvent {
  /** Ключ параметра */
  paramKey: string;
  /** Выбранный метод */
  method: MethodOption | null;
}

/**
 * Событие обновления значения result
 */
export interface ResultUpdateEvent {
  /** Ключ параметра */
  paramKey: string;
  /** Новое значение */
  value: string;
}
