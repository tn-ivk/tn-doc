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

        <!-- Переключатель режима (только если есть визуальный редактор) -->
        <SelectButton
          v-if="supportsVisualEditor"
          v-model="editorMode"
          :options="editorModes"
          optionLabel="label"
          optionValue="value"
          class="mode-toggle"
          :allowEmpty="false"
        />
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
        <!-- Визуальный редактор -->
        <Suspense v-if="editorMode === 'visual' && supportsVisualEditor">
          <template #default>
            <component
              :is="visualEditorComponent"
              :config="parsedConfig"
              :config-path="currentConfigPath"
              @update:config="handleVisualUpdate"
            />
          </template>
          <template #fallback>
            <div class="loading-state">
              <i class="pi pi-spinner pi-spin loading-icon"></i>
              <span>Загрузка редактора...</span>
            </div>
          </template>
        </Suspense>

        <!-- JSON редактор -->
        <JsonConfigEditor
          v-else
          :content="editedContent"
          @update:content="handleJsonUpdate"
          @validation-error="validationError = $event"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, shallowRef, type Component } from 'vue';
import { useConfigStore } from '../stores/configStore';
import { useVisualEditor } from '../composables/useVisualEditor';
import SelectButton from 'primevue/selectbutton';
import Message from 'primevue/message';
import JsonConfigEditor from './JsonConfigEditor.vue';
import type { DocumentTreeNode } from '../types/document.types';
import type { PassportEditConfig } from '../types/passport-config.types';

interface Props {
  selectedNode: DocumentTreeNode | null;
}

const props = defineProps<Props>();

const configStore = useConfigStore();
const { hasVisualEditor, loadVisualEditor } = useVisualEditor();

const isLoading = ref(false);
const error = ref<string | null>(null);
const originalContent = ref('');
const editedContent = ref('');
const validationError = ref<string | null>(null);

// Режим редактора
type EditorMode = 'visual' | 'json';
const editorMode = ref<EditorMode>('visual');
const editorModes = [
  { label: 'Визуальный', value: 'visual' },
  { label: 'JSON', value: 'json' }
];

// Визуальный редактор (lazy loaded)
const visualEditorComponent = shallowRef<Component | null>(null);

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

// Проверяем, поддерживается ли визуальный редактор для текущего конфига
const supportsVisualEditor = computed(() => {
  return hasVisualEditor(currentConfigPath.value);
});

// Парсинг JSON в типизированный объект
const parsedConfig = computed<PassportEditConfig | null>(() => {
  try {
    return JSON.parse(editedContent.value);
  } catch {
    return null;
  }
});

// Загружаем конфигурацию при изменении выбранного узла
watch(() => props.selectedNode, async (newNode) => {
  if (!newNode || !currentConfigPath.value) {
    originalContent.value = '';
    editedContent.value = '';
    validationError.value = null;
    error.value = null;
    visualEditorComponent.value = null;
    return;
  }

  await loadConfig();
}, { immediate: true });

// Загружаем визуальный редактор при необходимости
watch([currentConfigPath, supportsVisualEditor], async ([path, supports]) => {
  if (supports && path) {
    visualEditorComponent.value = await loadVisualEditor(path);
  } else {
    visualEditorComponent.value = null;
  }
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

    // Устанавливаем режим по умолчанию: визуальный, если поддерживается
    editorMode.value = supportsVisualEditor.value ? 'visual' : 'json';
  } catch (e: any) {
    error.value = `Не удалось загрузить конфигурацию: ${e.message}`;
    originalContent.value = '';
    editedContent.value = '';
  } finally {
    isLoading.value = false;
  }
}

// Обработка изменений из JSON редактора
function handleJsonUpdate(content: string) {
  editedContent.value = content;
  updateDirtyState();
}

// Обработка изменений из визуального редактора
function handleVisualUpdate(newConfig: PassportEditConfig) {
  // Сериализуем обратно в JSON с форматированием
  editedContent.value = JSON.stringify(newConfig, null, 2);
  validationError.value = null;
  updateDirtyState();
}

function updateDirtyState() {
  if (editedContent.value !== originalContent.value) {
    configStore.markDocumentConfigDirty(currentConfigPath.value, editedContent.value);
  } else {
    configStore.clearDocumentConfigDirty(currentConfigPath.value);
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
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.header-info {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex: 1;
  min-width: 0;
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

.mode-toggle {
  flex-shrink: 0;
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

:deep(.p-message) {
  margin: 0;
}

:deep(.p-message .p-message-wrapper) {
  padding: 0.75rem;
}

/* Стили для переключателя режима */
:deep(.p-selectbutton) {
  display: flex;
}

:deep(.p-selectbutton .p-button) {
  padding: 0.5rem 0.75rem;
  font-size: 0.875rem;
}
</style>
