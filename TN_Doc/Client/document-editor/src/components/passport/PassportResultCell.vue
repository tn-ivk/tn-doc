import { logger } from '@tn-doc/shared';
<template>
  <!-- Всегда отображаем InputText, чтобы визуально совпадать со столбцами Документы/Измерение -->
  <InputText
    :modelValue="isEditable ? parameter.values.result : displayValue"
    :disabled="!isEditable"
    :class="[
      'result-input',
      { 'elis-filled': isElisFilled },
      { 'manual-input--disabled': !isEditable }
    ]"
    type="text"
    @update:modelValue="handleValueChange"
  />
</template>

<script setup lang="ts">
import { logger } from '@tn-doc/shared';
import { computed } from 'vue';
import InputText from 'primevue/inputtext';
import type { PassportQualityParameter } from '@/types/passport.types';

interface Props {
  parameter: PassportQualityParameter;
  isEditable: boolean;
  isElisFilled?: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:result': [value: string];
}>();

const displayValue = computed(() => {
  if (!props.parameter.values.result) return '-';
  return props.parameter.values.result.replace('.', ',');
});

function handleValueChange(value: string | undefined) {
  const stringValue = value ?? '';
  emit('update:result', stringValue);
  logger.debug(`[PassportResultCell] Result изменено: ${props.parameter.key} -> ${stringValue}`);
}
</script>

<style scoped>
.result-input {
  width: 100%;
  font-size: 15px;
  text-align: center;
}

.result-input:deep(input) {
  text-align: center;
  font-size: 15px;
}

/* ELIS подсветка для InputText */
.result-input.elis-filled {
  background-color: var(--md-elis-highlight, #e8f5e9) !important;
  color: var(--md-text, #212121) !important;
}

.result-input.elis-filled:deep(input) {
  background-color: var(--md-elis-highlight, #e8f5e9) !important;
  color: var(--md-text, #212121) !important;
}

/* Disabled стиль как у колонки Документы */
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
