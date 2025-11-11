<template>
  <tr class="parameter-row">
    <!-- №: Номер строки -->
    <td class="cell-number">{{ index }}</td>

    <!-- Наименование показателя -->
    <td class="cell-name">{{ parameter.name }}</td>

    <!-- Метод испытаний -->
    <td class="cell-method">
      <PassportMethodSelect
        :parameter="parameter"
        @update:method="handleMethodUpdate"
      />
    </td>

    <!-- Документы (только если ELIS используется) -->
    <td v-if="isElisUsed" class="cell-documents td-documents">
      <PassportDocumentField :parameter="parameter" />
    </td>

    <!-- Измерение (объединенная колонка) -->
    <td class="cell-measurement">
      <PassportMeasurementInput
        :parameter="parameter"
        @update:measurement="handleMeasurementUpdate"
      />
    </td>

    <!-- Результат (может быть редактируемым) -->
    <td class="cell-result">
      <PassportResultCell
        :parameter="parameter"
        :isEditable="isResultEditable"
        @update:result="handleResultUpdate"
      />
    </td>
  </tr>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import PassportMethodSelect from './PassportMethodSelect.vue';
import PassportDocumentField from './PassportDocumentField.vue';
import PassportMeasurementInput from './PassportMeasurementInput.vue';
import PassportResultCell from './PassportResultCell.vue';
import type { PassportQualityParameter, MethodOption } from '@/types/passport.types';

interface Props {
  /** Параметр качества */
  parameter: PassportQualityParameter;
  /** Номер строки */
  index: number;
  /** Используется ли ELIS */
  isElisUsed: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:method': [event: { paramKey: string; method: MethodOption | null }];
  'update:measurement': [event: { paramKey: string; value: string }];
  'update:result': [event: { paramKey: string; value: string }];
}>();

// Component mounted without debug logging

/**
 * Определить, редактируема ли ячейка результата
 */
const isResultEditable = computed(() => {
  const selectedMethod = props.parameter.method.options.find(
    (m: MethodOption) => m.name === props.parameter.method.selected
  );

  if (!selectedMethod || !selectedMethod.limitValueActivate) {
    return false;
  }

  const measurementValue = parseFloat(props.parameter.values.measurement.replace(',', '.'));
  if (isNaN(measurementValue)) {
    return false;
  }

  return selectedMethod.limitValue !== undefined && measurementValue < selectedMethod.limitValue;
});

function handleMethodUpdate(method: MethodOption | null) {
  emit('update:method', { paramKey: props.parameter.key, method });
}

function handleMeasurementUpdate(value: string) {
  emit('update:measurement', { paramKey: props.parameter.key, value });
}

function handleResultUpdate(value: string) {
  emit('update:result', { paramKey: props.parameter.key, value });
}
</script>

<style scoped>
.parameter-row td {
  vertical-align: middle;
  border: 1px solid var(--md-outline-light, #E0E0E0);
}

/* Более специфичные селекторы для переопределения глобальных стилей */
.quality-table .parameter-row td {
  border: 1px solid var(--md-outline-light, #E0E0E0);
}

.cell-number {
  text-align: center;
  font-weight: 500;
}

.cell-name {
  white-space: pre-wrap;
  word-wrap: break-word;
  padding-left: 5px;
}

.cell-method,
.cell-measurement {
  padding: 4px;
}

.cell-documents {
  padding: 4px;
}

.cell-result {
  text-align: center;
  padding: 4px;
}

.manual-input--disabled {
  background-color: var(--md-surface-variant, #F1F3F4);
  color: var(--md-text-secondary, #5F6368);
  cursor: not-allowed;
}
</style>
