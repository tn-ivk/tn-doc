<template>
  <div class="quality-table-container">
    <table
      id="Edit"
      class="quality-table"
      :class="{ 'no-elis': !isElisUsed }"
    >
      <!-- Определение ширины колонок -->
      <colgroup>
        <col class="col-num">
        <col class="col-name">
        <col class="col-method">
        <col class="col-documents" v-if="isElisUsed">
        <col class="col-ivk">
        <col class="col-hal">
        <col class="col-result-value">
        <col class="col-result-text">
      </colgroup>

      <!-- Заголовок таблицы (двухстрочный) -->
      <thead>
        <!-- Первая строка заголовков -->
        <tr>
          <th rowspan="2">№</th>
          <th rowspan="2">Наименование показателя</th>
          <th rowspan="2">Метод испытаний</th>
          <th v-if="isElisUsed" rowspan="2" class="th-documents">Документы</th>
          <th rowspan="2">Измерение ИВК</th>
          <th rowspan="2">Измерение ХАЛ</th>
          <th colspan="2">Результат</th>
        </tr>

        <!-- Вторая строка заголовков (подзаголовки для Результата) -->
        <tr>
          <!-- Пустые ячейки для объединенных заголовков -->
          <th class="th-spacer" v-for="n in spacerColumnsCount" :key="`spacer-${n}`"></th>
          <!-- Подзаголовки для "Результат" -->
          <th>Значение</th>
          <th>Текст</th>
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
          @update:halValue="$emit('update:halValue', $event)"
          @update:method="$emit('update:method', $event)"
          @update:printValue="$emit('update:printValue', $event)"
        />
      </tbody>
    </table>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import PassportParameterRow from './PassportParameterRow.vue';
import type { PassportQualityParameter } from '@/types/passport.types';

interface Props {
  /** Список качественных параметров */
  parameters: PassportQualityParameter[];
  /** Используется ли ELIS */
  isElisUsed: boolean;
}

const props = defineProps<Props>();

defineEmits<{
  'update:halValue': [event: { paramKey: string; value: string }];
  'update:method': [event: { paramKey: string; methodName: string }];
  'update:printValue': [event: { paramKey: string; value: string }];
}>();

/**
 * Количество пустых ячеек во второй строке заголовка
 * (для выравнивания с объединенными ячейками в первой строке)
 */
const spacerColumnsCount = computed(() => {
  // №, Наименование, Метод, Документы (если ELIS), ИВК, ХАЛ
  return props.isElisUsed ? 6 : 5;
});
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
  border: 1px solid var(--md-outline, #CFD8DC);
  color: var(--md-text, #212121);
  font-size: 15px;
}

/* Ячейки тела таблицы */
.quality-table td {
  padding: 6px 10px;
  border: 1px solid var(--md-outline, #CFD8DC);
  background-color: white;
  vertical-align: middle;
}

/* Пустые ячейки во второй строке заголовка (для выравнивания) */
.th-spacer {
  display: none;
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
  min-width: 150px;
}

.col-ivk {
  width: 100px;
  text-align: center;
}

.col-hal {
  width: 100px;
  text-align: center;
}

.col-result-value {
  width: 100px;
  text-align: center;
}

.col-result-text {
  width: 120px;
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
