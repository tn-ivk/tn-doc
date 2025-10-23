<template>
  <div class="measurement-field">
    <InputNumber
      :modelValue="numericValue"
      :disabled="!parameter.editable"
      :class="[
        { 'p-invalid': !isValid },
        { 'elis-filled': parameter.elisFlags.measurement },
        { 'manual-input--disabled': !parameter.editable }
      ]"
      :minFractionDigits="0"
      :maxFractionDigits="parameter.roundValue || 10"
      class="measurement-input"
      @update:modelValue="handleValueChange"
    />

    <!-- Сообщение об ошибке валидации -->
    <small v-if="!isValid" class="p-error">
      {{ validationMessage }}
    </small>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import InputNumber from 'primevue/inputnumber';
import type { PassportQualityParameter } from '@/types/passport.types';

interface Props {
  parameter: PassportQualityParameter;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:measurement': [value: string];
}>();

const numericValue = computed(() => {
  if (!props.parameter.values.measurement) return null;
  const value = parseFloat(props.parameter.values.measurement.replace(',', '.'));
  return isNaN(value) ? null : value;
});

// Валидация поля
const isValid = computed(() => {
  // Проверка обязательных полей
  if (props.parameter.requiredFill) {
    if (!props.parameter.values.measurement || props.parameter.values.measurement === '') {
      return false;
    }
  }

  // Проверка количества знаков после запятой
  if (props.parameter.roundValue && props.parameter.values.measurement) {
    const value = props.parameter.values.measurement.replace(',', '.');
    const parts = value.split('.');
    if (parts.length > 1 && parts[1].length > props.parameter.roundValue) {
      return false;
    }
  }

  return true;
});

// Сообщение об ошибке валидации
const validationMessage = computed(() => {
  // Проверка обязательных полей
  if (props.parameter.requiredFill) {
    if (!props.parameter.values.measurement || props.parameter.values.measurement === '') {
      return `Поле "${props.parameter.name}" обязательно для заполнения`;
    }
  }

  // Проверка количества знаков после запятой
  if (props.parameter.roundValue && props.parameter.values.measurement) {
    const value = props.parameter.values.measurement.replace(',', '.');
    const parts = value.split('.');
    if (parts.length > 1 && parts[1].length > props.parameter.roundValue) {
      return `Максимум ${props.parameter.roundValue} знаков после запятой`;
    }
  }

  return '';
});

function handleValueChange(value: number | null) {
  const stringValue = value !== null ? value.toString().replace('.', ',') : '';
  emit('update:measurement', stringValue);
}
</script>

<style scoped>
.measurement-field {
  width: 100%;
}

.measurement-input {
  width: 100%;
  text-align: center;
  font-size: 15px;
}

.measurement-input:deep(input) {
  text-align: center;
  font-size: 15px;
}

/* Валидация - красная рамка при ошибке */
.measurement-input.p-invalid,
.measurement-input.p-invalid:deep(input),
.measurement-input.p-invalid:deep(.p-inputnumber-input) {
  border-color: var(--md-error, #dc3545) !important;
  box-shadow: none !important;
}

/* ELIS подсветка - зеленый фон для данных из ELIS */
.elis-filled {
  background-color: #8fd19e !important;
}

.elis-filled:deep(input) {
  background-color: #8fd19e !important;
}

/* Disabled стиль */
.manual-input--disabled {
  background-color: var(--md-surface-variant, #F1F3F4);
  color: var(--md-text-secondary, #5F6368);
  cursor: not-allowed;
}

.manual-input--disabled:deep(input) {
  background-color: var(--md-surface-variant, #F1F3F4);
  color: var(--md-text-secondary, #5F6368);
  cursor: not-allowed;
}

/* Disabled поле с ошибкой валидации */
.manual-input--disabled.p-invalid,
.manual-input--disabled.p-invalid:deep(input),
.manual-input--disabled.p-invalid:deep(.p-inputnumber-input) {
  border-color: var(--md-error, #dc3545) !important;
  background: color-mix(in srgb, var(--md-error) 5%, var(--md-disabled-bg)) !important;
}

/* Сообщение об ошибке */
.p-error {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.875rem;
  color: var(--md-error, #dc3545);
  line-height: 1.2;
}
</style>
