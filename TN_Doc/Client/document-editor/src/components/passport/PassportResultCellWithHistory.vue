<template>
  <div
    class="result-with-history"
  >
    <PassportResultCell
      :parameter="parameter"
      :canEdit="canEdit"
      :isElisFilled="isElisFilled"
      :editDisabledReason="editDisabledReason"
      :hasHistoryIndicator="lastSource !== DataSource.Unknown && lastSource !== DataSource.Auto"
      @result-edit="handleEditRequest"
    />

    <!-- Индикатор истории -->
    <FieldHistoryIndicator
      v-if="lastSource !== DataSource.Unknown && lastSource !== DataSource.Auto"
      :source="lastSource"
      :rightOffset="4"
    />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import PassportResultCell from './PassportResultCell.vue';
import FieldHistoryIndicator from '@/components/history/FieldHistoryIndicator.vue';
import { useFieldHistory } from '@/composables/useFieldHistory';
import { DataSource } from '@/types/history.types';
import type { PassportQualityParameter } from '@/types/passport.types';

const props = defineProps<{
  parameter: PassportQualityParameter;
  isEditable: boolean;
}>();

const emit = defineEmits<{
  'result-edit': [];
}>();

const { getLastSource } = useFieldHistory();

/**
 * Ключ для истории: result.{parameterKey}
 */
const historyKey = computed(() => `result.${props.parameter.key}`);

/**
 * Последний источник изменений
 */
const lastSource = computed(() => {
  return getLastSource(historyKey.value);
});

/**
 * Флаг ELIS-заполненности: определяется по последнему источнику в истории
 * ELIS и ReturnToELIS отображаются одинаково (зелёная подсветка)
 */
const isElisFilled = computed(() => {
  return lastSource.value === DataSource.ELIS || lastSource.value === DataSource.ReturnToELIS;
});

const canEdit = computed(() => props.isEditable);

const editDisabledReason = computed(() => {
  if (!props.isEditable) {
    return 'Балластный параметр синхронизируется с измерением';
  }
  return '';
});

/**
 * Обработка изменения результата
 */
const handleEditRequest = () => {
  emit('result-edit');
};
</script>

<style scoped>
.result-with-history {
  position: relative;
  display: flex;
  align-items: center;
  gap: 2px;
  width: 100%;
  max-width: 100%;
  overflow: hidden;
}

</style>
