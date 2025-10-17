<template>
  <div class="opc-settings">
    <div v-if="showTypeSelector" class="field field-type-selector">
      <SelectButton
        v-model="localSettings.Type"
        :options="opcTypes"
        option-label="label"
        option-value="value"
        :allowEmpty="false"
        @update:modelValue="handleTypeChange"
      />
    </div>

    <!-- OPC DA Settings -->
    <template v-if="localSettings.Type === OpcType.DA">
      <div class="field">
        <label for="da-host">Хост</label>
        <InputText
          id="da-host"
          v-model="localSettings.DaSettings!.Host"
          placeholder="127.0.0.1"
        />
      </div>

      <div class="field">
        <label for="da-progid">ProgId</label>
        <InputText
          id="da-progid"
          v-model="localSettings.DaSettings!.ProgId"
          placeholder="psregulopcda_01"
        />
      </div>

      <div class="field">
        <label for="da-prefix">Стартовый префикс</label>
        <InputText
          id="da-prefix"
          v-model="localSettings.DaSettings!.StartPrefix"
          placeholder="Root.PLC1.IVK_TN_01"
        />
      </div>

      <div class="field">
        <label for="da-updaterate">Частота обновления (мс)</label>
        <InputNumber
          id="da-updaterate"
          v-model="localSettings.DaSettings!.UpdateRate"
          :min="100"
          :max="10000"
        />
      </div>
    </template>

    <!-- OPC UA Settings -->
    <template v-if="localSettings.Type === OpcType.UA">
      <div class="field">
        <label for="ua-config">Файл конфигурации</label>
        <InputText
          id="ua-config"
          v-model="localSettings.UaSettings!.ConfigFilename"
          placeholder="opcua-config.xml"
        />
      </div>

      <div class="field">
        <label for="ua-prefix">Стартовый префикс</label>
        <InputText
          id="ua-prefix"
          v-model="localSettings.UaSettings!.StartPrefix"
          placeholder="ns=2;s=Root.PLC1"
        />
      </div>

      <div class="field">
        <label for="ua-updaterate">Частота обновления (мс)</label>
        <InputNumber
          id="ua-updaterate"
          v-model="localSettings.UaSettings!.UpdateRate"
          :min="100"
          :max="10000"
        />
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import type { OpcConnectionSettings } from '../types/config.types';
import { OpcType } from '../types/config.types';

import SelectButton from 'primevue/selectbutton';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';

interface Props {
  modelValue: OpcConnectionSettings | undefined;
  showTypeSelector?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  showTypeSelector: true
});

const emit = defineEmits<{
  (e: 'update:modelValue', value: OpcConnectionSettings): void;
}>();

const opcTypes = [
  { label: 'OPC DA', value: OpcType.DA },
  { label: 'OPC UA', value: OpcType.UA }
];

const localSettings = ref<OpcConnectionSettings>(
  props.modelValue || {
    Type: OpcType.UA,
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
);

// Синхронизация с внешними изменениями modelValue
watch(
  () => props.modelValue,
  (newValue) => {
    if (newValue && JSON.stringify(newValue) !== JSON.stringify(localSettings.value)) {
      // Маппинг числовых значений Type в строковые
      const mappedType = (newValue.Type as any) === 0 ? OpcType.DA :
                        (newValue.Type as any) === 1 ? OpcType.UA :
                        newValue.Type;

      localSettings.value = {
        ...newValue,
        Type: mappedType,
        // Гарантируем наличие настроек для обоих типов
        DaSettings: newValue.DaSettings || {
          Host: '127.0.0.1',
          ProgId: 'psregulopcda_01',
          StartPrefix: 'Root.PLC1.IVK_TN_01',
          UpdateRate: 500
        },
        UaSettings: newValue.UaSettings || {
          ConfigFilename: 'opcua-config.xml',
          StartPrefix: 'ns=2;s=Root.PLC1',
          UpdateRate: 500
        }
      };
    }
  },
  { immediate: true, deep: true }
);

onMounted(() => {
  if (!props.modelValue) {
    emit('update:modelValue', localSettings.value);
  }
});

watch(
  () => localSettings.value,
  (newValue) => {
    emit('update:modelValue', newValue);
  },
  { deep: true }
);

function handleTypeChange() {
  emit('update:modelValue', localSettings.value);
}
</script>

<style scoped>
.opc-settings {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.field-type-selector {
  gap: 0;
  margin-bottom: 0.5rem;
}

.field label {
  font-weight: 600;
  color: var(--text-color);
  font-size: 0.9rem;
}

/* Стили для полей ввода в модальном окне OPC */
:deep(.p-inputtext),
:deep(.p-inputnumber-input) {
  border: 1px solid #CFD8DC !important;
  border-radius: 8px !important;
  padding: 6px 10px !important;
  height: 37px !important;
  background-color: #ffffff !important;
  color: #212121 !important;
  font-size: 15px !important;
  transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out !important;
}

:deep(.p-inputtext:hover),
:deep(.p-inputnumber-input:hover) {
  border-color: #B0BEC5 !important;
}

:deep(.p-inputtext:focus),
:deep(.p-inputnumber-input:focus) {
  outline: none !important;
  border-color: #1E88E5 !important;
  box-shadow: 0 0 0 3px rgba(30, 136, 229, 0.35) !important;
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
