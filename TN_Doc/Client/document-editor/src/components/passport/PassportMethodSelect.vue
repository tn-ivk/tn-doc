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
  console.log('[PassportMethodSelect] Монтирование select метода для параметра:', props.parameter.key);
  console.log('[PassportMethodSelect] Выбранный метод:', props.parameter.method.selected);
  console.log('[PassportMethodSelect] Доступные методы:', props.parameter.method.options);
  console.log('[PassportMethodSelect] methodOptions:', methodOptions.value);
});

/**
 * Опции для Select компонента
 */
const methodOptions = computed(() => {
  const options = props.parameter.method.options.map(option => ({
    label: option.name,
    value: option.name,
    ...option
  }));

  console.log('[PassportMethodSelect] Computed methodOptions для', props.parameter.key + ':', options);

  // Проверка валидности опций
  options.forEach((opt, idx) => {
    if (!opt.label || opt.value === undefined) {
      console.error('[PassportMethodSelect] ОШИБКА: некорректная опция метода #' + idx + ':', opt);
    }
  });

  return options;
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
