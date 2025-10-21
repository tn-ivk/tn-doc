<template>
  <InputNumber
    :modelValue="numericValue"
    :disabled="!parameter.editable"
    :class="[
      validationClass,
      { 'elis-filled': parameter.elisFlags.hal },
      { 'manual-input--disabled': !parameter.editable }
    ]"
    :minFractionDigits="0"
    :maxFractionDigits="parameter.roundValue || 10"
    mode="decimal"
    locale="ru-RU"
    class="hal-input"
    @update:modelValue="handleValueChange"
    @input="handleInput"
  />
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import InputNumber from 'primevue/inputnumber';
import type { PassportQualityParameter } from '@/types/passport.types';

interface Props {
  /** Параметр качества */
  parameter: PassportQualityParameter;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:halValue': [value: string];
}>();

/**
 * Численное значение для InputNumber
 */
const numericValue = computed(() => {
  if (!props.parameter.values.hal) return null;
  const value = parseFloat(props.parameter.values.hal.replace(',', '.'));
  return isNaN(value) ? null : value;
});

/**
 * Класс валидации (correct-value / incorrect-value)
 */
const validationClass = computed(() => {
  // Проверка обязательного заполнения
  if (props.parameter.requiredFill) {
    if (!props.parameter.values.hal || props.parameter.values.hal === '') {
      return 'incorrect-value';
    }
  }

  // Проверка округления
  if (props.parameter.roundValue && props.parameter.values.hal) {
    const value = props.parameter.values.hal.replace(',', '.');
    const parts = value.split('.');
    if (parts.length > 1 && parts[1].length > props.parameter.roundValue) {
      return 'incorrect-value';
    }
  }

  return 'correct-value';
});

/**
 * Обработчик изменения значения
 */
function handleValueChange(value: number | null) {
  const stringValue = value !== null ? value.toString().replace('.', ',') : '';
  emit('update:halValue', stringValue);
}

/**
 * Обработчик ввода (для реакции на изменение в реальном времени)
 */
function handleInput(event: any) {
  // Обработка ввода с клавиатуры
  console.log(`[PassportHalInput] Input: ${props.parameter.key}`, event);
}
</script>

<style scoped>
.hal-input {
  width: 100%;
  text-align: center;
  font-size: 15px;
}

.hal-input:deep(input) {
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
