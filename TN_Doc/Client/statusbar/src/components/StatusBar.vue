<template>
  <div class="status-bar" :class="`status-bar--${store.overallHealth}`">
    <div class="status-bar__container">
      <div class="status-bar__section">
        <span class="status-bar__label">Устройства:</span>
        <StatusIndicator
          v-for="device in store.devices"
          :key="device.id"
          :label="device.name"
          :status="device.isConnected ? 'online' : 'offline'"
          :latency="device.latencyMs"
          :tooltip="`${device.name}: ${device.isConnected ? 'Подключено' : 'Отключено'}`"
          @click="handleDeviceClick(device)"
        />
      </div>

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
      </div>

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
          :title="`SignalR: ${signalRState}`"
        >
          <IconWifi />
        </span>
      </div>
    </div>

    <div v-if="store.error" class="status-bar__error">
      <IconWarning />
      {{ store.error }}
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

const { pause, resume } = useIntervalFn(() => {
  if (connectionState.value !== 'connected') {
    void store.fetchStatus();
  }
}, 5000);

on('statusUpdated', (data: StatusResponse) => {
  store.updateFromSignalR(data);
});

onMounted(() => {
  void store.fetchStatus();
});

function refresh() {
  void store.fetchStatus();
}

function handleDeviceClick(device: DeviceStatus) {
  console.log('Device clicked:', device);
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
  }

  &__refresh {
    background: none;
    border: 1px solid #dee2e6;
    border-radius: 4px;
    padding: 4px 8px;
    cursor: pointer;
    transition: all 0.2s;

    &:hover:not(:disabled) {
      background: #e9ecef;
    }

    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
  }

  &__timestamp {
    display: none;
  }

  &__connection {
    padding: 4px 8px;
    border-radius: 4px;

    &--connected { color: #28a745; }
    &--connecting { color: #ffc107; animation: pulse 1s infinite; }
    &--disconnected { color: #dc3545; }
  }

  &__error {
    background: #dc3545;
    color: white;
    padding: 4px 16px;
    text-align: center;
    font-size: 12px;
  }
  .spin { animation: spin 1s linear infinite; }
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}
@keyframes spin { from { transform: rotate(0deg);} to { transform: rotate(360deg);} }
</style>


