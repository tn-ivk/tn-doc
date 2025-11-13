import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { logger } from '@tn-doc/shared';
import type {
  StatusResponse,
  DeviceStatus,
  ServiceStatus,
  HealthStatus
} from '../types/status.types';
import { apiClient } from '@shared/api/ApiClient';

export const useStatusStore = defineStore('status', () => {
  // State
  const devices = ref<DeviceStatus[]>([]);
  const services = ref<ServiceStatus>({
    messagingService: { isConnected: false }
  });
  const lastUpdate = ref<Date | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);
  const updateHistory = ref<StatusResponse[]>([]);

  // Getters
  const allDevicesConnected = computed(() =>
    devices.value.length > 0 && devices.value.every(d => d.isConnected)
  );

  const criticalServicesConnected = computed(() =>
    services.value.messagingService.isConnected
  );

  const overallHealth = computed<HealthStatus>(() => {
    if (!criticalServicesConnected.value) return 'critical';
    if (!allDevicesConnected.value) return 'warning';
    return 'healthy';
  });

  const healthyDevicesCount = computed(() =>
    devices.value.filter(d => d.isConnected).length
  );

  // Actions
  async function fetchStatus() {
    try {
      isLoading.value = true;
      error.value = null;

      logger.debug('StatusStore: запрос статусов устройств');

      const response = await apiClient.get<StatusResponse>('/api/status');

      devices.value = response.devices;
      services.value = response.services;
      lastUpdate.value = new Date();

      logger.info('StatusStore: статусы успешно загружены', {
        deviceCount: response.devices.length,
        connectedDevices: response.devices.filter(d => d.isConnected).length
      });

      // Keep last 10 updates in history
      updateHistory.value.unshift(response);
      if (updateHistory.value.length > 10) {
        updateHistory.value.pop();
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Ошибка загрузки статуса';
      logger.error('StatusStore: ошибка загрузки статусов', {
        error: err instanceof Error ? err.message : String(err)
      });

      // При ошибке соединения с сервером устанавливаем все индикаторы в offline с ошибкой
      devices.value = devices.value.map(d => ({
        ...d,
        isConnected: false,
        error: 'Нет связи с сервером'
      }));

      services.value = {
        messagingService: { isConnected: false, error: 'Нет связи с сервером' },
        ...(services.value.elis && { elis: { isConnected: false, error: 'Нет связи с сервером' } })
      };

      lastUpdate.value = new Date();
    } finally {
      isLoading.value = false;
    }
  }

  function updateFromSignalR(data: StatusResponse) {
    devices.value = data.devices;
    services.value = data.services;
    lastUpdate.value = new Date();

    // Add to history
    updateHistory.value.unshift(data);
    if (updateHistory.value.length > 10) {
      updateHistory.value.pop();
    }
  }

  function clearError() {
    error.value = null;
  }

  return {
    // State
    devices,
    services,
    lastUpdate,
    isLoading,
    error,
    updateHistory,
    // Getters
    allDevicesConnected,
    criticalServicesConnected,
    overallHealth,
    healthyDevicesCount,
    // Actions
    fetchStatus,
    updateFromSignalR,
    clearError
  };
});