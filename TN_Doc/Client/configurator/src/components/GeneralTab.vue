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
  get: () => currentConfig.value?.ArmOpcConnectionSettings?.Type || OpcType.UA,
  set: (value: OpcType) => {
    const currentSettings = currentConfig.value?.ArmOpcConnectionSettings;
    if (currentSettings) {
      configStore.updateGeneralSettings({
        ArmOpcConnectionSettings: {
          ...currentSettings,
          Type: value
        }
      });
    } else {
      // Создаем новые настройки с дефолтными значениями
      configStore.updateGeneralSettings({
        ArmOpcConnectionSettings: {
          Type: value,
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
  padding: 0.25rem 0.5rem;
}

.field {
  margin-bottom: 1rem;
}

.field-horizontal {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 0.75rem;
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

.opc-controls {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex: 1;
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

/* Кастомные стили для переключателя OPC */
:deep(.p-selectbutton .p-togglebutton) {
  padding: 0.375rem 0.75rem !important;
  font-size: 0.9rem !important;
  border: 1px solid #dee2e6 !important;
  background-color: #ffffff !important;
  color: #495057 !important;
  transition: all 0.15s ease-in-out !important;
}

:deep(.p-selectbutton .p-togglebutton:hover) {
  background-color: #f8f9fa !important;
  border-color: #adb5bd !important;
}

:deep(.p-selectbutton .p-togglebutton.p-togglebutton-checked) {
  background-color: #0d6efd !important;
  border-color: #0d6efd !important;
  color: #ffffff !important;
}

:deep(.p-selectbutton .p-togglebutton.p-togglebutton-checked:hover) {
  background-color: #0b5ed7 !important;
  border-color: #0a58ca !important;
}

/* Исправляем внутренний контент активной кнопки */
:deep(.p-selectbutton .p-togglebutton.p-togglebutton-checked .p-togglebutton-content) {
  background-color: transparent !important;
  color: #ffffff !important;
}

:deep(.p-selectbutton .p-togglebutton.p-togglebutton-checked .p-togglebutton-label) {
  background-color: transparent !important;
  color: #ffffff !important;
}
</style>
