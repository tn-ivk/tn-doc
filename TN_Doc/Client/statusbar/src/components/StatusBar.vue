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
          :status="device.isConnected ? 'online' : 'offline'"
          :tooltip="`${device.name}: ${device.isConnected ? 'Подключено' : 'Отключено'}${device.error ? ` - ${device.error}` : ''}`"
          @click="handleDeviceClick(device)"
        />
      </div>

      <!-- Секция сервисов -->
      <div class="status-bar__section status-bar__section--services">
        <StatusIndicator
          label="MS"
          :status="store.services.messagingService.isConnected ? 'online' : 'offline'"
          tooltip="Messaging Service"
        />
        <StatusIndicator
          v-if="store.services.elis"
          label="ELIS"
          :status="store.services.elis.isConnected ? 'online' : 'offline'"
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
import type { DeviceStatus, StatusResponse } from '../types/status.types';

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
  padding: 0; // DEBUG: убираем возможный padding

  &__container {
    display: flex;
    align-items: center;
    justify-content: flex-start;
    padding: 0.25rem 0; // DEBUG: убрали padding слева и справа
    max-width: none; // DEBUG: убрали ограничение ширины
    margin: 0; // DEBUG: убрали auto-центрирование
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
    margin: 0; // DEBUG: убираем возможный margin
    padding: 0; // DEBUG: убираем возможный padding

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
