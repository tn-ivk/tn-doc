<template>
  <div class="form-field">
    <!-- Текстовое поле -->
    <InputText
      v-if="field.type === 'text'"
      v-model="fieldValue"
      :disabled="field.disabled"
      :class="validationClass"
      @input="handleInput"
    />

    <!-- Выпадающий список -->
    <Select
      v-else-if="field.type === 'select'"
      v-model="fieldValue"
      :options="field.options || []"
      option-label="name"
      option-value="id"
      :disabled="field.disabled"
      :class="validationClass"
      placeholder="Выберите значение"
      @change="handleInput"
    />

    <!-- Поле даты/времени -->
    <DatePicker
      v-else-if="field.type === 'datetime-local'"
      v-model="fieldValue"
      :disabled="field.disabled"
      show-time
      :class="validationClass"
      @update:model-value="handleInput"
    />

    <!-- Числовое поле -->
    <InputNumber
      v-else-if="field.type === 'number'"
      v-model="fieldValue"
      :disabled="field.disabled"
      :class="validationClass"
      @input="handleInput"
    />

    <!-- Многострочное текстовое поле -->
    <Textarea
      v-else-if="field.type === 'textarea'"
      v-model="fieldValue"
      :disabled="field.disabled"
      :class="validationClass"
      rows="3"
      @input="handleInput"
    />

    <!-- Сообщение об ошибке -->
    <small v-if="errorMessage" class="error-message">
      {{ errorMessage }}
    </small>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import DatePicker from 'primevue/datepicker'
import InputNumber from 'primevue/inputnumber'
import Textarea from 'primevue/textarea'
import type { FormField } from '@/types/document.types'

const props = defineProps<{
  field: FormField
  invalidChars: string[]
}>()

const emit = defineEmits<{
  'update:value': [value: string]
}>()

const fieldValue = ref(props.field.value)
const errorMessage = ref<string | null>(null)

// Синхронизация с prop при изменении извне
watch(() => props.field.value, (newValue) => {
  fieldValue.value = newValue
})

// Класс для валидации
const validationClass = computed(() => ({
  'p-invalid': !!errorMessage.value,
  'p-valid': !errorMessage.value && fieldValue.value
}))

// Обработчик изменения значения
function handleInput() {
  validate()
  emit('update:value', fieldValue.value)
}

// Валидация поля
function validate() {
  errorMessage.value = null

  // Проверка обязательного поля
  if (props.field.required && !fieldValue.value) {
    errorMessage.value = 'Поле должно быть заполнено!'
    return false
  }

  // Проверка недопустимых символов
  if (fieldValue.value && props.invalidChars.length > 0) {
    for (const char of props.invalidChars) {
      if (fieldValue.value.includes(char)) {
        errorMessage.value = `Некорректный символ: ${char}`
        return false
      }
    }
  }

  return true
}

// Экспортируем метод валидации для родителя
defineExpose({
  validate
})
</script>

<style scoped>
.form-field {
  width: 100%;
}

.error-message {
  display: block;
  color: var(--p-red-500);
  margin-top: 0.25rem;
  font-size: 0.875rem;
}

:deep(.p-invalid) {
  border-color: var(--p-red-500);
}

:deep(.p-valid) {
  border-color: var(--p-green-500);
}
</style>
