<template>
  <Select
    :modelValue="parameter.method.selected"
    :options="methodOptions"
    optionLabel="label"
    optionValue="value"
    :class="{ 'elis-filled': parameter.elisFlags.method }"
    placeholder="Выберите метод"
    class="method-select"
    @update:modelValue="handleMethodChange"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import Select from 'primevue/select';
import type { PassportQualityParameter } from '@/types/passport.types';

interface Props {
  /** Параметр качества */
  parameter: PassportQualityParameter;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:method': [methodName: string];
}>();

/**
 * Опции для Select компонента
 */
const methodOptions = computed(() => {
  return props.parameter.method.options.map(option => ({
    label: option.name,
    value: option.name,
    ...option
  }));
});

/**
 * Обработчик изменения метода
 */
function handleMethodChange(methodName: string) {
  emit('update:method', methodName);
  console.log(`[PassportMethodSelect] Метод изменён: ${props.parameter.key} -> ${methodName}`);
}
</script>

<style scoped>
.method-select {
  width: 100%;
  font-size: 15px;
}

/* ELIS подсветка */
.elis-filled {
  background-color: #8fd19e !important;
}

.elis-filled:deep(.p-select-label) {
  background-color: #8fd19e !important;
}
</style>
