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
import { computed, onMounted } from 'vue';
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

onMounted(() => {
  // Validate method options on mount
  methodOptions.value.forEach((opt, idx) => {
    if (!opt.label || opt.value === undefined || opt.label.trim() === '') {
      console.error('[PassportMethodSelect] ОШИБКА: некорректная опция метода #' + idx + ':', opt);
    }
  });
});

/**
 * Опции для Select компонента
 */
const methodOptions = computed(() => {
  // Фильтруем опции с пустыми name (технические записи "не выбрано")
  return props.parameter.method.options
    .filter(option => option.name && option.name.trim() !== '')
    .map(option => ({
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
