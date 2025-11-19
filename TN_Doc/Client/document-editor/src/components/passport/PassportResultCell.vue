<template>
  <div class="result-cell">
    <div
      class="result-value"
      :class="{
        'elis-filled': isElisFilled,
        'result-value--disabled': !canEdit
      }"
    >
      <span>{{ displayValue }}</span>
      <i
        v-if="showSyncIcon"
        class="pi pi-refresh sync-icon"
        v-tooltip.top="'Балластный параметр — результат синхронизирован с измерением'"
      />
    </div>
    <button
      class="result-edit-btn"
      :disabled="!canEdit"
      :title="!canEdit && editDisabledReason ? editDisabledReason : ''"
      type="button"
      @click="handleEditClick"
    >
      Редактировать
    </button>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import Tooltip from 'primevue/tooltip';
import type { PassportQualityParameter } from '@/types/passport.types';

interface Props {
  parameter: PassportQualityParameter;
  canEdit: boolean;
  isElisFilled?: boolean;
  editDisabledReason?: string;
  showSyncIcon?: boolean;
}

const props = defineProps<Props>();

defineOptions({
  directives: {
    tooltip: Tooltip
  }
});

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
  gap: 6px;
  align-items: center;
}

.result-value {
  width: 100%;
  min-height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--md-outline, #d5d7da);
  border-radius: 6px;
  font-size: 15px;
  padding: 4px 8px;
  background-color: white;
}

.result-value.elis-filled {
  background-color: var(--md-elis-highlight, #e8f5e9);
}

.result-value--disabled {
  background-color: var(--md-surface-variant, #f1f3f4);
  color: var(--md-text-secondary, #5f6368);
}

.result-edit-btn {
  width: 100%;
  height: 32px;
  border-radius: 6px;
  border: 1px solid var(--md-primary, #2f6fed);
  background-color: var(--md-primary, #2f6fed);
  color: #fff;
  font-size: 14px;
  cursor: pointer;
  transition: opacity 0.2s ease, box-shadow 0.2s ease;
}

.result-edit-btn:disabled {
  cursor: not-allowed;
  opacity: 0.5;
  background-color: var(--md-outline, #d5d7da);
  border-color: var(--md-outline, #d5d7da);
  color: var(--md-text-secondary, #5f6368);
}

.sync-icon {
  margin-left: 6px;
  font-size: 0.85rem;
  color: var(--md-primary, #2f6fed);
}
</style>
