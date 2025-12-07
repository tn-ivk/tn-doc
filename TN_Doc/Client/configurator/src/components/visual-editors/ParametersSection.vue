<script setup lang="ts">
import { computed } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Checkbox from 'primevue/checkbox';
import InputNumber from 'primevue/inputnumber';
import Select from 'primevue/select';
import Tag from 'primevue/tag';
import type { PassportParameter } from '@/types/passport-config.types';

interface Props {
  parameters: PassportParameter[];
}

interface Emits {
  (e: 'update:parameters', parameters: PassportParameter[]): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// Карта: slave key → master key
const slaveToMaster = computed(() => {
  const map = new Map<string, string>();
  for (const p of props.parameters) {
    if (p.SlaveKey) {
      map.set(p.SlaveKey, p.Key);
    }
  }
  return map;
});

function isSlaveOf(key: string): boolean {
  return slaveToMaster.value.has(key);
}

function getMasterKey(slaveKey: string): string {
  return slaveToMaster.value.get(slaveKey) || '';
}

// Доступные ключи для выбора в качестве slave
function getAvailableSlaves(currentKey: string) {
  const currentParam = props.parameters.find(p => p.Key === currentKey);
  const currentSlaveKey = currentParam?.SlaveKey;

  return props.parameters
    .filter(p =>
      p.Key !== currentKey &&      // Не сам себя
      (!isSlaveOf(p.Key) || p.Key === currentSlaveKey) &&  // Не уже slave другого, ИЛИ это текущий slave
      !p.SlaveKey                  // Не является мастером
    )
    .map(p => ({ label: p.Key, value: p.Key }));
}

function updateParameter(index: number, field: keyof PassportParameter, value: any) {
  const updated = [...props.parameters];
  updated[index] = { ...updated[index], [field]: value };
  emit('update:parameters', updated);
}

function clearSlaveKey(index: number) {
  const updated = [...props.parameters];
  const param = { ...updated[index] };
  delete param.SlaveKey;
  updated[index] = param;
  emit('update:parameters', updated);
}
</script>

<template>
  <div class="parameters-section">
    <DataTable
      :value="parameters"
      scrollable
      scrollHeight="flex"
      class="parameters-table"
      size="small"
      stripedRows
    >
      <Column field="Key" header="Ключ" :style="{ width: '180px' }">
        <template #body="{ data }">
          <code class="param-key">{{ data.Key }}</code>
        </template>
      </Column>

      <Column field="Name" header="Название" :style="{ minWidth: '250px' }">
        <template #body="{ data }">
          <span class="param-name">{{ data.Name }}</span>
        </template>
      </Column>

      <Column field="Use" header="Вкл" :style="{ width: '60px' }" headerClass="text-center" bodyClass="text-center">
        <template #body="{ data, index }">
          <Checkbox
            :modelValue="data.Use"
            @update:modelValue="updateParameter(index, 'Use', $event)"
            binary
          />
        </template>
      </Column>

      <Column field="Edit" header="Редакт." :style="{ width: '80px' }" headerClass="text-center" bodyClass="text-center">
        <template #body="{ data, index }">
          <Checkbox
            :modelValue="data.Edit"
            @update:modelValue="updateParameter(index, 'Edit', $event)"
            binary
          />
        </template>
      </Column>

      <Column field="IsBallast" header="Балласт" :style="{ width: '70px' }" headerClass="text-center" bodyClass="text-center">
        <template #body="{ data, index }">
          <Checkbox
            :modelValue="data.IsBallast"
            @update:modelValue="updateParameter(index, 'IsBallast', $event)"
            binary
          />
        </template>
      </Column>

      <Column header="Slave-связь" :style="{ width: '180px' }">
        <template #body="{ data, index }">
          <div class="slave-cell">
            <!-- Если этот параметр является slave другого -->
            <Tag
              v-if="isSlaveOf(data.Key)"
              :value="`← ${getMasterKey(data.Key)}`"
              severity="info"
            />
            <!-- Если этот параметр может быть master -->
            <template v-else>
              <Select
                :modelValue="data.SlaveKey || null"
                :options="getAvailableSlaves(data.Key)"
                optionLabel="label"
                optionValue="value"
                placeholder="—"
                class="slave-dropdown"
                showClear
                @update:modelValue="value => value ? updateParameter(index, 'SlaveKey', value) : clearSlaveKey(index)"
              />
            </template>
          </div>
        </template>
      </Column>

      <Column field="RoundValue" header="Округл." :style="{ width: '90px' }">
        <template #body="{ data, index }">
          <InputNumber
            :modelValue="data.RoundValue ?? null"
            @update:modelValue="updateParameter(index, 'RoundValue', $event)"
            :min="0"
            :max="10"
            class="round-input"
            placeholder="—"
          />
        </template>
      </Column>

      <Column field="RequiredFill" header="Обяз." :style="{ width: '70px' }" headerClass="text-center" bodyClass="text-center">
        <template #body="{ data, index }">
          <Checkbox
            :modelValue="data.RequiredFill ?? false"
            @update:modelValue="updateParameter(index, 'RequiredFill', $event)"
            binary
          />
        </template>
      </Column>
    </DataTable>
  </div>
</template>

<style scoped>
.parameters-section {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
}

.parameters-table {
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

.param-key {
  font-family: 'Consolas', 'Monaco', monospace;
  font-size: 0.85rem;
  background: var(--md-surface-variant, #ECEFF1);
  padding: 0.2rem 0.4rem;
  border-radius: 4px;
  word-break: break-all;
}

.param-name {
  font-size: 0.9rem;
  white-space: pre-line;
}

.slave-cell {
  min-width: 140px;
}

.slave-dropdown {
  width: 100%;
}

:deep(.slave-dropdown .p-select) {
  width: 100%;
}

.round-input {
  width: 70px;
}

:deep(.round-input .p-inputnumber-input) {
  width: 100%;
  text-align: center;
}

:deep(.p-tag) {
  font-size: 0.75rem;
  padding: 0.15rem 0.4rem;
}

:deep(.text-center) {
  text-align: center;
}
</style>
