<template>
  <!-- Строка ведущего параметра -->
  <tr class="parameter-row linked-group-leader">
    <!-- №: Номер строки -->
    <td class="cell-number">{{ startIndex }}</td>

    <!-- Наименование показателя -->
    <td class="cell-name">{{ leader.name }}</td>

    <!-- Метод испытаний с rowspan -->
    <td class="cell-method" :rowspan="totalRows">
      <PassportMethodSelectWithHistory
        :parameter="leader"
        @update:method="handleMethodUpdate"
        @manual-method="handleManualMethodRequest"
      />
    </td>

    <!-- Документы с rowspan (только если ELIS используется) -->
    <td v-if="isElisUsed" class="cell-documents td-documents" :rowspan="totalRows">
      <PassportDocumentField :parameter="leader" />
    </td>

    <!-- Измерение -->
    <td class="cell-measurement">
      <PassportMeasurementInputWithHistory
        :parameter="leader"
        @update:measurement="handleLeaderMeasurementUpdate"
      />
    </td>

    <!-- Результат -->
    <td class="cell-result">
      <PassportResultCellWithHistory
        :parameter="leader"
        :isEditable="isLeaderResultEditable"
        @result-edit="handleResultEditRequest(leader.key)"
      />
    </td>
  </tr>

  <!-- Строки ведомых параметров -->
  <tr
    v-for="(follower, idx) in followers"
    :key="follower.key"
    class="parameter-row linked-group-follower"
  >
    <!-- №: Номер строки -->
    <td class="cell-number">{{ startIndex + idx + 1 }}</td>

    <!-- Наименование показателя -->
    <td class="cell-name">{{ follower.name }}</td>

    <!-- Метод и Документы НЕ отрисовываются - используется rowspan от leader -->

    <!-- Измерение -->
    <td class="cell-measurement">
      <PassportMeasurementInputWithHistory
        :parameter="follower"
        @update:measurement="(value: string) => handleFollowerMeasurementUpdate(follower.key, value)"
      />
    </td>

    <!-- Результат -->
    <td class="cell-result">
      <PassportResultCellWithHistory
        :parameter="follower"
        :isEditable="isFollowerResultEditable(follower)"
        @result-edit="handleResultEditRequest(follower.key)"
      />
    </td>
  </tr>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import PassportMethodSelectWithHistory from './PassportMethodSelectWithHistory.vue';
import PassportDocumentField from './PassportDocumentField.vue';
import PassportMeasurementInputWithHistory from './PassportMeasurementInputWithHistory.vue';
import PassportResultCellWithHistory from './PassportResultCellWithHistory.vue';
import type { PassportQualityParameter, MethodOption } from '@/types/passport.types';

interface Props {
  /** Ведущий параметр группы (у которого есть linkedParameter) */
  leader: PassportQualityParameter;
  /** Ведомые параметры группы (указаны в linkedParameter) */
  followers: PassportQualityParameter[];
  /** Начальный индекс (номер первой строки группы) */
  startIndex: number;
  /** Используется ли ELIS */
  isElisUsed: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:method': [event: { paramKey: string; method: MethodOption | null }];
  'update:measurement': [event: { paramKey: string; value: string }];
  'update:result': [event: { paramKey: string; value: string }];
  'result-edit': [event: { paramKey: string }];
  'manual-method': [event: { paramKey: string }];
}>();

/**
 * Общее количество строк в группе (leader + followers)
 */
const totalRows = computed(() => 1 + props.followers.length);

/**
 * Определить, редактируема ли ячейка результата лидера
 * Условия редактируемости:
 * - editable = true (учитывает Edit из конфига И что параметр не Slave)
 * - isBallast = false (балластные показатели не редактируются)
 */
const isLeaderResultEditable = computed(() =>
  props.leader.editable && props.leader.isBallast !== true
);

/**
 * Определить, редактируема ли ячейка результата follower
 */
function isFollowerResultEditable(follower: PassportQualityParameter): boolean {
  return follower.editable && follower.isBallast !== true;
}

/**
 * Обработка изменения метода (применяется к ведущему параметру)
 * Синхронизация с ведомыми параметрами происходит в usePassportEditor
 */
function handleMethodUpdate(method: MethodOption | null) {
  emit('update:method', { paramKey: props.leader.key, method });
}

/**
 * Обработка изменения измерения у ведущего параметра
 */
function handleLeaderMeasurementUpdate(value: string) {
  emit('update:measurement', { paramKey: props.leader.key, value });
}

/**
 * Обработка изменения измерения у ведомого параметра
 */
function handleFollowerMeasurementUpdate(paramKey: string, value: string) {
  emit('update:measurement', { paramKey, value });
}

/**
 * Обработка запроса на редактирование результата
 */
function handleResultEditRequest(paramKey: string) {
  emit('result-edit', { paramKey });
}

/**
 * Обработка запроса на ручной ввод метода
 */
function handleManualMethodRequest() {
  emit('manual-method', { paramKey: props.leader.key });
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
  overflow: hidden;
}

.cell-documents {
  padding: 4px;
  overflow: hidden;
}

.cell-result {
  text-align: center;
  padding: 4px;
  overflow: hidden;
}

/* Стили для связанной группы */
.linked-group-leader td.cell-method,
.linked-group-leader td.cell-documents {
  vertical-align: middle;
}

/* Визуальное выделение связанной группы */
.linked-group-leader td,
.linked-group-follower td {
  background-color: var(--md-surface, #ffffff);
}

/* Тонкая левая граница для визуализации группировки */
.linked-group-leader td:first-child,
.linked-group-follower td:first-child {
  border-left: 3px solid var(--md-primary-light, #64B5F6);
}

/* Убираем нижнюю границу между строками одной группы для визуальной связи */
.linked-group-leader td {
  border-bottom-color: var(--md-outline-light, #E0E0E0);
}

.linked-group-follower:not(:last-child) td {
  border-bottom-color: var(--md-outline-light, #E0E0E0);
}
</style>
