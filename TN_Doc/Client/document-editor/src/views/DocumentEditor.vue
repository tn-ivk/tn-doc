<template>
  <div class="document-editor">
    <!-- Загрузка -->
    <div v-if="documentStore.loading" class="loading-container">
      <ProgressSpinner />
      <p>Загрузка документа...</p>
    </div>

    <!-- Ошибка -->
    <Message v-else-if="documentStore.error" severity="error">
      {{ documentStore.error }}
    </Message>

    <!-- Форма редактирования -->
    <div v-else-if="documentStore.config" class="editor-content">
      <!-- Заголовок -->
      <div class="editor-header">
        <h2>{{ documentStore.title }}</h2>
        <div class="header-actions">
          <Button
            label="Сохранить"
            icon="pi pi-save"
            :loading="documentStore.saving"
            :disabled="!documentStore.isValid"
            @click="handleSave"
          />
        </div>
      </div>

      <!-- Таблица с полями -->
      <div class="editor-form">
        <table class="form-table">
          <tbody>
            <tr v-for="field in documentStore.fields" :key="field.name">
              <td class="label-cell">
                {{ field.label }}
                <span v-if="field.required" class="required-mark">*</span>
              </td>
              <td class="value-cell">
                <FormField
                  :field="field"
                  :invalid-chars="documentStore.invalidChars"
                  @update:value="(value) => documentStore.updateField(field.name, value)"
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
import { onMounted, onBeforeUnmount } from 'vue'
import { useDocumentStore } from '@/stores/documentStore'
import FormField from '@/components/FormField.vue'
import ProgressSpinner from 'primevue/progressspinner'
import Message from 'primevue/message'
import Button from 'primevue/button'

const props = defineProps<{
  deviceId: string
  docType: string
  docId: number
}>()

const documentStore = useDocumentStore()

onMounted(async () => {
  try {
    await documentStore.loadDocument(props.deviceId, props.docType, props.docId)
  } catch (error) {
    console.error('Ошибка при загрузке документа:', error)
  }
})

onBeforeUnmount(() => {
  documentStore.reset()
})

async function handleSave() {
  try {
    await documentStore.saveDocument()
    // Показываем уведомление об успехе
    alert('Документ успешно сохранен!')
  } catch (error: any) {
    // Показываем ошибку
    alert(`Ошибка при сохранении: ${error.message}`)
  }
}
</script>

<style scoped>
.document-editor {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 400px;
  gap: 20px;
}

.editor-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  padding-bottom: 15px;
  border-bottom: 2px solid var(--p-surface-300);
}

.editor-header h2 {
  margin: 0;
  color: var(--p-text-color);
}

.header-actions {
  display: flex;
  gap: 10px;
}

.editor-form {
  background: white;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.form-table {
  width: 100%;
  border-collapse: collapse;
}

.form-table tbody tr {
  border-bottom: 1px solid var(--p-surface-200);
}

.form-table tbody tr:last-child {
  border-bottom: none;
}

.label-cell {
  width: 30%;
  padding: 12px 16px;
  font-weight: 500;
  color: var(--p-text-color);
  vertical-align: middle;
}

.required-mark {
  color: var(--p-red-500);
  margin-left: 4px;
}

.value-cell {
  width: 70%;
  padding: 12px 16px;
}
</style>
