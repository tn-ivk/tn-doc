<template>
  <select
    v-model="internalValue"
    :id="fieldId"
    :name="field.name"
    :disabled="field.disabled"
    :class="fieldClasses"
    @change="handleChange"
    v-tooltip="tooltipConfig"
  >
    <option value="">-- Выберите --</option>
    <option
      v-for="option in selectOptions"
      :key="option.value"
      :value="option.value"
    >
      {{ option.label }}
    </option>
  </select>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useFormStore } from '../stores/formStore'
import { useDictionaryStore } from '../stores/dictionaryStore'
import { useUserChange } from '../composables/useUserChange'
import { useValidation } from '../composables/useValidation'
import type { FieldConfig, SelectOption } from '../types/field.types'
import type { ValidationResult } from '../types/validation.types'

const props = defineProps<{
  field: FieldConfig
  modelValue: string | number | null | undefined
}>()

const emit = defineEmits<{
  'update:modelValue': [value: string | number | null | undefined]
}>()

const formStore = useFormStore()
const dictStore = useDictionaryStore()
const { handleUserChange } = useUserChange()
const { validate } = useValidation(formStore.invalidChars)

const internalValue = ref(props.modelValue)
const validationResult = ref<ValidationResult>({ valid: true })

// Генерация уникального ID для поля
const fieldId = computed(() => {
  return `field_${props.field.name}_${Date.now()}`
})

// Опции для select
const selectOptions = computed((): SelectOption[] => {
  // Если опции заданы в конфигурации поля
  if (props.field.options && props.field.options.length > 0) {
    return props.field.options
  }

  // Если это поле пользователя, берем из справочника
  if (props.field.name.includes('IOF')) {
    return dictStore.users.map(user => ({
      value: user.Id,
      label: user.Name
    }))
  }

  return []
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
const handleChange = (event: Event) => {
  const target = event.target as HTMLSelectElement
  const value = target.value

  // Преобразование значения в число если это ID пользователя
  const processedValue = props.field.name.includes('IOF') && value
    ? Number(value)
    : value

  internalValue.value = processedValue
  emit('update:modelValue', processedValue)

  // Валидация
  const rules = formStore.getValidationRules(props.field.name)
  validationResult.value = validate(String(value), rules)
  formStore.setValidationResult(props.field.name, validationResult.value)

  // Автозаполнение связанных полей
  if (props.field.name.includes('IOF') && processedValue) {
    handleUserChange(Number(processedValue), props.field)
  }

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
select.manual-input {
  width: 100%;
  padding: 4px 8px;
  border: 1px solid #ccc;
  border-radius: 4px;
  font-family: inherit;
  font-size: 14px;
  background-color: #ffffff;
  cursor: pointer;
}

select.manual-input:focus {
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
</style>
