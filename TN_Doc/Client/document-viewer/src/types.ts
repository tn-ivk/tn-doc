export interface ListItem {
  id: number;
  name: string;
}

export interface AdvancedProperty {
  key: string;
  value: unknown;
}

export interface DocumentRow {
  id: number;
  dt: string;
  description: string;
  dirId?: number;
  bikId?: number;
  advancedProperties?: AdvancedProperty[];
}

export interface EditResponse {
  useVue: boolean;
  url: string;
}

export interface Permissions {
  showEditAndSave: boolean;
  allowEditAndSave: boolean;
  showPrint: boolean;
  allowPrint: boolean;
  showExport: boolean;
  allowExport: boolean;
  showDictionaries: boolean;
  allowDictionaries: boolean;
}

export interface ViewerState {
  pdfUrl: string;
  loading: boolean;
  error: string | null;
}

export interface EditorState {
  src: string;
  isSaving: boolean;
  canSave: boolean;
  saveButtonText: string;
}
