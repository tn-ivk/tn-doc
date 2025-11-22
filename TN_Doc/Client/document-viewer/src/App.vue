<template>
  <div class="shell">
    <div class="shell__grid">
      <div class="shell__sidebar">
        <FiltersPanel
          :devices="devices"
          :docs="docs"
          :protocols="protocols"
          :selectedDeviceId="selectedDeviceId"
          :selectedDocId="selectedDocId"
          :protocolId="selectedProtocolId"
          :dateBegin="dateBegin"
          :dateEnd="dateEnd"
          :loading="tableLoading"
          @update:device="onDeviceChange"
          @update:doc="onDocChange"
          @update:protocol="setProtocol"
          @update:begin="(v) => (dateBegin = v)"
          @update:end="(v) => (dateEnd = v)"
          @refresh="refreshList"
        />

        <DocumentsTable
          :rows="rows"
          :selectedId="selectedRowId"
          :loading="tableLoading"
          @select="selectRow"
        />
      </div>

      <div class="shell__content">
        <ViewerPanel
          v-if="mode === 'view'"
          :templates="templates"
          :templateId="selectedTemplateId"
          :protocols="protocols"
          :protocolId="selectedProtocolId"
          :exportFormats="exportFormats"
          :exportFormat="selectedExportFormat"
          :printers="printers"
          :printerName="selectedPrinter"
          :pdfUrl="viewerState.pdfUrl"
          :loading="viewerState.loading"
          :permissions="permissions"
          :canEdit="canStartEdit"
          :message="viewerState.error"
          @update:template="onTemplateChange"
          @update:protocol="setProtocol"
          @update:exportFormat="(v) => (selectedExportFormat = v)"
          @update:printer="(v) => (selectedPrinter = v)"
          @export="handleExport"
          @print="handlePrint"
          @edit="openEditor"
          @open:dictionaries="openDictionaries"
          @open:configurator="openConfigurator"
        />

        <EditorHost
          v-else
          ref="editorHostRef"
          :src="editorState.src"
          :canSave="editorState.canSave && permissions.allowEditAndSave"
          :saving="editorState.isSaving"
          :saveText="editorState.saveButtonText"
          @save="handleSave"
          @close="backToViewer"
          @ready="onEditorReady"
          @canSaveChange="(value) => (editorState.canSave = value)"
        />
      </div>
    </div>

    <div v-if="toast.message" class="toast" :class="`toast--${toast.type}`">
      {{ toast.message }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue';
import { logger } from '@tn-doc/shared';
import FiltersPanel from './components/FiltersPanel.vue';
import DocumentsTable from './components/DocumentsTable.vue';
import ViewerPanel from './components/ViewerPanel.vue';
import EditorHost from './components/EditorHost.vue';
import type { DocumentRow, ListItem, Permissions } from './types';
import {
  canEditDocument,
  exportDocument,
  fetchDevicePrefix,
  fetchDocumentList,
  fetchDocs,
  fetchEditConfig,
  fetchExportFormats,
  fetchLastTemplateId,
  fetchPrinterList,
  fetchProtocols,
  fetchSaveButtonText,
  fetchTemplates,
  isProtocolUsed,
  printDocument,
  requestPdf,
  setLastTemplateId,
  fetchDevices
} from './api/homeApi';
import { defaultPermissions, loadPermissions, applySecurityUpdate } from './services/securityService';
import { buildFullTag } from './services/opcService';
import { MessagingService, type TagMessage } from './services/messagingService';
import { todayRange, toApiDate } from './utils/date';

declare const $: any;
declare function InitDirEditorComponent(): void;

type Mode = 'view' | 'edit';

const devices = ref<ListItem[]>([]);
const docs = ref<ListItem[]>([]);
const templates = ref<ListItem[]>([]);
const protocols = ref<ListItem[]>([]);
const exportFormats = ref<string[]>([]);
const printers = ref<string[]>([]);
const rows = ref<DocumentRow[]>([]);

const selectedDeviceId = ref<number | null>(null);
const selectedDocId = ref<number | null>(null);
const selectedTemplateId = ref<number | null>(null);
const selectedProtocolId = ref<number | null>(null);
const selectedExportFormat = ref<string | null>(null);
const selectedPrinter = ref<string | null>(null);
const selectedRowId = ref<number | null>(null);

let { begin: beginDefault, end: endDefault } = todayRange();
const tableLoading = ref(false);
const dateBegin = ref(beginDefault);
const dateEnd = ref(endDefault);

const viewerState = reactive({
  pdfUrl: '',
  loading: false,
  error: null as string | null
});

const editorState = reactive({
  src: '',
  isSaving: false,
  canSave: true,
  saveButtonText: 'Сохранить'
});

const permissions = ref<Permissions>({ ...defaultPermissions });
const canEditFlag = ref(false);
const mode = ref<Mode>('view');
const prefixTag = ref('IVK_TN_01');
const messaging = new MessagingService();
const dictionariesInitialized = ref(false);

const toast = reactive({
  message: '',
  type: 'info' as 'info' | 'success' | 'error'
});

const editorHostRef = ref<InstanceType<typeof EditorHost> | null>(null);

const currentDeviceName = computed(
  () => devices.value.find((d) => d.id === selectedDeviceId.value)?.name ?? ''
);

const canStartEdit = computed(
  () => canEditFlag.value && permissions.value.showEditAndSave && permissions.value.allowEditAndSave
);

function showToast(message: string, type: 'info' | 'success' | 'error' = 'info') {
  toast.message = message;
  toast.type = type;
  setTimeout(() => (toast.message = ''), 4000);
}

async function bootstrap() {
  try {
    const [deviceList, formats, printerList, loadedPermissions] = await Promise.all([
      fetchDevices(),
      fetchExportFormats(),
      fetchPrinterList(),
      loadPermissions()
    ]);

    devices.value = deviceList;
    exportFormats.value = formats;
    selectedExportFormat.value = formats[0] ?? null;
    printers.value = printerList;
    selectedPrinter.value = printerList[0] ?? null;
    permissions.value = loadedPermissions;

    if (deviceList.length) {
      selectedDeviceId.value = deviceList[0].id;
      await onDeviceChange(deviceList[0].id);
    }

    messaging.on(handleSignalR);
    await messaging.start();
  } catch (error: any) {
    logger.error('[document-viewer] Ошибка инициализации', { error: error?.message || error });
    showToast('Не удалось загрузить стартовые данные', 'error');
  }
}

onMounted(bootstrap);

onBeforeUnmount(async () => {
  messaging.off(handleSignalR);
  await messaging.stop();
});

async function handleSignalR({ deviceName, tagName, tagValue }: TagMessage) {
  if (deviceName === 'ARM') {
    permissions.value = applySecurityUpdate(tagName, tagValue, permissions.value);
  }

  const currentName = currentDeviceName.value;
  if (!currentName || deviceName !== currentName) {
    return;
  }

  const counterTag = buildFullTag(prefixTag.value, 'ARM.ARM_OnlineReportCounter');
  if (tagName === counterTag && selectedRowId.value) {
    await loadPdf(selectedRowId.value);
  }
}

async function onDeviceChange(id: number | null) {
  selectedDeviceId.value = id;
  selectedRowId.value = null;
  rows.value = [];
  mode.value = 'view';
  viewerState.pdfUrl = '';
  editorState.src = '';

  if (!id) {
    return;
  }

  prefixTag.value = await fetchDevicePrefix(id);
  await loadDocsForDevice(id);
  await refreshDocScope();
}

async function loadDocsForDevice(deviceId: number) {
  const list = await fetchDocs(deviceId);
  docs.value = list;

  if (!list.length) {
    selectedDocId.value = null;
    return;
  }

  const existing = list.find((item) => item.id === selectedDocId.value);
  selectedDocId.value = existing ? existing.id : list[0].id;
}

async function onDocChange(id: number | null) {
  selectedDocId.value = id;
  selectedRowId.value = null;
  viewerState.pdfUrl = '';
  mode.value = 'view';
  selectedProtocolId.value = null;

  if (id) {
    await refreshDocScope();
  }
}

async function refreshDocScope() {
  if (!selectedDeviceId.value || !selectedDocId.value) {
    return;
  }

  const [templatesList, lastTemplateId] = await Promise.all([
    fetchTemplates(selectedDeviceId.value, selectedDocId.value),
    fetchLastTemplateId(selectedDeviceId.value, selectedDocId.value)
  ]);
  templates.value = templatesList;

  const templateCandidate =
    templatesList.find((t) => t.id === lastTemplateId) ?? templatesList[0] ?? null;
  selectedTemplateId.value = templateCandidate ? templateCandidate.id : null;

  const protocolsUsed = await isProtocolUsed(selectedDocId.value);
  if (protocolsUsed) {
    const list = await fetchProtocols(selectedDeviceId.value, selectedDocId.value);
    protocols.value = list;
    const current = list.find((item) => item.id === selectedProtocolId.value);
    selectedProtocolId.value = (current ?? list[0])?.id ?? null;
  } else {
    protocols.value = [];
    selectedProtocolId.value = null;
  }

  editorState.saveButtonText = await fetchSaveButtonText(
    selectedDeviceId.value,
    selectedDocId.value
  );

  canEditFlag.value = await canEditDocument(selectedDeviceId.value, selectedDocId.value);
  await refreshList();
}

async function refreshList() {
  if (!selectedDeviceId.value || !selectedDocId.value) {
    return;
  }

  tableLoading.value = true;
  viewerState.error = null;

  try {
    const list = await fetchDocumentList(
      selectedDeviceId.value,
      selectedDocId.value,
      toApiDate(dateBegin.value),
      toApiDate(dateEnd.value)
    );
    rows.value = list;

    if (!list.find((i) => i.id === selectedRowId.value)) {
      selectedRowId.value = null;
      viewerState.pdfUrl = '';
    }
  } catch (error: any) {
    viewerState.error = 'Ошибка при загрузке данных';
    rows.value = [];
    logger.error('[document-viewer] Ошибка загрузки списка', { error: error?.message || error });
  } finally {
    tableLoading.value = false;
  }
}

function selectRow(id: number) {
  selectedRowId.value = id;
  if (mode.value === 'edit') {
    mode.value = 'view';
    editorState.src = '';
  }
  loadPdf(id);
}

async function loadPdf(rowId: number) {
  if (!selectedDeviceId.value || !selectedDocId.value) {
    return;
  }

  viewerState.loading = true;
  viewerState.error = null;
  try {
      const result = await requestPdf(
      selectedDeviceId.value,
      selectedDocId.value,
      rowId,
      selectedProtocolId.value
    );

    if (result) {
      const cacheBust = `t=${Date.now()}`;
      viewerState.pdfUrl = `/PDF/PDF.pdf?${cacheBust}#toolbar=0&view=FitH`;
    } else {
      viewerState.pdfUrl = '';
      viewerState.error = 'Документ не найден';
    }
  } catch (error: any) {
    viewerState.error = 'Не удалось загрузить PDF';
    viewerState.pdfUrl = '';
    logger.error('[document-viewer] Ошибка загрузки PDF', { error: error?.message || error });
  } finally {
    viewerState.loading = false;
  }
}

async function onTemplateChange(value: number | null) {
  selectedTemplateId.value = value;
  if (selectedDeviceId.value && selectedDocId.value && value) {
    await setLastTemplateId(selectedDeviceId.value, selectedDocId.value, value);
  }

  if (selectedRowId.value) {
    await loadPdf(selectedRowId.value);
  }
}

function setProtocol(value: number | null) {
  selectedProtocolId.value = value;
  if (selectedRowId.value) {
    loadPdf(selectedRowId.value);
  }
}

async function handleExport() {
  if (!selectedDeviceId.value || !selectedDocId.value || !selectedRowId.value) {
    showToast('Выберите документ для экспорта', 'error');
    return;
  }
  if (!selectedExportFormat.value) {
    showToast('Укажите формат экспорта', 'error');
    return;
  }

  try {
    const path = await exportDocument(
      selectedDeviceId.value,
      selectedDocId.value,
      selectedRowId.value,
      selectedExportFormat.value,
      selectedProtocolId.value
    );
    showToast(`Экспорт завершён: ${path}`, 'success');
  } catch (error: any) {
    logger.error('[document-viewer] Ошибка экспорта', { error: error?.message || error });
    showToast('Не удалось выполнить экспорт', 'error');
  }
}

async function handlePrint() {
  if (!selectedPrinter.value) {
    showToast('Укажите принтер', 'error');
    return;
  }

  try {
    await printDocument(selectedPrinter.value);
    showToast('Отправлено на печать', 'success');
  } catch (error: any) {
    logger.error('[document-viewer] Ошибка печати', { error: error?.message || error });
    showToast('Не удалось отправить документ на печать', 'error');
  }
}

async function openEditor() {
  if (!selectedDeviceId.value || !selectedDocId.value || !selectedRowId.value) {
    showToast('Выберите документ для редактирования', 'error');
    return;
  }

  try {
    const edit = await fetchEditConfig(
      selectedDeviceId.value,
      selectedDocId.value,
      selectedRowId.value
    );

    if (!edit.useVue || !edit.url) {
      showToast('Редактор недоступен для этого документа', 'error');
      return;
    }

    const url = new URL(edit.url, window.location.origin);
    url.searchParams.set('deviceGuid', String(selectedDeviceId.value));
    url.searchParams.set('deviceName', currentDeviceName.value);
    url.searchParams.set('tagPrefix', prefixTag.value);

    editorState.src = url.toString();
    mode.value = 'edit';
  } catch (error: any) {
    logger.error('[document-viewer] Ошибка открытия редактора', { error: error?.message || error });
    showToast('Не удалось открыть редактор', 'error');
  }
}

async function handleSave() {
  const frame = editorHostRef.value?.getFrame();
  const editorWindow = frame?.contentWindow as any;
  if (!editorWindow?.SaveDoc) {
    showToast('Редактор не готов к сохранению', 'error');
    return;
  }

  if (!selectedDeviceId.value || !selectedDocId.value || !selectedRowId.value) {
    showToast('Не выбран документ для сохранения', 'error');
    return;
  }

  editorState.isSaving = true;
  try {
    const result = await editorWindow.SaveDoc(
      currentDeviceName.value,
      selectedDeviceId.value,
      selectedDocId.value,
      selectedRowId.value,
      prefixTag.value
    );

    if (result) {
      mode.value = 'view';
      await loadPdf(selectedRowId.value);
      showToast('Документ сохранён', 'success');
    } else {
      showToast('Не получено подтверждение записи от ИВК', 'error');
    }
  } catch (error: any) {
    logger.error('[document-viewer] Ошибка сохранения', { error: error?.message || error });
    showToast('Сохранение завершилось ошибкой', 'error');
  } finally {
    editorState.isSaving = false;
  }
}

function backToViewer() {
  mode.value = 'view';
  editorState.src = '';
}

function onEditorReady() {
  editorState.canSave = true;
}

function openDictionaries() {
  try {
    if (!dictionariesInitialized.value && typeof InitDirEditorComponent === 'function') {
      InitDirEditorComponent();
      dictionariesInitialized.value = true;
    }
    if (typeof $ !== 'undefined') {
      $('#modal-window').modal({ backdrop: 'static', keyboard: false }).modal('show');
    }
  } catch (error: any) {
    logger.error('[document-viewer] Ошибка открытия справочников', { error: error?.message || error });
  }
}

function openConfigurator() {
  if (typeof $ !== 'undefined') {
    $('#configurator-window').modal({ backdrop: 'static', keyboard: false }).modal('show');
  }
}
</script>

<style scoped>
.shell {
  padding: 18px 8px 28px;
  background: radial-gradient(circle at 10% 10%, rgba(125, 211, 252, 0.08), transparent 25%),
    radial-gradient(circle at 82% 0%, rgba(74, 222, 128, 0.08), transparent 30%),
    var(--doc-bg);
  min-height: 100vh;
  color: var(--doc-text);
  color-scheme: dark;
}

.shell__grid {
  display: grid;
  grid-template-columns: 340px 1fr;
  gap: 16px;
  align-items: start;
}

.shell__sidebar {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.shell__content {
  min-height: 620px;
}

.toast {
  position: fixed;
  bottom: 18px;
  right: 18px;
  padding: 12px 16px;
  border-radius: 14px;
  color: #0f172a;
  font-weight: 600;
  background: #e5e7eb;
  box-shadow: var(--doc-shadow);
}

.toast--success {
  background: #bbf7d0;
}

.toast--error {
  background: #fecdd3;
}

@media (max-width: 1180px) {
  .shell__grid {
    grid-template-columns: 1fr;
  }
}
</style>
