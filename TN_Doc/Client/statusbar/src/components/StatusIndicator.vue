<template>
  <div
    class="status-indicator"
    :class="{ 'status-indicator--clickable': clickable }"
    @click="handleClick"
  >
    <Badge
      :value="label"
      :severity="badgeSeverity"
      :pt="{
        root: { class: 'status-indicator__badge' }
      }"
      v-tooltip.top="tooltip"
    >
      <template #value>
        <div class="status-indicator__content">
          <i :class="statusIconClass" class="status-indicator__icon"></i>
          <span class="status-indicator__label">{{ label }}</span>
          <span v-if="latency !== undefined" class="status-indicator__latency">
            {{ latency }}ms
          </span>
        </div>
      </template>
    </Badge>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import Badge from 'primevue/badge';
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

const badgeSeverity = computed(() => {
  switch (props.status) {
    case 'online':
      return 'success';
    case 'offline':
      return 'danger';
    case 'warning':
      return 'warn';
    default:
      return 'secondary';
  }
});

const statusIconClass = computed(() => {
  switch (props.status) {
    case 'online':
      return 'pi pi-check-circle';
    case 'offline':
      return 'pi pi-times-circle status-indicator__icon--blink';
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
  user-select: none;

  &--clickable {
    cursor: pointer;

    &:hover :deep(.status-indicator__badge) {
      opacity: 0.85;
      transform: scale(1.02);
    }
  }

  :deep(.status-indicator__badge) {
    transition: all 0.2s ease;
    padding: 0.35rem 0.65rem;
    font-size: 0.875rem;
    font-weight: 500;
    border-radius: 4px;
  }

  &__content {
    display: flex;
    align-items: center;
    gap: 0.4rem;
  }

  &__icon {
    font-size: 0.75rem;

    &--blink {
      animation: blink 1.5s ease-in-out infinite;
    }
  }

  &__label {
    font-weight: 500;
    font-size: 0.813rem;
  }

  &__latency {
    font-size: 0.688rem;
    opacity: 0.8;
    margin-left: 0.15rem;
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
