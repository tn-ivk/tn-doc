<template>
  <div class="devices-tab">
    <Splitter style="height: calc(100vh - 200px); min-height: 600px;">
      <SplitterPanel :size="25" :minSize="20" :maxSize="35">
        <DeviceList />
      </SplitterPanel>
      <SplitterPanel :size="75">
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
}
</style>
