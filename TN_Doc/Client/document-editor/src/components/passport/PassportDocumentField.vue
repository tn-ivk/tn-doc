<template>
  <div
    class="document-field-wrapper"
    :class="{ 'elis-missing-border': isElisMissing }"
    :title="isElisMissing ? 'Ожидалось из ЕЛИС' : documentTooltip"
  >
    <InputText
      :modelValue="documentNumber"
      type="text"
      disabled
      class="document-field manual-input--disabled"
    />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import InputText from 'primevue/inputtext';
import type { PassportQualityParameter } from '@/types/passport.types';

interface Props {
  /** Параметр качества */
  parameter: PassportQualityParameter;
  /** Флаг: ожидалось из ELIS, но не загружено */
  isElisMissing?: boolean;
}

const props = defineProps<Props>();

/**
 * Номер документа (или placeholder если пусто)
 */
const documentNumber = computed(() => {
  if (!props.parameter.document || !props.parameter.document.number) {
    return '—';
  }
  return props.parameter.document.number;
});

/**
 * Подсказка с дополнительной информацией о документе
 */
const documentTooltip = computed(() => {
  if (!props.parameter.document) {
    return '';
  }

  const parts: string[] = [];
  if (props.parameter.document.type) {
    parts.push(props.parameter.document.type);
  }
  if (props.parameter.document.date) {
    parts.push(props.parameter.document.date);
  }
  return parts.join(' • ');
});
</script>

<style scoped>
.document-field-wrapper {
  width: 100%;
}

.document-field {
  width: 100%;
  max-width: 100%;
  text-align: center;
  font-size: 15px;
  background-color: var(--md-surface-variant, #F1F3F4);
  color: var(--md-text-secondary, #5F6368);
  cursor: not-allowed;
  border: none;
  padding: 6px 12px;
  box-sizing: border-box;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.document-field:deep(input) {
  padding: 6px 12px;
  text-align: center;
  background-color: var(--md-surface-variant, #F1F3F4);
  color: var(--md-text-secondary, #5F6368);
  cursor: not-allowed;
  border: none;
  box-sizing: border-box;
  width: 100%;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  box-sizing: border-box;
}

.manual-input--disabled {
  opacity: 1;
}

/* Желтая рамка для полей, ожидавшихся из ELIS, но не загруженных */
.document-field-wrapper.elis-missing-border .document-field {
  border: 2px solid #f5c24c !important;
  border-radius: var(--md-radius, 4px);
}

.document-field-wrapper.elis-missing-border :deep(.p-inputtext) {
  border: 2px solid #f5c24c !important;
  border-radius: var(--md-radius, 4px);
}
</style>
