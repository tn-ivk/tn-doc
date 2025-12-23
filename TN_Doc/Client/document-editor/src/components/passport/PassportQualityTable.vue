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
          <th>Предпросмотр</th>
        </tr>
      </thead>

      <!-- Тело таблицы с группировкой параметров -->
      <tbody>
        <template v-for="group in parameterGroups" :key="group.leader.key">
          <!-- Связанная группа параметров (с rowspan) -->
          <PassportLinkedParameterGroup
            v-if="group.followers.length > 0"
            :leader="group.leader"
            :followers="group.followers"
            :startIndex="group.startIndex"
            :isElisUsed="isElisUsed"
            @update:method="$emit('update:method', $event)"
            @update:measurement="$emit('update:measurement', $event)"
            @update:result="$emit('update:result', $event)"
            @result-edit="handleResultEditRequest"
            @manual-method="handleManualMethodRequest"
          />
          <!-- Одиночный параметр (без группы) -->
          <PassportParameterRow
            v-else
            :parameter="group.leader"
            :index="group.startIndex"
            :isElisUsed="isElisUsed"
            @update:method="$emit('update:method', $event)"
            @update:measurement="$emit('update:measurement', $event)"
            @update:result="$emit('update:result', $event)"
            @result-edit="handleResultEditRequest"
            @manual-method="handleManualMethodRequest"
          />
        </template>
      </tbody>
    </table>
  </div>

  <ResultEditDialog
    :visible="resultDialogVisible"
    :parameter-name="activeResultParameter?.name"
    :initial-value="activeResultParameter?.values.result || ''"
    :measurement-value="activeResultParameter?.values.measurement || ''"
    @update:visible="handleResultDialogVisibility"
    @confirm="handleResultDialogConfirm"
  />

  <ManualMethodDialog
    :visible="manualMethodDialogVisible"
    :parameter-name="activeManualMethodParameter?.name"
    :is-elis-enabled="props.isElisUsed"
    :initial-method="getCurrentMethod()"
    @update:visible="handleManualMethodDialogVisibility"
    @confirm="handleManualMethodConfirm"
    @reset="handleManualMethodReset"
  />
</template>

<script setup lang="ts">
import { logger } from '@tn-doc/shared';
import { ref, computed } from 'vue';
import PassportParameterRow from './PassportParameterRow.vue';
import PassportLinkedParameterGroup from './PassportLinkedParameterGroup.vue';
import ResultEditDialog from './ResultEditDialog.vue';
import ManualMethodDialog, { type ManualMethodPayload } from './ManualMethodDialog.vue';
import { documentApi } from '@/services/api.service';
import type { PassportQualityParameter, MethodOption } from '@/types/passport.types';

/**
 * Группа параметров для отображения в таблице
 */
interface ParameterGroup {
  /** Ведущий параметр группы */
  leader: PassportQualityParameter;
  /** Ведомые параметры (linkedParameter) */
  followers: PassportQualityParameter[];
  /** Начальный индекс (номер первой строки) */
  startIndex: number;
}

interface Props {
  /** Список качественных параметров */
  parameters: PassportQualityParameter[];
  /** Используется ли ELIS */
  isElisUsed: boolean;
  /** Путь к файлу конфигурации (для добавления методов в справочник) */
  editConfigFilePath?: string;
  /** Callback для обновления локального списка методов после добавления в справочник */
  onMethodAddedToDictionary?: (paramKey: string, methodName: string) => void;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:method': [event: { paramKey: string; method: MethodOption | null }];
  'update:measurement': [event: { paramKey: string; value: string }];
  'update:result': [event: { paramKey: string; value: string }];
}>();

/**
 * Группировка параметров по linkedParameter.
 *
 * Логика:
 * - Параметры с role === 'Slave' пропускаются (они скрыты)
 * - Параметры с linkedParameter становятся лидерами групп
 * - Параметры, указанные в linkedParameter другого параметра, становятся followers
 * - Остальные параметры отображаются как одиночные (followers = [])
 */
const parameterGroups = computed<ParameterGroup[]>(() => {
  const groups: ParameterGroup[] = [];
  const processedKeys = new Set<string>();
  let currentIndex = 1;

  for (const param of props.parameters) {
    // Пропускаем уже обработанные параметры
    if (processedKeys.has(param.key)) continue;

    // Пропускаем Slave-параметры (они скрыты по SlaveKey механизму)
    if (param.role === 'Slave') continue;

    const group: ParameterGroup = {
      leader: param,
      followers: [],
      startIndex: currentIndex
    };

    processedKeys.add(param.key);
    currentIndex++;

    // Если у параметра есть linkedParameter, найти ведомый параметр
    if (param.linkedParameter) {
      const follower = props.parameters.find(p => p.key === param.linkedParameter);
      if (follower && !processedKeys.has(param.linkedParameter)) {
        group.followers.push(follower);
        processedKeys.add(param.linkedParameter);
        currentIndex++;
      }
    }

    groups.push(group);
  }

  return groups;
});

const resultDialogVisible = ref(false);
const manualMethodDialogVisible = ref(false);
const activeResultParameter = ref<PassportQualityParameter | null>(null);
const activeManualMethodParameter = ref<PassportQualityParameter | null>(null);

function handleResultEditRequest(event: { paramKey: string }) {
  const target = props.parameters.find(param => param.key === event.paramKey) || null;
  if (!target) {
    return;
  }
  const measurementValue = target.values.measurement;
  if (measurementValue === null || measurementValue === undefined || measurementValue.toString().trim() === '') {
    return;
  }
  activeResultParameter.value = target;
  resultDialogVisible.value = true;
}

function handleResultDialogConfirm(formattedValue: string) {
  if (!activeResultParameter.value) {
    return;
  }

  const paramKey = activeResultParameter.value.key;

  // Эмитим обновление результата
  emit('update:result', { paramKey, value: formattedValue });

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

async function handleManualMethodConfirm(payload: ManualMethodPayload) {
  if (!activeManualMethodParameter.value) {
    return;
  }

  const param = activeManualMethodParameter.value;

  // Добавляем метод в справочник, если путь к конфигурации доступен
  let methodId = Date.now(); // Временный ID по умолчанию
  if (props.editConfigFilePath && payload.name.trim()) {
    try {
      const result = await documentApi.addMethodToDictionary(
        props.editConfigFilePath,
        param.id,
        payload.name,
        payload.isDefault,
        payload.limitValueActivate,
        payload.limitValue,
        payload.limitValueString
      );
      methodId = result.id;
      logger.info('[PassportQualityTable] Метод добавлен в справочник', {
        methodId,
        methodName: payload.name,
        parameterId: param.id
      });

      // Обновляем локальный список методов для убирания предупреждения "отсутствует в справочнике"
      if (props.onMethodAddedToDictionary) {
        props.onMethodAddedToDictionary(param.key, payload.name);
      }
    } catch (error) {
      logger.error('[PassportQualityTable] Ошибка добавления метода в справочник', {
        error: error instanceof Error ? error.message : String(error),
        methodName: payload.name,
        parameterId: param.id
      });
      // Продолжаем сохранение в документ даже при ошибке добавления в справочник
    }
  }

  const newMethod: MethodOption = {
    id: methodId,
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

/**
 * Получить текущий метод активного параметра
 * Ищет метод по имени из method.selected в массиве method.options
 */
function getCurrentMethod(): MethodOption | undefined {
  if (!activeManualMethodParameter.value) {
    return undefined;
  }

  const param = activeManualMethodParameter.value;
  const selectedName = param.method.selected;

  if (!selectedName) {
    return undefined;
  }

  // Найти метод по имени
  return param.method.options.find(opt => opt.name === selectedName);
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
  vertical-align: top;
}

/* Колонка "Документы" */
.th-documents {
  min-width: 150px;
}

/* Ширина колонок */
.col-num {
  width: 30px;
  text-align: center;
}

.col-name {
  width: 405px;
}

.col-method {
  min-width: 180px;
}

.col-documents {
  width: 140px;
  text-align: center;
}

.col-measurement {
  width: 140px;
  text-align: center;
}

.col-result {
  width: 165px;
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

/* ===== Стили для связанных групп параметров (LinkedParameters) ===== */

/* Тонкая левая граница для визуализации группировки */
.quality-table :deep(.linked-group-leader td:first-child),
.quality-table :deep(.linked-group-follower td:first-child) {
  border-left: 3px solid var(--md-primary-light, #64B5F6);
}

/* Ячейки с rowspan центрируются вертикально */
.quality-table :deep(td[rowspan]) {
  vertical-align: top;
}

/* Границы групп - такие же как у обычных параметров (1px solid) */
/* Группы визуально выделены только синей левой границей */
</style>
