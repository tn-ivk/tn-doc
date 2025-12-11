<template>
  <Dialog
    :visible="visible"
    modal
    class="manual-signer-dialog"
    header="Ручной ввод"
    :closable="false"
    :style="{ minWidth: '400px' }"
    @update:visible="updateVisible"
  >
    <div class="dialog-body">
      <p class="field-label-text" v-if="fieldLabel">
        {{ fieldLabel }}
      </p>

      <div class="dialog-field">
        <input
          ref="nameInput"
          v-model="form.name"
          type="text"
          placeholder="И.О.Фамилия"
          @keyup.enter="handleConfirm"
        />
        <small v-if="validationError" class="validation-error">
          {{ validationError }}
        </small>
      </div>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <button class="btn btn-text" type="button" @click="handleCancel">
          Отмена
        </button>
        <button
          class="btn btn-primary"
          type="button"
          :disabled="Boolean(validationError) || !form.name.trim()"
          @click="handleConfirm"
        >
          Сохранить
        </button>
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { reactive, computed, watch, ref, nextTick } from 'vue';
import Dialog from 'primevue/dialog';

export interface ManualSignerPayload {
  name: string;
}

interface Props {
  visible: boolean;
  fieldLabel?: string;
  /** Текущее значение ФИО для предзаполнения */
  currentName?: string;
  /** Список некорректных символов */
  invalidChars?: string[];
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:visible': [value: boolean];
  confirm: [payload: ManualSignerPayload];
}>();

const nameInput = ref<HTMLInputElement | null>(null);

const form = reactive({
  name: ''
});

/**
 * Проверяет наличие недопустимых символов в имени
 */
const invalidCharFound = computed(() => {
  if (!form.name || !props.invalidChars || props.invalidChars.length === 0) {
    return null;
  }

  for (const char of props.invalidChars) {
    if (form.name.includes(char)) {
      return char;
    }
  }
  return null;
});

const validationError = computed(() => {
  if (invalidCharFound.value !== null) {
    return `Некорректный символ: ${invalidCharFound.value}`;
  }
  return '';
});

watch(
  () => props.visible,
  (next) => {
    if (next) {
      initializeForm();
      // Фокус на поле ввода при открытии
      nextTick(() => {
        nameInput.value?.focus();
      });
    }
  }
);

/**
 * Инициализация формы текущим значением (если есть)
 */
function initializeForm() {
  form.name = props.currentName || '';
}

function updateVisible(next: boolean) {
  emit('update:visible', next);
}

function handleCancel() {
  emit('update:visible', false);
}

function handleConfirm() {
  if (validationError.value || !form.name.trim()) {
    return;
  }

  const payload: ManualSignerPayload = {
    name: form.name.trim()
  };

  emit('confirm', payload);
}
</script>

<style scoped>
.manual-signer-dialog :global(.p-dialog) {
  padding: 10px;
}

.manual-signer-dialog :global(.p-dialog-header) {
  text-align: center;
  justify-content: center;
}

.manual-signer-dialog :global(.p-dialog-content) {
  padding: 1.5rem;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}

.dialog-body {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.field-label-text {
  font-weight: 600;
  margin: 0;
  text-align: center;
}

.dialog-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.dialog-field label {
  font-size: 13px;
  color: var(--md-text-secondary, #5f6368);
}

.dialog-field input {
  height: 36px;
  border-radius: 6px;
  border: 1px solid var(--md-outline, #d5d7da);
  padding: 0 10px;
  font-size: 14px;
}

.dialog-field input:focus {
  outline: none;
  border-color: var(--md-primary, #2f6fed);
  box-shadow: 0 0 0 2px rgba(47, 111, 237, 0.2);
}

.validation-error {
  color: var(--md-error, #d32f2f);
  font-size: 12px;
}

.btn {
  min-width: 100px;
  height: 34px;
  border-radius: 6px;
  border: none;
  cursor: pointer;
  font-size: 14px;
}

.btn-text {
  background: transparent;
  color: var(--md-text, #212121);
}

.btn-text:hover {
  background: rgba(0, 0, 0, 0.05);
}

.btn-primary {
  background: var(--md-primary, #2f6fed);
  color: #fff;
}

.btn-primary:hover {
  background: var(--md-primary-active, #1e54d4);
}

.btn-primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
