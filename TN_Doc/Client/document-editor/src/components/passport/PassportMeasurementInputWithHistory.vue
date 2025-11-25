<template>
  <div
    class="measurement-with-history"
    :class="{ 'elis-missing-border': isElisMissing }"
    :title="isElisMissing ? 'Ожидалось из ЕЛИС' : undefined"
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
      @mouseenter="(event) => onIndicatorHover(event)"
      @mouseleave="onIndicatorLeave"
    />

    <!-- Popup с историей -->
    <FieldHistoryPopup
      ref="historyPopup"
      :history="fieldHistory"
      :fieldLabel="`${parameter.name} (Измерение)`"
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
let hideTimeout: ReturnType<typeof setTimeout> | null = null;

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
 * Проверка, ожидалось ли поле из ELIS, но не было загружено
 */
const isElisMissing = computed(() => lastSource.value === DataSource.ElisMissing);

/**
 * Обработка изменения значения
 */
const handleChange = (newValue: string) => {
  // Отслеживаем ручное изменение
  trackManualChange(historyKey.value, newValue, props.parameter.values.measurement);

  // Передаем изменение дальше
  emit('update:measurement', newValue);
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
.measurement-with-history {
  position: relative;
  display: flex;
  align-items: center;
  gap: 2px;
  width: 100%;
  max-width: 100%;
  overflow: hidden;
}

/* Желтая рамка для полей, ожидавшихся из ELIS, но не загруженных */
.measurement-with-history.elis-missing-border :deep(.p-inputtext) {
  border: 2px solid #f5c24c !important;
}
</style>
