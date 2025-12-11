<template>
  <div class="signer-group">
    <!-- ИОФ (ComboBox с кнопкой редактирования) -->
    <div class="signer-field signer-iof">
      <div class="iof-select-container" :class="iofPaddingClass">
        <FormFieldWithHistory
          :field="iof"
          :modelValue="iofValue"
          :hide-label="true"
          :invalidChars="invalidChars"
          :hideDropdownIcon="true"
          :historyIndicatorOffset="4"
          @update:modelValue="handleIofChange"
        />

        <!-- Кнопка редактирования внутри комбобокса -->
        <button
          class="edit-signer-btn"
          type="button"
          :style="{ right: editButtonPosition }"
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

const { getLastSource, trackManualChange } = useFieldHistory();

/**
 * Последний источник изменений для поля IOF
 */
const lastSource = computed(() => {
  return getLastSource(props.iof.key);
});

/**
 * Есть ли индикатор истории (для расчёта позиции кнопки редактирования)
 */
const hasHistoryIndicator = computed(() => {
  const source = lastSource.value;
  return source !== DataSource.Unknown && source !== DataSource.Auto;
});

/**
 * Динамическая позиция кнопки редактирования
 * Если есть индикатор истории - сдвигаем левее (30px)
 * Если нет - прижимаем к правому краю (2px)
 */
const editButtonPosition = computed(() => {
  return hasHistoryIndicator.value ? '30px' : '2px';
});

/**
 * Динамический класс для padding текста в Select
 */
const iofPaddingClass = computed(() => {
  return hasHistoryIndicator.value ? 'iof-with-two-icons' : 'iof-with-one-icon';
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
  emit('update:iof', value);

  // Ищем label выбранной опции
  const selectedOption = props.iof.options?.find(opt => opt.value === value);
  const label = selectedOption?.label || '';

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
    // Создаём новую опцию
    // Генерируем уникальный ID (максимальный + 1 или 'manual_' + timestamp)
    if (props.iof.options && props.iof.options.length > 0) {
      const maxId = Math.max(0, ...props.iof.options.map(opt => {
        const id = parseInt(opt.value, 10);
        return isNaN(id) ? 0 : id;
      }));
      newValue = (maxId + 1).toString();
    } else {
      // Если список пуст, используем timestamp-based ID
      newValue = `manual_${Date.now()}`;
    }

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

/* Контейнер для Select и кнопки редактирования */
.iof-select-container {
  position: relative;
  width: 100%;
}

/* Кнопка редактирования внутри Select */
.edit-signer-btn {
  position: absolute;
  /* right управляется динамически через computed свойство editButtonPosition */
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

.edit-signer-btn:hover {
  background-color: transparent !important;
  color: var(--md-primary, #2f6fed) !important;
}

.edit-signer-btn:active {
  background-color: transparent !important;
  color: var(--md-primary-active, #1e54d4) !important;
}

/* Динамический padding в зависимости от наличия индикатора истории */
.iof-with-two-icons :deep(.form-field-with-history .p-select .p-select-label) {
  padding-right: 75px !important; /* Две иконки: карандаш + индикатор истории */
}

.iof-with-one-icon :deep(.form-field-with-history .p-select .p-select-label) {
  padding-right: 40px !important; /* Одна иконка: только карандаш */
}
</style>
