<template>
  <div
    class="form-field-with-history"
    :class="paddingClass"
  >
    <FormField
      :field="field"
      :modelValue="modelValue"
      :hideLabel="hideLabel"
      :invalidChars="invalidChars"
      :highlightColor="computedHighlightColor"
      :hideDropdownIcon="hideDropdownIcon"
      @update:modelValue="handleChange"
    />

    <!-- Контейнер для индикаторов -->
    <div v-if="hasIndicatorsSlot || showHistoryIndicator || showElisMissing" class="indicators-container">
      <slot name="indicators">
        <FieldHistoryIndicator
          v-if="showHistoryIndicator"
          :source="lastSource"
          :rightOffset="0"
        />
        <FieldHistoryIndicator
          v-if="showElisMissing"
          :source="DataSource.ElisMissing"
          :rightOffset="0"
        />
      </slot>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, useSlots } from 'vue';
import FormField from '@/components/FormField.vue';
import FieldHistoryIndicator from '@/components/history/FieldHistoryIndicator.vue';
import { useFieldHistory } from '@/composables/useFieldHistory';
import { useDocumentStore } from '@/stores/documentStore';
import { DataSource } from '@/types/history.types';
import type { FormField as FormFieldType } from '@/types/document.types';
import { normalizeValue, normalizeDateTimeForComparison } from '@/utils/passport-utils';

const props = defineProps<{
  field: FormFieldType;
  modelValue: any;
  hideLabel?: boolean;
  invalidChars?: string[];
  highlightColor?: string;
  hideDropdownIcon?: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: any): void;
}>();

const store = useDocumentStore();
const slots = useSlots();

const {
  getLastSource,
  trackManualChange,
  trackReturnToElis,
  hasElisMissing
} = useFieldHistory();

const ELIS_HIGHLIGHT_COLOR = 'var(--md-elis-highlight, #e8f5e9)';

/**
 * Последний источник изменений
 */
const lastSource = computed(() => {
  return getLastSource(props.field.key);
});

const showHistoryIndicator = computed(() => {
  return lastSource.value !== DataSource.Unknown && lastSource.value !== DataSource.Auto;
});

const showElisMissing = computed(() => {
  return hasElisMissing(props.field.key);
});

const hasIndicatorsSlot = computed(() => !!slots['indicators']);

/**
 * Динамический класс для padding текста
 * Если есть индикаторы - нужно больше места для них
 */
const paddingClass = computed(() => {
  return (hasIndicatorsSlot.value || showHistoryIndicator.value || showElisMissing.value)
    ? 'with-indicators'
    : '';
});

const computedHighlightColor = computed(() => {
  if (props.highlightColor) {
    return props.highlightColor;
  }

  // Подсвечиваем зелёным если последний источник ELIS или ReturnToELIS
  const source = lastSource.value;
  return (source === DataSource.ELIS || source === DataSource.ReturnToELIS)
    ? ELIS_HIGHLIGHT_COLOR
    : undefined;
});

/**
 * Обработка изменения значения
 * Проверяет возврат к оригинальному значению ELIS и записывает соответствующий тип в историю
 *
 * ВАЖНО: Для полей datetime-local и date использует специальную нормализацию по timestamp,
 * которая корректно сравнивает UTC и local time без конвертации исходных данных из ELIS
 */
const handleChange = (newValue: any) => {
  const fieldKey = props.field.key;
  const previousValue = props.modelValue;

  // Проверяем, есть ли оригинальное значение ELIS для этого поля
  const elisOriginal = store.formData[`${fieldKey}__elisOriginal`];

  if (elisOriginal !== undefined) {
    // Поле было заполнено из ELIS - проверяем возврат к оригиналу
    let normalizedNew: string;
    let normalizedOriginal: string;

    // Для полей даты/времени используем специальную нормализацию по timestamp
    // Это позволяет корректно сравнивать "2025-12-02T00:00:00Z" (UTC) и "2025-12-02T03:00:00" (local)
    if (props.field.type === 'datetime-local' || props.field.type === 'date') {
      normalizedNew = normalizeDateTimeForComparison(newValue);
      normalizedOriginal = normalizeDateTimeForComparison(elisOriginal);
    } else {
      // Для остальных полей используем стандартную нормализацию
      normalizedNew = normalizeValue(newValue);
      normalizedOriginal = normalizeValue(elisOriginal);
    }

    const isReturnToElis = normalizedNew === normalizedOriginal;

    if (isReturnToElis) {
      // Возврат к значению ELIS - записываем ReturnToELIS в историю
      trackReturnToElis(fieldKey, newValue, previousValue);
    } else {
      // Ручное изменение
      trackManualChange(fieldKey, newValue, previousValue);
    }
  } else {
    // Обычное поле без ELIS - ручное изменение
    trackManualChange(fieldKey, newValue, previousValue);
  }

  // Передаем изменение дальше (store.updateField обновит __elisFilled)
  emit('update:modelValue', newValue);
};
</script>

<style scoped>
.form-field-with-history {
  position: relative;
  overflow: visible;
}

/* Контейнер для индикаторов */
.indicators-container {
  position: absolute;
  top: 0;
  right: 4px;
  z-index: 10;
  --history-indicator-top: -4px;

  display: flex;
  flex-direction: row-reverse; /* последний добавленный будет справа */
  align-items: center;
  gap: 4px; /* расстояние между индикаторами */
}

/* Переопределение стилей индикаторов внутри контейнера */
.indicators-container :deep(.field-history-indicator) {
  position: relative; /* оставляем в потоке для flex */
}

/* Динамический padding в зависимости от наличия индикаторов */
.form-field-with-history.with-indicators :deep(.p-select-label) {
  padding-right: 35px !important; /* Место для индикаторов */
}

</style>
