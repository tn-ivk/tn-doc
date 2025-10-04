<template>
  <div
    class="status-indicator"
    :class="[
      `status-indicator--${status}`,
      { 'status-indicator--clickable': clickable }
    ]"
    @click="handleClick"
    v-tooltip.top="tooltip"
  >
    <i :class="statusIconClass" class="status-indicator__icon"></i>
    <span class="status-indicator__label">{{ label }}</span>
    <span v-if="latency !== undefined" class="status-indicator__latency">
      {{ latency }}ms
    </span>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { IndicatorStatus } from '../types/status.types';

interface Props {
  label: string;
  status: IndicatorStatus;
  latency?: number;
  tooltip?: string;
  clickable?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  clickable: true
});

const emit = defineEmits<{
  click: []
}>();

const statusIconClass = computed(() => {
  switch (props.status) {
    case 'online':
      return 'pi pi-link';
    case 'offline':
      return 'pi pi-times-circle status-indicator__icon--blink';
    case 'ndv':
      return 'pi pi-question-circle';
    case 'warning':
      return 'pi pi-exclamation-triangle';
    default:
      return 'pi pi-circle';
  }
});

function handleClick() {
  if (props.clickable) {
    emit('click');
  }
}
</script>

<style lang="scss" scoped>
.status-indicator {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0.2rem 0.5rem;
  border-radius: 4px;
  font-size: 0.85rem;
  font-weight: 500;
  line-height: 1;
  transition: all 0.2s ease;
  user-select: none;
  border: 1px solid transparent;

  &--online {
    background: var(--p-green-100);
    color: var(--p-green-700);
    border-color: var(--p-green-200);
  }

  &--offline {
    background: var(--p-red-100);
    color: var(--p-red-700);
    border-color: var(--p-red-200);
  }

  &--ndv {
    background: var(--p-surface-100);
    color: var(--p-surface-700);
    border-color: var(--p-surface-300);
  }

  &--warning {
    background: var(--p-yellow-100);
    color: var(--p-yellow-700);
    border-color: var(--p-yellow-200);
  }

  &--clickable {
    cursor: pointer;

    &:hover {
      opacity: 0.85;
      transform: scale(1.02);
    }

    // Кастомный стиль фокуса вместо стандартного outline
    &:focus {
      outline: none;
    }

    &:focus-visible {
      box-shadow: 0 0 0 3px rgba(13, 110, 253, 0.25);
    }
  }

  // Специфичные стили фокуса для каждого статуса
  &--online.status-indicator--clickable:focus-visible {
    box-shadow: 0 0 0 3px var(--p-green-200);
  }

  &--offline.status-indicator--clickable:focus-visible {
    box-shadow: 0 0 0 3px var(--p-red-200);
  }

  &--warning.status-indicator--clickable:focus-visible {
    box-shadow: 0 0 0 3px var(--p-yellow-200);
  }

  &--ndv.status-indicator--clickable:focus-visible {
    box-shadow: 0 0 0 3px var(--p-surface-300);
  }

  &__icon {
    font-size: 0.8rem;
    flex-shrink: 0;

    &--blink {
      animation: blink 1.5s ease-in-out infinite;
    }
  }

  &__label {
    font-weight: 500;
    font-size: 0.8rem;
    white-space: nowrap;
  }

  &__latency {
    font-size: 0.6rem;
    opacity: 0.8;
    white-space: nowrap;
  }
}

@keyframes blink {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.3;
  }
}
</style>
