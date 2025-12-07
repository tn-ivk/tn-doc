<script setup lang="ts">
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Checkbox from 'primevue/checkbox';
import InputText from 'primevue/inputtext';
import type { PassportAdditionalField } from '@/types/passport-config.types';
import { FIELD_TYPES } from '@/types/passport-config.types';

interface Props {
  fields: PassportAdditionalField[];
}

interface Emits {
  (e: 'update:fields', fields: PassportAdditionalField[]): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const fieldTypeOptions = FIELD_TYPES.map(ft => ({
  label: ft.label,
  value: ft.value
}));

function updateField(index: number, field: keyof PassportAdditionalField, value: any) {
  const updated = [...props.fields];
  updated[index] = { ...updated[index], [field]: value };
  emit('update:fields', updated);
}
</script>

<template>
  <div class="additional-fields-section">
    <DataTable
      :value="fields"
      scrollable
      scrollHeight="flex"
      class="fields-table"
      size="small"
      stripedRows
    >
      <Column field="Key" header="Ключ" :style="{ width: '200px' }">
        <template #body="{ data }">
          <code class="field-key">{{ data.Key }}</code>
        </template>
      </Column>

      <Column field="Name" header="Наименование" :style="{ minWidth: '250px' }">
        <template #body="{ data, index }">
          <InputText
            :modelValue="data.Name"
            @update:modelValue="updateField(index, 'Name', $event)"
            class="name-input"
          />
        </template>
      </Column>

      <Column field="Type" header="Тип" :style="{ width: '180px' }">
        <template #body="{ data }">
          <span class="type-label">{{ fieldTypeOptions.find(ft => ft.value === data.Type)?.label || data.Type }}</span>
        </template>
      </Column>

      <Column field="Use" header="Вкл" :style="{ width: '60px' }" headerClass="text-center" bodyClass="text-center">
        <template #body="{ data, index }">
          <Checkbox
            :modelValue="data.Use"
            @update:modelValue="updateField(index, 'Use', $event)"
            binary
          />
        </template>
      </Column>
    </DataTable>
  </div>
</template>

<style scoped>
.additional-fields-section {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
}

.fields-table {
  flex: 1;
  min-height: 0;
}

:deep(.p-datatable-tbody > tr > td) {
  padding: 0.5rem;
  vertical-align: middle;
}

:deep(.p-datatable-thead > tr > th) {
  padding: 0.5rem;
  font-weight: 600;
  background: var(--md-surface-variant, #F5F5F5);
}

.field-key {
  font-family: 'Consolas', 'Monaco', monospace;
  font-size: 0.85rem;
  background: var(--md-surface-variant, #ECEFF1);
  padding: 0.2rem 0.4rem;
  border-radius: 4px;
  word-break: break-all;
}

.name-input {
  width: 100%;
}

:deep(.name-input .p-inputtext) {
  width: 100%;
}

.type-label {
  font-size: 0.9rem;
  color: var(--md-on-surface, #424242);
}

:deep(.text-center) {
  text-align: center;
}
</style>
