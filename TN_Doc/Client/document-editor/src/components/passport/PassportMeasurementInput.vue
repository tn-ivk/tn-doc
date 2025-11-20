<template>
  <div class="measurement-field">
    <InputNumber
      v-tooltip.left="tooltipOptions"
      :modelValue="numericValue"
      :disabled="!parameter.editable"
      :class="[
        { 'p-invalid': !isValid },
        { 'elis-filled': isElisFilled },
        { 'manual-input--disabled': !parameter.editable },
        { 'has-tooltip-error': !isValid && tooltipOptions.value }
      ]"
      :minFractionDigits="0"
      :maxFractionDigits="8"
      class="measurement-input"
      @update:modelValue="handleValueChange"
    />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import Tooltip from 'primevue/tooltip';
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

// Регистрируем директиву tooltip
defineOptions({
  directives: {
    tooltip: Tooltip
  }
});

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

// Настройки для tooltip
const tooltipOptions = computed(() => {
  if (!isValid.value && validationMessage.value) {
    return {
      value: validationMessage.value,
      showOnFocus: true,
      showOnHover: true,
      class: 'p-error-tooltip',
      style: {
        backgroundColor: '#dc3545',
        color: 'white',
        fontSize: '0.875rem',
        borderRadius: '4px',
        maxWidth: '300px',
        lineHeight: '1.3'
      }
    };
  }
  return { value: '' };
});

function handleValueChange(value: number | null) {
  const stringValue = value !== null ? value.toString().replace('.', ',') : '';
  emit('update:measurement', stringValue);
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

/* Стиль для поля с ошибкой и tooltip */
.has-tooltip-error:deep(.p-inputnumber-input) {
  border-color: var(--md-error, #dc3545) !important;
}

/* ELIS подсветка - применяем ТОЛЬКО к самому input внутри PrimeVue InputNumber */
.measurement-input.elis-filled:deep(.p-inputnumber-input) {
  background-color: var(--md-elis-highlight, #e8f5e9) !important;
  color: var(--md-text, #212121) !important;
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

/* Стили для tooltip с ошибкой */
:deep(.p-error-tooltip),
:deep(.p-tooltip.p-error-tooltip),
:deep(.p-tooltip.p-error-tooltip .p-tooltip-text) {
  background-color: var(--md-error, #dc3545) !important;
  color: white !important;
  font-size: 0.875rem !important;
  padding: 0.75rem 1rem 0.75rem 1.25rem !important;
  border-radius: 6px !important;
  max-width: 300px !important;
  word-wrap: break-word !important;
  line-height: 1.4 !important;
  margin: 0.5rem !important;
}
</style>
