import { logger } from '@tn-doc/shared';
import { computed } from 'vue';
import { useRoute } from 'vue-router';

/**
 * Интерфейс OPC параметров устройства
 */
export interface OpcDeviceParams {
  /** ID устройства (используется для SaveDoc/UpdateDoc на бэкенде) */
  deviceGuid: string;
  /** Имя устройства (используется для WriteTag/ReadTag в TN_MessagingService, например: "ИВК-1", "ИВК-2") */
  deviceName: string;
  /** Префикс тега для OPC (например: "IVK_TN_01", "IVK_TN_02") */
  tagPrefix: string;
}

/**
 * Композабл для получения OPC параметров из URL query params
 *
 * Параметры передаются главным окном при открытии iframe с редактором:
 * /document-editor/1/Passport/123?deviceName=IVK-1&deviceGuid=1&tagPrefix=IVK
 *
 * Аналогично старой версии, где параметры передавались в SaveDoc():
 * SaveDoc(NameDevice, GuidDevice, DocGUID, IdDoc, PrefixTag)
 */
export function useOpcParams() {
  const route = useRoute();

  /**
   * OPC параметры устройства из URL
   */
  const opcParams = computed<OpcDeviceParams | null>(() => {
    const deviceName = route.query.deviceName as string;
    const deviceGuid = route.query.deviceGuid as string;
    const tagPrefix = route.query.tagPrefix as string;

    // Проверяем наличие всех обязательных параметров
    if (!deviceName || !deviceGuid || !tagPrefix) {
      logger.warn('[useOpcParams] Отсутствуют OPC параметры в URL:', {
        deviceName,
        deviceGuid,
        tagPrefix
      });
      return null;
    }

    return {
      deviceGuid,
      deviceName,
      tagPrefix
    };
  });

  /**
   * Доступны ли OPC параметры
   */
  const hasOpcParams = computed(() => opcParams.value !== null);

  /**
   * Логирование OPC параметров (для отладки)
   */
  function logOpcParams() {
    if (opcParams.value) {
      logger.debug('[useOpcParams] OPC параметры получены:', opcParams.value);
    } else {
      logger.warn('[useOpcParams] OPC параметры отсутствуют в URL');
    }
  }

  return {
    opcParams,
    hasOpcParams,
    logOpcParams
  };
}
