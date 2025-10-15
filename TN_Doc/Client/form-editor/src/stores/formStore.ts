import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { FieldConfig, FieldValue } from '../types/field.types'
import type { ValidationResult, ValidationRules } from '../types/validation.types'

export const useFormStore = defineStore('form', () => {
  // State
  const fields = ref<FieldConfig[]>([])
  const formData = ref<Record<string, FieldValue>>({})
  const validationResults = ref<Record<string, ValidationResult>>({})
  const invalidChars = ref<string[]>([])

  // Getters
  const isAllFieldsValid = computed(() => {
    return Object.values(validationResults.value).every(result => result.valid)
  })

  // Actions
  const initFields = (fieldConfigs: FieldConfig[], data: Record<string, any>) => {
    fields.value = fieldConfigs
    formData.value = { ...data }

    // Инициализация результатов валидации
    fieldConfigs.forEach(field => {
      validationResults.value[field.name] = { valid: true }
    })
  }

  const setInvalidChars = (chars: string[]) => {
    invalidChars.value = chars
  }

  const updateField = (fieldName: string, value: FieldValue) => {
    formData.value[fieldName] = value
  }

  const setValidationResult = (fieldName: string, result: ValidationResult) => {
    validationResults.value[fieldName] = result
  }

  const getFieldConfig = (fieldName: string): FieldConfig | undefined => {
    return fields.value.find(f => f.name === fieldName)
  }

  const getValidationRules = (fieldName: string): ValidationRules => {
    const field = getFieldConfig(fieldName)
    if (!field) {
      return {}
    }

    return {
      required: field.required,
      invalidChars: invalidChars.value,
      roundValue: field.roundValue
    }
  }

  return {
    // State
    fields,
    formData,
    validationResults,
    invalidChars,

    // Getters
    isAllFieldsValid,

    // Actions
    initFields,
    setInvalidChars,
    updateField,
    setValidationResult,
    getFieldConfig,
    getValidationRules
  }
})
