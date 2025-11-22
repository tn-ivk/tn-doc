<template>
  <section class="panel">
    <div class="panel__header">
      <div>
        <p class="panel__subtitle">Фильтры</p>
        <h2 class="panel__title">Документы</h2>
      </div>
      <button class="ghost" type="button" @click="emit('refresh')" :disabled="loading">
        <span v-if="loading" class="dot-pulse"></span>
        <span v-else>Получить данные</span>
      </button>
    </div>

    <div class="field">
      <label>Устройство</label>
      <select :value="selectedDeviceId ?? ''" @change="onDeviceChange">
        <option value="" disabled>Выберите устройство</option>
        <option v-for="device in devices" :key="device.id" :value="device.id">
          {{ device.name }}
        </option>
      </select>
    </div>

    <div class="field">
      <label>Документ</label>
      <select :value="selectedDocId ?? ''" @change="onDocChange">
        <option value="" disabled>Выберите документ</option>
        <option v-for="doc in docs" :key="doc.id" :value="doc.id">
          {{ doc.name }}
        </option>
      </select>
    </div>

    <div class="field two-col">
      <div>
        <label>Дата начала</label>
        <input type="date" :value="dateBegin" @input="emit('update:begin', ($event.target as HTMLInputElement).value)" />
      </div>
      <div>
        <label>Дата окончания</label>
        <input type="date" :value="dateEnd" @input="emit('update:end', ($event.target as HTMLInputElement).value)" />
      </div>
    </div>

    <div class="field" v-if="protocols.length">
      <label>Протокол</label>
      <select :value="protocolId ?? ''" @change="onProtocolChange">
        <option disabled value="">Выберите...</option>
        <option v-for="protocol in protocols" :key="protocol.id" :value="protocol.id">
          {{ protocol.name }}
        </option>
      </select>
    </div>
  </section>
</template>

<script setup lang="ts">
import type { ListItem } from '../types';

defineProps<{
  devices: ListItem[];
  docs: ListItem[];
  protocols: ListItem[];
  selectedDeviceId: number | null;
  selectedDocId: number | null;
  protocolId: number | null;
  dateBegin: string;
  dateEnd: string;
  loading: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:device', value: number | null): void;
  (e: 'update:doc', value: number | null): void;
  (e: 'update:protocol', value: number | null): void;
  (e: 'update:begin', value: string): void;
  (e: 'update:end', value: string): void;
  (e: 'refresh'): void;
}>();

function onDeviceChange(event: Event) {
  const value = Number((event.target as HTMLSelectElement).value);
  emit('update:device', Number.isNaN(value) ? null : value);
}

function onDocChange(event: Event) {
  const value = Number((event.target as HTMLSelectElement).value);
  emit('update:doc', Number.isNaN(value) ? null : value);
}

function onProtocolChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value;
  emit('update:protocol', value === '' ? null : Number(value));
}
</script>

<style scoped>
.panel {
  background: linear-gradient(135deg, rgba(45, 55, 72, 0.75), rgba(30, 41, 59, 0.9));
  border: 1px solid var(--doc-border);
  border-radius: var(--doc-radius);
  padding: 18px;
  box-shadow: var(--doc-shadow);
  display: flex;
  flex-direction: column;
  gap: var(--doc-gap);
}

.panel__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.panel__title {
  margin: 0;
  font-size: 20px;
  color: var(--doc-text);
}

.panel__subtitle {
  margin: 0;
  color: var(--doc-text-dim);
  font-size: 13px;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.field label {
  color: var(--doc-text-dim);
  font-size: 13px;
}

select,
input[type='date'] {
  background: var(--doc-surface);
  border: 1px solid var(--doc-border);
  border-radius: var(--doc-radius-sm);
  color: var(--doc-text);
  padding: 10px 12px;
  font-size: 14px;
  transition: border-color var(--doc-transition), box-shadow var(--doc-transition);
}

select:focus,
input[type='date']:focus {
  border-color: var(--doc-accent);
  box-shadow: 0 0 0 1px var(--doc-accent);
  outline: none;
}

.two-col {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.ghost {
  background: rgba(125, 211, 252, 0.12);
  border: 1px solid rgba(125, 211, 252, 0.35);
  color: var(--doc-text);
  border-radius: 999px;
  padding: 10px 16px;
  cursor: pointer;
  transition: transform var(--doc-transition), border-color var(--doc-transition);
}

.ghost:disabled {
  opacity: 0.6;
  cursor: default;
}

.ghost:not(:disabled):hover {
  transform: translateY(-1px);
  border-color: var(--doc-accent-strong);
}

.dot-pulse {
  display: inline-block;
  width: 54px;
  text-align: left;
}

.dot-pulse::after {
  content: '•••';
  font-size: 16px;
  letter-spacing: 1px;
}

@media (max-width: 1024px) {
  .two-col {
    grid-template-columns: 1fr;
  }
}
</style>
