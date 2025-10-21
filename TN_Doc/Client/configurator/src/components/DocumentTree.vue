<template>
  <div class="document-tree">
    <Tree
      v-model:selectionKeys="selectedKeys"
      :value="treeNodes"
      selectionMode="single"
      @node-select="onNodeSelect"
      class="tree-content"
    >
      <template #default="slotProps">
        <div class="tree-node-content">
          <i v-if="slotProps.node.icon" :class="slotProps.node.icon" class="node-icon"></i>
          <span>{{ slotProps.node.label }}</span>
        </div>
      </template>
    </Tree>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { storeToRefs } from 'pinia';
import Tree from 'primevue/tree';
import { useConfigStore } from '../stores/configStore';
import type { DocumentTreeNode } from '../types/document.types';
import type { TreeNode } from 'primevue/tree';

const emit = defineEmits<{
  nodeSelect: [node: DocumentTreeNode | null]
}>();

const configStore = useConfigStore();
const { currentConfig } = storeToRefs(configStore);
const selectedKeys = ref<Record<string, boolean>>({});

// Построение дерева из конфигурации
const treeNodes = computed<TreeNode[]>(() => {
  if (!currentConfig.value?.Devices) {
    return [];
  }

  const nodes: TreeNode[] = [];
  const usedDocuments = new Map<number, Set<string>>();

  // Собираем все используемые документы и шаблоны
  for (const device of currentConfig.value.Devices) {
    if (!device.Use || !device.Docs) continue;

    for (const doc of device.Docs) {
      if (!doc.Use) continue;

      if (!usedDocuments.has(doc.IdDoc)) {
        usedDocuments.set(doc.IdDoc, new Set());
      }

      // Добавляем используемые шаблоны
      if (doc.TemplateDocs) {
        for (const template of doc.TemplateDocs) {
          if (template.Use) {
            usedDocuments.get(doc.IdDoc)?.add(`${template.Id}`);
          }
        }
      }
    }
  }

  // Строим дерево только из используемых документов
  const documentMap = new Map<number, any>();

  for (const device of currentConfig.value.Devices) {
    if (!device.Use || !device.Docs) continue;

    for (const doc of device.Docs) {
      if (!usedDocuments.has(doc.IdDoc)) continue;

      if (!documentMap.has(doc.IdDoc)) {
        const children: TreeNode[] = [];

        // Добавляем только используемые шаблоны
        if (doc.TemplateDocs) {
          const usedTemplateIds = usedDocuments.get(doc.IdDoc);
          for (const template of doc.TemplateDocs) {
            if (usedTemplateIds?.has(`${template.Id}`)) {
              children.push({
                key: `doc-${doc.IdDoc}-template-${template.Id}`,
                label: template.Name || `Шаблон ${template.Id}`,
                icon: 'pi pi-file',
                type: 'template',
                data: {
                  type: 'template',
                  configPath: doc.PathToDocConfigFile,
                  editConfigPath: template.PathToDocEditConfigFile
                }
              });
            }
          }
        }

        documentMap.set(doc.IdDoc, {
          key: `doc-${doc.IdDoc}`,
          label: doc.Name || `Документ ${doc.IdDoc}`,
          icon: 'pi pi-folder',
          type: 'document',
          data: {
            type: 'document',
            configPath: doc.PathToDocConfigFile,
            editConfigPath: doc.PathToDocEditConfigFile
          },
          children: children.length > 0 ? children : undefined
        });
      }
    }
  }

  return Array.from(documentMap.values());
});

function onNodeSelect(node: TreeNode) {
  if (!node.data) {
    emit('nodeSelect', null);
    return;
  }

  const documentNode: DocumentTreeNode = {
    key: node.key as string,
    label: node.label as string,
    icon: node.icon,
    type: node.data.type,
    configPath: node.data.configPath,
    editConfigPath: node.data.editConfigPath
  };

  emit('nodeSelect', documentNode);
}

// Сбрасываем выбор при изменении конфигурации
watch(currentConfig, () => {
  selectedKeys.value = {};
  emit('nodeSelect', null);
});
</script>

<style scoped>
.document-tree {
  height: 100%;
  display: flex;
  flex-direction: column;
  padding: 0.5rem;
  overflow: auto;
}

.tree-content {
  flex: 1;
  border: none;
  background: transparent;
}

.tree-node-content {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.node-icon {
  font-size: 1rem;
  color: var(--md-primary, #1976D2);
}

:deep(.p-tree) {
  padding: 0;
  background: transparent;
  border: none;
}

:deep(.p-tree-node) {
  padding: 0.25rem 0;
}

:deep(.p-tree-node-content) {
  padding: 0.5rem;
  border-radius: 4px;
  transition: background-color 0.2s;
}

:deep(.p-tree-node-content:hover) {
  background-color: var(--md-surface-variant, #E3F2FD);
}

:deep(.p-tree-node-content.p-tree-node-selectable-selected) {
  background-color: var(--md-primary-light, #BBDEFB);
}
</style>
