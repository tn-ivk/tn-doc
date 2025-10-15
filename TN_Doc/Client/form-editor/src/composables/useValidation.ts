import type { ValidationResult, ValidationRules } from '../types/validation.types'

export function useValidation(invalidChars: string[] = []) {
  const checkEmpty = (value: string, required: boolean): ValidationResult => {
    if (!required) return { valid: true }
    if (!value || value.trim() === '') {
      return { valid: false, message: 'Поле должно быть заполнено!' }
    }
    return { valid: true }
  }

  const checkInvalidChars = (value: string): ValidationResult => {
    if (!value || invalidChars.length === 0) return { valid: true }

    for (const char of invalidChars) {
      if (value.includes(char)) {
        return { valid: false, message: `Некорректный символ: ${char}` }
      }
    }
    return { valid: true }
  }

  const checkRounding = (value: string, roundValue?: number): ValidationResult => {
    if (!roundValue || roundValue < 1) return { valid: true }
    if (!value || value.trim() === '') return { valid: true }

    const pattern = `^-?[0-9]+(?:\\.[0-9]{1,${roundValue}})?$`
    if (value.match(new RegExp(pattern, 'g'))) {
      return { valid: true }
    }
    return {
      valid: false,
      message: 'Кол-во знаков после запятой не соответствует заданным правилам!'
    }
  }

  const validate = (value: string, rules: ValidationRules): ValidationResult => {
    const emptyCheck = checkEmpty(value, rules.required || false)
    if (!emptyCheck.valid) return emptyCheck

    if (rules.invalidChars && rules.invalidChars.length > 0) {
      const charsCheck = checkInvalidChars(value)
      if (!charsCheck.valid) return charsCheck
    }

    return { valid: true }
  }

  const validateNumber = (value: string, rules: ValidationRules): ValidationResult => {
    const emptyCheck = checkEmpty(value, rules.required || false)
    if (!emptyCheck.valid) return emptyCheck

    const roundingCheck = checkRounding(value, rules.roundValue)
    if (!roundingCheck.valid) return roundingCheck

    return { valid: true }
  }

  return {
    validate,
    validateNumber
  }
}
