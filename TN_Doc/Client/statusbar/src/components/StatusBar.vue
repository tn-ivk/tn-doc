<template>
  <div class="status-bar">
    <!-- Основной контейнер статус-бара -->
    <div class="status-bar__container">
      <!-- Секция устройств -->
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

      <!-- Секция сервисов -->
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

      <!-- Секция действий -->
      <div class="status-bar__section status-bar__section--actions">
        <Button
          icon="pi pi-refresh"
          :loading="store.isLoading"
          severity="secondary"
          text
          rounded
          @click="refresh"
          v-tooltip.top="'Обновить статус'"
          aria-label="Обновить статус"
        />

        <Tag
          :icon="signalRIconClass"
          :severity="signalRSeverity"
          :value="signalRStateText"
          v-tooltip.top="`SignalR: ${signalRStateText}`"
        />

        <span v-if="store.lastUpdate" class="status-bar__last-update">
          <i class="pi pi-clock" style="font-size: 0.75rem; margin-right: 0.25rem"></i>
          {{ formattedLastUpdate }}
        </span>
      </div>
    </div>

    <!-- Уведомление об ошибке -->
    <Transition name="error">
      <Message
        v-if="store.error"
        severity="error"
        :closable="true"
        @close="store.clearError()"
        class="status-bar__error"
      >
        <template #icon>
          <i class="pi pi-exclamation-triangle"></i>
        </template>
        {{ store.error }}
      </Message>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { onMounted, computed } from 'vue';
import { useStatusStore } from '../stores/statusStore';
import { useSignalR } from '../composables/useSignalR';
import { useIntervalFn } from '@vueuse/core';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import Message from 'primevue/message';
import StatusIndicator from './StatusIndicator.vue';
import type { DeviceStatus, StatusResponse } from '../types/status.types';

const store = useStatusStore();
const { connectionState, on } = useSignalR('/statusHub');

const signalRSeverity = computed(() => {
  switch (connectionState.value) {
    case 'connected':
      return 'success';
    case 'connecting':
      return 'warn';
    case 'disconnected':
      return 'danger';
    default:
      return 'secondary';
  }
});

const signalRIconClass = computed(() => {
  switch (connectionState.value) {
    case 'connected':
      return 'pi pi-wifi';
    case 'connecting':
      return 'pi pi-spin pi-spinner';
    case 'disconnected':
      return 'pi pi-wifi status-bar__icon--disconnected';
    default:
      return 'pi pi-question-circle';
  }
});

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

function refresh() {
  store.fetchStatus();
}

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
  border-top: 2px solid var(--p-surface-200);
  z-index: 1000;
  font-size: 0.875rem;
  font-family: var(--p-font-family);
  box-shadow: 0 -2px 8px rgba(0, 0, 0, 0.08);

  &__container {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.4rem 0.75rem;
    max-width: 1400px;
    margin: 0 auto;
    gap: 0.65rem;
    flex-wrap: wrap;

    @media (max-width: 768px) {
      gap: 0.5rem;
      padding: 0.35rem 0.65rem;
    }
  }

  &__section {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex-wrap: wrap;

    &--actions {
      margin-left: auto;
      gap: 0.4rem;
    }

    @media (max-width: 768px) {
      gap: 0.4rem;
    }
  }

  &__label {
    font-weight: 600;
    color: var(--p-text-muted-color);
    font-size: 0.813rem;
    white-space: nowrap;
  }

  &__last-update {
    display: flex;
    align-items: center;
    font-size: 0.75rem;
    color: var(--p-text-muted-color);
    padding: 0.25rem 0.5rem;
    background: var(--p-surface-100);
    border-radius: var(--p-border-radius);
  }

  &__error {
    margin: 0;
    border-radius: 0;
    border: none;
    border-top: 1px solid var(--p-red-400);

    :deep(.p-message-close) {
      margin-left: 0.5rem;
    }
  }

  &__icon--disconnected {
    position: relative;

    &::after {
      content: '';
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%) rotate(45deg);
      width: 120%;
      height: 2px;
      background: currentColor;
    }
  }
}

// Анимация появления/исчезновения ошибки
.error-enter-active,
.error-leave-active {
  transition: all 0.3s ease;
}

.error-enter-from {
  opacity: 0;
  transform: translateY(-100%);
}

.error-leave-to {
  opacity: 0;
  transform: translateY(100%);
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

      &--actions {
        margin-left: 0;
        justify-content: flex-end;
      }
    }
  }
}
</style>
