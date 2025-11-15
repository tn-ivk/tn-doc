<template>
  <div class="form-field-with-history">
    <FormField
      :field="field"
      :modelValue="modelValue"
      :hideLabel="hideLabel"
      :invalidChars="invalidChars"
      :highlightColor="highlightColor"
      @update:modelValue="handleChange"
    />

    <!-- Индикатор истории (поверх поля) -->
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
      :fieldLabel="field.label"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import FormField from '@/components/FormField.vue';
import FieldHistoryIndicator from '@/components/history/FieldHistoryIndicator.vue';
import FieldHistoryPopup from '@/components/history/FieldHistoryPopup.vue';
import { useFieldHistory } from '@/composables/useFieldHistory';
import { DataSource } from '@/types/history.types';
import type { FormField as FormFieldType } from '@/types/document.types';

const props = defineProps<{
  field: FormFieldType;
  modelValue: any;
  hideLabel?: boolean;
  invalidChars?: string[];
  highlightColor?: string;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: any): void;
}>();

const {
  getFieldHistory,
  getLastSource,
  trackManualChange
} = useFieldHistory();

const historyPopup = ref<InstanceType<typeof FieldHistoryPopup>>();
let hideTimeout: ReturnType<typeof setTimeout> | null = null;

/**
 * История поля
 */
const fieldHistory = computed(() => {
  return getFieldHistory(props.field.key);
});

/**
 * Последний источник изменений
 */
const lastSource = computed(() => {
  return getLastSource(props.field.key);
});

/**
 * Обработка изменения значения
 */
const handleChange = (newValue: any) => {
  // Отслеживаем ручное изменение
  trackManualChange(props.field.key, newValue, props.modelValue);

  // Передаем изменение дальше
  emit('update:modelValue', newValue);
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
.form-field-with-history {
  position: relative;
}
</style>
