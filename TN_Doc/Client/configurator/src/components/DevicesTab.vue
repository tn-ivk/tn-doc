<template>
  <div class="devices-tab">
    <Splitter class="devices-splitter" @resizeend="onResizeEnd">
      <SplitterPanel :size="panelSizes[0]" :minSize="10">
        <DeviceList />
      </SplitterPanel>
      <SplitterPanel :size="panelSizes[1]">
        <DeviceEditor />
      </SplitterPanel>
    </Splitter>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import Splitter from 'primevue/splitter';
import SplitterPanel from 'primevue/splitterpanel';
import DeviceList from './DeviceList.vue';
import DeviceEditor from './DeviceEditor.vue';

const STORAGE_KEY = 'devices-splitter-sizes';
const DEFAULT_SIZES = [12, 88];

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
</script>

<style scoped>
/* Унифицированные spacing переменные */
.devices-tab {
  --space-3: 0.75rem;
}

.devices-tab {
  padding: var(--space-3);
  box-sizing: border-box;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.devices-splitter {
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
