<template>
  <div class="date-range-group">
    <!-- Начало периода -->
    <div class="date-field date-begin">
      <FormFieldWithHistory
        :field="beginField"
        :modelValue="beginValue"
        :hide-label="true"
        :invalidChars="invalidChars"
        @update:modelValue="emitBeginUpdate"
      />
    </div>

    <!-- Разделитель -->
    <span class="date-separator">-</span>

    <!-- Окончание периода -->
    <div class="date-field date-end">
      <FormFieldWithHistory
        :field="endField"
        :modelValue="endValue"
        :hide-label="true"
        :invalidChars="invalidChars"
        @update:modelValue="emitEndUpdate"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { logger } from '@tn-doc/shared';
import FormFieldWithHistory from './FormFieldWithHistory.vue';
import type { FormField } from '@/types/document.types';

const props = defineProps<{
  beginField: FormField;
  endField: FormField;
  beginValue: any;
  endValue: any;
  invalidChars?: string[];
}>();

const emit = defineEmits<{
  (e: 'update:begin', value: any): void;
  (e: 'update:end', value: any): void;
}>();

// ДИАГНОСТИКА: Добавляем обертки для эмитов с логированием
const emitBeginUpdate = (value: any) => {
  logger.info('[DateRangeFieldGroup] Обновление начала периода', {
    fieldKey: props.beginField.key,
    newValue: value,
    oldValue: props.beginValue,
    timestamp: new Date().toISOString()
  });
  emit('update:begin', value);
};

const emitEndUpdate = (value: any) => {
  logger.info('[DateRangeFieldGroup] Обновление конца периода', {
    fieldKey: props.endField.key,
    newValue: value,
    oldValue: props.endValue,
    timestamp: new Date().toISOString()
  });
  emit('update:end', value);
};
</script>

<style scoped>
.date-range-group {
  display: flex;
  align-items: center;
  gap: 1rem;
  width: 100%;
}

.date-field {
  flex: 1;
  min-width: 0; /* Для корректного flex shrink */
}

.date-separator {
  color: var(--md-gray-700, #666);
  font-size: 1.2rem;
  font-weight: 500;
  padding: 0 0.5rem;
  user-select: none;
}
</style>
