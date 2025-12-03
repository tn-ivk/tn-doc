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
      @click="onIndicatorClick"
    />

    <!-- Popup с историей -->
    <FieldHistoryPopup
      ref="historyPopup"
      :history="fieldHistory"
      :fieldLabel="parameter.name"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import PassportResultCell from './PassportResultCell.vue';
import FieldHistoryIndicator from '@/components/history/FieldHistoryIndicator.vue';
import FieldHistoryPopup from '@/components/history/FieldHistoryPopup.vue';
import { useFieldHistory } from '@/composables/useFieldHistory';
import { useDocumentStore } from '@/stores/documentStore';
import { DataSource } from '@/types/history.types';
import type { PassportQualityParameter } from '@/types/passport.types';
import { closeAllHistoryPopups } from '@/utils/historyPopupEvents';

const props = defineProps<{
  parameter: PassportQualityParameter;
  isEditable: boolean;
}>();

const emit = defineEmits<{
  'result-edit': [];
}>();

const {
  getFieldHistory,
  getLastSource
} = useFieldHistory();

const historyPopup = ref<InstanceType<typeof FieldHistoryPopup>>();

/**
 * Ключ для истории: result.{parameterKey}
 */
const historyKey = computed(() => `result.${props.parameter.key}`);

/**
 * История поля
 */
const fieldHistory = computed(() => {
  return getFieldHistory(historyKey.value);
});

/**
 * Последний источник изменений
 */
const lastSource = computed(() => {
  return getLastSource(historyKey.value);
});

// Используем флаг из formData, а не из истории - так корректно отрабатывает возврат к ELIS
const store = useDocumentStore();
const isElisFilled = computed(() => {
  const flag = store.formData[`${historyKey.value}__elisFilled`] === true;
  console.log(`[PassportResultCellWithHistory] isElisFilled для ${historyKey.value}:`, flag, 'lastSource:', lastSource.value);
  return flag;
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

/**
 * Обработчик клика на индикатор - показываем popup
 */
const onIndicatorClick = (event: MouseEvent) => {
  // Закрыть все другие popup-ы истории перед открытием нового
  closeAllHistoryPopups();
  historyPopup.value?.show(event);
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
