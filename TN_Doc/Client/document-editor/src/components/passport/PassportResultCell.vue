<template>
  <div class="result-cell">
    <div class="result-value-container">
      <div
        class="result-value"
        :class="{
          'elis-filled': isElisFilled,
          'result-value--disabled': !canEdit,
          [paddingClass]: true
        }"
      >
        <span>{{ displayValue }}</span>
      </div>

      <!-- Иконка редактирования внутри поля результата -->
      <button
        v-if="canEdit"
        class="edit-result-btn"
        :class="{ 'edit-result-btn--elis': isElisFilled }"
        :style="{ right: editButtonPosition }"
        type="button"
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
  isElisFilled?: boolean;
  editDisabledReason?: string;
  hasHistoryIndicator?: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  'result-edit': [];
}>();

const displayValue = computed(() => {
  if (!props.parameter.values.result) return '-';
  return props.parameter.values.result.replace('.', ',');
});

/**
 * Динамическая позиция кнопки редактирования
 * Если индикатор истории отображается - сдвигаем кнопку левее
 * Если индикатора нет - прижимаем к правому краю
 */
const editButtonPosition = computed(() => {
  return props.hasHistoryIndicator ? '30px' : '2px';
});

/**
 * Динамический класс для padding текста результата
 */
const paddingClass = computed(() => {
  return props.hasHistoryIndicator ? 'with-two-icons' : 'with-one-icon';
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
.result-value-container {
  position: relative;
  width: 100%;
  max-width: 100%;
  flex: 1;
  min-width: 0;
}

.result-value {
  width: 100%;
  max-width: 100%;
  min-height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--md-outline, #d5d7da);
  border-radius: 6px;
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

/* Иконка редактирования внутри поля результата */
.edit-result-btn {
  position: absolute;
  /* right управляется динамически через computed свойство editButtonPosition */
  top: 50%;
  transform: translateY(-50%);
  width: 28px;
  height: 28px;
  border: 1px solid transparent !important;
  background-color: transparent !important;
  color: var(--md-outline-light, #E0E0E0) !important;
  font-size: 14px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  z-index: 1;
}

/* Тёмная иконка для ELIS-заполненного поля (зелёный фон) */
.edit-result-btn--elis {
  color: var(--md-text, #212121) !important;
}

.edit-result-btn:hover {
  background-color: transparent !important;
  color: var(--md-primary, #2f6fed) !important;
}

.edit-result-btn:active {
  background-color: transparent !important;
  color: var(--md-primary-active, #1e54d4) !important;
}

/* Динамический padding в зависимости от наличия индикатора истории */
.result-value.with-two-icons {
  padding-right: 75px !important; /* Две иконки: кнопка + индикатор истории */
}

.result-value.with-one-icon {
  padding-right: 40px !important; /* Одна иконка: только кнопка */
}
</style>
