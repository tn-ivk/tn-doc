<template>
  <div class="devices-tab">
    <Splitter class="devices-splitter">
      <SplitterPanel :size="15" :minSize="15">
        <DeviceList />
      </SplitterPanel>
      <SplitterPanel :size="70">
        <DeviceEditor />
      </SplitterPanel>
    </Splitter>
  </div>
</template>

<script setup lang="ts">
import Splitter from 'primevue/splitter';
import SplitterPanel from 'primevue/splitterpanel';
import DeviceList from './DeviceList.vue';
import DeviceEditor from './DeviceEditor.vue';
</script>

<style scoped>
.devices-tab {
  padding: 0.5rem;
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
