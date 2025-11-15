<template>
  <div class="method-with-history">
    <PassportMethodSelect
      :parameter="parameter"
      @update:method="handleChange"
    />

    <!-- Индикатор истории -->
    <FieldHistoryIndicator
      v-if="lastSource !== DataSource.Unknown"
      :source="lastSource"
      @mouseenter="onIndicatorHover"
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

/**
 * Обработчик наведения на индикатор
 */
const onIndicatorHover = () => {
  // Отменяем таймер скрытия, если он был запущен
  if (hideTimeout) {
    clearTimeout(hideTimeout);
    hideTimeout = null;
  }

  // Показываем popup (без параметра event, OverlayPanel определит позицию автоматически)
  historyPopup.value?.show(undefined as any);
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
</style>
