/**
 * Типы для работы с API редактирования документов
 */

export interface DocumentEditConfig {
  /** ID документа */
  docId: number;
  /** Тип документа (Report, Act, Passport и т.д.) */
  docType: string;
  /** Заголовок формы */
  title: string;
  /** Список полей формы */
  fields: FormField[];
  /** Начальные значения полей (ключ поля -> значение) */
  initialValues: Record<string, any>;
  /** Идентификатор устройства (целое число) */
  deviceId: number;
  /** Справочники (лицензии/доверенности и т.д.) */
  dictionaries?: DocumentDictionaries;
  /** Список некорректных символов для данного устройства */
  invalidChars?: string[];
}

/** Справочники документа */
export interface DocumentDictionaries {
  /** Справочник лицензий/доверенностей */
  licenses?: License[];
}

/** Лицензия/доверенность */
export interface License {
  /** ID лицензии */
  id: number;
  /** Номер лицензии */
  licensesNumber: string;
  /** Дата лицензии */
  licensesDate: string;
}

export interface FormField {
  /** Уникальный ключ поля (используется для сохранения) */
  key: string;
  /** Отображаемое название поля */
  label: string;
  /** Тип поля: "select", "text", "number", "date", "datetime-local" */
  type: 'select' | 'text' | 'number' | 'date' | 'datetime-local';
  /** Обязательно ли поле для заполнения */
  required: boolean;
  /** Редактируемо ли поле */
  editable: boolean;
  /** Опции для select полей */
  options?: SelectOption[];
  /** Количество знаков после запятой для округления (для number полей) */
  roundValue?: number;
  /** Тег группы (для группировки полей при сохранении) */
  tag?: string;
  /**
   * Массив алиасов для маппинга с данными ELIS
   * Используется механизм fallback: перебираются все алиасы по порядку,
   * используется первое найденное значение
   * @example
   * // AdditionalInfo (camelCase):
   * elisAlias: ["labName", "laboratoryName"]
   *
   * // Parameters (русские названия):
   * elisAlias: ["Массовая доля воды(%)", "Массовая концентрация воды(%)"]
   */
  elisAlias?: string[];
}

export interface SelectOption {
  /** Значение опции */
  value: string;
  /** Отображаемый текст опции */
  label: string;
  /** Выбрана ли опция по умолчанию */
  selected: boolean;
  /** Дополнительные данные пользователя (Factory, FIO, LicId и т.д.) */
  data?: UserData;
}

/** Данные пользователя для автозаполнения связанных полей */
export interface UserData {
  /** Организация (предприятие) */
  factory?: string;
  /** Должность */
  post?: string;
  /** Полное ФИО (Фамилия Имя Отчество) */
  fio?: string;
  /** ID доверенности */
  licId?: number;
}

export interface SaveDocumentRequest {
  /** Значения полей формы */
  [key: string]: any;
}

export interface SaveDocumentResponse {
  /** Успешно ли сохранение */
  success: boolean;
  /** Сообщение о результате */
  message?: string;
  /** Ошибка (если есть) */
  error?: string;
}
