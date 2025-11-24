<template>
  <Dialog
    :visible="visible"
    modal
    class="result-edit-dialog"
    header="Редактирование результата"
    :closable="false"
    @update:visible="updateVisible"
  >
    <div class="dialog-body">
      <p class="parameter-name" v-if="parameterName">
        {{ parameterName }}
      </p>

      <div class="dialog-field">
        <label>Префикс</label>
        <select v-model="operator">
          <option value="less">менее</option>
          <option value="greater">более</option>
        </select>
      </div>

      <div class="dialog-field">
        <label>Значение</label>
        <input
          v-model="valueInput"
          type="text"
          placeholder="Например 4,0"
        />
        <small v-if="validationError" class="validation-error">
          {{ validationError }}
        </small>
      </div>

      <div class="dialog-preview">
        <span>Предпросмотр:</span>
        <strong>{{ preview }}</strong>
      </div>
    </div>

    <template #footer>
      <button class="btn btn-text" type="button" @click="handleCancel">
        Отмена
      </button>
      <button
        class="btn btn-primary"
        type="button"
        :disabled="Boolean(validationError)"
        @click="handleConfirm"
      >
        Применить
      </button>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import Dialog from 'primevue/dialog';

type ResultOperator = 'less' | 'greater';

interface Props {
  visible: boolean;
  parameterName?: string;
  initialValue?: string;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'update:visible': [value: boolean];
  confirm: [result: string];
}>();

const operator = ref<ResultOperator>('less');
const valueInput = ref('');

const operatorLabel = computed(() => (operator.value === 'less' ? 'менее' : 'более'));

const normalizedNumber = computed(() => {
  const normalized = valueInput.value.replace(',', '.').trim();
  if (!normalized) {
    return null;
  }
  const num = Number(normalized);
  return Number.isNaN(num) ? null : num;
});

const validationError = computed(() => {
  if (!valueInput.value.trim()) {
    return 'Введите числовое значение';
  }
  if (normalizedNumber.value === null) {
    return 'Неверный формат числа';
  }
  return '';
});

const formattedValue = computed(() => {
  if (!valueInput.value.trim()) {
    return '—';
  }
  const raw = valueInput.value.trim().replace('.', ',');
  return raw;
});

const preview = computed(() => `${operatorLabel.value} ${formattedValue.value}`);

watch(
  () => props.visible,
  (next) => {
    if (next) {
      applyInitialValue();
    }
  }
);

function applyInitialValue() {
  const raw = (props.initialValue || '').toLowerCase();
  if (raw.startsWith('более')) {
    operator.value = 'greater';
  } else {
    operator.value = 'less';
  }

  const valueMatch = raw.match(/([-+]?\d+[.,]?\d*)/);
  valueInput.value = valueMatch ? valueMatch[1].replace('.', ',') : '';
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
  emit('confirm', preview.value);
}
</script>

<style scoped>
.result-edit-dialog :global(.p-dialog-header) {
  text-align: center;
}

.result-edit-dialog :global(.p-dialog-content) {
  padding: 10px;
}

.dialog-body {
  display: flex;
  flex-direction: column;
  gap: 16px;
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

.dialog-field input,
.dialog-field select {
  height: 36px;
  border-radius: 6px;
  border: 1px solid var(--md-outline, #d5d7da);
  padding: 0 10px;
  font-size: 14px;
}

.dialog-preview {
  background: var(--md-surface-variant, #f1f3f4);
  border-radius: 6px;
  padding: 10px 12px;
  display: flex;
  justify-content: space-between;
  align-items: center;
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

