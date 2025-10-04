<template>
  <div class="device-editor">
    <div v-if="selectedDevices.length === 0" class="no-selection">
      <i class="pi pi-info-circle" style="font-size: 2rem; color: var(--text-color-secondary)"></i>
      <p>Выберите устройство из списка для редактирования</p>
    </div>

    <div v-else class="editor-content">
      <div class="editor-header">
        <h3 v-if="selectedDevices.length === 1">
          {{ selectedDevices[0].Name }}
        </h3>
        <h3 v-else>
          Групповое редактирование ({{ selectedDevices.length }} устройств)
        </h3>
      </div>

      <div class="editor-sections">
        <!-- Использование устройства -->
        <Panel header="Основные настройки">
          <div class="field">
            <label>Использовать устройство</label>
            <div class="flex align-items-center">
              <InputSwitch v-model="deviceUse" />
              <MixedStateWarning v-if="isMixed('Use')" class="ml-2" />
            </div>
          </div>
        </Panel>

        <!-- Список документов -->
        <Panel header="Документы" class="mt-3">
          <div class="field">
            <label>Используемые документы</label>
            <MultiSelect
              v-model="deviceDocs"
              :options="availableDocs"
              option-label="Name"
              option-value="IdDoc"
              placeholder="Выберите документы"
              class="w-full"
              display="chip"
            />
            <MixedStateWarning v-if="isMixed('Docs')" class="mt-2" />
          </div>
        </Panel>

        <!-- База данных -->
        <Panel header="Подключение к БД" class="mt-3">
          <div v-if="hasDBConnections">
            <div class="field">
              <label>Сервер</label>
              <InputText
                v-model="dbServer"
                placeholder="localhost"
                class="w-full"
              />
              <MixedStateWarning v-if="isMixed('DBConnectionStrings.Server')" class="mt-2" />
            </div>

            <div class="field">
              <label>База данных</label>
              <InputText
                v-model="dbDatabase"
                placeholder="ivk_db"
                class="w-full"
              />
              <MixedStateWarning v-if="isMixed('DBConnectionStrings.Database')" class="mt-2" />
            </div>

            <div class="field">
              <label>Таймаут (сек)</label>
              <InputNumber
                v-model="dbTimeout"
                :min="1"
                :max="300"
              />
              <MixedStateWarning v-if="isMixed('DBConnectionStrings.ConnectionTimeout')" class="mt-2" />
            </div>
          </div>
          <Message v-else severity="info">
            Подключения к БД не настроены
          </Message>
        </Panel>

        <!-- OPC настройки -->
        <Panel header="OPC подключение" class="mt-3">
          <OpcSettings
            v-if="deviceOpcSettings"
            v-model="deviceOpcSettings"
            :show-type-selector="true"
          />
          <Message v-else severity="info">
            OPC настройки не заданы
          </Message>
        </Panel>

        <!-- Недопустимые символы -->
        <Panel header="Недопустимые символы" class="mt-3">
          <div class="invalid-chars-section">
            <div class="field-checkbox">
              <Checkbox
                v-model="invalidChars"
                input-id="char-quote"
                value='"'
              />
              <label for="char-quote">Двойные кавычки (")</label>
            </div>

            <div class="field-checkbox">
              <Checkbox
                v-model="invalidChars"
                input-id="char-apostrophe"
                value="'"
              />
              <label for="char-apostrophe">Одинарные кавычки (')</label>
            </div>

            <div class="field-checkbox">
              <Checkbox
                v-model="invalidChars"
                input-id="char-backslash"
                value="\\"
              />
              <label for="char-backslash">Обратный слеш (\\)</label>
            </div>

            <MixedStateWarning v-if="isMixed('InvalidChars')" class="mt-2" />
          </div>
        </Panel>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { storeToRefs } from 'pinia';
import { useConfigStore } from '../stores/configStore';
import type { Device, OpcConnectionSettings } from '../types/config.types';
import _ from 'lodash';

import Panel from 'primevue/panel';
import InputSwitch from 'primevue/inputswitch';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import MultiSelect from 'primevue/multiselect';
import Checkbox from 'primevue/checkbox';
import Message from 'primevue/message';
import OpcSettings from './OpcSettings.vue';
import MixedStateWarning from './MixedStateWarning.vue';

const configStore = useConfigStore();
const { selectedDevices, hasMultipleSelection } = storeToRefs(configStore);

// Все доступные документы (из первого устройства как пример)
const availableDocs = computed(() => {
  if (selectedDevices.value.length === 0) return [];
  return selectedDevices.value[0].Docs || [];
});

// Проверка наличия подключений к БД
const hasDBConnections = computed(() => {
  return selectedDevices.value.some(d => d.DBConnectionStrings && d.DBConnectionStrings.length > 0);
});

// Проверка смешанного состояния для поля
function isMixed(fieldPath: string): boolean {
  if (!hasMultipleSelection.value) return false;

  const values = selectedDevices.value.map(d => _.get(d, fieldPath));
  return !values.every(v => _.isEqual(v, values[0]));
}

// Device Use
const deviceUse = computed({
  get: () => {
    if (selectedDevices.value.length === 0) return false;
    return selectedDevices.value[0].Use;
  },
  set: (value: boolean) => {
    selectedDevices.value.forEach(device => {
      configStore.updateDeviceSettings(device.IdDevice, 'Use', value);
    });
  }
});

// Device Docs
const deviceDocs = computed({
  get: () => {
    if (selectedDevices.value.length === 0) return [];
    return selectedDevices.value[0].Docs
      .filter(d => d.Use)
      .map(d => d.IdDoc);
  },
  set: (value: number[]) => {
    selectedDevices.value.forEach(device => {
      const updatedDocs = device.Docs.map(doc => ({
        ...doc,
        Use: value.includes(doc.IdDoc)
      }));
      configStore.updateDeviceSettings(device.IdDevice, 'Docs', updatedDocs);
    });
  }
});

// DB Server
const dbServer = computed({
  get: () => {
    if (selectedDevices.value.length === 0) return '';
    const conn = selectedDevices.value[0].DBConnectionStrings?.[0];
    return conn?.Server || '';
  },
  set: (value: string) => {
    selectedDevices.value.forEach(device => {
      if (device.DBConnectionStrings && device.DBConnectionStrings.length > 0) {
        const updated = [...device.DBConnectionStrings];
        updated[0] = { ...updated[0], Server: value };
        configStore.updateDeviceSettings(device.IdDevice, 'DBConnectionStrings', updated);
      }
    });
  }
});

// DB Database
const dbDatabase = computed({
  get: () => {
    if (selectedDevices.value.length === 0) return '';
    const conn = selectedDevices.value[0].DBConnectionStrings?.[0];
    return conn?.Database || '';
  },
  set: (value: string) => {
    selectedDevices.value.forEach(device => {
      if (device.DBConnectionStrings && device.DBConnectionStrings.length > 0) {
        const updated = [...device.DBConnectionStrings];
        updated[0] = { ...updated[0], Database: value };
        configStore.updateDeviceSettings(device.IdDevice, 'DBConnectionStrings', updated);
      }
    });
  }
});

// DB Timeout
const dbTimeout = computed({
  get: () => {
    if (selectedDevices.value.length === 0) return 30;
    const conn = selectedDevices.value[0].DBConnectionStrings?.[0];
    return conn?.ConnectionTimeout || 30;
  },
  set: (value: number) => {
    selectedDevices.value.forEach(device => {
      if (device.DBConnectionStrings && device.DBConnectionStrings.length > 0) {
        const updated = [...device.DBConnectionStrings];
        updated[0] = { ...updated[0], ConnectionTimeout: value };
        configStore.updateDeviceSettings(device.IdDevice, 'DBConnectionStrings', updated);
      }
    });
  }
});

// OPC Settings
const deviceOpcSettings = computed({
  get: () => {
    if (selectedDevices.value.length === 0) return undefined;
    return selectedDevices.value[0].OpcConnectionSettings;
  },
  set: (value: OpcConnectionSettings | undefined) => {
    selectedDevices.value.forEach(device => {
      configStore.updateDeviceSettings(device.IdDevice, 'OpcConnectionSettings', value);
    });
  }
});

// Invalid Chars
const invalidChars = computed({
  get: () => {
    if (selectedDevices.value.length === 0) return [];
    return selectedDevices.value[0].InvalidChars || [];
  },
  set: (value: string[]) => {
    selectedDevices.value.forEach(device => {
      configStore.updateDeviceSettings(device.IdDevice, 'InvalidChars', value);
    });
  }
});
</script>

<style scoped>
.device-editor {
  height: 100%;
  overflow-y: auto;
  padding: 0.5rem;
}

.no-selection {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: var(--text-color-secondary);
}

.no-selection p {
  margin-top: 1rem;
  font-size: 1.1rem;
}

.editor-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.editor-header h3 {
  margin: 0;
  color: var(--primary-color);
}

.editor-sections {
  display: flex;
  flex-direction: column;
}

.field {
  margin-bottom: 0.75rem;
}

.field label {
  display: block;
  margin-bottom: 0.25rem;
  font-weight: 600;
  color: var(--text-color);
  font-size: 0.9rem;
}

.field-checkbox {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
}

.field-checkbox label {
  margin: 0;
  font-weight: normal;
}

.invalid-chars-section {
  padding: 0.5rem 0;
}

.w-full {
  width: 100%;
}

.mt-2 {
  margin-top: 0.5rem;
}

.mt-3 {
  margin-top: 0.75rem;
}

/* Компактные панели */
:deep(.p-panel-header) {
  padding: 0.5rem 0.75rem;
  font-size: 0.9rem;
}

:deep(.p-panel-content) {
  padding: 0.5rem 0.75rem;
}

/* Компактные input элементы */
:deep(.p-inputtext),
:deep(.p-inputnumber-input),
:deep(.p-multiselect) {
  padding: 0.375rem 0.5rem;
  font-size: 0.9rem;
}

:deep(.p-inputswitch) {
  width: 2.5rem;
  height: 1.5rem;
}

.ml-2 {
  margin-left: 0.5rem;
}

.flex {
  display: flex;
}

.align-items-center {
  align-items: center;
}
</style>
