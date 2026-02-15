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

      <div v-else-if="currentConfig" class="configurator-main">
        <!-- Верхняя панель с вкладками и кнопками -->
        <div class="configurator-header">
          <Tabs value="0" class="tabs-container">
            <!-- Строка с заголовками вкладок и кнопками -->
            <div class="header-row">
              <TabList>
                <Tab value="0">Общие</Tab>
                <Tab value="1">Устройства</Tab>
                <Tab value="2">Документы</Tab>
              </TabList>
              <!-- Кнопки на одном уровне с TabList -->
              <div class="header-buttons">
                <button
                  type="button"
                  class="icon-btn save-btn"
                  @click="handleSave"
                  :disabled="!isDirty || isSaving"
                  aria-label="Применить"
                  title="Применить"
                >
                  <i class="pi pi-save" aria-hidden="true"></i>
                  <i v-if="isSaving" class="pi pi-spinner pi-spin busy-spinner" aria-hidden="true"></i>
                </button>
                <button
                  type="button"
                  class="icon-btn cancel-btn"
                  @click="handleCancel"
                  aria-label="Отмена"
                  title="Отмена"
                >
                  <i class="pi pi-times" aria-hidden="true"></i>
                </button>
              </div>
            </div>
            <!-- Контент вкладок на всю ширину -->
            <TabPanels>
              <TabPanel value="0">
                <GeneralTab />
              </TabPanel>
              <TabPanel value="1">
                <DevicesTab />
              </TabPanel>
              <TabPanel value="2">
                <DocumentsTab />
              </TabPanel>
            </TabPanels>
          </Tabs>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup lang="ts">
import { onMounted, onBeforeUnmount, computed } from 'vue';
import { storeToRefs } from 'pinia';
import { useToast } from 'primevue/usetoast';
import { logger } from '@tn-doc/shared';
import { useConfigStore } from './stores/configStore';

import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import Toast from 'primevue/toast';

import GeneralTab from './components/GeneralTab.vue';
import DevicesTab from './components/DevicesTab.vue';
import DocumentsTab from './components/DocumentsTab.vue';

const toast = useToast();
const configStore = useConfigStore();
const { currentConfig, isLoading, isSaving, isDirty, error } = storeToRefs(configStore);

onMounted(async () => {
  logger.info('App: компонент смонтирован');

  try {
    logger.debug('App: запрос загрузки конфигурации');
    await configStore.loadConfig();
    logger.info('App: конфигурация загружена успешно');
  } catch (e: any) {
    logger.error('App: ошибка загрузки конфигурации', {
      message: e.message,
      stack: e.stack
    });
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
    logger.info('App: сохранение конфигурации');
    await configStore.saveConfig();
    logger.info('App: конфигурация сохранена успешно');
    toast.add({
      severity: 'success',
      summary: 'Успешно',
      detail: 'Конфигурация сохранена',
      life: 3000
    });
  } catch (e: any) {
    logger.error('App: ошибка сохранения конфигурации', {
      message: e.message,
      stack: e.stack
    });
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
/* ===== Design Tokens: Spacing System (base: 4px) ===== */
.configurator-container {
  --space-1: 0.25rem;   /* 4px - микро-отступы */
  --space-2: 0.5rem;    /* 8px - между элементами в строке */
  --space-3: 0.75rem;   /* 12px - padding компонентов */
  --space-4: 1rem;      /* 16px - gap между секциями */
  --space-5: 1.25rem;   /* 20px - между Panel блоками */
  --space-6: 1.5rem;    /* 24px - крупные разделители */

  /* Transition tokens */
  --transition-fast: 0.15s ease;
  --transition-normal: 0.2s ease;
}

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
  padding: 0;
}

.configurator-main {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.configurator-header {
  padding: 0.5rem 1rem 0 1rem;
  border-bottom: 1px solid var(--md-outline, #CFD8DC);
  background-color: var(--md-surface, #FAFAFA);
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: visible;
}

.tabs-container {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
}

.header-row {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding-bottom: 0.75rem;
}

.header-buttons {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  margin-left: auto;
  flex-shrink: 0;
}

/* Стили кнопок - компактные filled кнопки */
.header-buttons button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  font-size: 0.9rem;
  font-weight: 400;
  line-height: 1;
  text-align: center;
  text-decoration: none;
  vertical-align: middle;
  cursor: pointer;
  user-select: none;
  border: 1px solid transparent;
  border-radius: 0.25rem;
  transition: color 0.15s ease-in-out, background-color 0.15s ease-in-out, border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
}

/* Иконочные кнопки под макет */
.header-buttons .icon-btn {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  border-width: 2px;
  border-style: solid;
  background-color: #ffffff;
  color: #1b6ec2; /* для иконки по умолчанию */
  border-color: #1b6ec2; /* контурная синяя */
}

.header-buttons .icon-btn i { font-size: 18px; line-height: 1; }
.header-buttons .icon-btn .busy-spinner {
  position: absolute;
  font-size: 14px;
}

/* Применить: hover/active — синяя заливка и белая иконка */
.header-buttons .save-btn:hover:not(:disabled),
.header-buttons .save-btn:active:not(:disabled) {
  background-color: #1b6ec2;
  color: #ffffff;
}

.header-buttons .save-btn:disabled {
  background-color: #ffffff;
  color: #9aa6b2;
  border-color: #d0d7de;
  opacity: 1;
}

/* Отмена: тёмно-серая заливка всегда */
.header-buttons .cancel-btn {
  background-color: #616b74;
  color: #ffffff;
  border-color: #616b74;
}

.header-buttons .cancel-btn:hover:not(:disabled) {
  background-color: #556068;
  border-color: #556068;
}

/* Фокус-ринги (особенно заметен на «Отмена») */
.header-buttons .icon-btn:focus-visible { outline: none; }
.header-buttons .save-btn:focus-visible {
  box-shadow: 0 0 0 2px #ffffff, 0 0 0 4px #1b6ec2; /* белый + синий */
}
.header-buttons .cancel-btn:focus-visible {
  box-shadow: 0 0 0 2px #ffffff, 0 0 0 4px #1b6ec2; /* белый + синий */
}

.header-buttons button:disabled {
  opacity: 0.65;
  cursor: not-allowed;
  pointer-events: none;
}

.header-buttons button i { vertical-align: middle; }

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
  padding: 0;
  gap: 0.25rem;
  display: flex;
  align-items: center;
  flex: 1;
  min-width: 0;
  background-color: transparent; /* Прозрачный фон, наследует от родителя */
}

:deep(.p-tab) {
  padding: 0.5rem 0.75rem;
  font-size: 0.9rem;
  min-height: auto;
  color: var(--md-text, #212121); /* Цвет заголовков вкладок - черный */
  font-weight: 400; /* Обычный вес шрифта для неактивных вкладок */
  background-color: transparent; /* Прозрачный фон */
}

:deep(.p-tab.p-tab-selected) {
  color: var(--md-text, #212121); /* Цвет активной вкладки - черный */
  font-weight: 600; /* Полужирный для активной вкладки */
  background-color: transparent; /* Прозрачный фон для активной вкладки */
}

:deep(.p-tabpanels) {
  padding: 0.25rem 1rem;
  flex: 1;
  overflow: hidden;
  min-height: 0;
  display: flex;
  flex-direction: column;
  background-color: transparent; /* Прозрачный фон, наследует от родителя */
}

:deep(.p-tabpanel) {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
  background-color: transparent; /* Прозрачный фон для панели вкладки */
}

:deep(.p-tabs) {
  display: flex;
  flex-direction: column;
  height: 100%;
  background-color: transparent; /* Прозрачный фон для контейнера вкладок */
}
</style>
