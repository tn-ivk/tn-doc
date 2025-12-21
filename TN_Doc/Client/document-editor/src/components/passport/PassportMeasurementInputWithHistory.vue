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
    <div
      v-if="lastSource !== DataSource.Unknown && lastSource !== DataSource.Auto"
      class="indicators-container"
    >
      <FieldHistoryIndicator
        :source="lastSource"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import PassportMeasurementInput from './PassportMeasurementInput.vue';
import FieldHistoryIndicator from '@/components/history/FieldHistoryIndicator.vue';
import { useFieldHistory } from '@/composables/useFieldHistory';
import { DataSource } from '@/types/history.types';
import type { PassportQualityParameter } from '@/types/passport.types';

const props = defineProps<{
  parameter: PassportQualityParameter;
}>();

const emit = defineEmits<{
  'update:measurement': [value: string];
}>();

const { getLastSource } = useFieldHistory();

/**
 * Ключ для истории: value.{parameterKey}
 */
const historyKey = computed(() => `value.${props.parameter.key}`);

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
</script>

<style scoped>
.measurement-with-history {
  position: relative;
  display: flex;
  align-items: center;
  gap: 2px;
  width: 100%;
  max-width: 100%;
  overflow: visible;
}

.indicators-container {
  position: absolute;
  top: 0;
  right: 4px;
  --history-indicator-top: -4px;
  display: flex;
  flex-direction: row-reverse;
  gap: 4px;
}

.indicators-container :deep(.field-history-indicator) {
  position: relative;
}
</style>
