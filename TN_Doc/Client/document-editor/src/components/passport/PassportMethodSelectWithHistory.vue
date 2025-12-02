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
import { ref, computed } from 'vue';
import PassportMethodSelect from './PassportMethodSelect.vue';
import FieldHistoryIndicator from '@/components/history/FieldHistoryIndicator.vue';
import FieldHistoryPopup from '@/components/history/FieldHistoryPopup.vue';
import { useFieldHistory } from '@/composables/useFieldHistory';
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

const isElisFilled = computed(() => lastSource.value === DataSource.ELIS);

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
