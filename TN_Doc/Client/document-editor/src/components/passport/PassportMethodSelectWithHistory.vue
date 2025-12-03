<template>
  <div
    class="method-with-history"
  >
    <PassportMethodSelect
      :parameter="parameter"
      :isElisFilled="isElisFilled"
      :hideEditButton="false"
      :hasHistoryIndicator="lastSource !== DataSource.Unknown && lastSource !== DataSource.Auto"
      @update:method="handleChange"
      @manual-method="handleManualMethod"
    />

    <!-- Индикатор истории -->
    <FieldHistoryIndicator
      v-if="lastSource !== DataSource.Unknown && lastSource !== DataSource.Auto"
      :source="lastSource"
      :rightOffset="4"
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
import { ref, computed, watch } from 'vue';
import PassportMethodSelect from './PassportMethodSelect.vue';
import FieldHistoryIndicator from '@/components/history/FieldHistoryIndicator.vue';
import FieldHistoryPopup from '@/components/history/FieldHistoryPopup.vue';
import { useFieldHistory } from '@/composables/useFieldHistory';
import { useDocumentStore } from '@/stores/documentStore';
import { DataSource } from '@/types/history.types';
import type { PassportQualityParameter, MethodOption } from '@/types/passport.types';
import { closeAllHistoryPopups } from '@/utils/historyPopupEvents';

const props = defineProps<{
  parameter: PassportQualityParameter;
}>();

const emit = defineEmits<{
  'update:method': [method: MethodOption | null];
  'manual-method': [];
}>();

const {
  getFieldHistory,
  getLastSource
} = useFieldHistory();

const store = useDocumentStore();

const historyPopup = ref<InstanceType<typeof FieldHistoryPopup>>();

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

// ДИАГНОСТИКА: Используем флаг из formData, как для measurement и result
const isElisFilled = computed(() => {
  const flag = store.formData[`${historyKey.value}__elisFilled`] === true;
  console.log(`[PassportMethodSelectWithHistory] ДИАГНОСТИКА для ${historyKey.value}:`, {
    flag,
    lastSource: lastSource.value,
    historyLength: fieldHistory.value.length,
    elisFilledFlagKey: `${historyKey.value}__elisFilled`,
    elisFilledFlagValue: store.formData[`${historyKey.value}__elisFilled`],
    elisOriginalKey: `${historyKey.value}__elisOriginal`,
    elisOriginalValue: store.formData[`${historyKey.value}__elisOriginal`],
    elisOptionKey: `${historyKey.value}__elisOption`,
    elisOptionValue: store.formData[`${historyKey.value}__elisOption`],
    currentMethodValue: store.formData[historyKey.value]
  });
  return flag;
});

// Отслеживаем изменения для диагностики
watch(isElisFilled, (newVal, oldVal) => {
  console.log(`[PassportMethodSelectWithHistory] isElisFilled изменился для ${historyKey.value}:`, {
    oldVal,
    newVal,
    lastSource: lastSource.value
  });
}, { immediate: true });

/**
 * Обработка изменения метода
 * История записывается централизованно в handleMethodUpdate (usePassportEditor)
 */
const handleChange = (newMethod: MethodOption | null) => {
  // Передаем изменение дальше (история записывается в композабле)
  emit('update:method', newMethod);
};

const handleManualMethod = () => {
  emit('manual-method');
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
.method-with-history {
  position: relative;
}

</style>
