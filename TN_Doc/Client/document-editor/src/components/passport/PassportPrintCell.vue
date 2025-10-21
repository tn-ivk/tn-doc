<template>
  <!-- Редактируемая ячейка печати -->
  <InputText
    v-if="isEditable"
    :modelValue="parameter.values.printValue"
    :class="{ 'elis-filled': parameter.elisFlags.printValue }"
    type="text"
    class="print-cell-input"
    @update:modelValue="handleValueChange"
  />

  <!-- Нередактируемая ячейка печати (просто текст) -->
  <span
    v-else
    :class="{ 'elis-filled-text': parameter.elisFlags.printValue }"
    class="print-cell-readonly"
  >
    {{ displayValue }}
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import InputText from 'primevue/inputtext';
import type { PassportQualityParameter } from '@/types/passport.types';

interface Props {
  /** Параметр качества */
  parameter: PassportQualityParameter;
  /** Редактируема ли ячейка */
  isEditable: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:printValue': [value: string];
}>();

/**
 * Отображаемое значение (с форматированием)
 */
const displayValue = computed(() => {
  if (!props.parameter.values.printValue) return '-';
  return props.parameter.values.printValue.replace('.', ',');
});

/**
 * Обработчик изменения значения
 */
function handleValueChange(value: string | undefined) {
  const stringValue = value ?? '';
  emit('update:printValue', stringValue);
  console.log(`[PassportPrintCell] PrintValue изменено: ${props.parameter.key} -> ${stringValue}`);
}
</script>

<style scoped>
.print-cell-input {
  width: 100%;
  border: none;
  background: transparent;
  text-align: center;
  font-family: inherit;
  font-size: 15px;
  padding: 2px;
  outline: none;
  box-sizing: border-box;
  margin: 0;
}

.print-cell-input:focus {
  outline: none;
  background: #f8f9fa;
}

.print-cell-readonly {
  display: block;
  width: 100%;
  text-align: center;
  font-size: 15px;
}

/* ELIS подсветка для input */
.elis-filled {
  background-color: #8fd19e !important;
}

/* ELIS подсветка для readonly текста */
.elis-filled-text {
  background-color: #8fd19e;
  display: inline-block;
  width: 100%;
  padding: 2px;
}
</style>
