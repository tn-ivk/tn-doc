<template>
  <div class="form-field">
    <label :for="field.key" class="field-label">
      {{ field.label }}
      <span v-if="field.required" class="required-mark">*</span>
    </label>

    <!-- Select (выпадающий список) -->
    <Dropdown
      v-if="field.type === 'select'"
      :id="field.key"
      v-model="localValue"
      :options="field.options"
      optionLabel="label"
      optionValue="value"
      :placeholder="`Выберите ${field.label.toLowerCase()}`"
      :disabled="!field.editable"
      :class="{ 'p-invalid': !isValid }"
      class="w-full"
      @change="handleChange"
    />

    <!-- Text (текстовое поле) -->
    <InputText
      v-else-if="field.type === 'text'"
      :id="field.key"
      v-model="localValue"
      :placeholder="field.label"
      :disabled="!field.editable"
      :class="{ 'p-invalid': !isValid }"
      class="w-full"
      @input="handleChange"
    />

    <!-- Number (числовое поле) -->
    <InputNumber
      v-else-if="field.type === 'number'"
      :id="field.key"
      v-model="localValue"
      :placeholder="field.label"
      :disabled="!field.editable"
      :class="{ 'p-invalid': !isValid }"
      :minFractionDigits="field.roundValue || 0"
      :maxFractionDigits="field.roundValue || 2"
      class="w-full"
      @input="handleChange"
    />

    <!-- Date (дата) -->
    <DatePicker
      v-else-if="field.type === 'date'"
      :id="field.key"
      v-model="localValue"
      :disabled="!field.editable"
      :class="{ 'p-invalid': !isValid }"
      dateFormat="dd.mm.yy"
      class="w-full"
      @date-select="handleChange"
    />

    <!-- DateTime (дата и время) -->
    <DatePicker
      v-else-if="field.type === 'datetime-local'"
      :id="field.key"
      v-model="localValue"
      :disabled="!field.editable"
      :class="{ 'p-invalid': !isValid }"
      dateFormat="dd.mm.yy"
      :showTime="true"
      hourFormat="24"
      class="w-full"
      @date-select="handleChange"
    />

    <!-- Сообщение об ошибке валидации -->
    <small v-if="!isValid" class="p-error">
      Поле "{{ field.label }}" обязательно для заполнения
    </small>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Dropdown from 'primevue/dropdown';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import DatePicker from 'primevue/datepicker';
import type { FormField } from '@/types/document.types';

const props = defineProps<{
  field: FormField;
  modelValue: any;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: any): void;
}>();

// Локальное значение для v-model
const localValue = ref(props.modelValue);

// Валидация
const isValid = computed(() => {
  if (!props.field.required) return true;
  if (localValue.value === null || localValue.value === undefined) return false;
  if (typeof localValue.value === 'string' && localValue.value.trim() === '') return false;
  return true;
});

// Синхронизация с внешним modelValue
watch(() => props.modelValue, (newValue) => {
  localValue.value = newValue;
});

// Обработка изменений
function handleChange() {
  emit('update:modelValue', localValue.value);
}
</script>

<style scoped>
.form-field {
  margin-bottom: 1rem;
}

.field-label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: var(--text-color);
}

.required-mark {
  color: var(--red-500);
  margin-left: 0.25rem;
}

.p-error {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.875rem;
}
</style>
