<template>
  <div class="configurator-container">
    <div class="configurator-header">
      <h1>Конфигуратор настроек приложения</h1>
    </div>

    <Toast />

    <div class="configurator-content">
      <Message v-if="error" severity="error" :closable="false">
        {{ error }}
      </Message>

      <ProgressBar v-if="isLoading" mode="indeterminate" class="loading-bar" />

      <TabView v-else-if="currentConfig">
        <TabPanel header="Общие" value="0">
          <GeneralTab />
        </TabPanel>
        <TabPanel header="Устройства" value="1">
          <DevicesTab />
        </TabPanel>
      </TabView>
    </div>

    <div class="configurator-footer">
      <Button
        label="Применить"
        icon="pi pi-check"
        @click="handleSave"
        :disabled="!isDirty || isSaving"
        :loading="isSaving"
        severity="success"
      />
      <Button
        label="Сбросить"
        icon="pi pi-times"
        @click="handleReset"
        :disabled="!isDirty"
        severity="secondary"
        outlined
        class="ml-2"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onBeforeUnmount, computed } from 'vue';
import { storeToRefs } from 'pinia';
import { useToast } from 'primevue/usetoast';
import { useConfigStore } from './stores/configStore';

import TabView from 'primevue/tabview';
import TabPanel from 'primevue/tabpanel';
import Button from 'primevue/button';
import Toast from 'primevue/toast';
import Message from 'primevue/message';
import ProgressBar from 'primevue/progressbar';

import GeneralTab from './components/GeneralTab.vue';
import DevicesTab from './components/DevicesTab.vue';

const toast = useToast();
const configStore = useConfigStore();
const { currentConfig, isLoading, isSaving, isDirty, error } = storeToRefs(configStore);

onMounted(async () => {
  try {
    await configStore.loadConfig();
  } catch (e: any) {
    toast.add({
      severity: 'error',
      summary: 'Ошибка',
      detail: 'Не удалось загрузить конфигурацию: ' + e.message,
      life: 5000
    });
  }

  // Предупреждение о несохранённых изменениях
  window.addEventListener('beforeunload', handleBeforeUnload);
});

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload);
});

function handleBeforeUnload(e: BeforeUnloadEvent) {
  if (isDirty.value) {
    e.preventDefault();
    e.returnValue = '';
  }
}

async function handleSave() {
  try {
    await configStore.saveConfig();
    toast.add({
      severity: 'success',
      summary: 'Успешно',
      detail: 'Конфигурация сохранена',
      life: 3000
    });
  } catch (e: any) {
    toast.add({
      severity: 'error',
      summary: 'Ошибка',
      detail: 'Не удалось сохранить конфигурацию: ' + e.message,
      life: 5000
    });
  }
}

function handleReset() {
  configStore.resetConfig();
  toast.add({
    severity: 'info',
    summary: 'Сброшено',
    detail: 'Изменения отменены',
    life: 3000
  });
}
</script>

<style scoped>
.configurator-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  max-width: 1400px;
  margin: 0 auto;
  padding: 1rem;
}

.configurator-header {
  margin-bottom: 1rem;
}

.configurator-header h1 {
  margin: 0;
  font-size: 1.75rem;
  color: var(--primary-color);
}

.configurator-content {
  flex: 1;
  overflow: auto;
  margin-bottom: 1rem;
}

.loading-bar {
  height: 4px;
}

.configurator-footer {
  border-top: 1px solid var(--surface-border);
  padding-top: 1rem;
  display: flex;
  justify-content: flex-end;
}

.ml-2 {
  margin-left: 0.5rem;
}
</style>
