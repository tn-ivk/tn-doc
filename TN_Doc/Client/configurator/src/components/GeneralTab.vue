<template>
  <div class="general-tab">
    <!-- Тип ИВК -->
    <Panel header="Тип ИВК" class="settings-panel ivk-type-panel">
      <div class="settings-container">
        <div class="field field-horizontal">
          <label for="ivk-type">Тип комплекса:</label>
          <Select
            id="ivk-type"
            v-model="selectedIvkType"
            :options="ivkTypeOptions"
            option-label="label"
            option-value="value"
            placeholder="Не выбрано"
            class="field-input-flex"
          />
        </div>
      </div>
    </Panel>

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
      <ToggleSwitch
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
          :allowEmpty="false"
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

    <!-- Настройки ЕЛИС -->
    <Panel header="Настройки ЕЛИС" class="settings-panel">
      <div class="settings-container">
        <div class="field field-horizontal">
          <label for="use-elis">Использовать ЕЛИС:</label>
          <ToggleSwitch
            id="use-elis"
            v-model="elisEnabled"
          />
        </div>

        <!-- Поля настроек ЕЛИС (отображаются только когда ЕЛИС включен) -->
        <template v-if="elisEnabled">
          <div class="field field-horizontal">
            <label for="elis-ost-key">OstKey:</label>
            <InputText
              id="elis-ost-key"
              v-model="elisOstKey"
              placeholder="Ключ ОСТ"
              class="field-input-flex"
            />
          </div>

          <div class="field field-horizontal">
            <label for="elis-sikn-key">SiknKey:</label>
            <InputText
              id="elis-sikn-key"
              v-model="elisSiknKey"
              placeholder="Ключ СИКН"
              class="field-input-flex"
            />
          </div>

          <div class="field field-horizontal">
            <label for="elis-client-name">ClientName:</label>
            <InputText
              id="elis-client-name"
              v-model="elisClientName"
              placeholder="Имя клиента"
              class="field-input-flex"
            />
          </div>

          <div class="field field-horizontal">
            <label for="elis-client-token">ClientToken:</label>
            <InputText
              id="elis-client-token"
              v-model="elisClientToken"
              placeholder="Токен клиента"
              class="field-input-flex"
              disabled
            />
          </div>
        </template>
      </div>
    </Panel>

    <!-- Настройки диагностики связи с ИВК -->
    <Panel header="Диагностика связи с ИВК" class="settings-panel">
      <div class="settings-container">
        <div class="field field-horizontal">
          <label for="diag-initial-poll">Начальный интервал опроса (сек):</label>
          <InputNumber
            id="diag-initial-poll"
            v-model="diagInitialPollSeconds"
            :min="1"
            :max="3600"
            class="field-input-flex"
          />
        </div>

        <div class="field field-horizontal">
          <label for="diag-max-poll">Максимальный интервал опроса (сек):</label>
          <InputNumber
            id="diag-max-poll"
            v-model="diagMaxPollSeconds"
            :min="60"
            :max="86400"
            class="field-input-flex"
          />
        </div>

        <div class="field field-horizontal">
          <label for="diag-poll-multiplier">Множитель интервала:</label>
          <InputNumber
            id="diag-poll-multiplier"
            v-model="diagPollMultiplier"
            :min="1.1"
            :max="10"
            :step="0.1"
            :minFractionDigits="1"
            :maxFractionDigits="1"
            class="field-input-flex"
          />
        </div>

        <div class="field field-horizontal">
          <label for="diag-failure-threshold">Порог сетевых ошибок:</label>
          <InputNumber
            id="diag-failure-threshold"
            v-model="diagNetworkFailureThreshold"
            :min="1"
            :max="100"
            class="field-input-flex"
          />
        </div>

        <div class="field field-horizontal">
          <label for="diag-max-retry">Максимум попыток до блокировки:</label>
          <InputNumber
            id="diag-max-retry"
            v-model="diagMaxRetryCount"
            :min="1"
            :max="1000"
            class="field-input-flex"
          />
        </div>
      </div>
    </Panel>

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
import InputNumber from 'primevue/inputnumber';
import ToggleSwitch from 'primevue/toggleswitch';
import SelectButton from 'primevue/selectbutton';
import Select from 'primevue/select';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import OpcSettings from './OpcSettings.vue';
import { OpcType, IvkType } from '../types/config.types';
import { detectIvkType, applyIvkType } from '../utils/ivkTypeUtils';

const configStore = useConfigStore();
const { currentConfig } = storeToRefs(configStore);

const showOpcDialog = ref(false);

// Тип ИВК
const ivkTypeOptions = [
  { label: 'Не выбрано', value: null },
  { label: 'ТН-01', value: IvkType.TN01 },
  { label: 'ТН-02', value: IvkType.TN02 }
];

const selectedIvkType = computed({
  get: () => {
    if (!currentConfig.value?.Devices) return null;
    return detectIvkType(currentConfig.value.Devices);
  },
  set: (value: IvkType | null) => {
    if (!value || !currentConfig.value?.Devices) return;
    applyIvkType(currentConfig.value.Devices, value);
  }
});

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

// ЕЛИС настройки
const elisEnabled = computed({
  get: () => currentConfig.value?.Elis?.Use || false,
  set: (value: boolean) => {
    const currentElis = currentConfig.value?.Elis;
    configStore.updateGeneralSettings({
      Elis: {
        Use: value,
        OstKey: currentElis?.OstKey || '',
        SiknKey: currentElis?.SiknKey || '',
        ClientName: currentElis?.ClientName || '',
        ClientToken: currentElis?.ClientToken || ''
      }
    });
  }
});

const elisOstKey = computed({
  get: () => currentConfig.value?.Elis?.OstKey || '',
  set: (value: string) => {
    const currentElis = currentConfig.value?.Elis;
    configStore.updateGeneralSettings({
      Elis: {
        Use: currentElis?.Use || false,
        OstKey: value,
        SiknKey: currentElis?.SiknKey || '',
        ClientName: currentElis?.ClientName || '',
        ClientToken: currentElis?.ClientToken || ''
      }
    });
  }
});

const elisSiknKey = computed({
  get: () => currentConfig.value?.Elis?.SiknKey || '',
  set: (value: string) => {
    const currentElis = currentConfig.value?.Elis;
    configStore.updateGeneralSettings({
      Elis: {
        Use: currentElis?.Use || false,
        OstKey: currentElis?.OstKey || '',
        SiknKey: value,
        ClientName: currentElis?.ClientName || '',
        ClientToken: currentElis?.ClientToken || ''
      }
    });
  }
});

const elisClientName = computed({
  get: () => currentConfig.value?.Elis?.ClientName || '',
  set: (value: string) => {
    const currentElis = currentConfig.value?.Elis;
    configStore.updateGeneralSettings({
      Elis: {
        Use: currentElis?.Use || false,
        OstKey: currentElis?.OstKey || '',
        SiknKey: currentElis?.SiknKey || '',
        ClientName: value,
        ClientToken: currentElis?.ClientToken || ''
      }
    });
  }
});

const elisClientToken = computed({
  get: () => currentConfig.value?.Elis?.ClientToken || '',
  set: (value: string) => {
    // Это поле только для чтения, setter не должен ничего делать
    // Но оставляем его для соответствия интерфейсу v-model
  }
});

// Настройки диагностики подключений
const getDefaultDiagnosticSettings = () => ({
  InitialPollSeconds: 60,
  MaxPollSeconds: 3600,
  PollMultiplier: 2.0,
  NetworkFailureThreshold: 3,
  MaxRetryCount: 24
});

const diagInitialPollSeconds = computed({
  get: () => currentConfig.value?.DeviceConnectionDiagnostic?.InitialPollSeconds ?? 60,
  set: (value: number) => {
    const current = currentConfig.value?.DeviceConnectionDiagnostic ?? getDefaultDiagnosticSettings();
    configStore.updateGeneralSettings({
      DeviceConnectionDiagnostic: { ...current, InitialPollSeconds: value }
    });
  }
});

const diagMaxPollSeconds = computed({
  get: () => currentConfig.value?.DeviceConnectionDiagnostic?.MaxPollSeconds ?? 3600,
  set: (value: number) => {
    const current = currentConfig.value?.DeviceConnectionDiagnostic ?? getDefaultDiagnosticSettings();
    configStore.updateGeneralSettings({
      DeviceConnectionDiagnostic: { ...current, MaxPollSeconds: value }
    });
  }
});

const diagPollMultiplier = computed({
  get: () => currentConfig.value?.DeviceConnectionDiagnostic?.PollMultiplier ?? 2.0,
  set: (value: number) => {
    const current = currentConfig.value?.DeviceConnectionDiagnostic ?? getDefaultDiagnosticSettings();
    configStore.updateGeneralSettings({
      DeviceConnectionDiagnostic: { ...current, PollMultiplier: value }
    });
  }
});

const diagNetworkFailureThreshold = computed({
  get: () => currentConfig.value?.DeviceConnectionDiagnostic?.NetworkFailureThreshold ?? 3,
  set: (value: number) => {
    const current = currentConfig.value?.DeviceConnectionDiagnostic ?? getDefaultDiagnosticSettings();
    configStore.updateGeneralSettings({
      DeviceConnectionDiagnostic: { ...current, NetworkFailureThreshold: value }
    });
  }
});

const diagMaxRetryCount = computed({
  get: () => currentConfig.value?.DeviceConnectionDiagnostic?.MaxRetryCount ?? 24,
  set: (value: number) => {
    const current = currentConfig.value?.DeviceConnectionDiagnostic ?? getDefaultDiagnosticSettings();
    configStore.updateGeneralSettings({
      DeviceConnectionDiagnostic: { ...current, MaxRetryCount: value }
    });
  }
});
</script>

<style scoped>
/* Унифицированные spacing переменные */
.general-tab {
  --space-1: 0.25rem;
  --space-2: 0.5rem;
  --space-3: 0.75rem;
  --space-4: 1rem;
}

.general-tab {
  padding: var(--space-2);
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow-y: auto;
}

.field {
  margin-bottom: var(--space-4);
  flex-shrink: 0;
}

.field-horizontal {
  display: flex;
  align-items: center;
  gap: var(--space-4);
  margin-bottom: var(--space-2);
}

.field-horizontal label {
  flex-shrink: 0;
  min-width: 280px;
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

/* Стили для Select (комбобокс типа ИВК) */
:deep(.field-input-flex.p-select) {
  flex: 1;
  border: 1px solid #CFD8DC !important;
  border-radius: 8px !important;
  height: 37px !important;
  background-color: #ffffff !important;
  font-size: 15px !important;
  transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out !important;
}

:deep(.field-input-flex.p-select:hover) {
  border-color: #B0BEC5 !important;
}

:deep(.field-input-flex.p-select.p-focus) {
  outline: none !important;
  border-color: #1E88E5 !important;
  box-shadow: 0 0 0 3px rgba(30, 136, 229, 0.35) !important;
}

:deep(.field-input-flex.p-select .p-select-label) {
  padding: 6px 10px !important;
  color: #212121 !important;
  font-size: 15px !important;
}

/* Панель типа ИВК — первая на вкладке */
.ivk-type-panel {
  margin-bottom: var(--space-4);
}

/* Стили для поля "Путь экспорта документов" */
:deep(.p-inputtext#export-path) {
  border: 1px solid #CFD8DC !important;
  border-radius: 8px !important;
  padding: 6px 10px !important;
  height: 37px !important;
  background-color: #ffffff !important;
  color: #212121 !important;
  font-size: 15px !important;
  transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out !important;
}

:deep(.p-inputtext#export-path:focus) {
  outline: none !important;
  border-color: #1E88E5 !important;
  box-shadow: 0 0 0 3px rgba(30, 136, 229, 0.35) !important;
}

:deep(.p-inputtext#export-path:hover) {
  border-color: #B0BEC5 !important;
}

/* Стили для панелей настроек */
:deep(.settings-panel.p-panel) {
  margin-top: var(--space-2);
  background: transparent !important;
  border: 1px solid var(--md-outline, #CFD8DC);
  border-radius: 8px;
  transition: box-shadow 0.2s ease;
}

:deep(.settings-panel.p-panel:hover) {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
}

:deep(.settings-panel .p-panel-header) {
  padding: var(--space-3);
  font-size: 0.9rem;
  background: transparent !important;
  border-bottom: 1px solid var(--md-outline, #CFD8DC);
}

:deep(.settings-panel .p-panel-content) {
  padding: var(--space-3);
  background: transparent !important;
}

.settings-container {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.settings-container .field-horizontal {
  margin-bottom: 0;
}

/* Стили для InputNumber с классом field-input-flex */
:deep(.field-input-flex.p-inputnumber) {
  flex: 1;
}

:deep(.field-input-flex.p-inputnumber .p-inputnumber-input) {
  width: 100%;
  border: 1px solid #CFD8DC !important;
  border-radius: 8px !important;
  padding: 6px 10px !important;
  height: 37px !important;
  background-color: #ffffff !important;
  color: #212121 !important;
  font-size: 15px !important;
  transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out !important;
}

:deep(.field-input-flex.p-inputnumber .p-inputnumber-input:focus) {
  outline: none !important;
  border-color: #1E88E5 !important;
  box-shadow: 0 0 0 3px rgba(30, 136, 229, 0.35) !important;
}

:deep(.field-input-flex.p-inputnumber .p-inputnumber-input:hover) {
  border-color: #B0BEC5 !important;
}

/* Стили для InputNumber с кнопками */
:deep(.field-input-flex.p-inputnumber.p-inputnumber-buttons-stacked .p-inputnumber-input) {
  border-top-right-radius: 0 !important;
  border-bottom-right-radius: 0 !important;
}

:deep(.field-input-flex.p-inputnumber .p-inputnumber-button-group) {
  border: 1px solid #CFD8DC !important;
  border-left: none !important;
  border-radius: 0 8px 8px 0 !important;
  overflow: hidden;
}

:deep(.field-input-flex.p-inputnumber .p-inputnumber-button) {
  background-color: #ffffff !important;
  border: none !important;
  color: #616161 !important;
  transition: all 0.15s ease-in-out !important;
  width: 2rem !important;
}

:deep(.field-input-flex.p-inputnumber .p-inputnumber-button:hover) {
  background-color: #F5F5F5 !important;
  color: #212121 !important;
}

:deep(.field-input-flex.p-inputnumber .p-inputnumber-button:active) {
  background-color: #BDBDBD !important;
}

/* Стили для полей ЕЛИС */
:deep(.p-inputtext#elis-ost-key),
:deep(.p-inputtext#elis-sikn-key),
:deep(.p-inputtext#elis-client-name),
:deep(.p-inputtext#elis-client-token) {
  border: 1px solid #CFD8DC !important;
  border-radius: 8px !important;
  padding: 6px 10px !important;
  height: 37px !important;
  background-color: #ffffff !important;
  color: #212121 !important;
  font-size: 15px !important;
  transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out !important;
}

:deep(.p-inputtext#elis-ost-key:focus),
:deep(.p-inputtext#elis-sikn-key:focus),
:deep(.p-inputtext#elis-client-name:focus) {
  outline: none !important;
  border-color: #1E88E5 !important;
  box-shadow: 0 0 0 3px rgba(30, 136, 229, 0.35) !important;
}

:deep(.p-inputtext#elis-ost-key:hover),
:deep(.p-inputtext#elis-sikn-key:hover),
:deep(.p-inputtext#elis-client-name:hover) {
  border-color: #B0BEC5 !important;
}

/* Disabled поле ClientToken */
:deep(.p-inputtext#elis-client-token:disabled) {
  background-color: #F5F5F5 !important;
  color: #9E9E9E !important;
  cursor: not-allowed !important;
  border-color: #E0E0E0 !important;
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

/* Стили для кнопки настроек OPC (многоточие) */
:deep(.p-button.p-button-icon-only.p-button-secondary.p-button-text.p-button-sm) {
  background-color: var(--md-surface-variant, #F1F3F4) !important;
  color: var(--md-text-muted, #495057) !important;
  border: 1px solid var(--md-outline, #CFD8DC) !important;
  border-radius: 0.25rem !important;
  transition: all 0.15s ease-in-out !important;
}

:deep(.p-button.p-button-icon-only.p-button-secondary.p-button-text.p-button-sm:hover) {
  background-color: var(--md-surface-variant, #F1F3F4) !important;
  color: var(--md-text, #212121) !important;
  border-color: var(--md-outline-light, #E0E0E0) !important;
}

/* Кастомные стили для переключателя OPC */
:deep(.p-selectbutton .p-togglebutton) {
  padding: 0.25rem 0.5rem !important;
  font-size: 0.8rem !important;
  border: 1px solid #CFD8DC !important;
  background-color: #ffffff !important;
  color: #212121 !important;
  transition: all 0.15s ease-in-out !important;
  min-height: 28px !important;
}

:deep(.p-selectbutton .p-togglebutton:hover) {
  background-color: #F1F3F4 !important;
  border-color: #B0BEC5 !important;
}

:deep(.p-selectbutton .p-togglebutton.p-togglebutton-checked) {
  background-color: #1E88E5 !important;
  border-color: #1E88E5 !important;
  color: #ffffff !important;
}

:deep(.p-selectbutton .p-togglebutton.p-togglebutton-checked:hover) {
  background-color: #1565C0 !important;
  border-color: #1565C0 !important;
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
