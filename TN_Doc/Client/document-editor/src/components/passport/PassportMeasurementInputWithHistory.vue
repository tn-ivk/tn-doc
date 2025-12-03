<template>
  <div
    class="measurement-with-history"
  >
    <PassportMeasurementInput
      :parameter="parameter"
      :isElisFilled="isElisFilled"
      @update:measurement="handleChange"
    />

    <!-- Индикатор истории -->
    <FieldHistoryIndicator
      v-if="lastSource !== DataSource.Unknown && lastSource !== DataSource.Auto"
      :source="lastSource"
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
import PassportMeasurementInput from './PassportMeasurementInput.vue';
import FieldHistoryIndicator from '@/components/history/FieldHistoryIndicator.vue';
import FieldHistoryPopup from '@/components/history/FieldHistoryPopup.vue';
import { useFieldHistory } from '@/composables/useFieldHistory';
import { DataSource } from '@/types/history.types';
import type { PassportQualityParameter } from '@/types/passport.types';
import { closeAllHistoryPopups } from '@/utils/historyPopupEvents';

const props = defineProps<{
  parameter: PassportQualityParameter;
}>();

const emit = defineEmits<{
  'update:measurement': [value: string];
}>();

const {
  getFieldHistory,
  getLastSource
} = useFieldHistory();

const historyPopup = ref<InstanceType<typeof FieldHistoryPopup>>();

/**
 * Ключ для истории: value.{parameterKey}
 */
const historyKey = computed(() => `value.${props.parameter.key}`);

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
 * Флаг ELIS-заполненности: определяется по последнему источнику в истории
 * ELIS и ReturnToELIS отображаются одинаково (зелёная подсветка)
 */
const isElisFilled = computed(() => {
  return lastSource.value === DataSource.ELIS || lastSource.value === DataSource.ReturnToELIS;
});

/**
 * Обработка изменения значения
 * История записывается централизованно в handleMeasurementUpdate (usePassportEditor)
 */
const handleChange = (newValue: string) => {
  // Передаем изменение дальше (история записывается в композабле)
  emit('update:measurement', newValue);
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
.measurement-with-history {
  position: relative;
  display: flex;
  align-items: center;
  gap: 2px;
  width: 100%;
  max-width: 100%;
  overflow: hidden;
}

</style>
