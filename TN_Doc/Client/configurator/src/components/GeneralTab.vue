<template>
  <div class="general-tab">
    <div class="field field-horizontal">
      <label for="export-path">Путь экспорта документов:</label>
      <InputText
        id="export-path"
        v-model="exportPath"
        placeholder="Путь для сохранения экспортируемых документов"
        class="field-input-flex"
      />
    </div>

    <div class="field field-horizontal">
      <label for="use-security">Функции безопасности:</label>
      <InputSwitch
        id="use-security"
        v-model="useSecurityFeatures"
      />
    </div>

    <Panel header="Настройки локального OPC-клиента" class="mt-2">
      <OpcSettings
        v-model="armOpcSettings"
        :show-type-selector="true"
      />
    </Panel>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { storeToRefs } from 'pinia';
import { useConfigStore } from '../stores/configStore';
import type { OpcConnectionSettings } from '../types/config.types';

import Panel from 'primevue/panel';
import InputText from 'primevue/inputtext';
import InputSwitch from 'primevue/inputswitch';
import OpcSettings from './OpcSettings.vue';

const configStore = useConfigStore();
const { currentConfig } = storeToRefs(configStore);

const exportPath = computed({
  get: () => currentConfig.value?.ExportDoc?.Path || '',
  set: (value: string) => {
    configStore.updateGeneralSettings({
      ExportDoc: { Path: value }
    });
  }
});

const useSecurityFeatures = computed({
  get: () => currentConfig.value?.UseSecurityFeatures || false,
  set: (value: boolean) => {
    configStore.updateGeneralSettings({
      UseSecurityFeatures: value
    });
  }
});

const armOpcSettings = computed({
  get: () => currentConfig.value?.ArmOpcConnectionSettings,
  set: (value: OpcConnectionSettings | undefined) => {
    configStore.updateGeneralSettings({
      ArmOpcConnectionSettings: value
    });
  }
});
</script>

<style scoped>
.general-tab {
  padding: 0.25rem 0.5rem;
}

.field {
  margin-bottom: 0.25rem;
}

.field-horizontal {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 0;
}

.field-horizontal label {
  flex-shrink: 0;
  min-width: 180px;
  font-weight: 600;
  color: var(--text-color);
  font-size: 0.9rem;
  margin: 0;
}

.field-input-flex {
  flex: 1;
}

.field label {
  display: block;
  margin-bottom: 0.25rem;
  font-weight: 600;
  color: var(--text-color);
  font-size: 0.9rem;
}

.field label.required::after {
  content: " *";
  color: var(--red-500);
}

.w-full {
  width: 100%;
}

.mt-2 {
  margin-top: 0.5rem;
}

/* Компактные панели */
:deep(.p-panel) {
  font-size: 0.9rem;
}

:deep(.p-panel-header) {
  padding: 0.5rem 0.75rem;
  font-size: 0.9rem;
}

:deep(.p-panel-content) {
  padding: 0.5rem 0.75rem;
}

/* Компактные input элементы */
:deep(.p-inputtext),
:deep(.p-inputnumber-input) {
  padding: 0.375rem 0.5rem;
  font-size: 0.9rem;
}

:deep(.p-inputswitch) {
  width: 2.5rem;
  height: 1.5rem;
}

:deep(.p-inputswitch .p-inputswitch-slider) {
  transform: translateX(0);
}

:deep(.p-inputswitch.p-inputswitch-checked .p-inputswitch-slider) {
  transform: translateX(1rem);
}
</style>
