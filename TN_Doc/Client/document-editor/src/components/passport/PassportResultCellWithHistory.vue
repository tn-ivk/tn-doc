<template>
  <div class="result-with-history">
    <PassportResultCell
      :parameter="parameter"
      :isEditable="isEditable"
      @update:result="handleChange"
    />

    <!-- Индикатор истории -->
    <FieldHistoryIndicator
      v-if="lastSource !== DataSource.Unknown"
      :source="lastSource"
      @click="onIndicatorClick"
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
  'update:result': [value: string];
}>();

const {
  getFieldHistory,
  getLastSource,
  trackManualChange
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

/**
 * Обработка изменения результата
 */
const handleChange = (newValue: string) => {
  // Отслеживаем ручное изменение
  trackManualChange(historyKey.value, newValue, props.parameter.values.result);

  // Передаем изменение дальше
  emit('update:result', newValue);
};

/**
 * Обработчик клика на индикатор
 */
const onIndicatorClick = () => {
  // OverlayPanel.show() может быть вызвана без параметра
  historyPopup.value?.show(undefined as any);
};
</script>

<style scoped>
.result-with-history {
  position: relative;
}
</style>
