<template>
  <div class="documents-tab">
    <Splitter class="documents-splitter">
      <SplitterPanel :size="30" :minSize="15">
        <DocumentTree @node-select="handleNodeSelect" />
      </SplitterPanel>
      <SplitterPanel :size="70">
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
