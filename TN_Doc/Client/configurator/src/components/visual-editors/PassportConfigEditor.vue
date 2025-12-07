<script setup lang="ts">
import { computed } from 'vue';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import ParametersSection from './ParametersSection.vue';
import AdditionalFieldsSection from './AdditionalFieldsSection.vue';
import type { PassportEditConfig, PassportParameter, PassportAdditionalField } from '@/types/passport-config.types';

interface Props {
  config: PassportEditConfig;
  configPath: string;
}

interface Emits {
  (e: 'update:config', config: PassportEditConfig): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const parametersCount = computed(() => props.config?.Parameters?.length ?? 0);
const additionalFieldsCount = computed(() => props.config?.AdditionalInfo?.length ?? 0);

function updateParameters(parameters: PassportParameter[]) {
  emit('update:config', {
    ...props.config,
    Parameters: parameters
  });
}

function updateAdditionalFields(fields: PassportAdditionalField[]) {
  emit('update:config', {
    ...props.config,
    AdditionalInfo: fields
  });
}
</script>

<template>
  <div class="passport-config-editor">
    <Tabs value="0" class="passport-tabs">
      <TabList>
        <Tab value="0">
          <i class="pi pi-list mr-2"></i>
          <span>Параметры ({{ parametersCount }})</span>
        </Tab>
        <Tab value="1">
          <i class="pi pi-info-circle mr-2"></i>
          <span>Дополнительные поля ({{ additionalFieldsCount }})</span>
        </Tab>
      </TabList>

      <TabPanels>
        <TabPanel value="0">
          <ParametersSection
            :parameters="config.Parameters"
            @update:parameters="updateParameters"
          />
        </TabPanel>

        <TabPanel value="1">
          <AdditionalFieldsSection
            :fields="config.AdditionalInfo"
            @update:fields="updateAdditionalFields"
          />
        </TabPanel>
      </TabPanels>
    </Tabs>
  </div>
</template>

<style scoped>
.passport-config-editor {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
}

.passport-tabs {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
}

:deep(.p-tablist) {
  flex-shrink: 0;
}

:deep(.p-tabpanels) {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

:deep(.p-tabpanel) {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  padding: 1rem 0;
}

:deep(.p-tab) {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.mr-2 {
  margin-right: 0.5rem;
}
</style>
