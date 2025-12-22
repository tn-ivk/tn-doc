<template>
  <div class="signer-group">
    <!-- ИОФ (ComboBox с кнопкой редактирования) -->
    <div class="signer-field signer-iof">
      <div class="iof-input-group">
        <div class="select-wrapper">
          <FormFieldWithHistory
            :field="iof"
            :modelValue="iofValue"
            :hide-label="true"
            :invalidChars="invalidChars"
            :hideDropdownIcon="true"
            @update:modelValue="handleIofChange"
          >
            <template #indicators>
              <FieldHistoryIndicator
                v-if="showHistoryIndicator"
                :source="lastSource"
                :rightOffset="0"
              />
              <FieldHistoryIndicator
                v-if="showElisMissing"
                :source="DataSource.ElisMissing"
                :rightOffset="0"
              />
            </template>
          </FormFieldWithHistory>
        </div>

        <!-- Кнопка редактирования справа от комбобокса -->
        <button
          class="edit-signer-btn"
          type="button"
          @click="handleEditClick"
          title="Ручной ввод..."
        >
          <i class="pi pi-pen-to-square"></i>
        </button>
      </div>
    </div>

    <!-- Должность (TextInput) -->
    <div class="signer-field signer-post">
      <FormFieldWithHistory
        :field="postField"
        :modelValue="postValue"
        :hide-label="true"
        :invalidChars="invalidChars"
        @update:modelValue="(value) => emit('update:post', value)"
      />
    </div>

    <!-- Предприятие (TextInput) -->
    <div class="signer-field signer-factory">
      <FormFieldWithHistory
        :field="factoryField"
        :modelValue="factoryValue"
        :hide-label="true"
        :invalidChars="invalidChars"
        @update:modelValue="(value) => emit('update:factory', value)"
      />
    </div>

    <!-- Диалог ручного ввода ФИО -->
    <ManualSignerDialog
      v-model:visible="isDialogVisible"
      :fieldLabel="iof.label"
      :currentName="currentSignerName"
      :invalidChars="invalidChars"
      @confirm="handleManualSignerConfirm"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import FormFieldWithHistory from './FormFieldWithHistory.vue';
import ManualSignerDialog from './ManualSignerDialog.vue';
import type { ManualSignerPayload } from './ManualSignerDialog.vue';
import FieldHistoryIndicator from '@/components/history/FieldHistoryIndicator.vue';
import type { FormField, SelectOption } from '@/types/document.types';
import { useFieldHistory } from '@/composables/useFieldHistory';
import { DataSource } from '@/types/history.types';

const props = defineProps<{
  iof: FormField;
  post: FormField;
  factory: FormField;
  iofValue: any;
  postValue: any;
  factoryValue: any;
  invalidChars?: string[];
}>();

const emit = defineEmits<{
  (e: 'update:iof', value: any): void;
  /** Событие для передачи label (ФИО) для сохранения в БД */
  (e: 'update:iof-label', label: string): void;
  (e: 'update:post', value: any): void;
  (e: 'update:factory', value: any): void;
  /** Событие для добавления новой опции в список пользователей */
  (e: 'add-manual-signer', payload: { name: string; optionId: string }): void;
}>();

// Состояние диалога
const isDialogVisible = ref(false);

const { getLastSource, trackManualChange, hasElisMissing } = useFieldHistory();

/**
 * Последний источник изменений для поля IOF
 */
const lastSource = computed(() => {
  return getLastSource(props.iof.key);
});

/**
 * Показать индикатор истории
 */
const showHistoryIndicator = computed(() => {
  const source = lastSource.value;
  return source !== DataSource.Unknown && source !== DataSource.Auto;
});

const showElisMissing = computed(() => {
  return hasElisMissing(props.iof.key);
});

/**
 * Текущее ФИО для предзаполнения диалога
 * Ищем label выбранной опции по value
 */
const currentSignerName = computed(() => {
  if (!props.iofValue || !props.iof.options) {
    return '';
  }

  const selectedOption = props.iof.options.find(opt => opt.value === props.iofValue);
  return selectedOption?.label || '';
});

// Создаем поля с placeholder для должности и предприятия
const postField: FormField = {
  ...props.post,
  label: 'Должность' // Placeholder будет отображаться в пустом поле
};

const factoryField: FormField = {
  ...props.factory,
  label: 'Предприятие' // Placeholder будет отображаться в пустом поле
};

/**
 * Обработчик клика на кнопку редактирования
 */
function handleEditClick() {
  isDialogVisible.value = true;
}

/**
 * Обработчик изменения IOF через комбобокс (выбор из списка)
 * Эмитит как value (ID), так и label (ФИО) для корректного сохранения в БД
 */
function handleIofChange(value: any) {
  // Ищем label выбранной опции
  const selectedOption = props.iof.options?.find(opt => opt.value === value);
  const label = selectedOption?.label || '';

  emit('update:iof', value);

  // Передаём label для сохранения в БД
  emit('update:iof-label', label);
}

/**
 * Обработчик подтверждения ручного ввода ФИО
 * Проверяет наличие дубликата, создаёт новую опцию и выбирает её
 */
function handleManualSignerConfirm(payload: ManualSignerPayload) {
  const name = payload.name.trim();
  if (!name) {
    return;
  }

  // Сохраняем текущее значение для истории
  const previousValue = props.iofValue;

  // Проверяем, существует ли уже такой пользователь в списке
  const existingOption = props.iof.options?.find(
    opt => opt.label.toLowerCase() === name.toLowerCase()
  );

  let newValue: string;

  if (existingOption) {
    // Пользователь уже есть в списке - просто выбираем его
    newValue = existingOption.value;
  } else {
    // Создаём новую опцию с уникальным ID
    // ВАЖНО: Используем префикс 'manual_' чтобы ID точно не совпал с существующим
    // пользователем из ДРУГОЙ группы (ResolveSignerIoF ищет по всем группам)
    newValue = `manual_${Date.now()}`;

    // Создаём новую опцию
    const newOption: SelectOption = {
      value: newValue,
      label: name,
      selected: false,
      data: undefined // Нет дополнительных данных для ручного ввода
    };

    // Добавляем опцию в список (мутация props.iof.options - допустима, т.к. это объект)
    if (props.iof.options) {
      props.iof.options.push(newOption);
    }

    // Эмитим событие для родительского компонента (для возможной синхронизации)
    emit('add-manual-signer', { name, optionId: newValue });
  }

  // Записываем ручное изменение в историю (ввод через диалог - это Manual)
  trackManualChange(props.iof.key, newValue, previousValue);

  // Выбираем опцию
  emit('update:iof', newValue);

  // Передаём label (ФИО) для сохранения в БД
  // Бэкенд использует {key}__label для разрешения ФИО, когда ID не найден в справочнике
  emit('update:iof-label', name);

  // Закрываем диалог
  isDialogVisible.value = false;
}
</script>

<style scoped>
.signer-group {
  display: flex;
  gap: 0.5rem;
  align-items: center;
  width: 100%;
}

.signer-field {
  flex: 1;
  min-width: 0; /* Для корректного flex shrink */
}

.signer-iof {
  flex: 2; /* ИОФ занимает больше места */
}

.signer-post {
  flex: 1.5; /* Должность чуть больше предприятия */
}

.signer-factory {
  flex: 1.5; /* Предприятие */
}

/* Контейнер для Select и кнопки (input group) */
.iof-input-group {
  position: relative;
  display: flex;
  width: 100%;
}

/* Обёртка для Select с индикаторами */
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
.signer-iof :deep(.form-field-with-history .p-select) {
  border-radius: var(--md-radius) 0 0 var(--md-radius) !important;
}
</style>
