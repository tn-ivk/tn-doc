<template>
  <div class="json-config-editor">
    <Textarea
      v-model="localContent"
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
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import Textarea from 'primevue/textarea';
import Message from 'primevue/message';

interface Props {
  content: string;
}

interface Emits {
  (e: 'update:content', value: string): void;
  (e: 'validation-error', error: string | null): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const localContent = ref(props.content);
const validationError = ref<string | null>(null);

// Синхронизация с внешним значением
watch(() => props.content, (newContent) => {
  if (newContent !== localContent.value) {
    localContent.value = newContent;
    validateJson(newContent);
  }
});

function handleInput() {
  validateJson(localContent.value);
  emit('update:content', localContent.value);
}

function validateJson(content: string) {
  try {
    if (content.trim()) {
      JSON.parse(content);
      validationError.value = null;
      emit('validation-error', null);
    }
  } catch (e: any) {
    validationError.value = e.message;
    emit('validation-error', e.message);
  }
}
</script>

<style scoped>
.json-config-editor {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
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
