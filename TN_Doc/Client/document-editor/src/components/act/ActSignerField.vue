<template>
  <div class="act-signer-field">
    <div class="signer-input-group">
      <div class="select-wrapper">
        <FormField
          :field="field"
          :modelValue="modelValue"
          :hide-label="true"
          :invalidChars="invalidChars"
          :hideDropdownIcon="true"
          @update:modelValue="handleSelectChange"
        />
      </div>

      <!-- Кнопка редактирования справа от комбобокса -->
      <button
        v-if="allowManualInput"
        class="edit-signer-btn"
        type="button"
        @click="openDialog"
        title="Ручной ввод..."
      >
        <i class="pi pi-pen-to-square"></i>
      </button>
    </div>

    <ActManualSignerDialog
      v-model:visible="isDialogVisible"
      :fieldLabel="field.label"
      :currentName="currentSignerName"
      :invalidChars="invalidChars"
      @confirm="handleManualConfirm"
    />
  </div>
</template>

<script setup lang="ts">
/**
 * Компонент поля IOF для Act с кнопкой ручного ввода
 *
 * В отличие от SignerFieldGroup (Passport), этот компонент:
 * - Содержит только поле IOF (без Post и Factory в одной группе)
 * - Не использует индикаторы истории (FormField вместо FormFieldWithHistory)
 * - Использует ActManualSignerDialog для ручного ввода
 */
import { ref, computed } from 'vue';
import FormField from '@/components/FormField.vue';
import ActManualSignerDialog from './ActManualSignerDialog.vue';
import type { ActManualSignerPayload } from './ActManualSignerDialog.vue';
import type { FormField as FormFieldType, SelectOption } from '@/types/document.types';

interface Props {
  field: FormFieldType;
  modelValue: any;
  invalidChars?: string[];
  allowManualInput?: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  /** Обновление значения (ID пользователя или manual_*) */
  (e: 'update:modelValue', value: string): void;
  /** Обновление label (ФИО для сохранения в БД) */
  (e: 'update:label', label: string): void;
}>();

// Состояние диалога
const isDialogVisible = ref(false);

/**
 * Текущее ФИО для предзаполнения диалога
 * Ищем label выбранной опции по value
 */
const currentSignerName = computed(() => {
  if (!props.modelValue || !props.field.options) {
    return '';
  }

  const selectedOption = props.field.options.find(opt => opt.value === props.modelValue);
  return selectedOption?.label || '';
});

/**
 * Открытие диалога ручного ввода
 */
function openDialog() {
  isDialogVisible.value = true;
}

/**
 * Обработчик выбора из списка (ComboBox)
 * Эмитит как value (ID), так и label (ФИО) для корректного сохранения в БД
 */
function handleSelectChange(value: string) {
  const selectedOption = props.field.options?.find(opt => opt.value === value);
  const label = selectedOption?.label || '';

  emit('update:modelValue', value);
  emit('update:label', label);
}

/**
 * Обработчик подтверждения ручного ввода ФИО
 * Проверяет наличие дубликата, создаёт новую опцию при необходимости
 */
function handleManualConfirm(payload: ActManualSignerPayload) {
  const name = payload.name.trim();
  if (!name) {
    return;
  }

  // Проверяем, существует ли уже такой пользователь в списке
  const existingOption = props.field.options?.find(
    opt => opt.label.toLowerCase() === name.toLowerCase()
  );

  if (existingOption) {
    // Пользователь уже есть в списке - выбираем его
    emit('update:modelValue', existingOption.value);
    emit('update:label', existingOption.label);
  } else {
    // Создаём новую опцию с уникальным ID
    // Префикс 'manual_' используется для пропуска автозаполнения в useActAutoFill
    const newId = `manual_${Date.now()}`;

    // Создаём новую опцию
    const newOption: SelectOption = {
      value: newId,
      label: name,
      selected: false,
      data: undefined // Нет дополнительных данных для ручного ввода
    };

    // Добавляем опцию в список (мутация props.field.options допустима, т.к. это объект)
    if (props.field.options) {
      props.field.options.push(newOption);
    }

    emit('update:modelValue', newId);
    emit('update:label', name);
  }

  // Закрываем диалог
  isDialogVisible.value = false;
}
</script>

<style scoped>
.act-signer-field {
  width: 100%;
}

/* Контейнер для Select и кнопки (input group) */
.signer-input-group {
  position: relative;
  display: flex;
  width: 100%;
}

/* Обёртка для Select */
.select-wrapper {
  position: relative;
  flex: 1;
}

/* Кнопка редактирования справа от Select */
.edit-signer-btn {
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

.edit-signer-btn:hover {
  background-color: rgba(0, 0, 0, 0.04) !important;
  color: var(--md-primary, #2f6fed) !important;
}

.edit-signer-btn:active {
  background-color: rgba(0, 0, 0, 0.08) !important;
  color: var(--md-primary-active, #1e54d4) !important;
}

/* Скругления Select для input-group */
.act-signer-field :deep(.form-field .p-select) {
  border-radius: var(--md-radius) 0 0 var(--md-radius) !important;
}
</style>
