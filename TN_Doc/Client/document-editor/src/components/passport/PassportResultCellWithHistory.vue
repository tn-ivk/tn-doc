<template>
  <PassportResultCell
    :parameter="parameter"
    :canEdit="canEdit"
    :isElisFilled="isElisFilled"
    :editDisabledReason="editDisabledReason"
    @result-edit="handleEditRequest"
  >
    <template #indicators>
      <FieldHistoryIndicator
        v-if="showHistoryIndicator"
        :source="lastSource"
        :rightOffset="0"
      />
      <FieldHistoryIndicator
        v-if="showElisMissing"
        :source="DataSource.ElisMissing"
        :rightOffset="0"
      />
    </template>
  </PassportResultCell>
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

const { getLastSource, hasElisMissing } = useFieldHistory();

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

const showElisMissing = computed(() => {
  return hasElisMissing(historyKey.value);
});

const showHistoryIndicator = computed(() => {
  return lastSource.value !== DataSource.Unknown && lastSource.value !== DataSource.Auto;
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
