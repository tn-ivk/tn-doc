import { logger } from '@tn-doc/shared';
import { watch } from 'vue';
import { useDocumentStore } from '@/stores/documentStore';
import { useFieldHistory } from '@/composables/useFieldHistory';
import type { UserData } from '@/types/document.types';
import { normalizeValue } from '@/utils/passport-utils';

/**
 * Композабл для автозаполнения связанных полей в документе "Паспорт качества"
 *
 * Логика:
 * - При выборе подписанта (Laboratory_IOF/Delive_IOF/Receive_IOF) автоматически заполняются:
 *   * Factory (организация)
 *   * Post (должность)
 * - Проверяется возврат к оригинальным значениям ЕЛИС:
 *   * Если Post/Factory вернулись к elisOriginal → trackReturnToElis + elisFilled=true
 *   * Если изменились → trackManualChange + elisFilled=false
 */
export function usePassportAutoFill() {
  const store = useDocumentStore();
  const { trackManualChange, trackReturnToElis } = useFieldHistory();

  /**
   * Найти опцию поля по значению
   */
  const findFieldOption = (fieldKey: string, value: string) => {
    const field = store.fields.find(f => f.key === fieldKey);
    return field?.options?.find(opt => opt.value === value);
  };

  /**
   * Умная запись истории изменения поля
   * Проверяет, вернулось ли значение к оригинальному из ЕЛИС
   *
   * @param fieldKey - ключ поля (например, "Laboratory_Factory")
   * @param newValue - новое значение
   * @param previousValue - предыдущее значение
   * @returns true если произошел возврат к ЕЛИС, false если ручное изменение
   */
  const trackFieldChange = (
    fieldKey: string,
    newValue: string,
    previousValue: string
  ): boolean => {
    // Проверяем наличие оригинального значения ЕЛИС
    const elisOriginal = store.formData[`${fieldKey}__elisOriginal`];

    // Если идет загрузка из ELIS - не пишем историю, но возвращаем результат сравнения
    // чтобы корректно проставить флаг elisFilled.
    if (store.isLoadingFromElis) {
      if (elisOriginal === undefined) {
        return false;
      }
      const normalizedNew = normalizeValue(newValue);
      const normalizedOriginal = normalizeValue(elisOriginal);
      return normalizedNew === normalizedOriginal;
    }

    if (elisOriginal === undefined) {
      // Поле никогда не заполнялось из ЕЛИС - обычное ручное изменение
      trackManualChange(fieldKey, newValue, previousValue);
      return false;
    }

    // Нормализуем значения для корректного сравнения
    const normalizedNew = normalizeValue(newValue);
    const normalizedOriginal = normalizeValue(elisOriginal);

    const isReturnToElis = normalizedNew === normalizedOriginal;

    if (isReturnToElis) {
      // Значение вернулось к оригинальному из ЕЛИС
      trackReturnToElis(fieldKey, newValue, previousValue);
      logger.debug('[PassportAutoFill] Возврат к значению ЕЛИС', {
        fieldKey,
        elisOriginal,
        newValue
      });
    } else {
      // Обычное ручное изменение
      trackManualChange(fieldKey, newValue, previousValue);
    }

    return isReturnToElis;
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

      // Проверяем возврат к ЕЛИС для каждого поля
      const isFactoryReturnToElis = trackFieldChange(
        'Laboratory_Factory',
        newFactory,
        previousFactory
      );
      const isPostReturnToElis = trackFieldChange(
        'Laboratory_Post',
        newPost,
        previousPost
      );

      // Обновляем поля и флаги одной операцией
      store.bulkUpdateFields({
        'Laboratory_Factory': newFactory,
        'Laboratory_Factory__elisFilled': isFactoryReturnToElis,
        'Laboratory_Post': newPost,
        'Laboratory_Post__elisFilled': isPostReturnToElis
      });

      logger.debug('[PassportAutoFill] Автозаполнение полей представителя лаборатории:', {
        factory: newFactory,
        factoryReturnToElis: isFactoryReturnToElis,
        post: newPost,
        postReturnToElis: isPostReturnToElis
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

      // Проверяем возврат к ЕЛИС для каждого поля
      const isFactoryReturnToElis = trackFieldChange(
        'Delive_Factory',
        newFactory,
        previousFactory
      );
      const isPostReturnToElis = trackFieldChange(
        'Delive_Post',
        newPost,
        previousPost
      );

      // Обновляем поля и флаги одной операцией
      store.bulkUpdateFields({
        'Delive_Factory': newFactory,
        'Delive_Factory__elisFilled': isFactoryReturnToElis,
        'Delive_Post': newPost,
        'Delive_Post__elisFilled': isPostReturnToElis
      });

      logger.debug('[PassportAutoFill] Автозаполнение полей представителя сдающей стороны:', {
        factory: newFactory,
        factoryReturnToElis: isFactoryReturnToElis,
        post: newPost,
        postReturnToElis: isPostReturnToElis
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

      // Проверяем возврат к ЕЛИС для каждого поля
      const isFactoryReturnToElis = trackFieldChange(
        'Receive_Factory',
        newFactory,
        previousFactory
      );
      const isPostReturnToElis = trackFieldChange(
        'Receive_Post',
        newPost,
        previousPost
      );

      // Обновляем поля и флаги одной операцией
      store.bulkUpdateFields({
        'Receive_Factory': newFactory,
        'Receive_Factory__elisFilled': isFactoryReturnToElis,
        'Receive_Post': newPost,
        'Receive_Post__elisFilled': isPostReturnToElis
      });

      logger.debug('[PassportAutoFill] Автозаполнение полей представителя принимающей стороны:', {
        factory: newFactory,
        factoryReturnToElis: isFactoryReturnToElis,
        post: newPost,
        postReturnToElis: isPostReturnToElis
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
