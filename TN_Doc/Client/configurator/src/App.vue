<template>
  <div class="configurator-container">
    <Toast />

    <div class="configurator-content">
      <div v-if="error" class="alert alert-danger" role="alert">
        {{ error }}
      </div>

      <div v-if="isLoading" class="text-center py-5">
        <div class="spinner-border text-primary" role="status">
          <span class="sr-only">Загрузка...</span>
        </div>
      </div>

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
      <button
        type="button"
        class="btn btn-outline-primary"
        @click="handleSave"
        :disabled="!isDirty || isSaving"
      >
        <i v-if="isSaving" class="fa fa-spinner fa-spin" aria-hidden="true"></i>
        <i v-else class="fa fa-floppy-o" aria-hidden="true"></i>
        {{ isSaving ? 'Сохранение...' : 'Применить' }}
      </button>
      <button
        type="button"
        class="btn btn-outline-danger ml-2"
        @click="handleCancel"
      >
        <i class="fa fa-times" aria-hidden="true"></i>
        Отмена
      </button>
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
import Toast from 'primevue/toast';

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

function handleCancel() {
  if (isDirty.value) {
    if (!confirm('У вас есть несохранённые изменения. Закрыть без сохранения?')) {
      return;
    }
  }

  // Сбросить изменения
  configStore.resetConfig();

  // Закрыть модальное окно (если открыто в iframe)
  if (window.parent !== window) {
    window.parent.postMessage({ action: 'closeConfiguratorModal' }, '*');
  }
}
</script>

<style scoped>
.configurator-container {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: calc(90vh - 56px); /* вычитаем высоту modal-header */
}

.configurator-content {
  flex: 1;
  overflow: auto;
  padding: 1rem;
}

.configurator-footer {
  border-top: 1px solid #dee2e6;
  padding: 1rem;
  display: flex;
  justify-content: flex-end;
  background-color: #f8f9fa;
}

.ml-2 {
  margin-left: 0.5rem;
}

.py-5 {
  padding-top: 3rem;
  padding-bottom: 3rem;
}
</style>
