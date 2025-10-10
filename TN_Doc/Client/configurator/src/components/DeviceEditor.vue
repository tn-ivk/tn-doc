<template>
  <div class="device-editor">
    <div v-if="selectedDevices.length === 0" class="no-selection">
      <i class="pi pi-info-circle" style="font-size: 2rem; color: var(--configurator-text-secondary)"></i>
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
        <!-- Использование устройства (без панели для уменьшения высоты) -->
        <div class="field field-horizontal compact-section">
          <label for="device-use">Использовать устройство:</label>
            <div class="flex align-items-center">
              <InputSwitch id="device-use" v-model="deviceUse" />
              <MixedStateWarning v-if="isMixed('Use')" class="ml-2" />
            </div>
          </div>

        <!-- Список документов -->
        <Panel header="Документы" class="mt-3">
          <template v-if="!hasMultipleSelection">
            <DataTable :value="docsList" dataKey="IdDoc" :rows="10" responsive-layout="scroll" class="docs-table">
              <Column field="Name" />
              <Column :bodyStyle="{ width: '3rem' }" :headerStyle="{ width: '3rem' }" bodyClass="col-use" class="col-use">
                <template #body="{ data }">
                  <div class="flex align-items-center gap-2">
                    <InputSwitch :model-value="data.Use" @update:model-value="v => onToggleDocUse(data.IdDoc, v)" />
                    <MixedStateWarning v-if="isMixed('Docs')" />
                  </div>
                </template>
              </Column>
              <Column>
                <template #body="{ data }">
                  <div class="flex align-items-center gap-2 flex-wrap">
                    <template v-if="getSelectedTemplates(data)?.length">
                      <Tag 
                        v-for="(tpl, i) in getSelectedTemplates(data).slice(0, 3)" 
                        :key="tpl.Id" 
                        :value="tpl.Name" 
                        rounded 
                        class="template-tag-clickable"
                        @click="toggleTemplateUse(data.IdDoc, tpl.Id)"
                      />
                      <span v-if="getSelectedTemplates(data).length > 3">+{{ getSelectedTemplates(data).length - 3 }}</span>
                    </template>
                    <Button label="Изменить…" size="small" text @click="openTemplatesDialog(data.IdDoc)" />
                  </div>
                </template>
              </Column>
            </DataTable>
          </template>
          <template v-else>
            <Message severity="info">Настройка документов доступна при выборе одного устройства</Message>
          </template>
        </Panel>

        <!-- Диалог выбора шаблонов для документа -->
        <Dialog v-model:visible="templatesDialogVisible" modal header="Выбор шаблонов" :style="{ width: '720px' }">
          <div class="grid">
            <div class="col-12">
              <MultiSelect
                v-model="dialogTemplateIds"
                :options="dialogTemplatesAll"
                option-label="Name"
                option-value="Id"
                class="w-full"
                placeholder="Выберите шаблоны…"
              />
            </div>
          </div>
          <template #footer>
            <Button label="Сохранить" class="btn-primary" @click="saveTemplatesForCurrentDoc" />
            <Button label="Отмена" severity="secondary" @click="templatesDialogVisible = false" />
          </template>
        </Dialog>

        <!-- База данных -->
        <Panel header="Подключение к БД" class="mt-3">
          <div v-if="hasDBConnections">
            <!-- Карточки подключений к БД -->
            <div class="db-connections-cards">
              <div 
                v-for="(conn, index) in allDBConnections" 
                :key="index" 
                class="db-connection-card"
                :class="{ 'connection-active': conn.Use, 'connection-inactive': !conn.Use }"
              >
                <div class="card-header">
                  <div class="connection-status">
                    <i :class="conn.Use ? 'pi pi-check-circle text-green-500' : 'pi pi-times-circle text-red-500'" />
                    <span class="connection-title">Подключение {{ index + 1 }}</span>
                  </div>
                  <div class="connection-toggle">
                    <InputSwitch 
                      :model-value="conn.Use" 
                      @update:model-value="v => toggleConnectionUse(index, v)"
                    />
                  </div>
                </div>
                
                <div class="card-content">
                  <div class="connection-field-horizontal">
                    <label>Сервер:</label>
                    <InputText
                      :model-value="conn.Server"
                      @update:model-value="v => updateConnectionField(index, 'Server', v)"
                      placeholder="localhost"
                      class="w-full"
                    />
                  </div>
                  
                  <div class="connection-field-horizontal">
                    <label>Пользователь:</label>
                    <InputText
                      :model-value="conn.Userid"
                      @update:model-value="v => updateConnectionField(index, 'Userid', v)"
                      placeholder="user"
                      class="w-full"
                    />
                  </div>
                  
                  <div class="connection-field-horizontal">
                    <label>Пароль:</label>
                    <InputText
                      :model-value="conn.Password"
                      placeholder="Пароль скрыт"
                      type="password"
                      readonly
                      class="w-full password-readonly"
                    />
                  </div>
                  
                  <div class="connection-field-horizontal">
                    <label>База данных:</label>
                    <InputText
                      :model-value="conn.Database"
                      @update:model-value="v => updateConnectionField(index, 'Database', v)"
                      placeholder="ivk_db"
                      class="w-full"
                    />
                  </div>
                  
                  <div class="connection-field-horizontal">
                    <label>Таймаут (сек):</label>
                    <InputNumber
                      :model-value="conn.ConnectionTimeout"
                      @update:model-value="v => updateConnectionField(index, 'ConnectionTimeout', v)"
                      :min="1"
                      :max="300"
                    />
                  </div>
                </div>
              </div>
            </div>
          </div>
          <Message v-else severity="info">
            Подключения к БД не настроены
          </Message>
        </Panel>

        <!-- OPC настройки -->
        <Panel header="OPC подключение" class="mt-3">
          <div v-if="deviceOpcSettings" class="opc-settings-container">
            <div class="field field-horizontal">
              <label>Тип OPC:</label>
              <div class="opc-controls">
                <SelectButton
                  v-model="deviceOpcType"
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
          </div>
          <Message v-else severity="info">
            OPC настройки не заданы
          </Message>
        </Panel>

        <!-- Модальное окно настроек OPC -->
        <Dialog
          v-model:visible="showOpcDialog"
          modal
          header="Настройки OPC"
          :style="{ width: '500px' }"
        >
          <OpcSettings
            v-model="deviceOpcSettings"
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

        <!-- Недопустимые символы -->
        <Panel header="Недопустимые символы" class="mt-3">
          <div class="field field-horizontal">
            <label>Недопустимые символы:</label>
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
            </div>
          </div>
          <MixedStateWarning v-if="isMixed('InvalidChars')" class="mt-2" />
        </Panel>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { useConfigStore } from '../stores/configStore';
import type { Device, OpcConnectionSettings } from '../types/config.types';
import { OpcType } from '../types/config.types';
import _ from 'lodash';

import Panel from 'primevue/panel';
import InputSwitch from 'primevue/inputswitch';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Password from 'primevue/password';
import MultiSelect from 'primevue/multiselect';
import Checkbox from 'primevue/checkbox';
import Message from 'primevue/message';
import SelectButton from 'primevue/selectbutton';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Tag from 'primevue/tag';
import OpcSettings from './OpcSettings.vue';
import MixedStateWarning from './MixedStateWarning.vue';
import DocumentTemplates from './DocumentTemplates.vue';

const configStore = useConfigStore();
const { selectedDevices, hasMultipleSelection } = storeToRefs(configStore);

const showOpcDialog = ref(false);

const opcTypes = [
  { label: 'OPC DA', value: OpcType.DA },
  { label: 'OPC UA', value: OpcType.UA }
];

// Документы текущего устройства (только при одиночном выборе)
const docsList = computed(() => {
  if (selectedDevices.value.length !== 1) return [] as Device['Docs'];
  return selectedDevices.value[0].Docs || [];
});

function onToggleDocUse(docId: number, use: boolean) {
  if (selectedDevices.value.length !== 1) return;
  const device = selectedDevices.value[0];
  const updatedDocs = device.Docs.map(d => d.IdDoc === docId ? { ...d, Use: use } : d);
  configStore.updateDeviceSettings(device.IdDevice, 'Docs', updatedDocs);
}

function getSelectedTemplates(doc: Device['Docs'][number]) {
  return (doc.TemplateDocs || []).filter(t => t.Use);
}

// Состояние диалога выбора шаблонов
const templatesDialogVisible = ref(false);
const dialogDocId = ref<number | null>(null);
const dialogTemplateIds = ref<number[]>([]);

const dialogTemplatesAll = computed(() => {
  if (selectedDevices.value.length !== 1 || dialogDocId.value == null) return [] as any[];
  const device = selectedDevices.value[0];
  const doc = device.Docs.find(d => d.IdDoc === dialogDocId.value);
  return doc?.TemplateDocs || [];
});

function templateNameById(id: number): string {
  const all = dialogTemplatesAll.value as any[];
  return all.find(t => t.Id === id)?.Name || String(id);
}

function openTemplatesDialog(docId: number) {
  if (selectedDevices.value.length !== 1) return;
  dialogDocId.value = docId;
  const device = selectedDevices.value[0];
  const doc = device.Docs.find(d => d.IdDoc === docId);
  dialogTemplateIds.value = (doc?.TemplateDocs || []).filter(t => t.Use).map(t => t.Id);
  templatesDialogVisible.value = true;
}

function saveTemplatesForCurrentDoc() {
  if (selectedDevices.value.length !== 1 || dialogDocId.value == null) return;
  const device = selectedDevices.value[0];
  const updatedDocs = device.Docs.map(d => {
    if (d.IdDoc !== dialogDocId.value) return d;
    const templates = (d.TemplateDocs || []).map(t => ({ ...t, Use: dialogTemplateIds.value.includes(t.Id) }));
    return { ...d, TemplateDocs: templates };
  });
  configStore.updateDeviceSettings(device.IdDevice, 'Docs', updatedDocs);
  templatesDialogVisible.value = false;
}

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

// DB User
const dbUser = computed({
  get: () => {
    if (selectedDevices.value.length === 0) return '';
    const conn = selectedDevices.value[0].DBConnectionStrings?.[0];
    return conn?.Userid || '';
  },
  set: (value: string) => {
    selectedDevices.value.forEach(device => {
      if (device.DBConnectionStrings && device.DBConnectionStrings.length > 0) {
        const updated = [...device.DBConnectionStrings];
        updated[0] = { ...updated[0], Userid: value };
        configStore.updateDeviceSettings(device.IdDevice, 'DBConnectionStrings', updated);
      }
    });
  }
});

// DB Password
const dbPassword = computed({
  get: () => {
    if (selectedDevices.value.length === 0) return '';
    const conn = selectedDevices.value[0].DBConnectionStrings?.[0];
    return conn?.Password || '';
  },
  set: (value: string) => {
    selectedDevices.value.forEach(device => {
      if (device.DBConnectionStrings && device.DBConnectionStrings.length > 0) {
        const updated = [...device.DBConnectionStrings];
        updated[0] = { ...updated[0], Password: value };
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

// All DB Connections
const allDBConnections = computed(() => {
  if (selectedDevices.value.length === 0) return [];
  return selectedDevices.value[0].DBConnectionStrings || [];
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

// OPC Type
const deviceOpcType = computed({
  get: () => {
    if (selectedDevices.value.length === 0) return OpcType.UA;
    const settings = selectedDevices.value[0].OpcConnectionSettings;
    if (!settings) return OpcType.UA;
    
    // Маппинг числовых значений в строковые
    if ((settings.Type as any) === 0) return OpcType.DA;
    if ((settings.Type as any) === 1) return OpcType.UA;
    
    // Если значение уже строковое, возвращаем как есть
    if (settings.Type === OpcType.DA || settings.Type === OpcType.UA) return settings.Type;
    
    return OpcType.UA;
  },
  set: (value: OpcType) => {
    selectedDevices.value.forEach(device => {
      const currentSettings = device.OpcConnectionSettings;
      if (currentSettings) {
        // Маппинг строковых значений в числовые для API
        const numericValue = value === OpcType.DA ? 0 : 1;
        
        configStore.updateDeviceSettings(device.IdDevice, 'OpcConnectionSettings', {
          ...currentSettings,
          Type: numericValue as any
        });
      } else {
        // Создаем новые настройки с дефолтными значениями
        const numericValue = value === OpcType.DA ? 0 : 1;
        
        configStore.updateDeviceSettings(device.IdDevice, 'OpcConnectionSettings', {
          Type: numericValue as any,
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
        });
      }
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

function handleTemplateUpdate(deviceId: number, docId: number, templateId: number, use: boolean) {
  configStore.updateDocumentTemplate(deviceId, docId, templateId, use);
}

// Переключение использования шаблона по клику на тег
function toggleTemplateUse(docId: number, templateId: number) {
  if (selectedDevices.value.length !== 1) return;
  
  const device = selectedDevices.value[0];
  const doc = device.Docs.find(d => d.IdDoc === docId);
  if (!doc) return;
  
  const template = doc.TemplateDocs?.find(t => t.Id === templateId);
  if (!template) return;
  
  // Переключаем признак использования шаблона
  const updatedDocs = device.Docs.map(d => {
    if (d.IdDoc !== docId) return d;
    const templates = (d.TemplateDocs || []).map(t => 
      t.Id === templateId ? { ...t, Use: !t.Use } : t
    );
    return { ...d, TemplateDocs: templates };
  });
  
  configStore.updateDeviceSettings(device.IdDevice, 'Docs', updatedDocs);
}

// Переключение использования подключения к БД
function toggleConnectionUse(connectionIndex: number, use: boolean) {
  if (selectedDevices.value.length !== 1) return;
  
  const device = selectedDevices.value[0];
  if (!device.DBConnectionStrings || connectionIndex >= device.DBConnectionStrings.length) return;
  
  const updatedConnections = [...device.DBConnectionStrings];
  updatedConnections[connectionIndex] = { ...updatedConnections[connectionIndex], Use: use };
  
  configStore.updateDeviceSettings(device.IdDevice, 'DBConnectionStrings', updatedConnections);
}

// Обновление поля подключения к БД
function updateConnectionField(connectionIndex: number, field: string, value: any) {
  if (selectedDevices.value.length !== 1) return;
  
  const device = selectedDevices.value[0];
  if (!device.DBConnectionStrings || connectionIndex >= device.DBConnectionStrings.length) return;
  
  const updatedConnections = [...device.DBConnectionStrings];
  updatedConnections[connectionIndex] = { ...updatedConnections[connectionIndex], [field]: value };
  
  configStore.updateDeviceSettings(device.IdDevice, 'DBConnectionStrings', updatedConnections);
}
</script>

<style scoped>
.device-editor {
  height: 100%;
  overflow: auto;
  padding: var(--configurator-spacing-2);
  box-sizing: border-box;
  background: var(--configurator-surface);
}

.no-selection {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: var(--configurator-text-secondary);
}

.no-selection p {
  margin-top: 1rem;
  font-size: 1.1rem;
  font-family: var(--configurator-font-family);
}

.editor-content {
  display: flex;
  flex-direction: column;
  gap: var(--configurator-spacing-1);
}

.editor-header {
  margin-bottom: var(--configurator-spacing-2);
  padding-bottom: var(--configurator-spacing-1);
  border-bottom: 2px solid var(--configurator-outline);
}

.editor-header h3 {
  margin: 0;
  color: var(--configurator-primary);
  font-family: var(--configurator-font-family);
  font-size: 1.25rem;
  font-weight: var(--configurator-font-weight-semibold);
  line-height: 1.3;
}

.editor-sections {
  display: flex;
  flex-direction: column;
  gap: var(--configurator-spacing-2);
}

.field {
  margin-bottom: 0.75rem;
}

.field label {
  display: block;
  margin-bottom: var(--configurator-spacing-1);
  font-weight: var(--configurator-font-weight-semibold);
  color: var(--configurator-text);
  font-size: var(--configurator-font-size-base);
  font-family: var(--configurator-font-family);
}

.field-checkbox {
  display: flex;
  align-items: center;
  gap: var(--configurator-spacing-1);
  margin-bottom: 0.75rem;
}

.field-checkbox label {
  margin: 0;
  font-weight: var(--configurator-font-weight-normal);
}

.invalid-chars-section {
  display: flex;
  gap: 1.5rem;
  align-items: center;
  flex-wrap: wrap;
}

.field-horizontal {
  display: flex;
  align-items: center;
  gap: var(--configurator-spacing-2);
  margin-bottom: 0;
}

.compact-section {
  padding: var(--configurator-spacing-2);
  background: var(--configurator-surface-variant);
  border-radius: var(--configurator-radius);
  border: 1px solid var(--configurator-outline);
}

.field-horizontal label {
  flex-shrink: 0;
  min-width: 180px;
  font-weight: var(--configurator-font-weight-semibold);
  color: var(--configurator-text);
  font-size: var(--configurator-font-size-base);
  font-family: var(--configurator-font-family);
  margin: 0;
}

.templates-section {
  border-top: 1px solid var(--configurator-outline);
  padding-top: var(--configurator-spacing-1);
}

.w-full {
  width: 100%;
}

.mt-2 {
  margin-top: var(--configurator-spacing-1);
}

.mt-3 {
  margin-top: 0.75rem;
}

.ml-2 {
  margin-left: var(--configurator-spacing-1);
}

.flex {
  display: flex;
}

.align-items-center {
  align-items: center;
}

/* Стили для информации о подключениях к БД */
.db-connections-info {
  border: 1px solid var(--configurator-outline);
  border-radius: var(--configurator-radius);
  padding: var(--configurator-spacing-1);
  background-color: var(--configurator-surface-variant);
}

.connection-item {
  margin-bottom: var(--configurator-spacing-1);
  padding: var(--configurator-spacing-1);
  border-radius: var(--configurator-radius);
  background-color: var(--configurator-surface);
  border: 1px solid var(--configurator-outline);
}

.connection-item:last-child {
  margin-bottom: 0;
}

.connection-header {
  display: flex;
  align-items: center;
  gap: var(--configurator-spacing-1);
  margin-bottom: var(--configurator-spacing-1);
}

.connection-title {
  font-weight: var(--configurator-font-weight-semibold);
  font-size: var(--configurator-font-size-base);
  font-family: var(--configurator-font-family);
  color: var(--configurator-text);
}

.connection-details {
  color: var(--configurator-text-secondary);
  font-size: 13px;
  font-family: var(--configurator-font-family);
}

/* Стили для OPC контролов */
.opc-settings-container {
  display: flex;
  flex-direction: column;
  gap: var(--configurator-spacing-1);
}

.opc-controls {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex: 1;
}

/* Карточки подключений к БД */
.db-connections-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: var(--configurator-spacing-2);
  margin-bottom: var(--configurator-spacing-2);
}

.db-connection-card {
  border: 2px solid var(--configurator-outline);
  border-radius: var(--configurator-radius);
  background-color: var(--configurator-surface);
  transition: all 0.3s ease;
  overflow: hidden;
  box-shadow: var(--configurator-shadow-2);
}

.db-connection-card.connection-active {
  border-color: var(--configurator-secondary);
  box-shadow: 0 0 0 1px rgba(67, 160, 71, 0.2);
}

.db-connection-card.connection-inactive {
  border-color: var(--configurator-error);
  box-shadow: 0 0 0 1px rgba(229, 57, 53, 0.2);
  opacity: 0.7;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem var(--configurator-spacing-2);
  background-color: var(--configurator-surface-variant);
  border-bottom: 1px solid var(--configurator-outline);
}

.connection-status {
  display: flex;
  align-items: center;
  gap: var(--configurator-spacing-1);
}

.connection-title {
  font-weight: var(--configurator-font-weight-semibold);
  font-size: var(--configurator-font-size-base);
  font-family: var(--configurator-font-family);
  color: var(--configurator-text);
}

.connection-toggle {
  display: flex;
  align-items: center;
}

.card-content {
  padding: var(--configurator-spacing-2);
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.connection-field-horizontal {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: var(--configurator-spacing-1);
}

.connection-field-horizontal label {
  font-size: 13px;
  font-weight: var(--configurator-font-weight-semibold);
  color: var(--configurator-text-secondary);
  margin: 0;
  min-width: 120px;
  flex-shrink: 0;
  font-family: var(--configurator-font-family);
}

.connection-field-horizontal .w-full {
  flex: 1;
}

/* Скрытие заголовков таблицы документов */
.docs-table :deep(.p-datatable-thead) {
  display: none;
}

/* Компактная высота строк таблицы документов */
.docs-table :deep(.p-datatable-tbody > tr) {
  line-height: 1.1;
}

.docs-table :deep(.p-datatable-tbody > tr > td) {
  padding-top: 0.25rem !important;
  padding-bottom: 0.25rem !important;
}

/* Чуть компактнее переключатель, чтобы строка стала ниже */
.docs-table :deep(.p-datatable-tbody > tr > td .p-inputswitch) {
  width: 2.25rem !important;
  height: 1.25rem !important;
}

/* Компактные теги шаблонов */
.docs-table :deep(.p-tag) {
  padding: 0.15rem 0.4rem !important;
  font-size: 0.85rem !important;
  line-height: 1.1 !important;
}

/* Компактные кнопки в таблице документов */
.docs-table :deep(.p-button.p-button-text.p-button-sm) {
  padding: 0.2rem 0.35rem !important;
  line-height: 1.1 !important;
}

/* Фиксированная ширина колонки "Использовать" */
.docs-table :deep(.p-datatable-tbody > tr > td.col-use),
.docs-table :deep(.p-datatable-thead > tr > th.col-use) {
  width: 3rem !important;
  min-width: 3rem !important;
  max-width: 3rem !important;
  padding-left: 0.25rem !important;
  padding-right: 0.25rem !important;
  text-align: center;
}

.docs-table :deep(.p-datatable-tbody > tr > td.col-use .flex.align-items-center) {
  justify-content: center;
  gap: 0.25rem;
}

/* Кликабельные теги шаблонов */
.template-tag-clickable {
  cursor: pointer !important;
  transition: all 0.2s ease !important;
}

.template-tag-clickable:hover {
  transform: scale(1.05) !important;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2) !important;
}

/* Стили для заблокированного поля пароля */
.password-readonly {
  background-color: var(--configurator-surface-variant) !important;
  color: var(--configurator-text-secondary) !important;
  cursor: not-allowed !important;
  opacity: 0.7 !important;
}

.password-readonly:focus {
  box-shadow: none !important;
  border-color: var(--configurator-outline) !important;
}
</style>