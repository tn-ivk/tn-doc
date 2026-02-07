import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
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
  const selectedDeviceId = ref<string | null>(null);

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

  const selectedDevice = computed<DeviceStatus | null>(() => {
    if (!selectedDeviceId.value) return null;
    return devices.value.find(d => d.id === selectedDeviceId.value) ?? null;
  });

  // Actions
  async function fetchStatus() {
    try {
      isLoading.value = true;
      error.value = null;

      const response = await apiClient.get<StatusResponse>('/api/status');

      devices.value = response.devices;
      services.value = response.services;
      lastUpdate.value = new Date();

      // Keep last 10 updates in history
      updateHistory.value.unshift(response);
      if (updateHistory.value.length > 10) {
        updateHistory.value.pop();
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Ошибка загрузки статуса';
      console.error('Failed to fetch status:', err);

      // При ошибке соединения с сервером устанавливаем все индикаторы в offline с ошибкой
      devices.value = devices.value.map(d => ({
        ...d,
        isConnected: false,
        isFullyConnected: false,
        error: 'Нет связи с сервером',
        channels: d.channels?.map(ch => ({
          ...ch,
          isConnected: false,
          error: 'Нет связи с сервером'
        })) || []
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

  function openDeviceDiagnostics(deviceId: string) {
    selectedDeviceId.value = deviceId;
  }

  function closeDeviceDiagnostics() {
    selectedDeviceId.value = null;
  }

  async function retryDevice(deviceId: string): Promise<DeviceStatus> {
    const response = await apiClient.post<DeviceStatus>(
      `/api/status/device/${deviceId}/retry`
    );

    // Обновляем устройство в списке
    const index = devices.value.findIndex(d => d.id === deviceId);
    if (index !== -1) {
      devices.value[index] = response;
    }

    return response;
  }

  return {
    // State
    devices,
    services,
    lastUpdate,
    isLoading,
    error,
    updateHistory,
    selectedDeviceId,
    // Getters
    allDevicesConnected,
    criticalServicesConnected,
    overallHealth,
    healthyDevicesCount,
    selectedDevice,
    // Actions
    fetchStatus,
    updateFromSignalR,
    clearError,
    openDeviceDiagnostics,
    closeDeviceDiagnostics,
    retryDevice
  };
});