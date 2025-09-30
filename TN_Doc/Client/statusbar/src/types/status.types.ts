export interface DeviceStatus {
  id: string;
  name: string;
  type: 'database' | 'opc' | 'service';
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

export interface ConnectionStatus {
  isConnected: boolean;
  latencyMs?: number;
  lastChecked?: Date;
  error?: string;
}

export interface StatusResponse {
  devices: DeviceStatus[];
  services: ServiceStatus;
  timestamp: string;
}


