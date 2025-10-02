<template>
  <div class="status-bar">
    <!-- Основной контейнер статус-бара -->
    <div class="status-bar__container">
      <!-- Секция устройств -->
      <div v-if="store.devices.length > 0" class="status-bar__section">
        <StatusIndicator
          v-for="device in store.devices"
          :key="device.id"
          :label="device.name"
          :status="getDeviceStatus(device)"
          :tooltip="`${device.name}: ${device.isConnected ? 'Подключено' : 'Отключено'}${device.error ? ` - ${device.error}` : ''}`"
          @click="handleDeviceClick(device)"
        />
      </div>

      <!-- Секция сервисов -->
      <div class="status-bar__section status-bar__section--services">
        <StatusIndicator
          label="MS"
          :status="getServiceStatus(store.services.messagingService.isConnected, store.services.messagingService.error)"
          tooltip="Messaging Service"
        />
        <StatusIndicator
          v-if="store.services.elis"
          label="ELIS"
          :status="getServiceStatus(store.services.elis.isConnected, store.services.elis.error)"
          tooltip="Лабораторная система"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { useStatusStore } from '../stores/statusStore';
import { useSignalR } from '../composables/useSignalR';
import { useIntervalFn } from '@vueuse/core';
import StatusIndicator from './StatusIndicator.vue';
import type { DeviceStatus, StatusResponse, IndicatorStatus } from '../types/status.types';

const store = useStatusStore();
const { connectionState, on } = useSignalR('/statusHub');

// Автообновление каждые 10 секунд если SignalR не подключен
const { pause, resume } = useIntervalFn(() => {
  if (connectionState.value !== 'connected') {
    store.fetchStatus();
  }
}, 10000);

// SignalR real-time обновления
on('statusUpdated', (data: StatusResponse) => {
  store.updateFromSignalR(data);
});

// Начальная загрузка
onMounted(() => {
  store.fetchStatus();
});

function handleDeviceClick(device: DeviceStatus) {
  console.log('Device clicked:', device);
  // Будущее: показать модальное окно с деталями устройства
}

function getDeviceStatus(device: DeviceStatus): IndicatorStatus {
  if (device.isConnected) {
    return 'online';
  }
  // Если есть ошибка "Нет связи с сервером", то это ndv (недостоверно)
  if (device.error?.includes('Нет связи с сервером')) {
    return 'ndv';
  }
  // Иначе устройство просто не на связи
  return 'offline';
}

function getServiceStatus(isConnected: boolean, error?: string): IndicatorStatus {
  if (isConnected) {
    return 'online';
  }
  // Если есть ошибка "Нет связи с сервером", то это ndv (недостоверно)
  if (error?.includes('Нет связи с сервером')) {
    return 'ndv';
  }
  return 'offline';
}
</script>

<style lang="scss" scoped>
.status-bar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  background: var(--p-surface-0);
  border-top: 1px solid var(--p-surface-200);
  z-index: 1000;
  font-size: 0.875rem;
  font-family: var(--p-font-family);
  box-shadow: 0 -2px 8px rgba(0, 0, 0, 0.08);

  &__container {
    display: flex;
    align-items: center;
    justify-content: flex-start;
    padding: 0.25rem 0.5rem;
    max-width: none;
    margin: 0;
    gap: 0.5rem;
    flex-wrap: wrap;

    @media (max-width: 768px) {
      gap: 0.45rem;
      padding: 0.25rem 0.5rem;
    }
  }

  &__section {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    flex-wrap: wrap;
    margin: 0;
    padding: 0;

    &--services {
      // Секция сервисов без дополнительного отступа
    }

    @media (max-width: 768px) {
      gap: 0.35rem;
    }
  }
}

// Адаптивность
@media (max-width: 1024px) {
  .status-bar {
    &__container {
      flex-direction: column;
      align-items: stretch;
    }

    &__section {
      justify-content: flex-start;
    }
  }
}
</style>

<style lang="scss">
// Глобальное резервирование места под fixed статус-бар
body {
  padding-bottom: 1.8rem !important;
}
</style>
