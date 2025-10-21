<template>
  <InputNumber
    :modelValue="numericValue"
    :disabled="!parameter.editable"
    :class="[
      validationClass,
      { 'elis-filled': parameter.elisFlags.measurement },
      { 'manual-input--disabled': !parameter.editable }
    ]"
    :minFractionDigits="0"
    :maxFractionDigits="parameter.roundValue || 10"
    mode="decimal"
    locale="ru-RU"
    class="measurement-input"
    @update:modelValue="handleValueChange"
  />
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

const validationClass = computed(() => {
  if (props.parameter.requiredFill) {
    if (!props.parameter.values.measurement || props.parameter.values.measurement === '') {
      return 'incorrect-value';
    }
  }

  if (props.parameter.roundValue && props.parameter.values.measurement) {
    const value = props.parameter.values.measurement.replace(',', '.');
    const parts = value.split('.');
    if (parts.length > 1 && parts[1].length > props.parameter.roundValue) {
      return 'incorrect-value';
    }
  }

  return 'correct-value';
});

function handleValueChange(value: number | null) {
  const stringValue = value !== null ? value.toString().replace('.', ',') : '';
  emit('update:measurement', stringValue);
}
</script>

<style scoped>
.measurement-input {
  width: 100%;
  text-align: center;
  font-size: 15px;
}

.measurement-input:deep(input) {
  text-align: center;
  font-size: 15px;
}

/* Валидация */
.correct-value {
  border-color: var(--md-outline, #CFD8DC);
}

.correct-value:deep(input) {
  border-color: var(--md-outline, #CFD8DC);
}

.incorrect-value {
  border-color: var(--md-error, #dc3545);
  background-color: #f8d7da;
}

.incorrect-value:deep(input) {
  border-color: var(--md-error, #dc3545);
  background-color: #f8d7da;
}

/* ELIS подсветка */
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
</style>
