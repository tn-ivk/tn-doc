<template>
  <div
    class="status-indicator"
    :class="[`status-indicator--${status}`]"
    :title="tooltip"
    @click="$emit('click')"
  >
    <span class="status-indicator__dot"></span>
    <span class="status-indicator__label">{{ label }}</span>
    <span v-if="latency !== undefined" class="status-indicator__latency">
      {{ latency }}ms
    </span>
  </div>
  </template>

<script setup lang="ts">
interface Props {
  label: string;
  status: 'online' | 'offline' | 'warning';
  latency?: number;
  tooltip?: string;
}

defineProps<Props>();
defineEmits<{
  click: []
}>();
</script>

<style lang="scss">
.status-indicator {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 8px;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(0, 0, 0, 0.05);
  }

  &--online {
    .status-indicator__dot {
      background: #28a745;
      box-shadow: 0 0 4px #28a745;
    }
  }

  &--offline {
    .status-indicator__dot {
      background: #dc3545;
      animation: blink 1s infinite;
    }
  }

  &--warning {
    .status-indicator__dot {
      background: #ffc107;
    }
  }

  &__dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    transition: all 0.3s;
  }

  &__label {
    font-weight: 500;
    color: #212529;
  }

  &__latency {
    font-size: 11px;
    color: #6c757d;
  }
}

@keyframes blink {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.3; }
}
</style>


