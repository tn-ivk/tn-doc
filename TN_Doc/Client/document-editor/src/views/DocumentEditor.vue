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
    <div v-else-if="store.isReady">
      <!-- Панель действий -->
      <div class="actions-bar">
        <h2 class="document-title">{{ store.documentTitle }}</h2>
        <div class="actions-buttons">
          <Button
            label="Сохранить"
            icon="pi pi-save"
            :loading="store.isSaving"
            :disabled="!store.hasUnsavedChanges"
            @click="handleSave"
            severity="primary"
          />
          <Button
            label="Отмена"
            icon="pi pi-times"
            @click="handleCancel"
            severity="secondary"
            outlined
          />
        </div>
      </div>

      <!-- Таблица редактирования -->
      <div class="editor-container">
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
</template>

<script setup lang="ts">
import { onMounted, onBeforeUnmount } from 'vue';
import { useRoute } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import { useDocumentStore } from '@/stores/documentStore';
import FormField from '@/components/FormField.vue';
import Message from 'primevue/message';
import ProgressSpinner from 'primevue/progressspinner';
import Button from 'primevue/button';

const route = useRoute();
const store = useDocumentStore();
const toast = useToast();
const confirm = useConfirm();

// Обработка сохранения документа
async function handleSave() {
  try {
    await store.saveDocument();
    toast.add({
      severity: 'success',
      summary: 'Успешно',
      detail: 'Документ успешно сохранён',
      life: 3000
    });
  } catch (error: any) {
    toast.add({
      severity: 'error',
      summary: 'Ошибка',
      detail: error.message || 'Не удалось сохранить документ',
      life: 5000
    });
  }
}

// Обработка отмены
function handleCancel() {
  if (store.hasUnsavedChanges) {
    confirm.require({
      message: 'У вас есть несохранённые изменения. Отменить изменения?',
      header: 'Подтверждение',
      icon: 'pi pi-exclamation-triangle',
      rejectLabel: 'Нет',
      acceptLabel: 'Да',
      accept: () => {
        // Перезагрузить данные из backend
        const { deviceId, docType, id } = route.params;
        store.loadConfig(
          parseInt(deviceId as string, 10),
          docType as string,
          parseInt(id as string, 10)
        );
        toast.add({
          severity: 'info',
          summary: 'Отменено',
          detail: 'Изменения отменены',
          life: 2000
        });
      }
    });
  } else {
    toast.add({
      severity: 'info',
      summary: 'Информация',
      detail: 'Нет несохранённых изменений',
      life: 2000
    });
  }
}

onMounted(async () => {
  const { deviceId, docType, id } = route.params;

  if (!deviceId || !docType || !id) {
    store.error = 'Отсутствуют обязательные параметры маршрута';
    return;
  }

  try {
    await store.loadConfig(
      parseInt(deviceId as string, 10),
      docType as string,
      parseInt(id as string, 10)
    );
    toast.add({
      severity: 'success',
      summary: 'Успешно',
      detail: 'Документ загружен',
      life: 2000
    });
  } catch (error: any) {
    console.error('Ошибка загрузки документа:', error);
    toast.add({
      severity: 'error',
      summary: 'Ошибка загрузки',
      detail: error.message || 'Не удалось загрузить документ',
      life: 5000
    });
  }
});

// Предупреждение перед закрытием с несохранёнными изменениями
const handleBeforeUnload = (e: BeforeUnloadEvent) => {
  if (store.hasUnsavedChanges) {
    e.preventDefault();
    e.returnValue = '';
  }
};

onMounted(() => {
  window.addEventListener('beforeunload', handleBeforeUnload);
});

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload);
  store.reset();
});
</script>

<style scoped>
.document-editor {
  padding: 0.5rem 1rem 1.5rem;
  background-color: var(--md-surface);
  font-family: 'Segoe UI', 'PT Astra Sans', 'Helvetica Neue', Arial, sans-serif;
}

.actions-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1rem;
  padding: 0.75rem 1rem;
  background: #ffffff;
  border: 1px solid var(--md-outline);
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(33, 33, 33, 0.04);
}

.document-title {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--md-text);
}

.actions-buttons {
  display: flex;
  gap: 0.5rem;
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 400px;
  gap: 1rem;
}

.editor-container {
  width: 100%;
  background: #ffffff;
  border: 1px solid var(--md-outline);
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 6px rgba(33, 33, 33, 0.04);
}

.editor-table {
  width: 100%;
  border-collapse: collapse;
  table-layout: fixed;
  font-size: 15px;
  color: var(--md-text);
}


.editor-table tr {
  height: 44px;
}

.editor-table td {
  border: 1px solid var(--md-outline);
  padding: 0.25rem 0.5rem;
  vertical-align: middle;
}

.editor-label-cell {
  width: 55%;
  background-color: transparent;
  font-weight: var(--md-font-weight-medium);
  color: var(--md-text);
}

.editor-input-cell {
  background-color: transparent;
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
