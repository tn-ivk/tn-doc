<template>
  <div class="general-tab">
    <Panel header="Путь экспорта документов">
      <div class="field field-horizontal">
        <label for="export-path">Путь:</label>
        <div class="field-input">
          <InputText
            id="export-path"
            v-model="exportPath"
            placeholder="Путь для сохранения экспортируемых документов"
            class="w-full"
          />
          <small v-if="!exportPath" class="p-error">Обязательное поле</small>
        </div>
      </div>
    </Panel>

    <Panel header="Функции безопасности" class="mt-2">
      <div class="field field-horizontal">
        <label for="use-security">Использовать:</label>
        <InputSwitch
          id="use-security"
          v-model="useSecurityFeatures"
        />
      </div>
    </Panel>

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
  padding: 0.5rem 1rem;
}

.field {
  margin-bottom: 0.5rem;
}

.field-horizontal {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 0;
}

.field-horizontal label {
  flex-shrink: 0;
  min-width: 100px;
  font-weight: 600;
  color: var(--text-color);
  font-size: 0.9rem;
  margin: 0;
}

.field-input {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
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
  margin-top: 0.75rem;
}
</style>
