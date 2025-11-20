<template>
  <div class="method-field">
    <div class="method-select-container">
      <Select
        :modelValue="selectedMethodOption"
        :options="methodOptions"
        optionLabel="name"
        :class="[
          { 'p-invalid': !isValid },
          { 'elis-filled': isElisFilled }
        ]"
        placeholder="Метод не выбран"
        class="method-select"
        @update:modelValue="handleMethodChange"
      />

      <!-- Иконка редактирования внутри комбобокса -->
      <button
        class="edit-method-btn"
        type="button"
        @click="handleEditClick"
        title="Создать ручной метод"
      >
        <i class="pi pi-pencil"></i>
      </button>
    </div>

    <!-- Сообщение об ошибке валидации -->
    <small v-if="!isValid" class="p-error">
      {{ validationMessage }}
    </small>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import Select from 'primevue/select';
import type { PassportQualityParameter, MethodOption } from '@/types/passport.types';

interface Props {
  /** Параметр качества */
  parameter: PassportQualityParameter;
  /** Используется ли подсветка ELIS */
  isElisFilled?: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:method': [method: MethodOption | null];
  'manual-method': [];
}>();

/**
 * Опции для Select компонента
 * Только стандартная фильтрация пустых опций от бэкенда
 */
const methodOptions = computed(() => {
  return props.parameter.method.options.filter(option => option.name && option.name.trim() !== '');
});

const selectedMethodOption = computed(() => {
  if (!props.parameter.method.selected) return null;
  return methodOptions.value.find(option => option.name === props.parameter.method.selected) || null;
});

// Валидация поля метода
const isValid = computed(() => {
  // Если метод испытаний обязателен для заполнения, проверяем что он выбран
  if (props.parameter.method.requiredFill) {
    return !!props.parameter.method.selected && props.parameter.method.selected.trim() !== '';
  }
  return true;
});

// Сообщение об ошибке валидации
const validationMessage = computed(() => {
  if (!isValid.value) {
    return `Необходимо выбрать метод испытаний`;
  }
  return '';
});

/**
 * Обработчик изменения метода
 */
function handleMethodChange(method: MethodOption | null) {
  emit('update:method', method);
}

/**
 * Обработчик клика на иконку редактирования
 */
function handleEditClick() {
  emit('manual-method');
}
</script>

<style scoped>
.method-field {
  width: 100%;
}

/* Контейнер для Select и иконки */
.method-select-container {
  position: relative;
  width: 100%;
}

.method-select {
  width: 100%;
  font-size: 15px;
}

/* Иконка редактирования внутри Select */
.edit-method-btn {
  position: absolute;
  right: 34px; /* Вплотную к dropdown */
  top: 50%;
  transform: translateY(-50%);
  width: 28px;
  height: 28px;
  border: 1px solid transparent !important;
  background-color: transparent !important;
  color: var(--md-text, #212121) !important;
  font-size: 14px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  z-index: 1;
}

.edit-method-btn:hover {
  background-color: transparent !important;
  color: var(--md-text, #212121) !important;
}

.edit-method-btn:active {
  background-color: transparent !important;
  color: var(--md-text, #212121) !important;
}

:deep(.method-field .p-select) {
  width: 100%;
}

/* Валидация - красная рамка при ошибке */
.method-select.p-invalid,
.method-select.p-invalid:deep(.p-select),
.method-select.p-invalid:deep(.p-select-label) {
  border-color: var(--md-error, #dc3545) !important;
  box-shadow: none !important;
}

/* ELIS подсветка */
:deep(.p-select.elis-filled) {
  background-color: #8fd19e !important;
}

:deep(.p-select.elis-filled .p-select-dropdown-icon),
:deep(.p-select.elis-filled .p-select-clear-icon) {
  color: var(--md-text, #212121) !important;
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
