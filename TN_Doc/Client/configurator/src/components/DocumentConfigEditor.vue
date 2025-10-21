<template>
  <div class="config-editor">
    <div v-if="!selectedNode" class="empty-state">
      <i class="pi pi-inbox empty-icon"></i>
      <p class="empty-text">Выберите документ или шаблон из списка слева</p>
    </div>

    <div v-else-if="!hasConfig" class="empty-state">
      <i class="pi pi-info-circle empty-icon"></i>
      <p class="empty-text">Для выбранного элемента не указан файл конфигурации</p>
    </div>

    <div v-else class="editor-container">
      <div class="editor-header">
        <div class="header-info">
          <i :class="selectedNode.icon" class="header-icon"></i>
          <div class="header-text">
            <h3 class="header-title">{{ selectedNode.label }}</h3>
            <span class="header-subtitle">{{ currentConfigPath }}</span>
          </div>
        </div>
      </div>

      <div v-if="isLoading" class="loading-state">
        <i class="pi pi-spinner pi-spin loading-icon"></i>
        <p>Загрузка конфигурации...</p>
      </div>

      <div v-else-if="error" class="error-state">
        <Message severity="error" :closable="false">
          {{ error }}
        </Message>
      </div>

      <div v-else class="editor-content">
        <Textarea
          v-model="editedContent"
          class="json-editor"
          :autoResize="false"
          spellcheck="false"
          @input="handleInput"
        />

        <div v-if="validationError" class="validation-error">
          <Message severity="error" :closable="false">
            <strong>Ошибка JSON:</strong> {{ validationError }}
          </Message>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useConfigStore } from '../stores/configStore';
import Textarea from 'primevue/textarea';
import Message from 'primevue/message';
import type { DocumentTreeNode } from '../types/document.types';

interface Props {
  selectedNode: DocumentTreeNode | null;
}

const props = defineProps<Props>();

const configStore = useConfigStore();

const isLoading = ref(false);
const error = ref<string | null>(null);
const originalContent = ref('');
const editedContent = ref('');
const validationError = ref<string | null>(null);

// Определяем путь к конфигу для загрузки
const currentConfigPath = computed(() => {
  if (!props.selectedNode) return '';

  // Для документов используем PathToDocConfigFile
  if (props.selectedNode.type === 'document') {
    return props.selectedNode.configPath || '';
  }

  // Для шаблонов используем PathToDocEditConfigFile
  if (props.selectedNode.type === 'template') {
    return props.selectedNode.editConfigPath || '';
  }

  return '';
});

const hasConfig = computed(() => {
  return currentConfigPath.value && currentConfigPath.value.length > 0;
});

// Загружаем конфигурацию при изменении выбранного узла
watch(() => props.selectedNode, async (newNode) => {
  if (!newNode || !currentConfigPath.value) {
    originalContent.value = '';
    editedContent.value = '';
    validationError.value = null;
    error.value = null;
    return;
  }

  await loadConfig();
}, { immediate: true });

async function loadConfig() {
  if (!currentConfigPath.value) return;

  isLoading.value = true;
  error.value = null;
  validationError.value = null;

  try {
    const content = await configStore.loadDocumentConfig(currentConfigPath.value);
    originalContent.value = content;
    editedContent.value = content;
  } catch (e: any) {
    error.value = `Не удалось загрузить конфигурацию: ${e.message}`;
    originalContent.value = '';
    editedContent.value = '';
  } finally {
    isLoading.value = false;
  }
}

function handleInput() {
  // Валидация JSON
  try {
    if (editedContent.value.trim()) {
      JSON.parse(editedContent.value);
      validationError.value = null;

      // Обновляем dirty state если содержимое изменилось
      if (editedContent.value !== originalContent.value) {
        configStore.markDocumentConfigDirty(currentConfigPath.value, editedContent.value);
      } else {
        configStore.clearDocumentConfigDirty(currentConfigPath.value);
      }
    }
  } catch (e: any) {
    validationError.value = e.message;
  }
}

// Предоставляем метод для сохранения (будет вызван из родительского компонента)
defineExpose({
  async save() {
    if (!currentConfigPath.value || validationError.value) {
      return false;
    }

    try {
      await configStore.saveDocumentConfig(currentConfigPath.value, editedContent.value);
      originalContent.value = editedContent.value;
      return true;
    } catch (e: any) {
      error.value = `Не удалось сохранить конфигурацию: ${e.message}`;
      return false;
    }
  }
});
</script>

<style scoped>
.config-editor {
  height: 100%;
  display: flex;
  flex-direction: column;
  background: var(--md-surface, #FFFFFF);
  border-radius: 4px;
  overflow: hidden;
}

.empty-state {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: var(--md-text-muted, #757575);
  padding: 2rem;
}

.empty-icon {
  font-size: 3rem;
  margin-bottom: 1rem;
  opacity: 0.5;
}

.empty-text {
  font-size: 1rem;
  text-align: center;
  margin: 0;
}

.editor-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.editor-header {
  padding: 1rem;
  border-bottom: 1px solid var(--md-outline, #CFD8DC);
  background: var(--md-surface-variant, #F5F5F5);
}

.header-info {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.header-icon {
  font-size: 1.5rem;
  color: var(--md-primary, #1976D2);
}

.header-text {
  flex: 1;
  min-width: 0;
}

.header-title {
  margin: 0;
  font-size: 1.1rem;
  font-weight: 500;
  color: var(--md-text, #212121);
}

.header-subtitle {
  font-size: 0.85rem;
  color: var(--md-text-muted, #757575);
  word-break: break-all;
}

.loading-state {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 2rem;
}

.loading-icon {
  font-size: 2rem;
  margin-bottom: 1rem;
  color: var(--md-primary, #1976D2);
}

.error-state {
  padding: 1rem;
}

.editor-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
  padding: 1rem;
}

.json-editor {
  flex: 1;
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  font-size: 0.9rem;
  line-height: 1.5;
  resize: none;
  border: 1px solid var(--md-outline, #CFD8DC);
  border-radius: 4px;
  padding: 0.75rem;
  min-height: 300px;
}

.json-editor:focus {
  outline: none;
  border-color: var(--md-primary, #1976D2);
  box-shadow: 0 0 0 2px rgba(25, 118, 210, 0.1);
}

.validation-error {
  margin-top: 0.75rem;
}

:deep(.p-message) {
  margin: 0;
}

:deep(.p-message .p-message-wrapper) {
  padding: 0.75rem;
}
</style>
