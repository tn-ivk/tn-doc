/**
 * Типы конфигурации приложения
 */

export enum OpcType {
  DA = 'DA',
  UA = 'UA'
}

export interface OpcDaSettings {
  StartPrefix: string;
  Host: string;
  ProgId: string;
  UpdateRate: number;
}

export interface OpcUaSettings {
  ConfigFilename: string;
  UpdateRate: number;
  StartPrefix?: string;
}

export interface OpcConnectionSettings {
  Type: OpcType;
  DaSettings?: OpcDaSettings;
  UaSettings?: OpcUaSettings;
}

export interface DBConnectionString {
  Use: boolean;
  GuidDevice: number;
  Server: string;
  Userid: string;
  Password: string;
  Database: string;
  ConnectionTimeout: number;
}

export interface UsedSI {
  UsedPR: boolean;
  UsedPP: boolean;
  UsedPVL: boolean;
  UsedPVS: boolean;
  UsedSecondSI_PP: boolean;
  UsedSecondSI_PVL: boolean;
  UsedSecondSI_PVS: boolean;
}

export interface TemplateDoc {
  Use: boolean;
  Id: number;
  Name: string;
  Description: string;
  PathToDocTemplateFile: string;
  PathToDocEditConfigFile: string;
}

export interface Document {
  Use: boolean;
  IdDoc: number;
  Name: string;
  Description: string;
  PathToDocDll?: string;
  PathToDocConfigFile?: string;
  PathToDocEditConfigFile?: string;
  LastUsedTemplateId?: number;
  TemplateDocs?: TemplateDoc[];
}

export interface Device {
  Use: boolean;
  IdDevice: number;
  Name: string;
  Description: string;
  Docs: Document[];
  DBConnectionStrings: DBConnectionString[];
  OpcConnectionSettings?: OpcConnectionSettings;
  UsedSI?: UsedSI;
  InvalidChars: string[];
}

export interface ExportDoc {
  Path: string;
}

export interface PrintSettings {
  LastSelectedPrinter?: string;
}

export interface Elis {
  Use: boolean;
  OstKey: string;
  SiknKey: string;
  ClientName: string;
  ClientToken: string;
}

export interface CfgApp {
  Devices: Device[];
  PrintSettings: PrintSettings;
  ExportDoc: ExportDoc;
  Elis: Elis;
  UseSecurityFeatures: boolean;
  ArmOpcConnectionSettings?: OpcConnectionSettings;
}

export interface ValidationError {
  field: string;
  message: string;
}

export interface ValidationResult {
  IsValid: boolean;
  Errors: string[];
  Warnings: string[];
}
