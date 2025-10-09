<template>
  <div class="device-list">
    <Listbox
      v-model="selectedIds"
      :options="filteredDevices"
      option-label="Name"
      option-value="IdDevice"
      multiple
      :metaKeySelection="true"
      class="device-listbox"
      listStyle="height: 100%"
    >
      <template #option="slotProps">
        <div 
          class="device-item" 
          :data-device-id="slotProps.option.IdDevice"
          @click="handleDeviceItemClick($event, slotProps.option.IdDevice)"
        >
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

// Обработка кликов на элементах устройств
function handleDeviceItemClick(event: MouseEvent, deviceId: number) {
  // Одиночный клик без модификаторов — выбрать только один элемент
  if (!event.ctrlKey && !event.shiftKey) {
    event.preventDefault();
    event.stopPropagation();
    configStore.selectDevices([deviceId]);
    return;
  }

  // С Ctrl/Shift — множественный выбор вручную, не полагаясь на Listbox
  event.preventDefault();
  event.stopPropagation();

  const current = [...selectedIds.value];
  const idx = current.indexOf(deviceId);

  if (event.ctrlKey) {
    // Ctrl — toggle элемента
    if (idx >= 0) {
      current.splice(idx, 1);
    } else {
      current.push(deviceId);
    }
    configStore.selectDevices(current);
    return;
  }

  if (event.shiftKey) {
    // Shift — диапазон от последнего выбранного к текущему
    // Берём последний выбранный как якорь, если есть
    const anchor = selectedIds.value.length > 0 ? selectedIds.value[selectedIds.value.length - 1] : deviceId;
    const idsOrdered = filteredDevices.value.map(d => d.IdDevice);
    const aIndex = idsOrdered.indexOf(anchor);
    const bIndex = idsOrdered.indexOf(deviceId);
    if (aIndex >= 0 && bIndex >= 0) {
      const [start, end] = aIndex <= bIndex ? [aIndex, bIndex] : [bIndex, aIndex];
      const range = idsOrdered.slice(start, end + 1);
      const set = new Set(current);
      range.forEach(id => set.add(id));
      configStore.selectDevices(Array.from(set));
      return;
    }
    // fallback: если не нашли индексы — выбрать только кликнутый
    configStore.selectDevices([deviceId]);
  }
}
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
  margin-top: 0.25rem;
  padding: 0 0.5rem;
  height: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
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
