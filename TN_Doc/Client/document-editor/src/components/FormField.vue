<template>
  <div
    class="form-field"
    :class="{ compact: hideLabel }"
  >
    <label
      v-if="!hideLabel"
      :for="field.key"
      class="field-label"
    >
      {{ field.label }}
      <span v-if="field.required" class="required-mark">*</span>
    </label>

    <!-- Select (выпадающий список) -->
    <Select
      v-if="field.type === 'select'"
      :id="field.key"
      v-model="localValue"
      :options="validSelectOptions"
      optionLabel="label"
      optionValue="value"
      :placeholder="hideLabel ? '' : `Выберите ${field.label.toLowerCase()}`"
      :disabled="!field.editable"
      :class="{ 'p-invalid': !isValid }"
      :style="fieldBackgroundStyle"
      class="field-control"
      appendTo="self"
      @change="handleChange"
    />

    <!-- Text (текстовое поле) -->
    <InputText
      v-else-if="field.type === 'text'"
      :id="field.key"
      v-model="localValue"
      :placeholder="hideLabel ? '' : field.label"
      :disabled="!field.editable"
      :class="{ 'p-invalid': !isValid }"
      :style="fieldBackgroundStyle"
      class="field-control"
      @input="handleChange"
    />

    <!-- Number (числовое поле) -->
    <InputNumber
      v-else-if="field.type === 'number'"
      :id="field.key"
      v-model="localValue"
      :placeholder="hideLabel ? '' : field.label"
      :disabled="!field.editable"
      :class="{ 'p-invalid': !isValid }"
      :style="fieldBackgroundStyle"
      :minFractionDigits="field.roundValue || 0"
      :maxFractionDigits="field.roundValue || 2"
      class="field-control"
      @input="handleChange"
    />

    <!-- Date (дата) -->
    <DatePicker
      v-else-if="field.type === 'date'"
      :id="field.key"
      v-model="localValue"
      :disabled="!field.editable"
      :class="{ 'p-invalid': !isValid }"
      :style="fieldBackgroundStyle"
      dateFormat="dd.mm.yy"
      updateModelType="replace"
      class="field-control"
      @update:modelValue="handleChange"
    />

    <!-- DateTime (дата и время) -->
    <DatePicker
      v-else-if="field.type === 'datetime-local'"
      :id="field.key"
      v-model="localValue"
      :disabled="!field.editable"
      :class="{ 'p-invalid': !isValid }"
      :style="fieldBackgroundStyle"
      dateFormat="dd.mm.yy"
      :showTime="true"
      hourFormat="24"
      updateModelType="replace"
      class="field-control"
      @update:modelValue="handleChange"
    />

    <!-- Сообщение об ошибке валидации -->
    <small v-if="!isValid" class="p-error">
      {{ validationMessage }}
    </small>
  </div>
</template>

<script setup lang="ts">
/**
 * Универсальный компонент для отображения полей формы
 *
 * @component FormField
 *
 * @prop {FormField} field - Конфигурация поля
 * @prop {any} modelValue - Значение поля
 * @prop {boolean} [hideLabel] - Скрыть label
 * @prop {string[]} [invalidChars] - Список недопустимых символов
 * @prop {string} [highlightColor] - Цвет подсветки фона элемента (опциональный)
 *
 * @example
 * // Стандартное использование (белый фон)
 * <FormField :field="field" :modelValue="value" />
 *
 * @example
 * // С подсветкой зелёным (ELIS данные)
 * <FormField
 *   :field="field"
 *   :modelValue="value"
 *   highlightColor="var(--md-elis-highlight)"
 * />
 *
 * @example
 * // С подсветкой жёлтым (предупреждение)
 * <FormField
 *   :field="field"
 *   :modelValue="value"
 *   highlightColor="#fff3cd"
 * />
 */
import { logger } from '@tn-doc/shared';
import { ref, computed, watch, onMounted } from 'vue';
import Select from 'primevue/select';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import DatePicker from 'primevue/datepicker';
import type { FormField } from '@/types/document.types';

const props = defineProps<{
  field: FormField;
  modelValue: any;
  hideLabel?: boolean;
  invalidChars?: string[];
  highlightColor?: string;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: any): void;
}>();

/**
 * Computed свойство для стилей фона элемента
 * Применяет цвет подсветки, если передан highlightColor
 */
const fieldBackgroundStyle = computed(() => {
  if (!props.highlightColor) return {};

  return {
    backgroundColor: props.highlightColor,
    transition: 'background-color 0.2s ease-in-out'
  };
});

/**
 * Конвертирует значение для datetime-local/date полей в объект Date
 */
function convertToDate(value: any, fieldType: string): any {
  if (!value) return value;

  // Для datetime-local и date полей конвертируем строку в Date
  if ((fieldType === 'datetime-local' || fieldType === 'date') && typeof value === 'string') {
    try {
      return new Date(value);
    } catch (e) {
      logger.error('FormField: ошибка конвертации даты', { error: e instanceof Error ? e.message : String(e) });
      return value;
    }
  }

  return value;
}

/**
 * Конвертирует объект Date обратно в строку ISO для сохранения
 */
function convertFromDate(value: any, fieldType: string): any {
  if (!value) return value;

  // Для datetime-local и date полей конвертируем Date в ISO строку
  if ((fieldType === 'datetime-local' || fieldType === 'date') && value instanceof Date) {
    // Проверяем что дата валидна
    if (isNaN(value.getTime())) {
      logger.error('FormField: невалидная дата', { value });
      return null;
    }

    // Конвертируем в ISO строку с локальным временем (без Z)
    return value.toISOString().slice(0, 19); // "2025-10-22T02:07:33"
  }

  return value;
}

onMounted(() => {
  // Validate select options on mount
  if (props.field.type === 'select' && props.field.options) {
    validSelectOptions.value.forEach((opt, idx) => {
      if (!opt.label || opt.value === undefined || opt.label.trim() === '' || opt.value === '') {
        logger.error('FormField: некорректная опция', { fieldKey: props.field.key, optionIndex: idx, option: opt });
      }
    });
  }
});

// Локальное значение для v-model
// Для datetime-local и date конвертируем строку в объект Date
const localValue = ref(convertToDate(props.modelValue, props.field.type));

// Валидные опции для select (фильтруем пустые)
const validSelectOptions = computed(() => {
  if (props.field.type !== 'select' || !props.field.options) {
    return [];
  }

  // Фильтруем опции с пустыми label или value
  return props.field.options.filter(opt => {
    const hasLabel = opt.label && opt.label.trim() !== '';
    const hasValue = opt.value !== undefined && opt.value !== null && opt.value !== '';
    return hasLabel && hasValue;
  });
});

// Найти некорректный символ в значении
const invalidCharFound = computed(() => {
  // Проверяем только текстовые поля
  if (props.field.type !== 'text') return null;
  if (!localValue.value || typeof localValue.value !== 'string') return null;
  if (!props.invalidChars || props.invalidChars.length === 0) return null;

  // Проверяем каждый некорректный символ
  for (const char of props.invalidChars) {
    if (localValue.value.includes(char)) {
      return char;
    }
  }
  return null;
});

// Валидация
const isValid = computed(() => {
  // Проверка на некорректные символы
  if (invalidCharFound.value !== null) return false;

  // Проверка обязательных полей
  if (!props.field.required) return true;
  if (localValue.value === null || localValue.value === undefined) return false;
  if (typeof localValue.value === 'string' && localValue.value.trim() === '') return false;
  return true;
});

// Сообщение об ошибке
const validationMessage = computed(() => {
  if (invalidCharFound.value !== null) {
    return `Некорректный символ: ${invalidCharFound.value}`;
  }
  return `Поле "${props.field.label}" обязательно для заполнения`;
});

// Синхронизация с внешним modelValue
watch(() => props.modelValue, (newValue) => {
  // Конвертируем новое значение для datetime-local/date полей
  localValue.value = convertToDate(newValue, props.field.type);
});

// Обработка изменений
function handleChange() {
  // Конвертируем Date обратно в строку для datetime-local/date полей перед отправкой
  const valueToEmit = convertFromDate(localValue.value, props.field.type);
  logger.debug('FormField: handleChange', { fieldKey: props.field.key, value: valueToEmit });
  emit('update:modelValue', valueToEmit);
}
</script>

<style scoped>


.field-control {
  width: 100%;
  font-family: var(--md-font-family);
  font-size: var(--md-font-size-base);
  color: var(--md-text);
  font-weight: var(--md-font-weight-normal);
}

::deep(.field-control.p-inputtext),
::deep(.field-control .p-inputtext),
::deep(.field-control.p-inputmask),
::deep(.field-control .p-inputnumber-input) {
  width: 100%;
}

::deep(.field-control .p-inputtext:focus),
::deep(.field-control.p-inputtext:focus),
::deep(.field-control .p-inputnumber-input:focus) {
  border-color: var(--md-primary);
  box-shadow: none;
  outline: none;
}

:deep(.field-control.p-select) {
  width: 100%;
  min-height: var(--md-control-height);
  height: var(--md-control-height);
  border-radius: var(--md-radius) !important;
  border: 1px solid var(--md-outline) !important;
  background: #ffffff !important;
  padding: 0 !important;
  display: flex;
  align-items: center;
  box-shadow: none !important;
  transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
}

:deep(.field-control.p-select:not(.p-disabled):hover) {
  border-color: color-mix(in srgb, var(--md-outline) 60%, var(--md-primary)) !important;
}

:deep(.field-control.p-select:not(.p-disabled).p-focus),
:deep(.field-control.p-select:not(.p-disabled):focus-within) {
  border-color: var(--md-primary) !important;
  background: var(--md-primary-light) !important;
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--md-primary) 35%, transparent) !important;
}

:deep(.field-control.p-select .p-select-label) {
  display: flex;
  align-items: center;
  height: var(--md-control-height);
  padding: 0 10px !important;
  color: var(--md-text) !important;
  font-size: var(--md-font-size-base) !important;
  line-height: 1.2;
}

:deep(.field-control.p-select .p-select-dropdown) {
  width: 32px !important;
  color: var(--md-text-secondary) !important;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

:deep(.field-control.p-select .p-select-dropdown .p-icon) {
  font-size: 0.85rem;
}

:deep(.field-control.p-select .p-select-label.p-placeholder) {
  color: var(--md-text-secondary);
}

:deep(.field-control .p-select-overlay) {
  margin-top: 2px;
  border: 1px solid var(--md-outline);
  border-radius: var(--md-radius);
  box-shadow: 0 6px 18px rgba(33, 33, 33, 0.12);
  background: #ffffff;
}

:deep(.field-control .p-select-list) {
  padding: 4px 0;
  font-size: var(--md-font-size-base);
}

:deep(.field-control .p-select-option) {
  padding: 6px 12px;
  color: var(--md-text);
  transition: background-color 0.15s ease-in-out, color 0.15s ease-in-out;
}

:deep(.field-control .p-select-option:not(.p-disabled):hover) {
  background: var(--md-primary-light);
  color: var(--md-text);
}

:deep(.field-control .p-select-option.p-focus) {
  background: var(--md-primary-light);
  color: var(--md-text);
}

:deep(.field-control .p-select-option.p-focus:not(.p-disabled):hover) {
  background: var(--md-primary);
  color: #ffffff;
}

:deep(.field-control.p-select.p-disabled) {
  background: var(--md-disabled-bg);
  border-color: var(--md-disabled-border);
  color: var(--md-disabled-text);
}

:deep(.field-control.p-select.p-disabled .p-select-label) {
  color: var(--md-disabled-text);
}

.field-control.p-invalid,
:deep(.field-control .p-inputtext.p-invalid),
:deep(.field-control .p-inputnumber-input.p-invalid),
:deep(.field-control.p-select.p-invalid) {
  border-color: var(--md-error) !important;
  box-shadow: none !important;
}

/* Стили для disabled полей с ошибками валидации */
.field-control.p-invalid:disabled,
:deep(.field-control .p-inputtext.p-invalid:disabled),
:deep(.field-control .p-inputnumber-input.p-invalid:disabled),
:deep(.field-control.p-select.p-invalid.p-disabled) {
  border-color: var(--md-error) !important;
  box-shadow: none !important;
  background: color-mix(in srgb, var(--md-error) 5%, var(--md-disabled-bg)) !important;
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
