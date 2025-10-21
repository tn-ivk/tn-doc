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
      <PassportDocumentField
        :parameter="parameter"
      />
    </td>

    <!-- Измерение ИВК (только чтение) -->
    <td class="cell-ivk manual-input--disabled">
      {{ formatValue(parameter.values.ivk) }}
    </td>

    <!-- Измерение ХАЛ (редактируемое) -->
    <td class="cell-hal">
      <PassportHalInput
        :parameter="parameter"
        @update:halValue="handleHalValueUpdate"
      />
    </td>

    <!-- Результат - Значение (только чтение) -->
    <td class="cell-result-value manual-input--disabled">
      {{ formatValue(parameter.values.result) }}
    </td>

    <!-- Результат - Текст (может быть редактируемым) -->
    <td
      class="cell-result-text"
      :class="{ 'manual-input--disabled': !isPrintCellEditable }"
    >
      <PassportPrintCell
        :parameter="parameter"
        :isEditable="isPrintCellEditable"
        @update:printValue="handlePrintValueUpdate"
      />
    </td>
  </tr>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import PassportMethodSelect from './PassportMethodSelect.vue';
import PassportDocumentField from './PassportDocumentField.vue';
import PassportHalInput from './PassportHalInput.vue';
import PassportPrintCell from './PassportPrintCell.vue';
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
  'update:halValue': [event: { paramKey: string; value: string }];
  'update:method': [event: { paramKey: string; methodName: string }];
  'update:printValue': [event: { paramKey: string; value: string }];
}>();

/**
 * Определить, редактируема ли ячейка печати
 */
const isPrintCellEditable = computed(() => {
  const selectedMethod = props.parameter.method.options.find(
    (m: MethodOption) => m.name === props.parameter.method.selected
  );

  if (!selectedMethod || !selectedMethod.limitValueActivate) {
    return false;
  }

  const halValue = parseFloat(props.parameter.values.hal.replace(',', '.'));
  if (isNaN(halValue)) {
    return false;
  }

  return selectedMethod.limitValue !== undefined && halValue < selectedMethod.limitValue;
});

/**
 * Форматирование значения (замена точки на запятую)
 */
function formatValue(value: string): string {
  if (!value) return '-';
  return value.replace('.', ',');
}

/**
 * Обработчик обновления значения ХАЛ
 */
function handleHalValueUpdate(value: string) {
  emit('update:halValue', {
    paramKey: props.parameter.key,
    value
  });
}

/**
 * Обработчик обновления метода испытаний
 */
function handleMethodUpdate(methodName: string) {
  emit('update:method', {
    paramKey: props.parameter.key,
    methodName
  });
}

/**
 * Обработчик обновления значения для печати
 */
function handlePrintValueUpdate(value: string) {
  emit('update:printValue', {
    paramKey: props.parameter.key,
    value
  });
}
</script>

<style scoped>
.parameter-row td {
  vertical-align: middle;
}

.cell-number {
  text-align: center;
  font-weight: 500;
}

.cell-name {
  white-space: pre-wrap;
  word-wrap: break-word;
}

.cell-method,
.cell-hal {
  padding: 4px;
}

.cell-ivk,
.cell-result-value,
.cell-result-text {
  text-align: center;
}

.manual-input--disabled {
  background-color: var(--md-surface-variant, #F1F3F4);
  color: var(--md-text-secondary, #5F6368);
  cursor: not-allowed;
}

/* Стили для ELIS подсветки */
.elis-filled-cell {
  background-color: #8fd19e !important;
}
</style>
