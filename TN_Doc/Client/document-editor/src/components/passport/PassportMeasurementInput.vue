<template>
  <div class="measurement-field">
    <InputNumber
      :modelValue="numericValue"
      :disabled="!parameter.editable"
      :class="[
        { 'p-invalid': !isValid },
        { 'elis-filled': isElisFilled },
        { 'manual-input--disabled': !parameter.editable }
      ]"
      :minFractionDigits="0"
      :maxFractionDigits="8"
      class="measurement-input"
      @update:modelValue="handleValueChange"
    />
    <!-- Подсказка об ошибке под полем -->
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
  isElisFilled?: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:measurement': [value: string];
}>();

/**
 * Количество знаков после запятой из конфигурации (RoundValue)
 * Используется для валидации и дополнения нулями
 */
const roundValue = computed(() => {
  return props.parameter.roundValue ?? 0;
});

/**
 * Получить количество знаков после запятой в строке
 */
const getDecimalPlaces = (value: string): number => {
  if (!value) return 0;
  const normalized = value.replace(',', '.');
  const parts = normalized.split('.');
  return parts.length > 1 ? parts[1].length : 0;
};

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

// Сообщение об ошибке валидации (короткий текст для экономии места)
const validationMessage = computed(() => {
  // Проверка обязательных полей
  if (props.parameter.requiredFill) {
    if (!props.parameter.values.measurement || props.parameter.values.measurement === '') {
      return 'поле обязательно';
    }
  }

  // Проверка количества знаков после запятой
  if (props.parameter.roundValue && props.parameter.values.measurement) {
    const value = props.parameter.values.measurement.replace(',', '.');
    const parts = value.split('.');
    if (parts.length > 1 && parts[1].length > props.parameter.roundValue) {
      return `макс ${props.parameter.roundValue} знаков`;
    }
  }

  return '';
});

function handleValueChange(value: number | null) {
  if (value === null) {
    emit('update:measurement', '');
    return;
  }

  // Преобразуем число в строку
  let stringValue = value.toString();

  // Получаем текущее количество знаков после запятой
  const currentDecimalPlaces = getDecimalPlaces(stringValue);
  const requiredDecimalPlaces = roundValue.value;

  // Если знаков меньше, чем требуется - дополняем нулями
  // Если знаков больше - оставляем как есть (для показа ошибки валидации)
  if (currentDecimalPlaces < requiredDecimalPlaces && requiredDecimalPlaces > 0) {
    stringValue = value.toFixed(requiredDecimalPlaces);
  }

  // Заменяем точку на запятую для русской локали
  emit('update:measurement', stringValue.replace('.', ','));
}
</script>

<style scoped>
.measurement-field {
  width: 100%;
  max-width: 100%;
  flex: 1;
  min-width: 0;
}

.measurement-input {
  width: 100%;
  max-width: 100%;
  text-align: center;
  font-size: 15px;
}

.measurement-input:deep(input) {
  text-align: center;
  font-size: 15px;
  box-sizing: border-box;
  width: 100%;
  max-width: 100%;
}

/* Валидация - красная рамка при ошибке */
.measurement-input.p-invalid,
.measurement-input.p-invalid:deep(input),
.measurement-input.p-invalid:deep(.p-inputnumber-input) {
  border-color: var(--md-error, #dc3545) !important;
  box-shadow: none !important;
}

/* Подсказка об ошибке под полем */
.p-error {
  display: block;
  margin-top: 2px;
  font-size: 0.75rem;
  color: var(--md-error, #dc3545);
  text-align: center;
}

/* ELIS подсветка - применяем ТОЛЬКО к самому input внутри PrimeVue InputNumber */
.measurement-input.elis-filled:deep(.p-inputnumber-input) {
  background-color: var(--md-elis-highlight, #e8f5e9) !important;
  color: var(--md-text, #212121) !important;
}

/* Disabled стиль - только цвет текста (фон сохраняется для ELIS) */
.manual-input--disabled:deep(input),
.manual-input--disabled:deep(.p-inputnumber-input),
.measurement-input:deep(input:disabled),
.measurement-input:deep(.p-inputnumber-input:disabled) {
  color: var(--md-text-secondary, #5F6368) !important;
  cursor: not-allowed;
}

/* Disabled без ELIS - серый фон */
.manual-input--disabled:not(.elis-filled):deep(input),
.manual-input--disabled:not(.elis-filled):deep(.p-inputnumber-input),
.measurement-input:not(.elis-filled):deep(input:disabled),
.measurement-input:not(.elis-filled):deep(.p-inputnumber-input:disabled) {
  background-color: var(--md-surface-variant, #F1F3F4) !important;
}

/* Disabled поле с ошибкой валидации */
.manual-input--disabled.p-invalid,
.manual-input--disabled.p-invalid:deep(input),
.manual-input--disabled.p-invalid:deep(.p-inputnumber-input) {
  border-color: var(--md-error, #dc3545) !important;
  background: color-mix(in srgb, var(--md-error) 5%, var(--md-disabled-bg)) !important;
}
</style>
