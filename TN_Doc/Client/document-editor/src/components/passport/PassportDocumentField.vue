<template>
  <InputText
    :modelValue="documentNumber"
    :class="{ 'elis-filled': isElisFilled }"
    type="text"
    disabled
    class="document-field manual-input--disabled"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import InputText from 'primevue/inputtext';
import type { PassportQualityParameter } from '@/types/passport.types';

interface Props {
  /** Параметр качества */
  parameter: PassportQualityParameter;
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
 * Заполнен ли документ из ELIS
 */
const isElisFilled = computed(() => {
  return props.parameter.document?.elisFilled || false;
});
</script>

<style scoped>
.document-field {
  width: 100%;
  text-align: center;
  font-size: 15px;
  background-color: var(--md-surface-variant, #F1F3F4);
  color: var(--md-text-secondary, #5F6368);
  cursor: not-allowed;
  border: none;
  padding: 6px 12px;
  box-sizing: border-box;
}

.document-field:deep(input) {
  padding: 6px 12px;
  text-align: center;
  background-color: var(--md-surface-variant, #F1F3F4);
  color: var(--md-text-secondary, #5F6368);
  cursor: not-allowed;
  border: none;
  box-sizing: border-box;
}

/* ELIS подсветка */
.elis-filled {
  background-color: var(--md-elis-highlight, #e8f5e9) !important;
  color: var(--md-text, #212121) !important;
}

.elis-filled:deep(input) {
  background-color: var(--md-elis-highlight, #e8f5e9) !important;
  color: var(--md-text, #212121) !important;
}

.manual-input--disabled {
  opacity: 1;
}
</style>
