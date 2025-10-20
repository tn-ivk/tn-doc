import { watch } from 'vue';
import { useDocumentStore } from '@/stores/documentStore';
import type { UserData, License } from '@/types/document.types';

/**
 * Композабл для автозаполнения связанных полей в документе "Акт"
 *
 * Логика:
 * - При выборе подписанта (Delive_IOF/Receive_IOF) автоматически заполняются:
 *   * Factory (организация)
 *   * FIO (полное ФИО)
 *   * Lic_Date и Lic_Number (данные доверенности)
 */
export function useActAutoFill() {
  const store = useDocumentStore();

  /**
   * Найти опцию поля по значению
   */
  const findFieldOption = (fieldKey: string, value: string) => {
    const field = store.fields.find(f => f.key === fieldKey);
    return field?.options?.find(opt => opt.value === value);
  };

  /**
   * Найти лицензию по ID
   */
  const findLicense = (licId: number): License | undefined => {
    return store.config?.dictionaries?.licenses?.find(l => l.id === licId);
  };

  /**
   * Автозаполнение связанных полей при выборе подписанта сдающей стороны
   */
  const handleDeliveIOFChange = (newValue: string) => {
    if (!newValue) {
      // Очищаем связанные поля
      store.updateField('Delive_Factory', '');
      store.updateField('Delive_FIO', '');
      store.updateField('Delive_Lic_Date', '');
      store.updateField('Delive_Lic_Number', '');
      return;
    }

    const option = findFieldOption('Delive_IOF', newValue);
    if (option?.data) {
      const userData = option.data as UserData;

      // Заполняем связанные поля из данных пользователя
      store.updateField('Delive_Factory', userData.factory || '');
      store.updateField('Delive_FIO', userData.fio || '');

      // Находим и заполняем данные лицензии
      if (userData.licId) {
        const license = findLicense(userData.licId);
        if (license) {
          store.updateField('Delive_Lic_Date', license.licensesDate || '');
          store.updateField('Delive_Lic_Number', license.licensesNumber || '');
        } else {
          store.updateField('Delive_Lic_Date', '');
          store.updateField('Delive_Lic_Number', '');
        }
      } else {
        store.updateField('Delive_Lic_Date', '');
        store.updateField('Delive_Lic_Number', '');
      }

      console.log('[ActAutoFill] Автозаполнение полей сдающей стороны:', {
        factory: userData.factory,
        fio: userData.fio,
        licId: userData.licId
      });
    }
  };

  /**
   * Автозаполнение связанных полей при выборе подписанта принимающей стороны
   */
  const handleReceiveIOFChange = (newValue: string) => {
    if (!newValue) {
      // Очищаем связанные поля
      store.updateField('Receive_Factory', '');
      store.updateField('Receive_FIO', '');
      store.updateField('Receive_Lic_Date', '');
      store.updateField('Receive_Lic_Number', '');
      return;
    }

    const option = findFieldOption('Receive_IOF', newValue);
    if (option?.data) {
      const userData = option.data as UserData;

      // Заполняем связанные поля из данных пользователя
      store.updateField('Receive_Factory', userData.factory || '');
      store.updateField('Receive_FIO', userData.fio || '');

      // Находим и заполняем данные лицензии
      if (userData.licId) {
        const license = findLicense(userData.licId);
        if (license) {
          store.updateField('Receive_Lic_Date', license.licensesDate || '');
          store.updateField('Receive_Lic_Number', license.licensesNumber || '');
        } else {
          store.updateField('Receive_Lic_Date', '');
          store.updateField('Receive_Lic_Number', '');
        }
      } else {
        store.updateField('Receive_Lic_Date', '');
        store.updateField('Receive_Lic_Number', '');
      }

      console.log('[ActAutoFill] Автозаполнение полей принимающей стороны:', {
        factory: userData.factory,
        fio: userData.fio,
        licId: userData.licId
      });
    }
  };

  /**
   * Установка watchers для автозаполнения
   */
  const setupAutoFillWatchers = () => {
    // Отслеживаем изменения поля Delive_IOF (сдающая сторона)
    watch(() => store.formData['Delive_IOF'], (newValue) => {
      handleDeliveIOFChange(newValue);
    });

    // Отслеживаем изменения поля Receive_IOF (принимающая сторона)
    watch(() => store.formData['Receive_IOF'], (newValue) => {
      handleReceiveIOFChange(newValue);
    });
  };

  return {
    setupAutoFillWatchers,
    handleDeliveIOFChange,
    handleReceiveIOFChange
  };
}
