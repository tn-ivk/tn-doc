// Типы валидации

export interface ValidationResult {
  valid: boolean
  message?: string
}

export interface ValidationRules {
  required?: boolean
  invalidChars?: string[]
  roundValue?: number
}
