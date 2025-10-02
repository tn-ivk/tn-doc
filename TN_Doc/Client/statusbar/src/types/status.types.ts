/**
 * Типы для статус-бара TN_Doc
 */

export interface DeviceStatus {
  id: string;
  name: string;
  type: 'database' | 'opc' | 'service';
  isConnected: boolean;
  latencyMs?: number;
  lastChecked?: Date;
  error?: string;
}

export interface ConnectionStatus {
  isConnected: boolean;
  latencyMs?: number;
  lastChecked?: Date;
  error?: string;
}

export interface ServiceStatus {
  messagingService: ConnectionStatus;
  elis?: ConnectionStatus;
  opcDa?: ConnectionStatus;
  opcUa?: ConnectionStatus;
}

export interface StatusResponse {
  devices: DeviceStatus[];
  services: ServiceStatus;
  timestamp: string;
}

export type HealthStatus = 'healthy' | 'warning' | 'critical';
export type ConnectionState = 'disconnected' | 'connecting' | 'connected';
export type IndicatorStatus = 'online' | 'offline' | 'ndv' | 'warning';