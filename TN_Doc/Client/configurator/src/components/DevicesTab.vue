<template>
  <div class="devices-tab">
    <Splitter style="height: 550px">
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
}
</style>
