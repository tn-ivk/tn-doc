import { ref, computed, onMounted, onBeforeUnmount } from 'vue';
import { useRoute } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useDocumentStore } from '@/stores/documentStore';

/**
 * Композабл с общей логикой редактирования документов
 * Переиспользуется в DocumentEditor и DocumentActEditor
 */
export function useDocumentEditor() {
  const route = useRoute();
  const store = useDocumentStore();
  const toast = useToast();

  /**
   * Функция сохранения документа (только ошибки в Toast)
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
      await store.saveDocument();
      // Успешное сохранение - НЕ показываем Toast
      // Главное окно само обработает результат
      return true;
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
   */
  const loadDocument = async () => {
    const { deviceId, docType, id } = route.params;

    if (!deviceId || !docType || !id) {
      store.error = 'Отсутствуют обязательные параметры маршрута';
      return;
    }

    await store.loadConfig(
      parseInt(deviceId as string, 10),
      docType as string,
      parseInt(id as string, 10)
    );
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
