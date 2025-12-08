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
    />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import PassportMethodSelect from './PassportMethodSelect.vue';
import FieldHistoryIndicator from '@/components/history/FieldHistoryIndicator.vue';
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

const { getLastSource } = useFieldHistory();

/**
 * Ключ для истории: method.{parameterKey}
 */
const historyKey = computed(() => `method.${props.parameter.key}`);

/**
 * Последний источник изменений
 */
const lastSource = computed(() => {
  return getLastSource(historyKey.value);
});

/**
 * Флаг ELIS-заполненности: определяется по последнему источнику в истории
 * ELIS и ReturnToELIS отображаются одинаково (зелёная подсветка)
 */
const isElisFilled = computed(() => {
  return lastSource.value === DataSource.ELIS || lastSource.value === DataSource.ReturnToELIS;
});

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
</script>

<style scoped>
.method-with-history {
  position: relative;
}

</style>
