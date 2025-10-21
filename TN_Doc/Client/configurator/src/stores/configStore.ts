import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { CfgApp, Device, ValidationResult } from '../types/config.types';
import apiService from '../services/api.service';
import _ from 'lodash';

export const useConfigStore = defineStore('config', () => {
  // State
  const originalConfig = ref<CfgApp | null>(null);
  const currentConfig = ref<CfgApp | null>(null);
  const selectedDeviceIds = ref<number[]>([]);
  const isLoading = ref(false);
  const isSaving = ref(false);
  const error = ref<string | null>(null);
  const validationErrors = ref<Map<string, string>>(new Map());

  // Хранилище для измененных конфигураций документов
  const dirtyDocumentConfigs = ref<Map<string, string>>(new Map());

  // Computed
  const isDirty = computed(() => {
    if (!originalConfig.value || !currentConfig.value) return false;
    // Проверяем изменения в основной конфигурации
    const configDirty = !_.isEqual(originalConfig.value, currentConfig.value);
    // Проверяем изменения в конфигурациях документов
    const docConfigsDirty = dirtyDocumentConfigs.value.size > 0;
    return configDirty || docConfigsDirty;
  });

  const selectedDevices = computed<Device[]>(() => {
    if (!currentConfig.value || selectedDeviceIds.value.length === 0) {
      return [];
    }
    return currentConfig.value.Devices.filter(d =>
      selectedDeviceIds.value.includes(d.IdDevice)
    );
  });

  const hasMultipleSelection = computed(() => selectedDeviceIds.value.length > 1);

  // Actions
  async function loadConfig() {
    isLoading.value = true;
    error.value = null;

    try {
      const config = await apiService.getConfig();
      originalConfig.value = _.cloneDeep(config);
      currentConfig.value = _.cloneDeep(config);
    } catch (e: any) {
      error.value = e.message || 'Не удалось загрузить конфигурацию';
      throw e;
    } finally {
      isLoading.value = false;
    }
  }

  async function saveConfig() {
    if (!currentConfig.value) {
      throw new Error('Нет конфигурации для сохранения');
    }

    isSaving.value = true;
    error.value = null;

    try {
      // Валидация перед сохранением
      const validationResult = await apiService.validateConfig(currentConfig.value);
      if (!validationResult.IsValid) {
        const errorMessage = validationResult.Errors.join('\n');
        error.value = errorMessage;
        throw new Error(errorMessage);
      }

      // Сохранение основной конфигурации
      await apiService.saveConfig(currentConfig.value);

      // Сохранение измененных конфигураций документов
      for (const [path, content] of dirtyDocumentConfigs.value.entries()) {
        await apiService.saveDocumentConfig(path, content);
      }

      // Обновляем оригинальную конфигурацию и очищаем dirty state
      originalConfig.value = _.cloneDeep(currentConfig.value);
      dirtyDocumentConfigs.value.clear();
    } catch (e: any) {
      error.value = e.message || 'Не удалось сохранить конфигурацию';
      throw e;
    } finally {
      isSaving.value = false;
    }
  }

  function updateGeneralSettings(settings: Partial<CfgApp>) {
    if (!currentConfig.value) return;

    currentConfig.value = {
      ...currentConfig.value,
      ...settings
    };
  }

  function updateDeviceSettings<K extends keyof Device>(
    deviceId: number,
    field: K,
    value: Device[K]
  ) {
    if (!currentConfig.value) return;

    const deviceIndex = currentConfig.value.Devices.findIndex(
      d => d.IdDevice === deviceId
    );

    if (deviceIndex !== -1) {
      currentConfig.value.Devices[deviceIndex][field] = value;
    }
  }

  function updateMultipleDevicesSettings<K extends keyof Device>(
    deviceIds: number[],
    field: K,
    value: Device[K]
  ) {
    if (!currentConfig.value) return;

    deviceIds.forEach(deviceId => {
      updateDeviceSettings(deviceId, field, value);
    });
  }

  function selectDevices(ids: number[]) {
    selectedDeviceIds.value = ids;
  }

  async function validateConfig(): Promise<ValidationResult> {
    if (!currentConfig.value) {
      return {
        IsValid: false,
        Errors: ['Нет конфигурации для валидации'],
        Warnings: []
      };
    }

    try {
      const result = await apiService.validateConfig(currentConfig.value);
      return result;
    } catch (e: any) {
      return {
        IsValid: false,
        Errors: [e.message || 'Ошибка валидации'],
        Warnings: []
      };
    }
  }

  function updateDocumentTemplate(
    deviceId: number,
    docId: number,
    templateId: number,
    use: boolean
  ) {
    if (!currentConfig.value) return;

    const device = currentConfig.value.Devices.find(d => d.IdDevice === deviceId);
    if (!device) return;

    const doc = device.Docs.find(d => d.IdDoc === docId);
    if (!doc || !doc.TemplateDocs) return;

    const template = doc.TemplateDocs.find(t => t.Id === templateId);
    if (template) {
      template.Use = use;
    }
  }

  function resetConfig() {
    if (originalConfig.value) {
      currentConfig.value = _.cloneDeep(originalConfig.value);
    }
    // Сбрасываем изменения в конфигурациях документов
    dirtyDocumentConfigs.value.clear();
  }

  async function loadDocumentConfig(configPath: string): Promise<string> {
    try {
      const content = await apiService.loadDocumentConfig(configPath);
      // Форматируем JSON для удобного чтения
      const parsed = JSON.parse(content);
      return JSON.stringify(parsed, null, 2);
    } catch (e: any) {
      throw new Error(`Не удалось загрузить конфигурацию: ${e.message}`);
    }
  }

  async function saveDocumentConfig(configPath: string, content: string): Promise<void> {
    try {
      await apiService.saveDocumentConfig(configPath, content);
      // Удаляем из dirty state после успешного сохранения
      dirtyDocumentConfigs.value.delete(configPath);
    } catch (e: any) {
      throw new Error(`Не удалось сохранить конфигурацию: ${e.message}`);
    }
  }

  function markDocumentConfigDirty(configPath: string, content: string) {
    dirtyDocumentConfigs.value.set(configPath, content);
  }

  function clearDocumentConfigDirty(configPath: string) {
    dirtyDocumentConfigs.value.delete(configPath);
  }

  return {
    // State
    originalConfig,
    currentConfig,
    selectedDeviceIds,
    isLoading,
    isSaving,
    error,
    validationErrors,

    // Computed
    isDirty,
    selectedDevices,
    hasMultipleSelection,

    // Actions
    loadConfig,
    saveConfig,
    updateGeneralSettings,
    updateDeviceSettings,
    updateMultipleDevicesSettings,
    updateDocumentTemplate,
    selectDevices,
    validateConfig,
    resetConfig,
    loadDocumentConfig,
    saveDocumentConfig,
    markDocumentConfigDirty,
    clearDocumentConfigDirty
  };
});
