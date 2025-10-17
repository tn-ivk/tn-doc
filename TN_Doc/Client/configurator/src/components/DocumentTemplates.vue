<template>
  <div v-if="document && document.TemplateDocs && document.TemplateDocs.length > 0" class="document-templates">
    <div class="template-header">
      <strong>{{ document.Name }}</strong> - Шаблоны:
    </div>
    <div class="template-list">
      <div
        v-for="template in document.TemplateDocs"
        :key="template.Id"
        class="template-item"
      >
        <Checkbox
          :model-value="template.Use"
          :input-id="`template-${document.IdDoc}-${template.Id}`"
          @update:model-value="(val) => updateTemplate(template.Id, val)"
        />
        <label :for="`template-${document.IdDoc}-${template.Id}`" class="template-label">
          {{ template.Name }}
        </label>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { Document } from '../types/config.types';
import Checkbox from 'primevue/checkbox';

interface Props {
  document: Document;
  deviceId: number;
}

const props = defineProps<Props>();
const emit = defineEmits<{
  (e: 'update:template', deviceId: number, docId: number, templateId: number, use: boolean): void;
}>();

function updateTemplate(templateId: number, use: boolean) {
  emit('update:template', props.deviceId, props.document.IdDoc, templateId, use);
}
</script>

<style scoped>
.document-templates {
  margin-bottom: 0.5rem;
}

.template-header {
  font-size: 0.9rem;
  margin-bottom: 0.25rem;
  color: var(--text-color);
}

.template-list {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  margin-left: 1rem;
}

.template-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.template-label {
  font-size: 0.875rem;
  color: var(--text-color-secondary);
  margin: 0;
  font-weight: normal;
}
</style>
