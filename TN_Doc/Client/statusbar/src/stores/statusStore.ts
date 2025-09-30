import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { StatusResponse, DeviceStatus, ServiceStatus } from '../types/status.types';
import { apiClient } from '@shared/api/ApiClient';

export const useStatusStore = defineStore('status', () => {
  const devices = ref<DeviceStatus[]>([]);
  const services = ref<ServiceStatus>({
    messagingService: { isConnected: false }
  });
  const lastUpdate = ref<Date | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);
  const updateHistory = ref<StatusResponse[]>([]);

  const allDevicesConnected = computed(() =>
    devices.value.every(d => d.isConnected)
  );

  const criticalServicesConnected = computed(() =>
    services.value.messagingService.isConnected
  );

  const overallHealth = computed(() => {
    if (!criticalServicesConnected.value) return 'critical';
    if (!allDevicesConnected.value) return 'warning';
    return 'healthy';
  });

  async function fetchStatus() {
    try {
      isLoading.value = true;
      error.value = null;

      const response = await apiClient.get<StatusResponse>('/api/status');

      devices.value = response.devices;
      services.value = response.services;
      lastUpdate.value = new Date();

      updateHistory.value.unshift(response);
      if (updateHistory.value.length > 10) {
        updateHistory.value.pop();
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Unknown error';
      console.error('Failed to fetch status:', err);
    } finally {
      isLoading.value = false;
    }
  }

  function updateFromSignalR(data: StatusResponse) {
    devices.value = data.devices;
    services.value = data.services;
    lastUpdate.value = new Date();
  }

  return {
    devices,
    services,
    lastUpdate,
    isLoading,
    error,
    updateHistory,
    allDevicesConnected,
    criticalServicesConnected,
    overallHealth,
    fetchStatus,
    updateFromSignalR
  } as const;
});


