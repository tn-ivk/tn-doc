<template>
  <div class="document-editor">
    <!-- Индикатор загрузки -->
    <div v-if="store.isLoading" class="loading-container">
      <ProgressSpinner />
      <p>Загрузка документа...</p>
    </div>

    <!-- Сообщение об ошибке -->
    <Message v-else-if="store.error" severity="error" :closable="false">
      {{ store.error }}
    </Message>

    <!-- Редактор документа -->
    <div v-else-if="store.isReady" class="editor-wrapper">
      <div class="editor-container">
        <div class="editor-table-wrapper">
          <table class="editor-table">
            <tbody>
              <tr v-for="field in store.fields" :key="field.key">
                <td class="editor-label-cell">
                  <div class="label-wrapper">
                    <span class="label-text">{{ field.label }}</span>
                    <span v-if="field.required" class="required-mark">*</span>
                  </div>
                </td>
                <td class="editor-input-cell">
                  <FormField
                    :field="field"
                    :modelValue="store.formData[field.key]"
                    :hide-label="true"
                    @update:modelValue="(value) => store.updateField(field.key, value)"
                  />
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, watch } from 'vue';
import { useRoute } from 'vue-router';
import FormField from '@/components/FormField.vue';
import Message from 'primevue/message';
import ProgressSpinner from 'primevue/progressspinner';
import { useDocumentEditor } from '@/composables/useDocumentEditor';
import { useActAutoFill } from '@/composables/useActAutoFill';

const route = useRoute();

// Используем общую логику редактирования документов
const {
  store,
  loadDocument,
  exposeSaveDoc,
  notifyParentAboutSaveState,
  setupBeforeUnloadHandler
} = useDocumentEditor();

// Используем логику автозаполнения для Актов
const { setupAutoFillWatchers } = useActAutoFill();

// Загружаем документ при монтировании
onMounted(async () => {
  console.log('[DocumentActEditor] Монтирование компонента');
  console.log('[DocumentActEditor] route.params:', route.params);
  console.log('[DocumentActEditor] route.path:', route.path);
  console.log('[DocumentActEditor] route.fullPath:', route.fullPath);

  const { deviceId, id } = route.params;
  const docType = 'Act'; // Для этого компонента тип документа всегда Act

  console.log('[DocumentActEditor] Извлечённые параметры:', {
    deviceId,
    docType,
    id,
    deviceIdType: typeof deviceId,
    docTypeType: typeof docType,
    idType: typeof id
  });

  if (!deviceId || !id) {
    console.error('[DocumentActEditor] Отсутствуют параметры маршрута!');
    store.error = 'Отсутствуют обязательные параметры маршрута';
    return;
  }

  console.log('[DocumentActEditor] Загружаем документ с параметрами:', {
    deviceId: parseInt(deviceId as string, 10),
    docType: docType,
    id: parseInt(id as string, 10)
  });

  await loadDocument(
    parseInt(deviceId as string, 10),
    docType,
    parseInt(id as string, 10)
  );

  // Настраиваем автозаполнение связанных полей для Актов
  setupAutoFillWatchers();
});

// Экспонируем SaveDoc() для главного окна
exposeSaveDoc();

// Отслеживаем валидацию и уведомляем главное окно о состоянии кнопки
watch(() => store.canSave, (canSave) => {
  notifyParentAboutSaveState(canSave);
}, { immediate: true });

// Предупреждение перед закрытием с несохранёнными изменениями
setupBeforeUnloadHandler();
</script>

<style scoped>
/* Переиспользуем стили из базового DocumentEditor */
.document-editor {
  background-color: var(--md-surface);
  font-family: 'Segoe UI', 'PT Astra Sans', 'Helvetica Neue', Arial, sans-serif;
  height: 100%;
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
  padding: 0;
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 400px;
  gap: 1rem;
}

.editor-wrapper {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.editor-container {
  width: 100%;
  background: #ffffff;
  border: 1px solid var(--md-outline);
  border-radius: 8px;
  box-shadow: 0 2px 6px rgba(33, 33, 33, 0.04);
  flex: 1;
  display: flex;
  min-height: 0;
}

.editor-table-wrapper {
  overflow: auto;
  flex: 1;
}

.editor-table {
  width: 100%;
  border-collapse: collapse;
  table-layout: auto;
  font-size: 15px;
  color: var(--md-text);
}

.editor-table td {
  border: 1px solid var(--md-outline);
  padding: 0.2rem 0.5rem;
  vertical-align: middle;
}

.editor-label-cell {
  width: 1%;
  max-width: 50%;
  background-color: transparent;
  font-weight: var(--md-font-weight-medium);
  color: var(--md-text);
  white-space: nowrap;
  padding-right: 0.75rem;
}

.label-text {
  display: inline-block;
  line-height: 1.3;
}

.required-mark {
  margin-left: 4px;
  color: var(--md-error);
  font-weight: 600;
}

.editor-table tr:first-child td:first-child {
  border-top-left-radius: 8px;
}

.editor-table tr:first-child td:last-child {
  border-top-right-radius: 8px;
}

.editor-table tr:last-child td:first-child {
  border-bottom-left-radius: 8px;
}

.editor-table tr:last-child td:last-child {
  border-bottom-right-radius: 8px;
}
</style>
