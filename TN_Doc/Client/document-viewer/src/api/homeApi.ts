import axios from 'axios';
import type { DocumentRow, EditResponse, ListItem } from '../types';

const api = axios.create({
  baseURL: '',
  timeout: 20000,
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json',
    'X-Requested-With': 'XMLHttpRequest'
  }
});

export async function fetchDevices(): Promise<ListItem[]> {
  const { data } = await api.get<ListItem[]>('/Home/GetListDevices');
  return data;
}

export async function fetchDevicePrefix(idDevice: number): Promise<string> {
  const { data } = await api.get<string>('/Home/GetNameDBForDevice', {
    params: { IdDevice: idDevice }
  });
  return data === 'IVK_TN_02' ? 'IVK_TN_02' : 'IVK_TN_01';
}

export async function fetchDocs(idDevice: number): Promise<ListItem[]> {
  const { data } = await api.get<ListItem[]>('/Home/GetListDocs', {
    params: { idDevice }
  });
  return data;
}

export async function fetchTemplates(idDevice: number, idDoc: number): Promise<ListItem[]> {
  const { data } = await api.get<ListItem[]>('/Home/GetTemplatesDoc', {
    params: { IdDevice: idDevice, IdDoc: idDoc }
  });
  return data;
}

export async function fetchLastTemplateId(idDevice: number, idDoc: number): Promise<number | null> {
  const { data } = await api.get<number>('/Home/GetIdTemplateDoc', {
    params: { IdDevice: idDevice, IdDoc: idDoc }
  });
  return data ?? null;
}

export async function setLastTemplateId(idDevice: number, idDoc: number, idTemplateDoc: number) {
  await api.get('/Home/SetIdTemplateDoc', {
    params: { IdDevice: idDevice, IdDoc: idDoc, IdTemplateDoc: idTemplateDoc }
  });
}

export async function fetchDocumentList(
  idDevice: number,
  idDoc: number,
  begin: string,
  end: string
): Promise<DocumentRow[]> {
  const { data } = await api.get<DocumentRow[]>('/Home/GetList', {
    params: { IdDevice: idDevice, IdDoc: idDoc, DTBegin: begin, DTEnd: end }
  });
  return data;
}

export async function requestPdf(
  idDevice: number,
  idDoc: number,
  rowId: number,
  protocolNumber: number | null
): Promise<boolean> {
  const { data } = await api.get<boolean>('/Home/GetDoc', {
    params: {
      IdDevice: idDevice,
      IdDoc: idDoc,
      id: rowId,
      protocolNumber
    }
  });
  return data;
}

export async function fetchEditConfig(
  idDevice: number,
  idDoc: number,
  rowId: number
): Promise<EditResponse> {
  const { data } = await api.get<EditResponse>('/Home/GetDocEdit', {
    params: { IdDevice: idDevice, IdDoc: idDoc, id: rowId }
  });
  return data;
}

export async function fetchPrinterList(): Promise<string[]> {
  const { data } = await api.get<string[]>('/Print/GetListPrinters');
  return data;
}

export async function printDocument(printerName: string) {
  await api.get('/Print/PrintDoc', { params: { printerName } });
}

export async function fetchExportFormats(): Promise<string[]> {
  const { data } = await api.get<string[]>('/Export/GetListFormats');
  return data;
}

export async function exportDocument(
  idDevice: number,
  idDoc: number,
  rowId: number,
  format: string,
  protocolNumber: number | null
): Promise<string> {
  const { data } = await api.get<string>('/Export/ExportDoc', {
    params: { IdDevice: idDevice, IdDoc: idDoc, id: rowId, format, protocolNumber }
  });
  return data;
}

export async function fetchProtocols(idDevice: number, idDoc: number): Promise<ListItem[]> {
  const { data } = await api.get<ListItem[]>('/Home/GetListProtocolNumber', {
    params: { IdDevice: idDevice, IdDoc: idDoc }
  });
  return data;
}

export async function isProtocolUsed(idDoc: number): Promise<boolean> {
  const { data } = await api.get<boolean>('/Home/IsProtocolNumberUsed', {
    params: { idDoc }
  });
  return data;
}

export async function fetchSaveButtonText(idDevice: number, idDoc: number): Promise<string> {
  const { data } = await api.get<string>('/Home/GetSaveBtnText', {
    params: { IdDevice: idDevice, IdDoc: idDoc }
  });
  return data;
}

export async function canEditDocument(idDevice: number, idDoc: number): Promise<boolean> {
  const { data } = await api.get<{ canEdit: boolean }>('/Home/CanEditDocument', {
    params: { idDevice, idDoc }
  });
  return !!data?.canEdit;
}

export async function isSecurityEnabled(): Promise<boolean> {
  const { data } = await api.get<boolean>('/Home/IsUsedSecurity');
  return data;
}

export async function isElisEnabled(idDevice: number): Promise<boolean> {
  const { data } = await api.get<boolean>('/Home/IsUsedElis', {
    params: { idDevice }
  });
  return data;
}
