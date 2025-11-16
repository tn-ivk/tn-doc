<template>
  <div class="form-field-with-history">
    <FormField
      :field="field"
      :modelValue="modelValue"
      :hideLabel="hideLabel"
                  :invalidChars="invalidChars"
                  :highlightColor="computedHighlightColor"
      @update:modelValue="handleChange"
    />

    <!-- Индикатор истории (поверх поля) -->
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
import { logger } from '@tn-doc/shared';

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

const ELIS_HIGHLIGHT_COLOR = 'var(--md-elis-highlight, #e8f5e9)';

const historyPopup = ref<InstanceType<typeof FieldHistoryPopup>>();
let hideTimeout: ReturnType<typeof setTimeout> | null = null;

/**
 * История поля
 */
const fieldHistory = computed(() => {
  const history = getFieldHistory(props.field.key);
  logger.debug(`[FormFieldWithHistory] История для поля "${props.field.key}": ${JSON.stringify(history)}`);
  return history;
});

/**
 * Последний источник изменений
 */
const lastSource = computed(() => {
  const source = getLastSource(props.field.key);
  logger.debug(`[FormFieldWithHistory] Последний источник для поля "${props.field.key}": ${source}`);
  return source;
});

const computedHighlightColor = computed(() => {
  if (props.highlightColor) {
    return props.highlightColor;
  }

  return lastSource.value === DataSource.ELIS ? ELIS_HIGHLIGHT_COLOR : undefined;
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
const onIndicatorHover = (event: MouseEvent) => {
  logger.debug(`[FormFieldWithHistory] onIndicatorHover - поле "${props.field.key}"`);
  logger.debug(`[FormFieldWithHistory] История: ${JSON.stringify(fieldHistory.value)}`);
  logger.debug(`[FormFieldWithHistory] historyPopup.value: ${historyPopup.value ? 'определен' : 'undefined'}`);
  logger.debug(`[FormFieldWithHistory] event: ${event ? 'передан' : 'undefined'}`);

  // Отменяем таймер скрытия, если он был запущен
  if (hideTimeout) {
    logger.debug('[FormFieldWithHistory] Отменён таймер скрытия');
    clearTimeout(hideTimeout);
    hideTimeout = null;
  }

  // Показываем popup, передавая событие для правильного позиционирования
  logger.debug('[FormFieldWithHistory] Вызов historyPopup.show()');
  historyPopup.value?.show(event);
};

/**
 * Обработчик ухода курсора с индикатора
 */
const onIndicatorLeave = () => {
  logger.debug(`[FormFieldWithHistory] onIndicatorLeave - поле "${props.field.key}"`);

  // Запускаем таймер скрытия с задержкой 300ms
  hideTimeout = setTimeout(() => {
    logger.debug('[FormFieldWithHistory] Таймер скрытия истёк, скрываем popup');
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
