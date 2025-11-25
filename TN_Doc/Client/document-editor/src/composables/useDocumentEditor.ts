import { ref, computed, onMounted, onBeforeUnmount } from 'vue';
import { useToast } from 'primevue/usetoast';
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
