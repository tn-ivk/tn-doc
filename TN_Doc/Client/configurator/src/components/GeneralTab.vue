<template>
  <div class="general-tab">
    <div class="field field-horizontal">
      <label for="export-path">Путь экспорта документов:</label>
      <InputText
        id="export-path"
        v-model="exportPath"
        placeholder="Путь для сохранения экспортируемых документов"
        class="field-input-flex export-path-input"
      />
    </div>

    <div class="field field-horizontal">
      <label for="use-security">Функции безопасности:</label>
      <InputSwitch
        id="use-security"
        v-model="useSecurityFeatures"
      />
    </div>

    <div class="field field-horizontal">
      <label>Настройки локального OPC-клиента:</label>
      <div class="opc-controls">
        <SelectButton
          v-model="opcType"
          :options="opcTypes"
          option-label="label"
          option-value="value"
        />
        <Button
          icon="pi pi-ellipsis-h"
          @click="showOpcDialog = true"
          severity="secondary"
          text
          size="small"
          aria-label="Настройки OPC"
          class="opc-settings-button"
        />
      </div>
    </div>

    <!-- Модальное окно настроек OPC -->
    <Dialog
      v-model:visible="showOpcDialog"
      modal
      header="Настройки OPC"
      :style="{ width: '500px' }"
    >
      <OpcSettings
        v-model="armOpcSettings"
        :show-type-selector="false"
      />
      <template #footer>
        <Button
          label="Закрыть"
          icon="pi pi-times"
          @click="showOpcDialog = false"
          severity="secondary"
        />
      </template>
    </Dialog>
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
import SelectButton from 'primevue/selectbutton';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import OpcSettings from './OpcSettings.vue';
import { OpcType } from '../types/config.types';

const configStore = useConfigStore();
const { currentConfig } = storeToRefs(configStore);

const showOpcDialog = ref(false);

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

const opcTypes = [
  { label: 'OPC DA', value: OpcType.DA },
  { label: 'OPC UA', value: OpcType.UA }
];

const opcType = computed({
  get: () => {
    // Получаем значение из API (может быть числом или строкой)
    const apiType = currentConfig.value?.ArmOpcConnectionSettings?.Type;
    
    // Маппинг числовых значений в строковые
    if ((apiType as any) === 0) return OpcType.DA;
    if ((apiType as any) === 1) return OpcType.UA;
    
    // Если значение уже строковое, возвращаем как есть
    if (apiType === OpcType.DA || apiType === OpcType.UA) return apiType;
    
    // Если не удалось замапить, возвращаем undefined для неопределенного состояния
    return undefined;
  },
  set: (value: OpcType) => {
    const currentSettings = currentConfig.value?.ArmOpcConnectionSettings;
    if (currentSettings) {
      // Маппинг строковых значений в числовые для API
      const numericValue = value === OpcType.DA ? 0 : 1;
      
      configStore.updateGeneralSettings({
        ArmOpcConnectionSettings: {
          ...currentSettings,
          Type: numericValue as any // Временно используем any для обхода типов
        }
      });
    } else {
      // Создаем новые настройки с дефолтными значениями
      const numericValue = value === OpcType.DA ? 0 : 1;
      
      configStore.updateGeneralSettings({
        ArmOpcConnectionSettings: {
          Type: numericValue as any, // Временно используем any для обхода типов
          DaSettings: {
            Host: '127.0.0.1',
            ProgId: 'psregulopcda_01',
            StartPrefix: 'Root.PLC1.IVK_TN_01',
            UpdateRate: 500
          },
          UaSettings: {
            ConfigFilename: 'opcua-config.xml',
            StartPrefix: 'ns=2;s=Root.PLC1',
            UpdateRate: 500
          }
        }
      });
    }
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
  padding: var(--configurator-spacing-2);
  background: var(--configurator-surface);
  height: 100%;
  overflow: auto;
}

.field {
  margin-bottom: var(--configurator-spacing-2);
  padding: var(--configurator-spacing-2);
  background: var(--configurator-surface-variant);
  border-radius: var(--configurator-radius);
  border: 1px solid var(--configurator-outline);
}

.field-horizontal {
  display: flex;
  align-items: center;
  gap: var(--configurator-spacing-2);
  margin-bottom: 0;
}

.field-horizontal label {
  flex-shrink: 0;
  min-width: 180px;
  font-weight: var(--configurator-font-weight-semibold);
  color: var(--configurator-text);
  font-size: var(--configurator-font-size-base);
  margin: 0;
}

.field-input-flex {
  flex: 1;
}

.field label {
  display: block;
  margin-bottom: var(--configurator-spacing-1);
  font-weight: var(--configurator-font-weight-semibold);
  color: var(--configurator-text);
  font-size: var(--configurator-font-size-base);
}

.field label.required::after {
  content: " *";
  color: var(--configurator-error);
}

.w-full {
  width: 100%;
}

.opc-controls {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex: 1;
}

.mt-2 {
  margin-top: var(--configurator-spacing-1);
}

/* Стили для поля пути экспорта */
.export-path-input {
  background: #E3F2FD !important;
  border: 2px solid var(--configurator-primary) !important;
  color: var(--configurator-text) !important;
  font-weight: var(--configurator-font-weight-semibold) !important;
}

.export-path-input:focus {
  background: #BBDEFB !important;
  border-color: var(--configurator-primary-hover) !important;
  box-shadow: 0 0 0 3px rgba(30, 136, 229, 0.3) !important;
}

/* Стили для кнопки настроек OPC */
.opc-settings-button {
  background: var(--configurator-primary) !important;
  color: white !important;
  border: 2px solid var(--configurator-primary) !important;
  border-radius: var(--configurator-radius) !important;
  padding: 8px 12px !important;
  height: 37px !important;
  min-width: 37px !important;
  transition: all 0.2s ease-in-out !important;
  box-shadow: 0 2px 4px rgba(30, 136, 229, 0.3) !important;
}

.opc-settings-button:hover {
  background: var(--configurator-primary-hover) !important;
  border-color: var(--configurator-primary-hover) !important;
  box-shadow: 0 4px 8px rgba(30, 136, 229, 0.4) !important;
  transform: translateY(-1px) !important;
}

.opc-settings-button:active {
  transform: translateY(0) !important;
  box-shadow: 0 2px 4px rgba(30, 136, 229, 0.3) !important;
}
</style>
