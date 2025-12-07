// Параметр качества
export interface PassportParameter {
  Id: number;
  Key: string;
  Name: string;
  Use: boolean;
  Edit: boolean;
  IsBallast: boolean;
  SlaveKey?: string;
  RequiredFill?: boolean;
  RoundValue?: number;
}

// Дополнительное поле
export interface PassportAdditionalField {
  Id: number;
  Use: boolean;
  Key: string;
  Type: 'text' | 'list' | 'datetime-local' | 'number';
  Name: string;
}

// Метод (readonly, не редактируется визуально)
export interface PassportMethod {
  Id: number;
  Use: boolean;
  IdParameter: number;
  Name: string;
  LimitValueActivate: boolean;
  LimitValue: number;
  LimitValueString: string;
  IsDefault?: boolean;
}

// Полная конфигурация
export interface PassportEditConfig {
  Parameters: PassportParameter[];
  AdditionalInfo: PassportAdditionalField[];
  Methods: PassportMethod[]; // Только для сериализации, не редактируется
}

// Типы полей для dropdown
export const FIELD_TYPES = [
  { label: 'Текст', value: 'text' },
  { label: 'Список (справочник)', value: 'list' },
  { label: 'Дата и время', value: 'datetime-local' },
  { label: 'Число', value: 'number' }
] as const;

export type FieldTypeValue = typeof FIELD_TYPES[number]['value'];
