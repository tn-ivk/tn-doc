/**
 * Конфигурация документа для редактирования
 */
export interface DocumentConfig {
  docId: number
  docType: string
  deviceId: string
  title: string
  fields: FormField[]
  dictionaries: Dictionaries
  invalidChars: string[]
}

/**
 * Поле формы
 */
export interface FormField {
  name: string
  label: string
  type: FieldType
  required: boolean
  disabled: boolean
  value: string
  options?: SelectOption[] | null
}

/**
 * Опция для выпадающего списка
 */
export interface SelectOption {
  id: number | string
  name: string
}

/**
 * Тип поля
 */
export type FieldType = 'text' | 'select' | 'datetime-local' | 'textarea' | 'number'

/**
 * Справочники
 */
export interface Dictionaries {
  deliveryUsers?: SelectOption[]
  receiveUsers?: SelectOption[]
}

/**
 * Данные для сохранения документа
 */
export interface SaveDocumentData {
  docID: number
  values: FormFieldValue[]
}

/**
 * Значение поля для сохранения
 */
export interface FormFieldValue {
  key: string
  value: string
  tag: string
}
