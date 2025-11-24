<template>
  <Dialog
    :visible="visible"
    modal
    class="manual-method-dialog"
    header="Редактирование метода испытаний"
    :closable="false"
    @update:visible="updateVisible"
  >
    <div class="dialog-body">
      <p class="parameter-name" v-if="parameterName">
        {{ parameterName }}
      </p>

      <div class="dialog-field">
        <label>Название метода</label>
        <input
          v-model="form.name"
          type="text"
          placeholder="Например ГОСТ 12345"
        />
        <small v-if="validationError" class="validation-error">
          {{ validationError }}
        </small>
      </div>

      <label class="checkbox-field" v-if="!isElisEnabled">
        <input type="checkbox" v-model="form.limitValueActivate" />
        <span>Использовать предельное значение</span>
      </label>

      <div class="dialog-field" v-if="form.limitValueActivate">
        <label>Предельное значение</label>
        <input
          v-model="form.limitValue"
          type="text"
          placeholder="Например 0,5"
        />
      </div>

      <div class="dialog-field" v-if="form.limitValueActivate">
        <label>Строка результата при превышении</label>
        <input
          v-model="form.limitValueString"
          type="text"
          placeholder="Например менее 0,5"
        />
      </div>

      <label class="checkbox-field">
        <input type="checkbox" v-model="form.isDefault" />
        <span>Сделать основным методом</span>
      </label>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <button class="btn btn-text" type="button" @click="handleReset">
          Сброс
        </button>
        <button class="btn btn-text" type="button" @click="handleCancel">
          Отмена
        </button>
        <button
          class="btn btn-primary"
          type="button"
          :disabled="Boolean(validationError)"
          @click="handleConfirm"
        >
          Сохранить
        </button>
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { reactive, computed, watch } from 'vue';
import Dialog from 'primevue/dialog';

import type { MethodOption } from '@/types/passport.types';

export interface ManualMethodPayload {
  name: string;
  limitValueActivate: boolean;
  limitValue?: number;
  limitValueString?: string;
  isDefault: boolean;
}

interface Props {
  visible: boolean;
  parameterName?: string;
  isElisEnabled?: boolean;
  /** Исходный метод для инициализации формы */
  initialMethod?: MethodOption;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:visible': [value: boolean];
  confirm: [payload: ManualMethodPayload];
  reset: [];
}>();

const form = reactive({
  name: '',
  limitValueActivate: false,
  limitValue: '',
  limitValueString: '',
  isDefault: false
});

const validationError = computed(() => {
  if (form.limitValueActivate) {
    const numeric = Number(form.limitValue.replace(',', '.'));
    if (Number.isNaN(numeric)) {
      return 'Некорректное предельное значение';
    }
  }
  return '';
});

watch(
  () => props.visible,
  (next) => {
    if (next) {
      initializeForm();
    }
  }
);

/**
 * Инициализация формы данными из initialMethod (если есть)
 * или сброс к пустому состоянию
 */
function initializeForm() {
  if (props.initialMethod) {
    // Инициализация данными существующего метода
    form.name = props.initialMethod.name || '';
    form.limitValueActivate = props.initialMethod.limitValueActivate || false;
    form.limitValue = props.initialMethod.limitValue !== undefined
      ? String(props.initialMethod.limitValue).replace('.', ',')
      : '';
    form.limitValueString = props.initialMethod.limitValueString || '';
    form.isDefault = props.initialMethod.isDefault || false;
  } else {
    // Сброс формы к пустому состоянию
    resetForm();
  }
}

function resetForm() {
  form.name = '';
  form.limitValueActivate = false;
  form.limitValue = '';
  form.limitValueString = '';
  form.isDefault = false;
}

function updateVisible(next: boolean) {
  emit('update:visible', next);
}

function handleCancel() {
  emit('update:visible', false);
}

function handleConfirm() {
  if (validationError.value) {
    return;
  }

  const payload: ManualMethodPayload = {
    name: form.name.trim(),
    limitValueActivate: form.limitValueActivate,
    isDefault: form.isDefault
  };

  if (form.limitValueActivate) {
    const numeric = Number(form.limitValue.replace(',', '.'));
    if (!Number.isNaN(numeric)) {
      payload.limitValue = numeric;
    }
    if (form.limitValueString.trim()) {
      payload.limitValueString = form.limitValueString.trim();
    }
  }

  emit('confirm', payload);
}

function handleReset() {
  emit('reset');
  emit('update:visible', false);
}
</script>

<style scoped>
.manual-method-dialog :global(.p-dialog) {
  padding: 10px;
}

.manual-method-dialog :global(.p-dialog-header) {
  text-align: center;
  justify-content: center;
}

.manual-method-dialog :global(.p-dialog-content) {
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

.parameter-name {
  font-weight: 600;
  margin: 0;
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

.checkbox-field {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
}

.validation-error {
  color: var(--md-error, #d32f2f);
  font-size: 12px;
}

.btn {
  min-width: 120px;
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

.btn-primary {
  background: var(--md-primary, #2f6fed);
  color: #fff;
}

.btn-primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>

