<template>
  <div class="general-tab">
    <Panel header="Общие настройки">
      <div class="field">
        <label for="export-path" class="required">Путь экспорта документов</label>
        <InputText
          id="export-path"
          v-model="exportPath"
          placeholder="/opt/TN_Doc/Export"
          class="w-full"
        />
        <small v-if="!exportPath" class="p-error">Обязательное поле</small>
      </div>

      <div class="field">
        <label for="use-security">Использовать функции безопасности</label>
        <InputSwitch
          id="use-security"
          v-model="useSecurityFeatures"
        />
      </div>
    </Panel>

    <Panel header="Настройки локального OPC-клиента" class="mt-3">
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
  padding: 1rem;
}

.field {
  margin-bottom: 1.5rem;
}

.field label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 600;
  color: var(--text-color);
}

.field label.required::after {
  content: " *";
  color: var(--red-500);
}

.w-full {
  width: 100%;
}

.mt-3 {
  margin-top: 1rem;
}
</style>
