<template>
  <div class="method-field">
    <div class="method-select-container">
      <div class="select-wrapper">
        <Select
          :modelValue="selectedMethodOption"
          :options="methodOptions"
          optionLabel="name"
          :class="[
            { 'p-invalid': !isValid },
            { 'elis-filled': isElisFilled },
            { 'unknown-method': showDictionaryWarning },
            'no-dropdown-icon',
            paddingClass
          ]"
          placeholder="Метод не выбран"
          class="method-select"
          panelClass="method-select-panel"
          @update:modelValue="handleMethodChange"
        />

        <!-- Контейнер для индикаторов -->
        <div v-if="$slots['indicators']" class="indicators-container">
          <slot name="indicators"></slot>
        </div>
      </div>

      <!-- Кнопка редактирования справа от комбобокса -->
      <button
        v-if="!hideEditButton"
        class="edit-method-btn"
        :class="{ 'edit-method-btn--elis': isElisFilled }"
        type="button"
        @click="handleEditClick"
        title="Редактирование..."
      >
        <i class="pi pi-pen-to-square"></i>
      </button>
    </div>

    <!-- Сообщение об ошибке валидации -->
    <small v-if="!isValid" class="p-error">
      {{ validationMessage }}
    </small>
    <small v-else-if="showDictionaryWarning" class="method-warning">
      отсутствует в справочнике
    </small>
  </div>
</template>

<script setup lang="ts">
import { computed, useSlots } from 'vue';
import Select from 'primevue/select';
import type { PassportQualityParameter, MethodOption } from '@/types/passport.types';

interface Props {
  /** Параметр качества */
  parameter: PassportQualityParameter;
  /** Используется ли подсветка ELIS */
  isElisFilled?: boolean;
  /** Скрыть кнопку редактирования */
  hideEditButton?: boolean;
}

const props = defineProps<Props>();
const slots = useSlots();

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

const showDictionaryWarning = computed(() => {
  if (!props.parameter.method.selected) {
    return false;
  }
  return props.parameter.method.isInDictionary === false;
});

/**
 * Динамический класс для padding текста
 * Если есть индикаторы - нужно больше места для них
 */
const paddingClass = computed(() => {
  return slots['indicators'] ? 'with-indicators' : '';
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

/* Контейнер для Select и кнопки (input group) */
.method-select-container {
  position: relative;
  display: flex;
  width: 100%;
}

/* Обёртка для Select с индикаторами */
.select-wrapper {
  position: relative;
  flex: 1;
  min-width: 0;
}

.method-select {
  width: 100%;
  font-size: 15px;
}

/* Контейнер для индикаторов */
.indicators-container {
  position: absolute;
  top: 0;
  right: 4px;
  z-index: 10;
  --history-indicator-top: -4px;

  display: flex;
  flex-direction: row-reverse; /* последний добавленный будет справа */
  align-items: center;
  gap: 4px; /* расстояние между индикаторами */
}

/* Переопределение стилей индикаторов внутри контейнера */
.indicators-container :deep(.field-history-indicator) {
  position: relative; /* оставляем в потоке для flex */
}

/* Кнопка редактирования справа от Select */
.edit-method-btn {
  width: 28px;
  height: 38px;
  border: 1px solid var(--md-outline) !important;
  border-left: none !important;
  background-color: transparent !important;
  color: var(--md-text, #212121) !important;
  font-size: 14px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 0 var(--md-radius) var(--md-radius) 0 !important;
  transition: background-color 0.15s ease-in-out, color 0.15s ease-in-out;
}

/* Кнопка для ELIS-заполненного поля */
.edit-method-btn--elis {
  background-color: transparent !important;
  border-color: var(--md-outline);
}

.edit-method-btn:hover {
  background-color: rgba(0, 0, 0, 0.04) !important;
  color: var(--md-primary, #2f6fed) !important;
}

.edit-method-btn--elis:hover {
  background-color: rgba(0, 0, 0, 0.04) !important;
}

.edit-method-btn:active {
  background-color: rgba(0, 0, 0, 0.08) !important;
  color: var(--md-primary-active, #1e54d4) !important;
}

/* Базовые стили для Select компонента */
:deep(.method-select.p-select) {
  width: 100%;
  border: 1px solid var(--md-outline);
  border-radius: var(--md-radius) 0 0 var(--md-radius) !important;
  background: #ffffff;
  transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
}

:deep(.method-select.p-select .p-select-label) {
  min-width: 0;
  overflow: hidden;
  white-space: normal;
  overflow-wrap: anywhere;
  word-break: break-word;
  line-height: 1.2;
  max-height: 2.4em;
}

:deep(.method-select.p-select:not(.p-disabled):hover) {
  border-color: var(--md-primary);
}

:deep(.method-select.p-select:not(.p-disabled).p-focus) {
  border-color: var(--md-primary);
  box-shadow: 0 0 0 1px rgba(33, 150, 243, 0.3);
}

/* Валидация - красная рамка при ошибке */
.method-select.p-invalid,
.method-select.p-invalid:deep(.p-select),
.method-select.p-invalid:deep(.p-select-label) {
  border-color: var(--md-error, #dc3545) !important;
  box-shadow: none !important;
}

.method-select.unknown-method,
.method-select.unknown-method:deep(.p-select),
.method-select.unknown-method:deep(.p-select-label) {
  border-color: #f5c24c !important;
  box-shadow: 0 0 0 1px rgba(245, 194, 76, 0.3) !important;
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

.method-warning {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.875rem;
  color: #b87902;
  line-height: 1.2;
}

/* Скрытие dropdown иконки для унификации с комбобоксом подписантов */
:deep(.no-dropdown-icon .p-select-dropdown) {
  display: none !important;
}

/* Динамический padding в зависимости от наличия индикаторов */
:deep(.with-indicators .p-select-label) {
  padding-right: 35px !important; /* Место для индикаторов */
}

</style>

<!-- Глобальные стили для выпадающего списка методов -->
<style>
.method-select-panel.p-select-overlay {
  margin-top: 2px;
  border: 1px solid var(--md-outline);
  border-radius: var(--md-radius);
  box-shadow: 0 6px 18px rgba(33, 33, 33, 0.12);
  background: #ffffff;
}

.method-select-panel .p-select-list {
  padding: 4px 0;
  font-size: var(--md-font-size-base);
}

.method-select-panel .p-select-option {
  padding: 6px 12px;
  color: var(--md-text);
  transition: background-color 0.15s ease-in-out, color 0.15s ease-in-out;
}

.method-select-panel .p-select-option:not(.p-disabled):hover {
  background: var(--md-primary-light);
  color: var(--md-text);
}

.method-select-panel .p-select-option.p-focus {
  background: var(--md-primary-light);
  color: var(--md-text);
}

.method-select-panel .p-select-option.p-focus:not(.p-disabled):hover {
  background: var(--md-primary);
  color: #ffffff;
}
</style>
