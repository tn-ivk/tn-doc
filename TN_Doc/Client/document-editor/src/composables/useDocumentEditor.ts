import { ref, computed, onMounted, onBeforeUnmount } from 'vue';
import { useToast } from 'primevue/usetoast';
import { logger } from '@tn-doc/shared';
import { useDocumentStore } from '@/stores/documentStore';
import { usePassportSave } from './usePassportSave';
import { useOpcParams } from './useOpcParams';

/**
 * Композабл с общей логикой редактирования документов
 * Переиспользуется в DocumentEditor и DocumentActEditor
 */
export function useDocumentEditor() {
  const store = useDocumentStore();
  const toast = useToast();
  const { saveDocumentWithOpc } = usePassportSave();
  const { opcParams } = useOpcParams();

  /**
   * Восстанавливает elisOriginal для полей с elisFilled=true при загрузке документа.
   *
   * Это необходимо для корректной работы механизма "возврата к значению ELIS":
   * - При первичной загрузке протокола ELIS, elisOriginal устанавливается в handleElisData
   * - При повторном открытии документа, elisOriginal не сохраняется на сервер,
   *   поэтому его нужно восстановить из текущего значения
   *
   * Логика: если elisFilled=true, значит значение не было изменено пользователем,
   * следовательно текущее значение равно оригинальному ELIS значению.
   */
  const restoreElisOriginals = () => {
    const formData = store.formData;

    // Найти все ключи с elisFilled = true
    const elisFilledKeys = Object.keys(formData)
      .filter(key => key.endsWith('__elisFilled') && formData[key] === true);

    let restoredCount = 0;

    for (const elisFilledKey of elisFilledKeys) {
      const baseKey = elisFilledKey.replace('__elisFilled', '');
      const originalKey = `${baseKey}__elisOriginal`;

      // Пропускаем, если elisOriginal уже существует
      if (formData[originalKey] !== undefined) {
        continue;
      }

      const currentValue = formData[baseKey];

      // Пропускаем только отсутствующие значения
      if (currentValue === undefined || currentValue === null) {
        continue;
      }

      // Особая обработка для методов испытаний
      if (baseKey.startsWith('method.')) {
        if (typeof currentValue === 'string' && currentValue.trim() !== '') {
          try {
            const methodObj = JSON.parse(currentValue);
            // Сохраняем имя метода для сравнения
            formData[originalKey] = methodObj.name || methodObj.Name;
            // Сохраняем полный объект для возможности возврата к нему
            formData[`${baseKey}__elisOption`] = methodObj;
            restoredCount++;

            logger.debug('[restoreElisOriginals] Восстановлен elisOriginal для метода', {
              key: baseKey,
              methodName: formData[originalKey]
            });
          } catch (e) {
            // Если не удалось распарсить JSON, используем значение как есть
            formData[originalKey] = currentValue;
            restoredCount++;

            logger.warn('[restoreElisOriginals] Не удалось распарсить метод, используем как строку', {
              key: baseKey,
              value: currentValue
            });
          }
        }
      } else {
        // Для остальных полей просто копируем текущее значение
        formData[originalKey] = currentValue;
        restoredCount++;

        logger.debug('[restoreElisOriginals] Восстановлен elisOriginal', {
          key: baseKey,
          value: currentValue
        });
      }
    }

    if (restoredCount > 0) {
      logger.info('[restoreElisOriginals] Восстановлено elisOriginal значений', {
        count: restoredCount
      });
    }

    // Восстанавливаем userData в опциях IOF полей для корректной работы автозаполнения Post/Factory
    restoreUserDataInSignerOptions();
  };

  /**
   * Восстанавливает userData в опциях полей IOF (подписанты).
   *
   * При первичной загрузке ELIS, userData (post, factory) сохраняется в опции IOF поля.
   * При повторном открытии документа эти данные теряются, т.к. опции загружаются из конфигурации.
   * Эта функция восстанавливает userData из elisOriginal значений Post/Factory,
   * чтобы механизм автозаполнения работал корректно.
   */
  const restoreUserDataInSignerOptions = () => {
    const formData = store.formData;
    const config = store.config;

    if (!config) return;

    // Группы полей подписантов: IOF -> Post, Factory
    const signerGroups = [
      { iof: 'Laboratory_IOF', post: 'Laboratory_Post', factory: 'Laboratory_Factory' },
      { iof: 'Delive_IOF', post: 'Delive_Post', factory: 'Delive_Factory' },
      { iof: 'Receive_IOF', post: 'Receive_Post', factory: 'Receive_Factory' }
    ];

    for (const group of signerGroups) {
      const iofValue = formData[group.iof];
      const postElisOriginal = formData[`${group.post}__elisOriginal`];
      const factoryElisOriginal = formData[`${group.factory}__elisOriginal`];

      // Проверяем, есть ли ELIS данные для восстановления
      if (!iofValue || (postElisOriginal === undefined && factoryElisOriginal === undefined)) {
        continue;
      }

      // Найти поле IOF в конфигурации
      const iofField = config.fields.find(f => f.key === group.iof);
      if (!iofField?.options) {
        continue;
      }

      // Найти текущую выбранную опцию
      const option = iofField.options.find(opt => opt.value === iofValue);
      if (!option) {
        continue;
      }

      // Восстанавливаем userData с ELIS значениями
      // Приоритет: elisOriginal > существующие данные в опции > пустая строка
      const restoredData = {
        ...(option.data || {}),
        post: postElisOriginal ?? option.data?.post ?? '',
        factory: factoryElisOriginal ?? option.data?.factory ?? ''
      };

      option.data = restoredData;

      logger.debug('[restoreUserDataInSignerOptions] Восстановлен userData для IOF', {
        iofKey: group.iof,
        iofValue,
        post: restoredData.post,
        factory: restoredData.factory
      });
    }
  };

  /**
   * Функция сохранения документа с поддержкой OPC тегов
   * Для паспортов: выполняется polling OPC тегов
   * Для актов: выполняется запись в OPC тег без polling
   * Для остальных документов: обычное сохранение
   *
   * OPC параметры получаются из URL query params, которые передает главное окно
   */
  const handleSave = async (): Promise<boolean> => {
    // Проверяем валидацию перед сохранением
    if (store.hasValidationErrors) {
      toast.add({
        severity: 'warn',
        summary: 'Ошибка валидации',
        detail: 'Заполните все обязательные поля',
        life: 5000
      });
      return false;
    }

    try {
      // Используем новую логику сохранения с поддержкой OPC
      // Передаем OPC параметры из URL
      const success = await saveDocumentWithOpc(opcParams.value);

      if (!success) {
        // Polling завершился по таймауту
        toast.add({
          severity: 'warn',
          summary: 'Предупреждение',
          detail: 'Документ сохранен, но ИВК не подтвердил запись в течение 10 секунд',
          life: 7000
        });
      }

      // Успешное сохранение - НЕ показываем Toast
      // Главное окно само обработает результат
      return success;
    } catch (error: any) {
      // Показываем Toast только при ошибке
      toast.add({
        severity: 'error',
        summary: 'Ошибка сохранения',
        detail: error.message || 'Не удалось сохранить документ',
        life: 5000
      });
      return false;
    }
  };

  /**
   * Загрузка конфигурации документа
   * @param deviceId ID устройства
   * @param docType Тип документа
   * @param id ID документа
   */
  const loadDocument = async (deviceId: number, docType: string, id: number) => {
    await store.loadConfig(deviceId, docType, id);

    // Восстанавливаем elisOriginal для полей с elisFilled=true
    // Это нужно для корректной работы "возврата к значению ELIS" при повторном открытии документа
    restoreElisOriginals();
  };

  /**
   * Экспонируем SaveDoc() для главного окна
   */
  const exposeSaveDoc = () => {
    onMounted(() => {
      (window as any).SaveDoc = async function() {
        return await handleSave();
      };
    });

    onBeforeUnmount(() => {
      (window as any).SaveDoc = undefined;
    });
  };

  /**
   * Уведомление главного окна о состоянии кнопки сохранения
   */
  const notifyParentAboutSaveState = (canSave: boolean) => {
    if (window.parent) {
      window.parent.postMessage(
        canSave ? 'ButtonSaveOn' : 'ButtonSaveOff',
        '*'
      );
    }
  };

  /**
   * Предупреждение перед закрытием с несохранёнными изменениями
   */
  const handleBeforeUnload = (e: BeforeUnloadEvent) => {
    if (store.hasUnsavedChanges) {
      e.preventDefault();
      e.returnValue = '';
    }
  };

  /**
   * Установка обработчика beforeunload
   */
  const setupBeforeUnloadHandler = () => {
    onMounted(() => {
      window.addEventListener('beforeunload', handleBeforeUnload);
    });

    onBeforeUnmount(() => {
      window.removeEventListener('beforeunload', handleBeforeUnload);
      store.reset();
    });
  };

  return {
    store,
    handleSave,
    loadDocument,
    exposeSaveDoc,
    notifyParentAboutSaveState,
    setupBeforeUnloadHandler
  };
}
