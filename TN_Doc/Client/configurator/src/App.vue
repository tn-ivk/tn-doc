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

      <Tabs v-else-if="currentConfig" value="0">
        <TabList>
          <Tab value="0">Общие</Tab>
          <Tab value="1">Устройства</Tab>
        </TabList>
        <TabPanels>
          <TabPanel value="0">
            <GeneralTab />
          </TabPanel>
          <TabPanel value="1">
            <DevicesTab />
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>

    <div class="configurator-footer">
      <button
        type="button"
        class="btn btn-primary save-btn"
        @click="handleSave"
        :disabled="!isDirty || isSaving"
      >
        <i v-if="isSaving" class="fa fa-spinner fa-spin" aria-hidden="true"></i>
        <i v-else class="fa fa-floppy-o" aria-hidden="true"></i>
        <span class="ml-1">{{ isSaving ? 'Сохранение...' : 'Применить' }}</span>
      </button>
      <button
        type="button"
        class="btn btn-danger cancel-btn ml-2"
        @click="handleCancel"
      >
        <i class="fa fa-times" aria-hidden="true"></i>
        <span class="ml-1">Отмена</span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onBeforeUnmount, computed } from 'vue';
import { storeToRefs } from 'pinia';
import { useToast } from 'primevue/usetoast';
import { useConfigStore } from './stores/configStore';

import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
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
  overflow: hidden;
  min-height: 0; /* важно для корректной усадки flex-элемента и отсутствия лишнего скролла */
  padding: 0.25rem 1rem;
}

.configurator-footer {
  border-top: 1px solid #dee2e6;
  padding: 0.5rem 1rem;
  display: flex;
  justify-content: flex-end;
  background-color: #f8f9fa;
}

/* Стили кнопок - компактные filled кнопки */
.configurator-footer button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.25rem 0.75rem;
  font-size: 0.9rem;
  font-weight: 400;
  line-height: 1.5;
  text-align: center;
  text-decoration: none;
  vertical-align: middle;
  cursor: pointer;
  user-select: none;
  border: 1px solid transparent;
  border-radius: 0.25rem;
  transition: color 0.15s ease-in-out, background-color 0.15s ease-in-out, border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
}

.configurator-footer button.btn-primary {
  color: #ffffff; /* Белый текст */
  background-color: #1b6ec2; /* Основной синий */
  border-color: #1861ac; /* Синий (темнее) для границы */
}

.configurator-footer button.btn-primary:hover:not(:disabled) {
  background-color: #155a9e; /* Темно-синий при наведении */
  border-color: #155a9e;
}

.configurator-footer button.btn-primary:disabled {
  background-color: #cccccc; /* Серый для disabled */
  border-color: #cccccc;
  color: #666666; /* Темно-серый текст для disabled */
}

.configurator-footer button.btn-danger {
  color: #fff;
  background-color: #dc3545;
  border-color: #dc3545;
}

.configurator-footer button.btn-danger:hover:not(:disabled) {
  background-color: #bb2d3b;
  border-color: #b02a37;
}

.configurator-footer button:disabled {
  opacity: 0.65;
  cursor: not-allowed;
  pointer-events: none;
}

.configurator-footer button i {
  font-size: 1em;
  vertical-align: middle;
}

.ml-1 {
  margin-left: 0.25rem;
}

.ml-2 {
  margin-left: 0.5rem;
}

.py-5 {
  padding-top: 3rem;
  padding-bottom: 3rem;
}

/* Компактные вкладки */
:deep(.p-tablist) {
  padding: 0.125rem 0.5rem;
  gap: 0.25rem;
}

:deep(.p-tab) {
  padding: 0.25rem 0.75rem;
  font-size: 0.9rem;
  min-height: auto;
  color: #212121; /* Цвет заголовков вкладок - черный */
}

:deep(.p-tab.p-tab-selected) {
  color: #212121; /* Цвет активной вкладки - черный */
  font-weight: 600;
}

:deep(.p-tabpanels) {
  padding: 0;
}
</style>
