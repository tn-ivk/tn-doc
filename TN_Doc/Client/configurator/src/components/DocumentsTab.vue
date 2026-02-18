<template>
  <div class="documents-tab">
    <Splitter class="documents-splitter" @resizeend="onResizeEnd">
      <SplitterPanel :size="panelSizes[0]" :minSize="10">
        <DocumentTree @node-select="handleNodeSelect" />
      </SplitterPanel>
      <SplitterPanel :size="panelSizes[1]">
        <DocumentConfigEditor :selected-node="selectedNode" />
      </SplitterPanel>
    </Splitter>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import Splitter from 'primevue/splitter';
import SplitterPanel from 'primevue/splitterpanel';
import DocumentTree from './DocumentTree.vue';
import DocumentConfigEditor from './DocumentConfigEditor.vue';
import type { DocumentTreeNode } from '../types/document.types';

const STORAGE_KEY = 'documents-splitter-sizes';
const DEFAULT_SIZES = [20, 80];

function loadSizes(): number[] {
  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved) {
      const parsed = JSON.parse(saved);
      if (Array.isArray(parsed) && parsed.length === 2 && parsed.every((v: unknown) => typeof v === 'number')) {
        return parsed;
      }
    }
  } catch { /* ignore */ }
  return DEFAULT_SIZES;
}

const panelSizes = ref(loadSizes());

function onResizeEnd(event: { sizes: number[] }) {
  panelSizes.value = event.sizes;
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(event.sizes));
  } catch { /* ignore */ }
}

const selectedNode = ref<DocumentTreeNode | null>(null);

function handleNodeSelect(node: DocumentTreeNode | null) {
  selectedNode.value = node;
}
</script>

<style scoped>
.documents-tab {
  padding: 0.5rem;
  box-sizing: border-box;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.documents-splitter {
  flex: 1;
  min-height: 0;
}

/* Прозрачный фон для Splitter и его панелей */
:deep(.p-splitter) {
  background-color: transparent;
}

:deep(.p-splitter-panel) {
  background-color: transparent;
}

:deep(.p-splitter-gutter) {
  background-color: var(--md-outline, #CFD8DC);
}
</style>
