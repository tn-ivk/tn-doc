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

    <!-- Таблица редактирования -->
    <div v-else-if="store.isReady" class="editor-container">
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
</template>

<script setup lang="ts">
import { onMounted, onBeforeUnmount } from 'vue';
import { useRoute } from 'vue-router';
import { useDocumentStore } from '@/stores/documentStore';
import FormField from '@/components/FormField.vue';
import Message from 'primevue/message';
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
</script>

<style scoped>

.document-editor {
  padding: 0.5rem 1rem 1.5rem;
  background-color: var(--md-surface);
  font-family: 'Segoe UI', 'PT Astra Sans', 'Helvetica Neue', Arial, sans-serif;
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
  height: 56px;
}

.editor-table td {
  border: 1px solid var(--md-outline);
  padding: 0.5rem 0.75rem;
  vertical-align: middle;
}

.editor-label-cell {
  width: 50%;
  background-color: var(--md-surface-variant);
  font-weight: 600;
  color: var(--md-text);
}

.editor-input-cell {
  background-color: #ffffff;
}

.label-text {
  display: inline-block;
  line-height: 1.4;
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
