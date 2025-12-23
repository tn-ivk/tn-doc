<template>
  <div class="result-cell">
    <div class="result-input-group">
      <div class="result-wrapper">
        <div
          class="result-value"
          :class="[
            {
              'elis-filled': isElisFilled,
              'result-value--disabled': !canEdit,
              'result-value--full-radius': !showEditButton
            }
          ]"
        >
          <span>{{ displayValue }}</span>
        </div>

        <!-- Контейнер для индикаторов -->
        <div v-if="$slots['indicators']" class="indicators-container">
          <slot name="indicators"></slot>
        </div>
      </div>

      <!-- Кнопка редактирования справа от результата -->
      <button
        v-if="showEditButton"
        class="edit-result-btn"
        :class="{
          'edit-result-btn--elis': isElisFilled,
          'edit-result-btn--disabled': !canEdit
        }"
        type="button"
        :disabled="!canEdit"
        @click="handleEditClick"
        title="Редактирование..."
      >
        <i class="pi pi-pen-to-square"></i>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { PassportQualityParameter } from '@/types/passport.types';

interface Props {
  parameter: PassportQualityParameter;
  canEdit: boolean;
  showEditButton?: boolean;
  isElisFilled?: boolean;
  editDisabledReason?: string;
}

const props = defineProps<Props>();

const showEditButton = computed(() => props.showEditButton ?? true);

const emit = defineEmits<{
  'result-edit': [];
}>();

const displayValue = computed(() => {
  if (!props.parameter.values.result) return '-';
  return props.parameter.values.result.replace('.', ',');
});

function handleEditClick() {
  if (!props.canEdit) {
    return;
  }
  emit('result-edit');
}
</script>

<style scoped>
.result-cell {
  display: flex;
  flex-direction: column;
  align-items: center;
  width: 100%;
}

/* Контейнер для поля результата и иконки */
.result-input-group {
  position: relative;
  display: flex;
  width: 100%;
  max-width: 100%;
}

.result-wrapper {
  position: relative;
  flex: 1;
  min-width: 0;
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

.result-value {
  width: 100%;
  max-width: 100%;
  min-height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--md-outline);
  border-radius: var(--md-radius) 0 0 var(--md-radius) !important;
  font-size: 15px;
  padding: 4px 8px;
  background-color: white;
  box-sizing: border-box;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.result-value.elis-filled {
  background-color: var(--md-elis-highlight, #e8f5e9);
}

.result-value--disabled {
  background-color: var(--md-surface-variant, #f1f3f4);
  color: var(--md-text-secondary, #5f6368);
}

/* Полное скругление, когда кнопки редактирования нет */
.result-value--full-radius {
  border-radius: var(--md-radius) !important;
}

/* Иконка редактирования внутри поля результата */
.edit-result-btn {
  width: 28px;
  height: 36px;
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

/* Тёмная иконка для ELIS-заполненного поля (зелёный фон) */
.edit-result-btn--elis {
  color: var(--md-text, #212121) !important;
}

.edit-result-btn:hover {
  background-color: rgba(0, 0, 0, 0.04) !important;
  color: var(--md-primary, #2f6fed) !important;
}

.edit-result-btn:disabled,
.edit-result-btn:disabled:hover {
  background-color: var(--md-surface-variant, #f1f3f4) !important;
  color: var(--md-text-secondary, #5f6368) !important;
  cursor: not-allowed;
}

.edit-result-btn:active {
  background-color: rgba(0, 0, 0, 0.08) !important;
  color: var(--md-primary-active, #1e54d4) !important;
}

</style>
