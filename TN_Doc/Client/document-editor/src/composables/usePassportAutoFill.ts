import { watch } from 'vue';
import { useDocumentStore } from '@/stores/documentStore';
import type { UserData } from '@/types/document.types';

/**
 * Композабл для автозаполнения связанных полей в документе "Паспорт качества"
 *
 * Логика:
 * - При выборе подписанта (Laboratory_IOF/Delive_IOF/Receive_IOF) автоматически заполняются:
 *   * Factory (организация)
 *   * Post (должность)
 */
export function usePassportAutoFill() {
  const store = useDocumentStore();

  /**
   * Найти опцию поля по значению
   */
  const findFieldOption = (fieldKey: string, value: string) => {
    const field = store.fields.find(f => f.key === fieldKey);
    return field?.options?.find(opt => opt.value === value);
  };

  /**
   * Автозаполнение связанных полей при выборе представителя лаборатории
   */
  const handleLaboratoryIOFChange = (newValue: string) => {
    if (!newValue) {
      // Очищаем связанные поля
      store.updateField('Laboratory_Factory', '');
      store.updateField('Laboratory_Post', '');
      return;
    }

    const option = findFieldOption('Laboratory_IOF', newValue);
    if (option?.data) {
      const userData = option.data as UserData;

      // Заполняем связанные поля из данных пользователя
      store.updateField('Laboratory_Factory', userData.factory || '');
      store.updateField('Laboratory_Post', userData.post || '');

      console.log('[PassportAutoFill] Автозаполнение полей представителя лаборатории:', {
        factory: userData.factory,
        post: userData.post
      });
    }
  };

  /**
   * Автозаполнение связанных полей при выборе представителя сдающей стороны
   */
  const handleDeliveIOFChange = (newValue: string) => {
    if (!newValue) {
      // Очищаем связанные поля
      store.updateField('Delive_Factory', '');
      store.updateField('Delive_Post', '');
      return;
    }

    const option = findFieldOption('Delive_IOF', newValue);
    if (option?.data) {
      const userData = option.data as UserData;

      // Заполняем связанные поля из данных пользователя
      store.updateField('Delive_Factory', userData.factory || '');
      store.updateField('Delive_Post', userData.post || '');

      console.log('[PassportAutoFill] Автозаполнение полей представителя сдающей стороны:', {
        factory: userData.factory,
        post: userData.post
      });
    }
  };

  /**
   * Автозаполнение связанных полей при выборе представителя принимающей стороны
   */
  const handleReceiveIOFChange = (newValue: string) => {
    if (!newValue) {
      // Очищаем связанные поля
      store.updateField('Receive_Factory', '');
      store.updateField('Receive_Post', '');
      return;
    }

    const option = findFieldOption('Receive_IOF', newValue);
    if (option?.data) {
      const userData = option.data as UserData;

      // Заполняем связанные поля из данных пользователя
      store.updateField('Receive_Factory', userData.factory || '');
      store.updateField('Receive_Post', userData.post || '');

      console.log('[PassportAutoFill] Автозаполнение полей представителя принимающей стороны:', {
        factory: userData.factory,
        post: userData.post
      });
    }
  };

  /**
   * Установка watchers для автозаполнения
   */
  const setupAutoFillWatchers = () => {
    // Отслеживаем изменения поля Laboratory_IOF (представитель лаборатории)
    watch(() => store.formData['Laboratory_IOF'], (newValue) => {
      handleLaboratoryIOFChange(newValue);
    });

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
    handleLaboratoryIOFChange,
    handleDeliveIOFChange,
    handleReceiveIOFChange
  };
}
