<template>
  <div
    class="form-field-with-history"
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

    <!-- Индикатор истории (поверх поля) -->
    <FieldHistoryIndicator
      v-if="lastSource !== DataSource.Unknown"
      :source="lastSource"
      @click="onIndicatorClick"
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
import { useDocumentStore } from '@/stores/documentStore';
import { DataSource } from '@/types/history.types';
import type { FormField as FormFieldType } from '@/types/document.types';
import { closeAllHistoryPopups } from '@/utils/historyPopupEvents';
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

const {
  getFieldHistory,
  getLastSource,
  trackManualChange,
  trackReturnToElis
} = useFieldHistory();

const ELIS_HIGHLIGHT_COLOR = 'var(--md-elis-highlight, #e8f5e9)';

const historyPopup = ref<InstanceType<typeof FieldHistoryPopup>>();

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
.form-field-with-history {
  position: relative;
}

</style>
