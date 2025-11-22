<template>
  <section class="viewer">
    <header class="viewer__toolbar">
      <div class="stack">
        <label class="label">Шаблон</label>
        <select :value="templateId ?? ''" @change="onTemplateChange">
          <option disabled value="">Не выбран</option>
          <option v-for="template in templates" :key="template.id" :value="template.id">
            {{ template.name }}
          </option>
        </select>
      </div>

      <div class="stack" v-if="protocols.length">
        <label class="label">Протокол</label>
        <select :value="protocolId ?? ''" @change="onProtocolChange">
          <option disabled value="">Не выбран</option>
          <option v-for="protocol in protocols" :key="protocol.id" :value="protocol.id">
            {{ protocol.name }}
          </option>
        </select>
      </div>

      <div class="spacer"></div>

      <div class="stack" v-if="permissions.showExport">
        <label class="label">Экспорт</label>
        <div class="inline">
          <select
            :disabled="!permissions.allowExport"
            :value="exportFormat ?? ''"
            @change="onExportFormatChange"
          >
            <option disabled value="">Формат</option>
            <option v-for="format in exportFormats" :key="format" :value="format">
              {{ format.toUpperCase() }}
            </option>
          </select>
          <button class="ghost" type="button" :disabled="!canExport" @click="emit('export')">
            Экспорт
          </button>
        </div>
      </div>

      <div class="stack" v-if="permissions.showPrint">
        <label class="label">Печать</label>
        <div class="inline">
          <select
            :disabled="!permissions.allowPrint || !printers.length"
            :value="printerName ?? ''"
            @change="onPrinterChange"
          >
            <option disabled value="">Принтер</option>
            <option v-for="printer in printers" :key="printer" :value="printer">
              {{ printer }}
            </option>
          </select>
          <button
            class="ghost ghost--strong"
            type="button"
            :disabled="!canPrint"
            @click="emit('print')"
          >
            Печать
          </button>
        </div>
      </div>

      <div class="menu">
        <button
          v-if="permissions.showDictionaries"
          class="icon"
          type="button"
          title="Справочники"
          :disabled="!permissions.allowDictionaries"
          @click="emit('open:dictionaries')"
        >
          <i class="fa fa-book" aria-hidden="true"></i>
        </button>
        <button class="icon" type="button" title="Конфигуратор" @click="emit('open:configurator')">
          <i class="fa fa-cog" aria-hidden="true"></i>
        </button>
      </div>

      <button
        class="primary"
        type="button"
        :disabled="!canStartEdit"
        v-if="permissions.showEditAndSave"
        @click="emit('edit')"
      >
        Редактирование
      </button>
    </header>

    <div class="viewer__body">
      <div v-if="message" class="banner">{{ message }}</div>
      <div class="frame">
        <div v-if="loading" class="frame__overlay">Загрузка PDF...</div>
        <iframe
          v-if="pdfUrl"
          :src="pdfUrl"
          class="frame__content"
          title="Документ"
          allowfullscreen
        ></iframe>
        <div v-else class="frame__placeholder">
          <p>Выберите запись в таблице слева, чтобы отобразить PDF</p>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import type { ListItem, Permissions } from '../types';
import { computed } from 'vue';

const props = defineProps<{
  templates: ListItem[];
  templateId: number | null;
  protocols: ListItem[];
  protocolId: number | null;
  exportFormats: string[];
  exportFormat: string | null;
  printers: string[];
  printerName: string | null;
  pdfUrl: string;
  loading: boolean;
  permissions: Permissions;
  canEdit: boolean;
  message?: string | null;
}>();

const emit = defineEmits<{
  (e: 'update:template', value: number | null): void;
  (e: 'update:protocol', value: number | null): void;
  (e: 'update:exportFormat', value: string | null): void;
  (e: 'update:printer', value: string | null): void;
  (e: 'export'): void;
  (e: 'print'): void;
  (e: 'edit'): void;
  (e: 'open:dictionaries'): void;
  (e: 'open:configurator'): void;
}>();

const canExport = computed(
  () => props.permissions.allowExport && !!props.exportFormat
);

const canPrint = computed(
  () => props.permissions.allowPrint && !!props.printerName
);

const canStartEdit = computed(
  () => props.permissions.allowEditAndSave && props.canEdit
);

function onTemplateChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value;
  emit('update:template', value ? Number(value) : null);
}

function onProtocolChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value;
  emit('update:protocol', value ? Number(value) : null);
}

function onExportFormatChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value;
  emit('update:exportFormat', value || null);
}

function onPrinterChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value;
  emit('update:printer', value || null);
}
</script>

<style scoped>
.viewer {
  background: linear-gradient(160deg, rgba(15, 23, 42, 0.95), rgba(17, 24, 39, 0.92));
  border-radius: var(--doc-radius);
  border: 1px solid var(--doc-border);
  box-shadow: var(--doc-shadow);
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-height: 520px;
}

.viewer__toolbar {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, auto));
  align-items: end;
  gap: 12px;
}

.stack {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.label {
  color: var(--doc-text-dim);
  font-size: 12px;
}

select {
  background: var(--doc-surface);
  color: var(--doc-text);
  border: 1px solid var(--doc-border);
  border-radius: 12px;
  padding: 10px 12px;
}

.inline {
  display: flex;
  gap: 8px;
  align-items: center;
}

.spacer {
  flex: 1;
}

.menu {
  display: inline-flex;
  gap: 8px;
  justify-self: end;
}

.icon {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  background: var(--doc-surface-weak);
  border: 1px solid var(--doc-border);
  color: var(--doc-text);
  cursor: pointer;
  transition: transform var(--doc-transition), border-color var(--doc-transition);
}

.icon:hover {
  transform: translateY(-1px);
  border-color: var(--doc-accent);
}

.icon:disabled {
  opacity: 0.45;
  cursor: not-allowed;
  transform: none;
}

.primary {
  background: linear-gradient(90deg, var(--doc-accent-strong), #93c5fd);
  color: #0f172a;
  border: none;
  border-radius: 12px;
  padding: 11px 18px;
  font-weight: 700;
  cursor: pointer;
  box-shadow: 0 10px 30px rgba(56, 189, 248, 0.2);
}

.primary:disabled {
  opacity: 0.5;
  cursor: default;
}

.ghost {
  background: rgba(125, 211, 252, 0.12);
  border: 1px solid rgba(125, 211, 252, 0.35);
  color: var(--doc-text);
  border-radius: 12px;
  padding: 10px 14px;
  cursor: pointer;
  transition: transform var(--doc-transition), border-color var(--doc-transition);
}

.ghost--strong {
  border-color: rgba(125, 211, 252, 0.55);
}

.ghost:disabled {
  opacity: 0.6;
  cursor: default;
}

.ghost:not(:disabled):hover {
  transform: translateY(-1px);
  border-color: rgba(125, 211, 252, 0.85);
}

.viewer__body {
  display: flex;
  flex-direction: column;
  gap: 10px;
  flex: 1;
}

.banner {
  background: rgba(248, 113, 113, 0.1);
  border: 1px solid rgba(248, 113, 113, 0.3);
  color: var(--doc-text);
  padding: 12px 14px;
  border-radius: 12px;
}

.frame {
  position: relative;
  flex: 1;
  border-radius: 16px;
  overflow: hidden;
  border: 1px solid var(--doc-border);
  background: radial-gradient(circle at 20% 20%, rgba(56, 189, 248, 0.1), transparent 35%),
    radial-gradient(circle at 80% 0%, rgba(34, 197, 94, 0.14), transparent 40%),
    var(--doc-panel);
}

.frame__content {
  width: 100%;
  height: 70vh;
  border: none;
  display: block;
}

.frame__placeholder {
  display: grid;
  place-items: center;
  height: 320px;
  color: var(--doc-text-dim);
  font-size: 14px;
}

.frame__overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(115deg, rgba(15, 23, 42, 0.9), rgba(34, 197, 94, 0.08));
  display: grid;
  place-items: center;
  color: var(--doc-text);
  font-weight: 600;
}

@media (max-width: 1080px) {
  .viewer__toolbar {
    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  }
}
</style>
