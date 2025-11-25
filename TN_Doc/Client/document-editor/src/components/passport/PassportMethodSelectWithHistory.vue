<template>
  <div
    class="method-with-history"
    :class="{ 'elis-missing-border': isElisMissing }"
    :title="isElisMissing ? 'Ожидалось из ЕЛИС' : undefined"
  >
    <PassportMethodSelect
      :parameter="parameter"
      :isElisFilled="isElisFilled"
      :hideEditButton="false"
      :hasHistoryIndicator="lastSource !== DataSource.Unknown"
      @update:method="handleChange"
      @manual-method="handleManualMethod"
    />

    <!-- Индикатор истории -->
    <FieldHistoryIndicator
      v-if="lastSource !== DataSource.Unknown"
      :source="lastSource"
      :rightOffset="4"
      @mouseenter="(event) => onIndicatorHover(event)"
      @mouseleave="onIndicatorLeave"
    />

    <!-- Popup с историей -->
    <FieldHistoryPopup
      ref="historyPopup"
      :history="fieldHistory"
      :fieldLabel="`${parameter.name} (Метод испытаний)`"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import PassportMethodSelect from './PassportMethodSelect.vue';
import FieldHistoryIndicator from '@/components/history/FieldHistoryIndicator.vue';
import FieldHistoryPopup from '@/components/history/FieldHistoryPopup.vue';
import { useFieldHistory } from '@/composables/useFieldHistory';
import { DataSource } from '@/types/history.types';
import type { PassportQualityParameter, MethodOption } from '@/types/passport.types';

const props = defineProps<{
  parameter: PassportQualityParameter;
}>();

const emit = defineEmits<{
  'update:method': [method: MethodOption | null];
  'manual-method': [];
}>();

const {
  getFieldHistory,
  getLastSource,
  trackManualChange
} = useFieldHistory();

const historyPopup = ref<InstanceType<typeof FieldHistoryPopup>>();
let hideTimeout: ReturnType<typeof setTimeout> | null = null;

/**
 * Ключ для истории: method.{parameterKey}
 */
const historyKey = computed(() => `method.${props.parameter.key}`);

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
 * Обработка изменения метода
 */
const handleChange = (newMethod: MethodOption | null) => {
  // Отслеживаем ручное изменение
  const newValue = newMethod?.name || '';
  const previousValue = props.parameter.method.selected || '';
  trackManualChange(historyKey.value, newValue, previousValue);

  // Передаем изменение дальше
  emit('update:method', newMethod);
};

const handleManualMethod = () => {
  emit('manual-method');
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
.method-with-history {
  position: relative;
}

/* Желтая рамка для полей, ожидавшихся из ELIS, но не загруженных */
.method-with-history.elis-missing-border :deep(.p-select) {
  border: 2px solid #f5c24c !important;
}
</style>
