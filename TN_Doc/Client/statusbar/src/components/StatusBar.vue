<template>
  <div class="status-bar" :class="`status-bar--${store.overallHealth}`">
    <div class="status-bar__container">
      <!-- Devices Section -->
      <div v-if="store.devices.length > 0" class="status-bar__section">
        <span class="status-bar__label">Устройства:</span>
        <StatusIndicator
          v-for="device in store.devices"
          :key="device.id"
          :label="device.name"
          :status="device.isConnected ? 'online' : 'offline'"
          :latency="device.latencyMs"
          :tooltip="`${device.name}: ${device.isConnected ? 'Подключено' : 'Отключено'}${device.error ? ` - ${device.error}` : ''}`"
          @click="handleDeviceClick(device)"
        />
      </div>

      <!-- Services Section -->
      <div class="status-bar__section">
        <span class="status-bar__label">Сервисы:</span>
        <StatusIndicator
          label="MS"
          :status="store.services.messagingService.isConnected ? 'online' : 'offline'"
          :latency="store.services.messagingService.latencyMs"
          tooltip="Messaging Service"
        />
        <StatusIndicator
          v-if="store.services.elis"
          label="ELIS"
          :status="store.services.elis.isConnected ? 'online' : 'offline'"
          :latency="store.services.elis.latencyMs"
          tooltip="Лабораторная система"
        />
        <StatusIndicator
          v-if="store.services.opcDa"
          label="OPC DA"
          :status="store.services.opcDa.isConnected ? 'online' : 'offline'"
          :latency="store.services.opcDa.latencyMs"
          tooltip="OPC DA сервер"
        />
        <StatusIndicator
          v-if="store.services.opcUa"
          label="OPC UA"
          :status="store.services.opcUa.isConnected ? 'online' : 'offline'"
          :latency="store.services.opcUa.latencyMs"
          tooltip="OPC UA сервер"
        />
      </div>

      <!-- Actions Section -->
      <div class="status-bar__section status-bar__section--actions">
        <button
          class="status-bar__refresh"
          @click="refresh"
          :disabled="store.isLoading"
          title="Обновить статус"
        >
          <IconRefresh :spinning="store.isLoading" />
        </button>

        <span
          class="status-bar__connection"
          :class="`status-bar__connection--${signalRState}`"
          :title="`SignalR: ${signalRStateText}`"
        >
          <IconWifi />
        </span>

        <span v-if="store.lastUpdate" class="status-bar__last-update" title="Последнее обновление">
          {{ formattedLastUpdate }}
        </span>
      </div>
    </div>

    <!-- Error notification -->
    <div v-if="store.error" class="status-bar__error" @click="store.clearError()">
      <IconWarning />
      <span>{{ store.error }}</span>
      <button class="status-bar__error-close" title="Закрыть">&times;</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, computed } from 'vue';
import { useStatusStore } from '../stores/statusStore';
import { useSignalR } from '../composables/useSignalR';
import { useIntervalFn } from '@vueuse/core';
import StatusIndicator from './StatusIndicator.vue';
import IconRefresh from './icons/IconRefresh.vue';
import IconWifi from './icons/IconWifi.vue';
import IconWarning from './icons/IconWarning.vue';
import type { DeviceStatus, StatusResponse } from '../types/status.types';

const store = useStatusStore();
const { connectionState, on } = useSignalR('/statusHub');

const signalRState = computed(() => connectionState.value);

const signalRStateText = computed(() => {
  switch (connectionState.value) {
    case 'connected':
      return 'Подключено';
    case 'connecting':
      return 'Подключение...';
    case 'disconnected':
      return 'Отключено';
    default:
      return 'Неизвестно';
  }
});

const formattedLastUpdate = computed(() => {
  if (!store.lastUpdate) return '';
  const date = store.lastUpdate;
  return `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}:${String(date.getSeconds()).padStart(2, '0')}`;
});

// Auto-refresh every 10 seconds as fallback when SignalR is not connected
const { pause, resume } = useIntervalFn(() => {
  if (connectionState.value !== 'connected') {
    store.fetchStatus();
  }
}, 10000);

// SignalR real-time updates
on('statusUpdated', (data: StatusResponse) => {
  store.updateFromSignalR(data);
});

// Initial fetch
onMounted(() => {
  store.fetchStatus();
});

function refresh() {
  store.fetchStatus();
}

function handleDeviceClick(device: DeviceStatus) {
  console.log('Device clicked:', device);
  // Future: Show device details modal
}
</script>

<style lang="scss">
.status-bar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  background: #f8f9fa;
  border-top: 1px solid #dee2e6;
  z-index: 1000;
  font-size: 14px;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;

  &--critical {
    background: #fff5f5;
    border-top-color: #dc3545;
  }

  &--warning {
    background: #fffaf0;
    border-top-color: #ffc107;
  }

  &__container {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 8px 16px;
    max-width: 1400px;
    margin: 0 auto;
    gap: 16px;
  }

  &__section {
    display: flex;
    align-items: center;
    gap: 12px;

    &--actions {
      margin-left: auto;
    }
  }

  &__label {
    font-weight: 600;
    color: #495057;
    font-size: 13px;
  }

  &__refresh {
    background: none;
    border: 1px solid #dee2e6;
    border-radius: 4px;
    padding: 4px 8px;
    cursor: pointer;
    transition: all 0.2s;
    display: flex;
    align-items: center;
    color: #495057;

    &:hover:not(:disabled) {
      background: #e9ecef;
    }

    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
  }

  &__connection {
    padding: 4px 8px;
    border-radius: 4px;
    display: flex;
    align-items: center;

    &--connected {
      color: #28a745;
    }

    &--connecting {
      color: #ffc107;
      animation: pulse 1s infinite;
    }

    &--disconnected {
      color: #dc3545;
    }
  }

  &__last-update {
    font-size: 11px;
    color: #6c757d;
  }

  &__error {
    background: #dc3545;
    color: white;
    padding: 4px 16px;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    font-size: 12px;
    cursor: pointer;
    position: relative;

    &-close {
      background: none;
      border: none;
      color: white;
      font-size: 20px;
      line-height: 1;
      cursor: pointer;
      padding: 0 4px;
      margin-left: 8px;

      &:hover {
        opacity: 0.8;
      }
    }
  }
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}
</style>