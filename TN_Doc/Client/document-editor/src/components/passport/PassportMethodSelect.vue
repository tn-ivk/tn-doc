<template>
  <Select
    :modelValue="parameter.method.selected || null"
    :options="methodOptions"
    optionLabel="label"
    optionValue="value"
    :class="{ 'elis-filled': parameter.elisFlags.method }"
    placeholder="Метод не выбран"
    showClear
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
  console.log('[PassportMethodSelect] Монтирование select метода для параметра:', props.parameter.key);
  console.log('[PassportMethodSelect] Выбранный метод:', props.parameter.method.selected);
  console.log('[PassportMethodSelect] Количество опций:', methodOptions.value.length);
});

/**
 * Опции для Select компонента
 * Только стандартная фильтрация пустых опций от бэкенда
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
function handleMethodChange(methodName: string | null) {
  // Если пользователь очистил значение (showClear), передаём пустую строку
  emit('update:method', methodName || '');
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
