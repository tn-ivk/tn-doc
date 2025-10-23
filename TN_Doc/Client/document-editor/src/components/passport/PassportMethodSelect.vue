<template>
  <div class="method-field">
    <Select
      :modelValue="parameter.method.selected || null"
      :options="methodOptions"
      optionLabel="label"
      optionValue="value"
      :class="[
        { 'p-invalid': !isValid },
        { 'elis-filled': parameter.elisFlags.method }
      ]"
      placeholder="Метод не выбран"
      showClear
      class="method-select"
      @update:modelValue="handleMethodChange"
    />

    <!-- Сообщение об ошибке валидации -->
    <small v-if="!isValid" class="p-error">
      {{ validationMessage }}
    </small>
  </div>
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

// Валидация поля метода
const isValid = computed(() => {
  // Если параметр обязателен для заполнения, метод тоже должен быть выбран
  if (props.parameter.requiredFill) {
    return !!props.parameter.method.selected && props.parameter.method.selected.trim() !== '';
  }
  return true;
});

// Сообщение об ошибке валидации
const validationMessage = computed(() => {
  if (!isValid.value) {
    return `Необходимо выбрать метод испытаний для "${props.parameter.name}"`;
  }
  return '';
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
.method-field {
  width: 100%;
}

.method-select {
  width: 100%;
  font-size: 15px;
}

/* Валидация - красная рамка при ошибке */
.method-select.p-invalid,
.method-select.p-invalid:deep(.p-select),
.method-select.p-invalid:deep(.p-select-label) {
  border-color: var(--md-error, #dc3545) !important;
  box-shadow: none !important;
}

/* ELIS подсветка */
.elis-filled {
  background-color: #8fd19e !important;
}

.elis-filled:deep(.p-select-label) {
  background-color: #8fd19e !important;
}

/* Сообщение об ошибке */
.p-error {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.875rem;
  color: var(--md-error, #dc3545);
  line-height: 1.2;
}
</style>
