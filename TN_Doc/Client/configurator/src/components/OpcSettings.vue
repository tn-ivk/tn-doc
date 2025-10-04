<template>
  <div class="opc-settings">
    <div v-if="showTypeSelector" class="field field-type-selector">
      <SelectButton
        v-model="localSettings.Type"
        :options="opcTypes"
        option-label="label"
        option-value="value"
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
  gap: 0.5rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.field-type-selector {
  gap: 0;
}

.field label {
  font-weight: 600;
  color: var(--text-color);
  font-size: 0.9rem;
}
</style>
