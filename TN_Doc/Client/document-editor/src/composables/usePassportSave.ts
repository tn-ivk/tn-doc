import { useDocumentStore } from '@/stores/documentStore';
import { documentApi } from '@/services/api.service';
import { opcApi } from '@/services/opc.service';
import type { PassportEditConfig } from '@/types/passport.types';
import type { OpcDeviceParams } from './useOpcParams';

/**
 * Композабл для сохранения паспортов качества с поддержкой OPC тегов
 *
 * Логика работы (аналог старого EditDoc.js):
 * 1. Сохранить документ через SaveDoc
 * 2. Записать в OPC тег ARM.ARM_FillActAndPassport = true
 * 3. Polling тега ARM.ARM_FillActAndPassportResult (ждем увеличения значения)
 * 4. Объединить данные из localStorage ('dataPassport')
 * 5. Обновить документ через UpdateDoc
 *
 * OPC параметры теперь передаются из главного окна через URL query params,
 * а не получаются из конфигурации на бэкенде.
 */
export function usePassportSave() {
  const store = useDocumentStore();

  /**
   * Основная функция сохранения паспорта качества
   * @param opcParams OPC параметры устройства (из URL query params)
   * @returns true при успехе, false при ошибке
   */
  async function savePassportWithOpc(opcParams: OpcDeviceParams | null): Promise<boolean> {
    if (!store.config) {
      throw new Error('Конфигурация документа не загружена');
    }

    const config = store.config as PassportEditConfig;

    // Проверяем, что это паспорт (DocGUID == 1)
    const isPassport = config.docType === 'Passport';
    if (!isPassport) {
      console.warn('[usePassportSave] Документ не является паспортом, пропускаем OPC логику');
      // Для не-паспортов просто сохраняем
      await documentApi.saveDocument(
        config.deviceId,
        config.docType,
        config.docId,
        store.formData
      );
      return true;
    }

    // Шаг 1: Сохранить документ
    console.log('[usePassportSave] Шаг 1: Сохранение паспорта...');
    await documentApi.saveDocument(
      config.deviceId,
      config.docType,
      config.docId,
      store.formData
    );
    console.log('[usePassportSave] Шаг 1: Паспорт успешно сохранен');

    // Проверяем наличие OPC параметров
    if (!opcParams) {
      console.warn('[usePassportSave] Отсутствуют OPC параметры, пропускаем OPC логику');
      return true;
    }

    try {
      // Шаг 2: Записать в OPC тег ARM.ARM_FillActAndPassport = true
      console.log('[usePassportSave] Шаг 2: Запись в OPC тег ARM.ARM_FillActAndPassport...');
      const triggerTagName = opcApi.getFullTagName('ARM.ARM_FillActAndPassport', opcParams.tagPrefix);
      await opcApi.writeTag(
        opcParams.deviceName, // Имя устройства ("ИВК-1"), а не ID!
        triggerTagName,
        true,
        2, // namespaceIndex
        0  // indexArray для записи
      );
      console.log('[usePassportSave] Шаг 2: Тег успешно записан');

      // Шаг 3: Polling тега ARM.ARM_FillActAndPassportResult
      console.log('[usePassportSave] Шаг 3: Ожидание подтверждения от ИВК (polling)...');
      const resultTagName = opcApi.getFullTagName('ARM.ARM_FillActAndPassportResult', opcParams.tagPrefix);

      const pollingSuccess = await opcApi.pollTag(
        opcParams.deviceName, // Имя устройства ("ИВК-1"), а не ID!
        resultTagName,
        (currentValue: number, initialValue: number) => currentValue > initialValue,
        5000, // maxDuration (5 секунд)
        500,  // pollInterval (500 мс)
        2,    // namespaceIndex
        0     // indexArray для чтения
      );

      if (!pollingSuccess) {
        console.warn('[usePassportSave] Polling завершился по таймауту, документ сохранен, но ИВК не подтвердил запись');
        // Возвращаем false, чтобы показать пользователю предупреждение
        return false;
      }

      console.log('[usePassportSave] Шаг 3: ИВК подтвердил запись');

      // Шаг 4: Объединить данные из localStorage
      console.log('[usePassportSave] Шаг 4: Объединение данных из localStorage...');
      let mergedData = { ...store.formData };

      try {
        const storedPassportData = localStorage.getItem('dataPassport');
        if (storedPassportData) {
          const passportData = JSON.parse(storedPassportData);
          mergedData = {
            ...mergedData,
            ...passportData
          };
          console.log('[usePassportSave] Данные из localStorage успешно объединены');
        } else {
          console.log('[usePassportSave] Данные в localStorage отсутствуют');
        }
      } catch (error) {
        console.error('[usePassportSave] Ошибка при чтении/парсинге данных из localStorage:', error);
        // Продолжаем без объединения данных
      }

      // Шаг 5: Обновить документ через UpdateDoc
      console.log('[usePassportSave] Шаг 5: Обновление документа с объединенными данными...');
      await documentApi.updateDocument(
        config.deviceId,
        config.docType,
        config.docId,
        mergedData
      );
      console.log('[usePassportSave] Шаг 5: Документ успешно обновлен');

      return true;
    } catch (opcError: any) {
      console.error('[usePassportSave] Ошибка при работе с OPC:', opcError);
      // Документ сохранен, но OPC операция не удалась
      throw new Error(`Документ сохранен, но не удалось записать в ИВК: ${opcError.message}`);
    }
  }

  
  /**
   * Универсальная функция сохранения документа с поддержкой OPC
   * Автоматически выбирает нужную стратегию в зависимости от типа документа
   * @param opcParams OPC параметры устройства (из URL query params)
   */
  async function saveDocumentWithOpc(opcParams: OpcDeviceParams | null): Promise<boolean> {
    if (!store.config) {
      throw new Error('Конфигурация документа не загружена');
    }

    const docType = store.config.docType;

    // Для паспортов используем полную логику с polling
    if (docType === 'Passport') {
      return await savePassportWithOpc(opcParams);
    }

    
    // Для остальных документов (Report, Jornal и т.д.) просто сохраняем
    console.log(`[usePassportSave] Сохранение документа типа ${docType} без OPC логики`);
    await documentApi.saveDocument(
      store.config.deviceId,
      store.config.docType,
      store.config.docId,
      store.formData
    );
    return true;
  }

  return {
    savePassportWithOpc,
    saveDocumentWithOpc
  };
}
