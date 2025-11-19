<template>
  <div class="result-with-history">
    <PassportResultCell
      :parameter="parameter"
      :canEdit="canEdit"
      :isElisFilled="isElisFilled"
      :showSyncIcon="parameter.isBallast === true"
      :editDisabledReason="editDisabledReason"
      @result-edit="handleEditRequest"
    />

    <!-- Индикатор истории -->
    <FieldHistoryIndicator
      v-if="lastSource !== DataSource.Unknown"
      :source="lastSource"
      @mouseenter="(event) => onIndicatorHover(event)"
      @mouseleave="onIndicatorLeave"
    />

    <!-- Popup с историей -->
    <FieldHistoryPopup
      ref="historyPopup"
      :history="fieldHistory"
      :fieldLabel="`${parameter.name} (Результат)`"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import PassportResultCell from './PassportResultCell.vue';
import FieldHistoryIndicator from '@/components/history/FieldHistoryIndicator.vue';
import FieldHistoryPopup from '@/components/history/FieldHistoryPopup.vue';
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

const {
  getFieldHistory,
  getLastSource
} = useFieldHistory();

const historyPopup = ref<InstanceType<typeof FieldHistoryPopup>>();
let hideTimeout: ReturnType<typeof setTimeout> | null = null;

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

const isElisFilled = computed(() => lastSource.value === DataSource.ELIS);

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
 * Обработчик наведения на индикатор
 */
const onIndicatorHover = (event: MouseEvent) => {
  // Отменяем таймер скрытия, если он был запущен
  if (hideTimeout) {
    clearTimeout(hideTimeout);
    hideTimeout = null;
  }

  // Показываем popup, передавая событие для правильного позиционирования
  historyPopup.value?.show(event);
};

/**
 * Обработчик ухода курсора с индикатора
 */
const onIndicatorLeave = () => {
  // Запускаем таймер скрытия с задержкой 300ms
  hideTimeout = setTimeout(() => {
    historyPopup.value?.hide();
    hideTimeout = null;
  }, 300);
};
</script>

<style scoped>
.result-with-history {
  position: relative;
}
</style>
