<template>
  <component
    :is="componentType"
    v-model="internalValue"
    :id="fieldId"
    :name="field.name"
    :disabled="field.disabled"
    :class="fieldClasses"
    @input="handleInput"
    v-tooltip="tooltipConfig"
  />
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useFormStore } from '../stores/formStore'
import { useValidation } from '../composables/useValidation'
import type { FieldConfig } from '../types/field.types'
import type { ValidationResult } from '../types/validation.types'

const props = defineProps<{
  field: FieldConfig
  modelValue: string | number | null | undefined
}>()

const emit = defineEmits<{
  'update:modelValue': [value: string | number | null | undefined]
}>()

const formStore = useFormStore()
const { validate, validateNumber } = useValidation(formStore.invalidChars)

const internalValue = ref(props.modelValue)
const validationResult = ref<ValidationResult>({ valid: true })

// Определяем тип компонента
const componentType = computed(() => {
  return props.field.type === 'textarea' ? 'textarea' : 'input'
})

// Генерация уникального ID для поля
const fieldId = computed(() => {
  return `field_${props.field.name}_${Date.now()}`
})

// Классы для поля с учетом валидации
const fieldClasses = computed(() => ({
  'manual-input': true,
  'correct-value': validationResult.value.valid && internalValue.value,
  'incorrect-value': !validationResult.value.valid,
  'manual-input--disabled': props.field.disabled
}))

// Конфигурация tooltip для отображения ошибок валидации
const tooltipConfig = computed(() => {
  if (!validationResult.value.valid && validationResult.value.message) {
    return {
      value: validationResult.value.message,
      pt: {
        text: 'text-sm'
      }
    }
  }
  return { disabled: true }
})

// Обработка изменений значения
const handleInput = (event: Event) => {
  const target = event.target as HTMLInputElement | HTMLTextAreaElement
  const value = target.value

  internalValue.value = value
  emit('update:modelValue', value)

  // Валидация
  const rules = formStore.getValidationRules(props.field.name)

  // Используем validateNumber для числовых полей
  if (props.field.type === 'number') {
    validationResult.value = validateNumber(value, rules)
  } else {
    validationResult.value = validate(value, rules)
  }

  formStore.setValidationResult(props.field.name, validationResult.value)

  // Отправка сообщения в родительское окно
  notifyParentWindow()
}

// Синхронизация с внешним значением
watch(() => props.modelValue, (newValue) => {
  internalValue.value = newValue
})

// Уведомление родительского окна о статусе валидации
const notifyParentWindow = () => {
  const allValid = formStore.isAllFieldsValid
  window.top?.postMessage(
    allValid ? 'ButtonSaveOn' : 'ButtonSaveOff',
    '*'
  )
}
</script>

<style scoped>
.manual-input {
  width: 100%;
  padding: 4px 8px;
  border: 1px solid #ccc;
  border-radius: 4px;
  font-family: inherit;
  font-size: 14px;
}

.manual-input:focus {
  outline: none;
  border-color: var(--md-primary);
}

.correct-value {
  background-color: #ffffff;
  border-color: #4caf50;
}

.incorrect-value {
  background-color: #ffebee;
  border-color: #f44336;
}

.manual-input--disabled {
  background-color: var(--md-disabled-bg, #f5f5f5);
  cursor: not-allowed;
  opacity: 0.6;
}

textarea.manual-input {
  resize: vertical;
  min-height: 60px;
}
</style>
