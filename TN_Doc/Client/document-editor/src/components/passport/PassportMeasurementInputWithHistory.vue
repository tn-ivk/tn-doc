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
      v-if="lastSource !== DataSource.Unknown"
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
  getLastSource,
  trackManualChange
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

const isElisFilled = computed(() => lastSource.value === DataSource.ELIS);

/**
 * Обработка изменения значения
 */
const handleChange = (newValue: string) => {
  // Если поле загружено из ЕЛИС - не трекать как ручное изменение
  // Это предотвращает ложные записи "Отредактировано вручную" при нормализации значений
  if (props.parameter.elisFlags.measurement) {
    // Просто передаём изменение дальше без записи в историю
    emit('update:measurement', newValue);
    return;
  }

  // Отслеживаем ручное изменение
  trackManualChange(historyKey.value, newValue, props.parameter.values.measurement);

  // Передаем изменение дальше
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
