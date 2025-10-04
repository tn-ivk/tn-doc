<template>
  <div class="device-list">
    <div class="search-field">
      <IconField iconPosition="left">
        <InputIcon class="pi pi-search" />
        <InputText
          v-model="searchQuery"
          placeholder="Поиск устройств..."
          class="w-full"
        />
      </IconField>
    </div>

    <Listbox
      v-model="selectedIds"
      :options="filteredDevices"
      option-label="Name"
      option-value="IdDevice"
      multiple
      filter
      :filter-fields="['Name', 'Description']"
      class="device-listbox"
      listStyle="height: 520px"
    >
      <template #option="slotProps">
        <div class="device-item">
          <i :class="slotProps.option.Use ? 'pi pi-check-circle text-green-500' : 'pi pi-times-circle text-red-500'" />
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
import InputText from 'primevue/inputtext';
import IconField from 'primevue/iconfield';
import InputIcon from 'primevue/inputicon';

const configStore = useConfigStore();
const { currentConfig, selectedDeviceIds } = storeToRefs(configStore);

const searchQuery = ref('');

const filteredDevices = computed(() => {
  if (!currentConfig.value?.Devices) return [];

  let devices = currentConfig.value.Devices;

  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase();
    devices = devices.filter(d =>
      d.Name.toLowerCase().includes(query) ||
      (d.Description && d.Description.toLowerCase().includes(query))
    );
  }

  return devices;
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
  padding: 1rem;
}

.search-field {
  margin-bottom: 1rem;
}

.w-full {
  width: 100%;
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

.text-green-500 {
  color: var(--green-500);
}

.text-red-500 {
  color: var(--red-500);
}
</style>
