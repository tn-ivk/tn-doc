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

    <!-- Форма редактирования -->
    <div v-else-if="store.isReady" class="editor-container">
      <!-- Заголовок -->
      <div class="editor-header">
        <h2>{{ store.documentTitle }}</h2>
        <Badge
          v-if="store.hasUnsavedChanges"
          value="Несохранённые изменения"
          severity="warning"
        />
      </div>

      <!-- Поля формы -->
      <div class="editor-fields">
        <FormField
          v-for="field in store.fields"
          :key="field.key"
          :field="field"
          :modelValue="store.formData[field.key]"
          @update:modelValue="(value) => store.updateField(field.key, value)"
        />
      </div>

      <!-- Кнопки действий -->
      <div class="editor-actions">
        <Button
          label="Сохранить"
          icon="pi pi-save"
          severity="success"
          :loading="store.isSaving"
          :disabled="!store.hasUnsavedChanges"
          @click="handleSave"
        />
        <Button
          label="Отмена"
          icon="pi pi-times"
          severity="secondary"
          :disabled="store.isSaving"
          @click="handleCancel"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onBeforeUnmount } from 'vue';
import { useRoute } from 'vue-router';
import { useDocumentStore } from '@/stores/documentStore';
import FormField from '@/components/FormField.vue';
import Button from 'primevue/button';
import Message from 'primevue/message';
import Badge from 'primevue/badge';
import ProgressSpinner from 'primevue/progressspinner';

const route = useRoute();
const store = useDocumentStore();

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
  } catch (error) {
    console.error('Ошибка загрузки документа:', error);
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

async function handleSave() {
  try {
    await store.saveDocument();
    alert('Документ успешно сохранён');
  } catch (error: any) {
    alert(`Ошибка сохранения: ${error.message}`);
  }
}

function handleCancel() {
  if (store.hasUnsavedChanges) {
    const confirmed = confirm('У вас есть несохранённые изменения. Вы уверены, что хотите отменить?');
    if (!confirmed) return;
  }

  // Закрыть окно или вернуться назад
  window.history.back();
}
</script>

<style scoped>
.document-editor {
  padding: 2rem;
  max-width: 1200px;
  margin: 0 auto;
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
  background: var(--surface-card);
  border-radius: var(--border-radius);
  padding: 2rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.editor-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--surface-border);
}

.editor-header h2 {
  margin: 0;
  color: var(--text-color);
}

.editor-fields {
  margin-bottom: 2rem;
}

.editor-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  padding-top: 1rem;
  border-top: 1px solid var(--surface-border);
}
</style>
