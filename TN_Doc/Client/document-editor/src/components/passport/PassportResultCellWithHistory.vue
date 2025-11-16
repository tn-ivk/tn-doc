<template>
  <div class="result-with-history">
    <PassportResultCell
      :parameter="parameter"
      :isEditable="isEditable"
      :isElisFilled="isElisFilled"
      @update:result="handleChange"
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
  'update:result': [value: string];
}>();

const {
  getFieldHistory,
  getLastSource,
  trackManualChange
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
