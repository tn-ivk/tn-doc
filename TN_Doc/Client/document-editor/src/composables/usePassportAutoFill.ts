import { logger } from '@tn-doc/shared';
import { watch } from 'vue';
import { useDocumentStore } from '@/stores/documentStore';
import { useFieldHistory } from '@/composables/useFieldHistory';
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
  const { trackManualChange } = useFieldHistory();

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
    const previousFactory = store.formData['Laboratory_Factory'];
    const previousPost = store.formData['Laboratory_Post'];

    if (!newValue) {
      // Очищаем связанные поля
      store.updateField('Laboratory_Factory', '');
      store.updateField('Laboratory_Post', '');

      // Создаем записи истории для очистки полей
      trackManualChange('Laboratory_Factory', '', previousFactory);
      trackManualChange('Laboratory_Post', '', previousPost);
      return;
    }

    const option = findFieldOption('Laboratory_IOF', newValue);
    if (option?.data) {
      const userData = option.data as UserData;
      const newFactory = userData.factory || '';
      const newPost = userData.post || '';

      // Заполняем связанные поля из данных пользователя
      store.updateField('Laboratory_Factory', newFactory);
      store.updateField('Laboratory_Post', newPost);

      // Создаем записи истории для автозаполненных полей
      trackManualChange('Laboratory_Factory', newFactory, previousFactory);
      trackManualChange('Laboratory_Post', newPost, previousPost);

      logger.debug('[PassportAutoFill] Автозаполнение полей представителя лаборатории:', {
        factory: newFactory,
        post: newPost
      });
    }
  };

  /**
   * Автозаполнение связанных полей при выборе представителя сдающей стороны
   */
  const handleDeliveIOFChange = (newValue: string) => {
    const previousFactory = store.formData['Delive_Factory'];
    const previousPost = store.formData['Delive_Post'];

    if (!newValue) {
      // Очищаем связанные поля
      store.updateField('Delive_Factory', '');
      store.updateField('Delive_Post', '');

      // Создаем записи истории для очистки полей
      trackManualChange('Delive_Factory', '', previousFactory);
      trackManualChange('Delive_Post', '', previousPost);
      return;
    }

    const option = findFieldOption('Delive_IOF', newValue);
    if (option?.data) {
      const userData = option.data as UserData;
      const newFactory = userData.factory || '';
      const newPost = userData.post || '';

      // Заполняем связанные поля из данных пользователя
      store.updateField('Delive_Factory', newFactory);
      store.updateField('Delive_Post', newPost);

      // Создаем записи истории для автозаполненных полей
      trackManualChange('Delive_Factory', newFactory, previousFactory);
      trackManualChange('Delive_Post', newPost, previousPost);

      logger.debug('[PassportAutoFill] Автозаполнение полей представителя сдающей стороны:', {
        factory: newFactory,
        post: newPost
      });
    }
  };

  /**
   * Автозаполнение связанных полей при выборе представителя принимающей стороны
   */
  const handleReceiveIOFChange = (newValue: string) => {
    const previousFactory = store.formData['Receive_Factory'];
    const previousPost = store.formData['Receive_Post'];

    if (!newValue) {
      // Очищаем связанные поля
      store.updateField('Receive_Factory', '');
      store.updateField('Receive_Post', '');

      // Создаем записи истории для очистки полей
      trackManualChange('Receive_Factory', '', previousFactory);
      trackManualChange('Receive_Post', '', previousPost);
      return;
    }

    const option = findFieldOption('Receive_IOF', newValue);
    if (option?.data) {
      const userData = option.data as UserData;
      const newFactory = userData.factory || '';
      const newPost = userData.post || '';

      // Заполняем связанные поля из данных пользователя
      store.updateField('Receive_Factory', newFactory);
      store.updateField('Receive_Post', newPost);

      // Создаем записи истории для автозаполненных полей
      trackManualChange('Receive_Factory', newFactory, previousFactory);
      trackManualChange('Receive_Post', newPost, previousPost);

      logger.debug('[PassportAutoFill] Автозаполнение полей представителя принимающей стороны:', {
        factory: newFactory,
        post: newPost
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
