// Типы полей формы редактирования

export interface FieldConfig {
  name: string
  label: string
  type: 'text' | 'number' | 'select' | 'textarea'
  required?: boolean
  disabled?: boolean
  roundValue?: number // Для числовых полей
  autoFill?: string[] // Поля для автозаполнения
  options?: SelectOption[] // Опции для select
}

export interface SelectOption {
  value: string | number
  label: string
}

export interface User {
  Id: number
  Name: string
  Post?: string
  Factory?: string
  LicenseNumber?: string
  LicenseDate?: string
}

export interface License {
  Id: number
  Number: string
  Date: string
}

export interface Dictionaries {
  Users?: User[]
  Licenses?: License[]
}

export interface FormEditorConfig {
  fields: FieldConfig[]
  data: Record<string, any>
  dictionaries: Dictionaries
  invalidChars?: string[]
}

export type FieldValue = string | number | null | undefined
