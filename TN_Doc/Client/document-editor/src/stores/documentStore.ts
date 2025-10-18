import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { documentApi } from '@/services/api.service';
import type { DocumentEditConfig, FormField } from '@/types/document.types';

/**
 * Store для управления состоянием документа
 */
export const useDocumentStore = defineStore('document', () => {
  // State
  const config = ref<DocumentEditConfig | null>(null);
  const formData = ref<Record<string, any>>({});
  const isDirty = ref(false);
  const isLoading = ref(false);
  const isSaving = ref(false);
  const error = ref<string | null>(null);

  // Getters
  const isReady = computed(() => config.value !== null && !isLoading.value);
  const hasUnsavedChanges = computed(() => isDirty.value);
  const documentTitle = computed(() => config.value?.title || 'Загрузка...');
  const fields = computed(() => config.value?.fields || []);

  /**
   * Загрузить конфигурацию документа
   */
  async function loadConfig(deviceId: number, docType: string, id: number) {
    isLoading.value = true;
    error.value = null;

    try {
      const loadedConfig = await documentApi.getEditConfig(deviceId, docType, id);
      config.value = loadedConfig;

      // Инициализируем formData начальными значениями
      formData.value = { ...loadedConfig.initialValues };
      isDirty.value = false;

      console.log('[DocumentStore] Конфигурация загружена:', loadedConfig);
    } catch (err: any) {
      error.value = err.response?.data?.error || err.message || 'Не удалось загрузить конфигурацию документа';
      console.error('[DocumentStore] Ошибка загрузки конфигурации:', err);
      throw err;
    } finally {
      isLoading.value = false;
    }
  }

  /**
   * Обновить значение поля формы
   */
  function updateField(key: string, value: any) {
    formData.value[key] = value;
    isDirty.value = true;
    console.log(`[DocumentStore] Поле "${key}" обновлено:`, value);
  }

  /**
   * Сохранить документ
   */
  async function saveDocument() {
    if (!config.value) {
      throw new Error('Конфигурация документа не загружена');
    }

    isSaving.value = true;
    error.value = null;

    try {
      const response = await documentApi.saveDocument(
        config.value.deviceId,
        config.value.docType,
        config.value.docId,
        formData.value
      );

      if (response.success) {
        isDirty.value = false;
        console.log('[DocumentStore] Документ успешно сохранён');
        return response;
      } else {
        throw new Error(response.error || 'Не удалось сохранить документ');
      }
    } catch (err: any) {
      error.value = err.response?.data?.error || err.message || 'Ошибка при сохранении документа';
      console.error('[DocumentStore] Ошибка сохранения:', err);
      throw err;
    } finally {
      isSaving.value = false;
    }
  }

  /**
   * Сбросить состояние store
   */
  function reset() {
    config.value = null;
    formData.value = {};
    isDirty.value = false;
    isLoading.value = false;
    isSaving.value = false;
    error.value = null;
  }

  return {
    // State
    config,
    formData,
    isDirty,
    isLoading,
    isSaving,
    error,

    // Getters
    isReady,
    hasUnsavedChanges,
    documentTitle,
    fields,

    // Actions
    loadConfig,
    updateField,
    saveDocument,
    reset
  };
});
