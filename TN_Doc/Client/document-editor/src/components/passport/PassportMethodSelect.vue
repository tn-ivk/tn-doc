<template>
  <Select
    :modelValue="parameter.method.selected"
    :options="methodOptions"
    optionLabel="label"
    optionValue="value"
    :class="{ 'elis-filled': parameter.elisFlags.method }"
    placeholder="Выберите метод"
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
  console.log('[PassportMethodSelect] Количество опций (включая "Не выбрано"):', methodOptions.value.length);
});

/**
 * Опции для Select компонента
 */
const methodOptions = computed(() => {
  // Фильтруем опции с пустыми name (технические записи "не выбрано")
  const validOptions = props.parameter.method.options
    .filter(option => option.name && option.name.trim() !== '')
    .map(option => ({
      label: option.name,
      value: option.name,
      ...option
    }));

  // Добавляем явную пустую опцию в начало списка
  return [
    {
      label: '(Не выбрано)',
      value: '',
      isDefault: false,
      limitValueActivate: false
    },
    ...validOptions
  ];
});

/**
 * Обработчик изменения метода
 */
function handleMethodChange(methodName: string | null) {
  // Если пользователь очистил значение (showClear) или выбрал "(Не выбрано)"
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
