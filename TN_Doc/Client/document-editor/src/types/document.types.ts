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
}

export interface SelectOption {
  /** Значение опции */
  value: string;
  /** Отображаемый текст опции */
  label: string;
  /** Выбрана ли опция по умолчанию */
  selected: boolean;
  /** Дополнительные данные (для сложных случаев) */
  data?: Record<string, any>;
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
