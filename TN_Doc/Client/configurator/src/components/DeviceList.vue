<template>
  <div class="device-list">
    <Listbox
      v-model="selectedIds"
      :options="filteredDevices"
      option-label="Name"
      option-value="IdDevice"
      multiple
      class="device-listbox"
      listStyle="height: 100%"
    >
      <template #option="slotProps">
        <div class="device-item">
          <i :class="slotProps.option.Use ? 'pi pi-check-circle text-blue-500' : 'pi pi-times-circle text-red-500'" />
          <span class="device-name">{{ slotProps.option.Name }}</span>
          <span v-if="slotProps.option.Description" class="device-description">
            {{ slotProps.option.Description }}
          </span>
        </div>
      </template>
    </Listbox>

    <div class="selection-info">
      Выбрано: {{ selectedIds.length }} из {{ filteredDevices.length }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { storeToRefs } from 'pinia';
import { useConfigStore } from '../stores/configStore';

import Listbox from 'primevue/listbox';

const configStore = useConfigStore();
const { currentConfig, selectedDeviceIds } = storeToRefs(configStore);

const filteredDevices = computed(() => {
  if (!currentConfig.value?.Devices) return [];
  return currentConfig.value.Devices;
});

const selectedIds = computed({
  get: () => selectedDeviceIds.value,
  set: (value: number[]) => {
    configStore.selectDevices(value);
  }
});
</script>

<style scoped>
.device-list {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 0.5rem;
}

.device-listbox {
  flex: 1;
  width: 100%;
}

.device-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.device-name {
  font-weight: 600;
}

.device-description {
  font-size: 0.875rem;
  color: var(--text-color-secondary);
}

.selection-info {
  margin-top: 0.5rem;
  padding: 0.5rem;
  background-color: var(--surface-50);
  border-radius: 4px;
  text-align: center;
  font-size: 0.875rem;
  color: var(--text-color-secondary);
}

.text-blue-500 {
  color: var(--blue-500);
}

.text-red-500 {
  color: var(--red-500);
}
</style>
