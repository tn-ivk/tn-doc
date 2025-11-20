<template>
  <div class="quality-table-container">
    <table
      id="Edit"
      class="quality-table"
      :class="{ 'no-elis': !isElisUsed }"
    >
      <!-- Определение ширины колонок -->
      <colgroup>
        <col class="col-num">          <!-- № -->
        <col class="col-name">         <!-- Наименование -->
        <col class="col-method">       <!-- Метод -->
        <col class="col-documents" v-if="isElisUsed">  <!-- Документы (условная) -->
        <col class="col-measurement">  <!-- Измерение -->
        <col class="col-result">       <!-- Результат -->
      </colgroup>

      <!-- Заголовок таблицы (ОДНА строка вместо двух) -->
      <thead>
        <tr>
          <th>№</th>
          <th>Наименование показателя</th>
          <th>Метод испытаний</th>
          <th v-if="isElisUsed" class="th-documents">Документы</th>
          <th>Измерение</th>
          <th>Результат</th>
        </tr>
      </thead>

      <!-- Тело таблицы -->
      <tbody>
        <PassportParameterRow
          v-for="(param, index) in parameters"
          :key="param.key"
          :parameter="param"
          :index="index + 1"
          :isElisUsed="isElisUsed"
          @update:method="$emit('update:method', $event)"
          @update:measurement="$emit('update:measurement', $event)"
          @update:result="$emit('update:result', $event)"
          @result-edit="handleResultEditRequest"
          @manual-method="handleManualMethodRequest"
        />
      </tbody>
    </table>
  </div>

  <ResultEditDialog
    :visible="resultDialogVisible"
    :parameter-name="activeResultParameter?.name"
    :initial-value="activeResultParameter?.values.result || ''"
    @update:visible="handleResultDialogVisibility"
    @confirm="handleResultDialogConfirm"
  />

  <ManualMethodDialog
    :visible="manualMethodDialogVisible"
    :parameter-name="activeManualMethodParameter?.name"
    @update:visible="handleManualMethodDialogVisibility"
    @confirm="handleManualMethodConfirm"
    @reset="handleManualMethodReset"
  />
</template>

<script setup lang="ts">
import { ref } from 'vue';
import PassportParameterRow from './PassportParameterRow.vue';
import ResultEditDialog from './ResultEditDialog.vue';
import ManualMethodDialog, { type ManualMethodPayload } from './ManualMethodDialog.vue';
import type { PassportQualityParameter, MethodOption } from '@/types/passport.types';

interface Props {
  /** Список качественных параметров */
  parameters: PassportQualityParameter[];
  /** Используется ли ELIS */
  isElisUsed: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:method': [event: { paramKey: string; method: MethodOption | null }];
  'update:measurement': [event: { paramKey: string; value: string }];
  'update:result': [event: { paramKey: string; value: string }];
}>();

const resultDialogVisible = ref(false);
const manualMethodDialogVisible = ref(false);
const activeResultParameter = ref<PassportQualityParameter | null>(null);
const activeManualMethodParameter = ref<PassportQualityParameter | null>(null);

function handleResultEditRequest(event: { paramKey: string }) {
  const target = props.parameters.find(param => param.key === event.paramKey) || null;
  if (!target) {
    return;
  }
  activeResultParameter.value = target;
  resultDialogVisible.value = true;
}

function handleResultDialogConfirm(formattedValue: string) {
  if (!activeResultParameter.value) {
    return;
  }
  emit('update:result', {
    paramKey: activeResultParameter.value.key,
    value: formattedValue
  });
  resultDialogVisible.value = false;
  activeResultParameter.value = null;
}

function handleResultDialogVisibility(next: boolean) {
  resultDialogVisible.value = next;
  if (!next) {
    activeResultParameter.value = null;
  }
}

function handleManualMethodRequest(event: { paramKey: string }) {
  const target = props.parameters.find(param => param.key === event.paramKey) || null;
  if (!target) {
    return;
  }
  activeManualMethodParameter.value = target;
  manualMethodDialogVisible.value = true;
}

function handleManualMethodConfirm(payload: ManualMethodPayload) {
  if (!activeManualMethodParameter.value) {
    return;
  }

  const param = activeManualMethodParameter.value;
  const newMethod: MethodOption = {
    id: Date.now(),
    use: true,
    idParameter: param.id,
    name: payload.name,
    isDefault: payload.isDefault,
    limitValueActivate: payload.limitValueActivate,
    limitValue: payload.limitValue,
    limitValueString: payload.limitValueString,
    source: 'manual'
  };

  emit('update:method', { paramKey: param.key, method: newMethod });
  manualMethodDialogVisible.value = false;
  activeManualMethodParameter.value = null;
}

function handleManualMethodReset() {
  if (!activeManualMethodParameter.value) {
    return;
  }

  emit('update:method', { paramKey: activeManualMethodParameter.value.key, method: null });
  manualMethodDialogVisible.value = false;
  activeManualMethodParameter.value = null;
}

function handleManualMethodDialogVisibility(next: boolean) {
  manualMethodDialogVisible.value = next;
  if (!next) {
    activeManualMethodParameter.value = null;
  }
}
</script>

<style scoped>
.quality-table-container {
  width: 100%;
  background: #ffffff;
  border: 1px solid var(--md-outline);
  border-radius: 8px;
  box-shadow: 0 2px 6px rgba(33, 33, 33, 0.04);
  overflow: auto;
  flex: 1;
  display: flex;
  min-height: 0;
}

.quality-table {
  width: 100%;
  border-collapse: collapse;
  table-layout: fixed;
  font-family: 'Segoe UI', 'PT Astra Sans', -apple-system, BlinkMacSystemFont, 'Helvetica Neue', Arial, sans-serif;
  font-size: 15px;
  color: var(--md-text);
}

/* Заголовки таблицы */
.quality-table th {
  background-color: var(--md-surface-variant, #F1F3F4);
  font-weight: 600;
  text-align: center;
  padding: 6px 10px;
  border: 1px solid var(--md-outline-light, #E0E0E0);
  color: var(--md-text, #212121);
  font-size: 15px;
}

/* Ячейки тела таблицы */
.quality-table td {
  padding: 6px 10px;
  border: 1px solid var(--md-outline-light, #E0E0E0);
  background-color: white;
  vertical-align: middle;
}

/* Колонка "Документы" */
.th-documents {
  min-width: 150px;
}

/* Ширина колонок */
.col-num {
  width: 40px;
  text-align: center;
}

.col-name {
  min-width: 200px;
  max-width: 400px;
}

.col-method {
  min-width: 180px;
}

.col-documents {
  width: 130px;
  text-align: center;
}

.col-measurement {
  width: 130px;
  text-align: center;
}

.col-result {
  width: 130px;
  text-align: center;
}

/* Скрытие колонки "Документы" когда ELIS не используется */
.quality-table.no-elis .col-documents,
.quality-table.no-elis .th-documents {
  display: none;
}

/* Скругление углов */
.quality-table thead tr:first-child th:first-child {
  border-top-left-radius: 8px;
}

.quality-table thead tr:first-child th:last-child {
  border-top-right-radius: 8px;
}

.quality-table tbody tr:last-child td:first-child {
  border-bottom-left-radius: 8px;
}

.quality-table tbody tr:last-child td:last-child {
  border-bottom-right-radius: 8px;
}
</style>
