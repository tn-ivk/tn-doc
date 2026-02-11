<template>
  <div class="text-field-with-reset">
    <div class="reset-input-group">
      <div class="input-wrapper">
        <FormFieldWithHistory
          :field="field"
          :modelValue="modelValue"
          :hide-label="true"
          :invalidChars="invalidChars"
          :highlightColor="highlightColor"
          @update:modelValue="(value) => emit('update:modelValue', value)"
        />
      </div>

      <!-- Кнопка сброса на значение по умолчанию -->
      <button
        class="reset-btn"
        type="button"
        @click="handleReset"
        title="Значение по умолчанию"
      >
        <i class="pi pi-ban"></i>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import FormFieldWithHistory from './FormFieldWithHistory.vue';
import type { FormField } from '@/types/document.types';
import { useFieldHistory } from '@/composables/useFieldHistory';
import { useDocumentStore } from '@/stores/documentStore';
import { normalizeForComparison } from '@/utils/field-compare-utils';

const DEFAULT_VALUE = '\u2015'; // Длинное тире ―

const props = defineProps<{
  field: FormField;
  modelValue: any;
  invalidChars?: string[];
  highlightColor?: string;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: any): void;
}>();

const store = useDocumentStore();
const { trackManualChange, trackReturnToElis } = useFieldHistory();

/**
 * Обработчик кнопки сброса на значение по умолчанию.
 * Если значение изменилось — записывает ручное изменение в историю
 * (или ReturnToELIS, если дефолт совпадает с оригиналом ELIS).
 */
function handleReset() {
  const newValue = DEFAULT_VALUE;
  const previousValue = props.modelValue;

  // Если значение не изменилось — ничего не делаем
  if (previousValue === newValue) {
    return;
  }

  // Трекаем изменение в истории (аналогично FormFieldWithHistory.handleChange)
  const fieldKey = props.field.key;
  const elisOriginal = store.formData[`${fieldKey}__elisOriginal`];

  if (elisOriginal !== undefined) {
    const normalizedNew = normalizeForComparison(props.field.type, newValue);
    const normalizedOriginal = normalizeForComparison(props.field.type, elisOriginal);

    if (normalizedNew === normalizedOriginal) {
      trackReturnToElis(fieldKey, newValue, previousValue, props.field.type);
    } else {
      trackManualChange(fieldKey, newValue, previousValue, props.field.type);
    }
  } else {
    trackManualChange(fieldKey, newValue, previousValue, props.field.type);
  }

  emit('update:modelValue', newValue);
}
</script>

<style scoped>
/* Контейнер для InputText и кнопки (input group) */
.reset-input-group {
  position: relative;
  display: flex;
  width: 100%;
}

/* Обёртка для InputText с индикаторами */
.input-wrapper {
  position: relative;
  flex: 1;
}

/* Кнопка сброса справа от InputText */
.reset-btn {
  width: 28px;
  height: 38px;
  border: 1px solid var(--md-outline) !important;
  border-left: none !important;
  background-color: transparent !important;
  color: var(--md-text, #212121) !important;
  font-size: 14px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 0 var(--md-radius) var(--md-radius) 0 !important;
  transition: background-color 0.15s ease-in-out, color 0.15s ease-in-out;
}

.reset-btn:hover {
  background-color: rgba(0, 0, 0, 0.04) !important;
  color: var(--md-primary, #2f6fed) !important;
}

.reset-btn:active {
  background-color: rgba(0, 0, 0, 0.08) !important;
  color: var(--md-primary-active, #1e54d4) !important;
}

/* Скругления InputText для input-group */
.text-field-with-reset :deep(.form-field-with-history .p-inputtext) {
  border-radius: var(--md-radius) 0 0 var(--md-radius) !important;
}
</style>
