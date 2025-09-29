# План реализации строки состояния и начала миграции на Vue.js

## 📋 Обзор задачи

Реализация строки состояния как первого этапа полной миграции фронтенда TN_Doc на Vue.js. Строка состояния станет MVP для новой архитектуры и заложит фундамент для поэтапной миграции всего интерфейса.

## 🎯 Цели и требования

### Стратегические цели
- **Пилотный проект** для миграции на Vue.js
- **Установка инфраструктуры** для будущих Vue-компонентов
- **Минимальный риск** для существующего функционала
- **Основа для масштабирования** на весь проект

### Функциональные требования
- **Индикаторы устройств**: Статус подключения к БД для всех сконфигурированных устройств (ИВК-1, ИВК-2, ИВК-РСУ)
- **Индикатор MS**: Статус подключения к MessagingService (`http://localhost:5010/SignalRApp`)
- **OPC сервер**: Статус подключения к OPC DA/UA (планируется)
- **ELIS сервис**: Доступность лабораторной системы (планируется)
- **Реальное время**: Push через SignalR + fallback на polling
- **Визуализация**: Цветовая индикация, иконки состояния, время отклика
- **UX**: Возможность ручного обновления, история событий, tooltips

## 🏗️ Архитектура решения

### Структура проекта
```
TN_Doc/
├── Client/                          # Vue.js приложения
│   ├── package.json                 # Корневой package.json (workspaces)
│   ├── statusbar/                   # Модуль строки состояния
│   │   ├── package.json
│   │   ├── vite.config.ts
│   │   ├── tsconfig.json
│   │   └── src/
│   │       ├── main.ts
│   │       ├── App.vue
│   │       ├── components/
│   │       │   ├── StatusBar.vue
│   │       │   └── StatusIndicator.vue
│   │       ├── stores/
│   │       │   └── statusStore.ts
│   │       ├── composables/
│   │       │   ├── useSignalR.ts
│   │       │   └── useStatusPolling.ts
│   │       └── types/
│   │           └── status.types.ts
│   ├── shared/                      # Общие компоненты (для будущего)
│   │   ├── api/
│   │   │   └── ApiClient.ts
│   │   ├── components/
│   │   ├── composables/
│   │   └── types/
│   └── vite.config.base.ts          # Базовая конфигурация
├── wwwroot/
│   └── statusbar/                   # Скомпилированные файлы
└── Controllers/
    └── StatusController.cs           # API endpoint

```

### Технологический стек
- **Frontend**: Vue 3.4 + TypeScript 5.4
- **State Management**: Pinia
- **Build Tool**: Vite 5.2
- **Real-time**: SignalR 8.0
- **Utilities**: VueUse, @vueuse/core
- **Backend**: ASP.NET Core 8.0

## 💻 Детальный план реализации

### Фаза 1: Инфраструктура и базовая конфигурация

#### 1.1 Корневая структура Client
```json
// TN_Doc/Client/package.json
{
  "name": "@tn-doc/client",
  "private": true,
  "workspaces": [
    "statusbar",
    "shared"
  ],
  "scripts": {
    "dev": "npm run dev --workspace=statusbar",
    "build": "npm run build --workspace=statusbar",
    "build:all": "npm run build --workspaces --if-present",
    "type-check": "npm run type-check --workspaces --if-present"
  },
  "devDependencies": {
    "@types/node": "^20.11.0",
    "typescript": "^5.4.5"
  }
}
```

#### 1.2 Базовая конфигурация Vite
```typescript
// TN_Doc/Client/vite.config.base.ts
import { resolve } from 'node:path';

export function createBaseConfig(dirname: string) {
  return {
    resolve: {
      alias: {
        '@': resolve(dirname, 'src'),
        '@shared': resolve(dirname, '../shared/src')
      }
    },
    server: {
      proxy: {
        '/api': {
          target: 'http://localhost:5000',
          changeOrigin: true
        },
        '/statusHub': {
          target: 'http://localhost:5000',
          ws: true,
          changeOrigin: true
        }
      }
    }
  };
}
```

### Фаза 2: Модуль строки состояния

#### 2.1 Конфигурация модуля
```json
// TN_Doc/Client/statusbar/package.json
{
  "name": "@tn-doc/statusbar",
  "private": true,
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "vue-tsc --noEmit && vite build",
    "type-check": "vue-tsc --noEmit",
    "preview": "vite preview"
  },
  "dependencies": {
    "@microsoft/signalr": "^8.0.5",
    "@vueuse/core": "^10.7.0",
    "pinia": "^2.1.7",
    "vue": "^3.4.21"
  },
  "devDependencies": {
    "@vitejs/plugin-vue": "^5.0.4",
    "@vue/tsconfig": "^0.5.1",
    "sass": "^1.71.0",
    "typescript": "^5.4.5",
    "vite": "^5.2.0",
    "vue-tsc": "^2.0.0"
  }
}
```

#### 2.2 Vite конфигурация
```typescript
// TN_Doc/Client/statusbar/vite.config.ts
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'node:path';
import { createBaseConfig } from '../vite.config.base';

export default defineConfig({
  ...createBaseConfig(__dirname),
  plugins: [vue()],
  build: {
    outDir: resolve(__dirname, '../../wwwroot/statusbar'),
    emptyOutDir: true,
    sourcemap: process.env.NODE_ENV !== 'production',
    rollupOptions: {
      input: resolve(__dirname, 'src/main.ts'),
      output: {
        entryFileNames: 'status-bar.js',
        chunkFileNames: 'status-bar-[hash].js',
        assetFileNames: 'status-bar.[ext]'
      }
    }
  },
  server: {
    port: 5173,
    strictPort: true
  }
});
```

#### 2.3 TypeScript конфигурация
```json
// TN_Doc/Client/statusbar/tsconfig.json
{
  "extends": "@vue/tsconfig/tsconfig.dom.json",
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "lib": ["ES2022", "DOM"],
    "strict": true,
    "jsx": "preserve",
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noEmit": true,
    "paths": {
      "@/*": ["./src/*"],
      "@shared/*": ["../shared/src/*"]
    }
  },
  "include": ["src/**/*.ts", "src/**/*.tsx", "src/**/*.vue"],
  "exclude": ["node_modules", "dist"]
}
```

### Фаза 3: Компоненты и логика

#### 3.1 Типы данных
```typescript
// TN_Doc/Client/statusbar/src/types/status.types.ts
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
```

#### 3.2 API клиент
```typescript
// TN_Doc/Client/shared/src/api/ApiClient.ts
export class ApiClient {
  private baseUrl: string;
  private cache = new Map<string, { data: any; timestamp: number }>();
  private cacheTimeout = 5000; // 5 seconds

  constructor(baseUrl = '') {
    this.baseUrl = baseUrl;
  }

  async get<T>(endpoint: string, useCache = false): Promise<T> {
    if (useCache) {
      const cached = this.cache.get(endpoint);
      if (cached && Date.now() - cached.timestamp < this.cacheTimeout) {
        return cached.data as T;
      }
    }

    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      credentials: 'same-origin',
      headers: {
        'Accept': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error(`API Error: ${response.statusText}`);
    }

    const data = await response.json();

    if (useCache) {
      this.cache.set(endpoint, { data, timestamp: Date.now() });
    }

    return data as T;
  }
}

export const apiClient = new ApiClient();
```

#### 3.3 Pinia Store
```typescript
// TN_Doc/Client/statusbar/src/stores/statusStore.ts
import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { StatusResponse, DeviceStatus } from '../types/status.types';
import { apiClient } from '@shared/api/ApiClient';

export const useStatusStore = defineStore('status', () => {
  // State
  const devices = ref<DeviceStatus[]>([]);
  const services = ref<ServiceStatus>({
    messagingService: { isConnected: false }
  });
  const lastUpdate = ref<Date | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);
  const updateHistory = ref<StatusResponse[]>([]);

  // Getters
  const allDevicesConnected = computed(() =>
    devices.value.every(d => d.isConnected)
  );

  const criticalServicesConnected = computed(() =>
    services.value.messagingService.isConnected
  );

  const overallHealth = computed(() => {
    if (!criticalServicesConnected.value) return 'critical';
    if (!allDevicesConnected.value) return 'warning';
    return 'healthy';
  });

  // Actions
  async function fetchStatus() {
    try {
      isLoading.value = true;
      error.value = null;

      const response = await apiClient.get<StatusResponse>('/api/status');

      devices.value = response.devices;
      services.value = response.services;
      lastUpdate.value = new Date();

      // Keep last 10 updates in history
      updateHistory.value.unshift(response);
      if (updateHistory.value.length > 10) {
        updateHistory.value.pop();
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Unknown error';
      console.error('Failed to fetch status:', err);
    } finally {
      isLoading.value = false;
    }
  }

  function updateFromSignalR(data: StatusResponse) {
    devices.value = data.devices;
    services.value = data.services;
    lastUpdate.value = new Date();
  }

  return {
    // State
    devices,
    services,
    lastUpdate,
    isLoading,
    error,
    updateHistory,
    // Getters
    allDevicesConnected,
    criticalServicesConnected,
    overallHealth,
    // Actions
    fetchStatus,
    updateFromSignalR
  };
});
```

#### 3.4 SignalR Composable
```typescript
// TN_Doc/Client/statusbar/src/composables/useSignalR.ts
import { ref, onMounted, onUnmounted } from 'vue';
import * as signalR from '@microsoft/signalr';
import type { StatusResponse } from '../types/status.types';

export function useSignalR(hubUrl: string) {
  const connection = ref<signalR.HubConnection | null>(null);
  const connectionState = ref<'disconnected' | 'connecting' | 'connected'>('disconnected');
  const error = ref<string | null>(null);

  async function connect() {
    try {
      connectionState.value = 'connecting';

      connection.value = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, { withCredentials: true })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            if (retryContext.elapsedMilliseconds < 60000) {
              // Retry every 5 seconds for first minute
              return 5000;
            } else {
              // Then retry every 30 seconds
              return 30000;
            }
          }
        })
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      connection.value.onreconnecting(() => {
        connectionState.value = 'connecting';
      });

      connection.value.onreconnected(() => {
        connectionState.value = 'connected';
      });

      connection.value.onclose(() => {
        connectionState.value = 'disconnected';
      });

      await connection.value.start();
      connectionState.value = 'connected';
      error.value = null;
    } catch (err) {
      connectionState.value = 'disconnected';
      error.value = err instanceof Error ? err.message : 'Connection failed';
      console.error('SignalR connection failed:', err);
    }
  }

  function on(eventName: string, callback: (...args: any[]) => void) {
    connection.value?.on(eventName, callback);
  }

  function off(eventName: string, callback?: (...args: any[]) => void) {
    if (callback) {
      connection.value?.off(eventName, callback);
    } else {
      connection.value?.off(eventName);
    }
  }

  async function disconnect() {
    if (connection.value) {
      await connection.value.stop();
      connection.value = null;
      connectionState.value = 'disconnected';
    }
  }

  onMounted(() => {
    connect();
  });

  onUnmounted(() => {
    disconnect();
  });

  return {
    connection,
    connectionState,
    error,
    connect,
    disconnect,
    on,
    off
  };
}
```

#### 3.5 Основной компонент
```vue
<!-- TN_Doc/Client/statusbar/src/components/StatusBar.vue -->
<template>
  <div class="status-bar" :class="`status-bar--${store.overallHealth}`">
    <div class="status-bar__container">
      <!-- Devices Section -->
      <div class="status-bar__section">
        <span class="status-bar__label">Устройства:</span>
        <StatusIndicator
          v-for="device in store.devices"
          :key="device.id"
          :label="device.name"
          :status="device.isConnected ? 'online' : 'offline'"
          :latency="device.latencyMs"
          :tooltip="`${device.name}: ${device.isConnected ? 'Подключено' : 'Отключено'}`"
          @click="handleDeviceClick(device)"
        />
      </div>

      <!-- Services Section -->
      <div class="status-bar__section">
        <span class="status-bar__label">Сервисы:</span>
        <StatusIndicator
          label="MS"
          :status="store.services.messagingService.isConnected ? 'online' : 'offline'"
          :latency="store.services.messagingService.latencyMs"
          tooltip="Messaging Service"
        />
        <StatusIndicator
          v-if="store.services.elis"
          label="ELIS"
          :status="store.services.elis.isConnected ? 'online' : 'offline'"
          :latency="store.services.elis.latencyMs"
          tooltip="Лабораторная система"
        />
      </div>

      <!-- Actions Section -->
      <div class="status-bar__section status-bar__section--actions">
        <button
          class="status-bar__refresh"
          @click="refresh"
          :disabled="store.isLoading"
          title="Обновить статус"
        >
          <i class="fa fa-refresh" :class="{ 'fa-spin': store.isLoading }"></i>
        </button>

        <span v-if="store.lastUpdate" class="status-bar__timestamp">
          Обновлено: {{ formatTime(store.lastUpdate) }}
        </span>

        <span
          class="status-bar__connection"
          :class="`status-bar__connection--${signalRState}`"
          :title="`SignalR: ${signalRState}`"
        >
          <i class="fa fa-wifi"></i>
        </span>
      </div>
    </div>

    <!-- Error notification -->
    <div v-if="store.error" class="status-bar__error">
      <i class="fa fa-exclamation-triangle"></i>
      {{ store.error }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, computed } from 'vue';
import { useStatusStore } from '../stores/statusStore';
import { useSignalR } from '../composables/useSignalR';
import { useIntervalFn } from '@vueuse/core';
import StatusIndicator from './StatusIndicator.vue';
import type { DeviceStatus, StatusResponse } from '../types/status.types';

const store = useStatusStore();
const { connectionState, on } = useSignalR('/statusHub');

const signalRState = computed(() => connectionState.value);

// Auto-refresh every 5 seconds as fallback
const { pause, resume } = useIntervalFn(() => {
  if (connectionState.value !== 'connected') {
    store.fetchStatus();
  }
}, 5000);

// SignalR real-time updates
on('statusUpdated', (data: StatusResponse) => {
  store.updateFromSignalR(data);
});

// Initial fetch
onMounted(() => {
  store.fetchStatus();
});

function refresh() {
  store.fetchStatus();
}

function handleDeviceClick(device: DeviceStatus) {
  console.log('Device clicked:', device);
  // Future: Show device details modal
}

function formatTime(date: Date): string {
  return date.toLocaleTimeString('ru-RU');
}
</script>

<style lang="scss">
.status-bar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  background: #f8f9fa;
  border-top: 1px solid #dee2e6;
  z-index: 1000;
  font-size: 14px;

  &--critical {
    background: #fff5f5;
    border-top-color: #dc3545;
  }

  &--warning {
    background: #fffaf0;
    border-top-color: #ffc107;
  }

  &__container {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 8px 16px;
    max-width: 1400px;
    margin: 0 auto;
  }

  &__section {
    display: flex;
    align-items: center;
    gap: 12px;

    &--actions {
      margin-left: auto;
    }
  }

  &__label {
    font-weight: 600;
    color: #495057;
  }

  &__refresh {
    background: none;
    border: 1px solid #dee2e6;
    border-radius: 4px;
    padding: 4px 8px;
    cursor: pointer;
    transition: all 0.2s;

    &:hover:not(:disabled) {
      background: #e9ecef;
    }

    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
  }

  &__timestamp {
    color: #6c757d;
    font-size: 12px;
  }

  &__connection {
    padding: 4px 8px;
    border-radius: 4px;

    &--connected {
      color: #28a745;
    }

    &--connecting {
      color: #ffc107;
      animation: pulse 1s infinite;
    }

    &--disconnected {
      color: #dc3545;
    }
  }

  &__error {
    background: #dc3545;
    color: white;
    padding: 4px 16px;
    text-align: center;
    font-size: 12px;
  }
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}
</style>
```

#### 3.6 Индикатор компонент
```vue
<!-- TN_Doc/Client/statusbar/src/components/StatusIndicator.vue -->
<template>
  <div
    class="status-indicator"
    :class="[`status-indicator--${status}`]"
    :title="tooltip"
    @click="$emit('click')"
  >
    <span class="status-indicator__dot"></span>
    <span class="status-indicator__label">{{ label }}</span>
    <span v-if="latency !== undefined" class="status-indicator__latency">
      {{ latency }}ms
    </span>
  </div>
</template>

<script setup lang="ts">
interface Props {
  label: string;
  status: 'online' | 'offline' | 'warning';
  latency?: number;
  tooltip?: string;
}

defineProps<Props>();
defineEmits<{
  click: []
}>();
</script>

<style lang="scss">
.status-indicator {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 8px;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(0, 0, 0, 0.05);
  }

  &--online {
    .status-indicator__dot {
      background: #28a745;
      box-shadow: 0 0 4px #28a745;
    }
  }

  &--offline {
    .status-indicator__dot {
      background: #dc3545;
      animation: blink 1s infinite;
    }
  }

  &--warning {
    .status-indicator__dot {
      background: #ffc107;
    }
  }

  &__dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    transition: all 0.3s;
  }

  &__label {
    font-weight: 500;
    color: #212529;
  }

  &__latency {
    font-size: 11px;
    color: #6c757d;
  }
}

@keyframes blink {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.3; }
}
</style>
```

#### 3.7 Точка входа
```typescript
// TN_Doc/Client/statusbar/src/main.ts
import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';

// Wait for DOM to be ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initApp);
} else {
  initApp();
}

function initApp() {
  const container = document.getElementById('status-bar');

  if (!container) {
    console.warn('Status bar container not found');
    return;
  }

  const app = createApp(App);
  const pinia = createPinia();

  app.use(pinia);
  app.mount(container);
}
```

```vue
<!-- TN_Doc/Client/statusbar/src/App.vue -->
<template>
  <StatusBar />
</template>

<script setup lang="ts">
import StatusBar from './components/StatusBar.vue';
</script>
```

### Фаза 4: Backend реализация

#### 4.1 Startup.cs изменения
```csharp
// TN_Doc/Startup.cs - добавить в ConfigureServices
services.AddSignalR();
services.AddSingleton<IMemoryCache, MemoryCache>();
services.AddHostedService<StatusMonitoringService>();

// TN_Doc/Startup.cs - добавить в Configure
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<StatusHub>("/statusHub");
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});
```

#### 4.2 Status Controller
```csharp
// TN_Doc/Controllers/StatusController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
using System.Diagnostics;
using TN_Doc.Services;

namespace TN_Doc.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly IAppConfigService _configService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<StatusController> _logger;
    private const string CacheKey = "status_data";
    private const int CacheExpirationSeconds = 5;

    public StatusController(
        IAppConfigService configService,
        IMemoryCache cache,
        ILogger<StatusController> logger)
    {
        _configService = configService;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetStatus()
    {
        // Try to get from cache first
        if (_cache.TryGetValue(CacheKey, out StatusResponse cachedData))
        {
            return Ok(cachedData);
        }

        var response = await GenerateStatusResponse();

        // Cache the response
        _cache.Set(CacheKey, response, TimeSpan.FromSeconds(CacheExpirationSeconds));

        return Ok(response);
    }

    private async Task<StatusResponse> GenerateStatusResponse()
    {
        var config = _configService.GetAppCfg();
        var devices = new List<DeviceStatus>();

        // Check database connections for each device
        foreach (var device in config.Devices.Where(d => d.Use))
        {
            var status = await CheckDatabaseConnection(device);
            devices.Add(status);
        }

        // Check services
        var services = new ServiceStatus
        {
            MessagingService = await CheckMessagingService(),
            Elis = config.Elis?.Use == true ? await CheckElisService() : null,
            OpcDa = config.OpcConnectionSettings?.Any(o => o.Type == "DA") == true
                ? await CheckOpcDaService() : null,
            OpcUa = config.OpcConnectionSettings?.Any(o => o.Type == "UA") == true
                ? await CheckOpcUaService() : null
        };

        return new StatusResponse
        {
            Devices = devices,
            Services = services,
            Timestamp = DateTime.Now.ToString("o")
        };
    }

    private async Task<DeviceStatus> CheckDatabaseConnection(Device device)
    {
        var stopwatch = Stopwatch.StartNew();
        var status = new DeviceStatus
        {
            Id = device.IdDevice.ToString(),
            Name = device.Name,
            Type = "database",
            IsConnected = false
        };

        try
        {
            using var connection = new MySqlConnection(device.ConnString);
            connection.ConnectionTimeout = 2; // 2 second timeout
            await connection.OpenAsync();

            // Simple query to verify connection
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync();

            status.IsConnected = true;
            status.LatencyMs = (int)stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Database connection check failed for {device.Name}: {ex.Message}");
            status.Error = ex.Message;
        }
        finally
        {
            status.LastChecked = DateTime.Now;
        }

        return status;
    }

    private async Task<ConnectionStatus> CheckMessagingService()
    {
        var status = new ConnectionStatus { IsConnected = false };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var response = await client.GetAsync("http://localhost:5010/health");

            status.IsConnected = response.IsSuccessStatusCode;
            status.LatencyMs = (int)stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Messaging service check failed: {ex.Message}");
            status.Error = ex.Message;
        }
        finally
        {
            status.LastChecked = DateTime.Now;
        }

        return status;
    }

    private async Task<ConnectionStatus> CheckElisService()
    {
        // Implementation for ELIS service check
        return new ConnectionStatus
        {
            IsConnected = true,
            LatencyMs = 10,
            LastChecked = DateTime.Now
        };
    }

    private async Task<ConnectionStatus> CheckOpcDaService()
    {
        // Implementation for OPC DA check
        return new ConnectionStatus
        {
            IsConnected = true,
            LatencyMs = 15,
            LastChecked = DateTime.Now
        };
    }

    private async Task<ConnectionStatus> CheckOpcUaService()
    {
        // Implementation for OPC UA check
        return new ConnectionStatus
        {
            IsConnected = true,
            LatencyMs = 12,
            LastChecked = DateTime.Now
        };
    }
}

// DTOs
public class StatusResponse
{
    public List<DeviceStatus> Devices { get; set; }
    public ServiceStatus Services { get; set; }
    public string Timestamp { get; set; }
}

public class DeviceStatus
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public bool IsConnected { get; set; }
    public int? LatencyMs { get; set; }
    public DateTime? LastChecked { get; set; }
    public string Error { get; set; }
}

public class ServiceStatus
{
    public ConnectionStatus MessagingService { get; set; }
    public ConnectionStatus Elis { get; set; }
    public ConnectionStatus OpcDa { get; set; }
    public ConnectionStatus OpcUa { get; set; }
}

public class ConnectionStatus
{
    public bool IsConnected { get; set; }
    public int? LatencyMs { get; set; }
    public DateTime? LastChecked { get; set; }
    public string Error { get; set; }
}
```

#### 4.3 SignalR Hub
```csharp
// TN_Doc/Hubs/StatusHub.cs
using Microsoft.AspNetCore.SignalR;

namespace TN_Doc.Hubs;

public class StatusHub : Hub
{
    private readonly ILogger<StatusHub> _logger;

    public StatusHub(ILogger<StatusHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}
```

#### 4.4 Background Service
```csharp
// TN_Doc/Services/StatusMonitoringService.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using TN_Doc.Hubs;

namespace TN_Doc.Services;

public class StatusMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<StatusHub> _hubContext;
    private readonly ILogger<StatusMonitoringService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);
    private StatusResponse _lastStatus;

    public StatusMonitoringService(
        IServiceProvider serviceProvider,
        IHubContext<StatusHub> hubContext,
        ILogger<StatusMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Status monitoring service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var statusController = new StatusController(
                    scope.ServiceProvider.GetRequiredService<IAppConfigService>(),
                    scope.ServiceProvider.GetRequiredService<IMemoryCache>(),
                    scope.ServiceProvider.GetRequiredService<ILogger<StatusController>>()
                );

                var currentStatus = await statusController.GenerateStatusResponse();

                // Check if status changed
                if (HasStatusChanged(currentStatus))
                {
                    _lastStatus = currentStatus;
                    await _hubContext.Clients.All.SendAsync(
                        "statusUpdated",
                        currentStatus,
                        stoppingToken
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in status monitoring");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private bool HasStatusChanged(StatusResponse current)
    {
        if (_lastStatus == null) return true;

        // Compare device statuses
        foreach (var device in current.Devices)
        {
            var lastDevice = _lastStatus.Devices.FirstOrDefault(d => d.Id == device.Id);
            if (lastDevice == null || lastDevice.IsConnected != device.IsConnected)
            {
                return true;
            }
        }

        // Compare service statuses
        if (current.Services.MessagingService.IsConnected !=
            _lastStatus.Services.MessagingService.IsConnected)
        {
            return true;
        }

        return false;
    }
}
```

### Фаза 5: Интеграция в Layout

```html
<!-- TN_Doc/Views/Shared/_Layout.cshtml -->
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"]</title>
    <!-- Existing styles -->
    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/site.css" />
    <!-- ... other styles ... -->

    <!-- Status bar styles -->
    <link rel="stylesheet" href="/statusbar/status-bar.css?v=@ViewData["Version"]" />
</head>
<body>
    <div class="container">
        <main role="main" class="pb-5"> <!-- Увеличен padding для status bar -->
            @RenderBody()
        </main>
    </div>

    <!-- Status bar container -->
    <div id="status-bar"></div>

    @await RenderSectionAsync("Scripts", required: false)

    <!-- Status bar script -->
    <script type="module" src="/statusbar/status-bar.js?v=@ViewData["Version"]"></script>
</body>
</html>
```

### Фаза 6: CI/CD Pipeline

```yaml
# .gitlab-ci.yml
stages:
  - build-frontend
  - build-backend
  - test
  - package

variables:
  NODE_VERSION: "20"
  DOTNET_VERSION: "8.0"

# Build frontend
build:statusbar:
  stage: build-frontend
  image: node:${NODE_VERSION}
  before_script:
    - cd TN_Doc/Client
    - npm ci
  script:
    - npm run build:all
    - npm run type-check
  artifacts:
    paths:
      - TN_Doc/wwwroot/statusbar/
    expire_in: 7 days
  cache:
    key: ${CI_COMMIT_REF_SLUG}-npm
    paths:
      - TN_Doc/Client/node_modules/
      - TN_Doc/Client/*/node_modules/

# Build backend
build:backend:
  stage: build-backend
  image: mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}
  dependencies:
    - build:statusbar
  script:
    - dotnet restore
    - dotnet build -c Release
    - dotnet publish TN_Doc/TN_Doc.csproj -c Release -o ./publish
  artifacts:
    paths:
      - publish/
    expire_in: 7 days
```

## 🚀 План миграции

### Этап 1: MVP строки состояния (2 недели)
- [x] Настройка инфраструктуры Vue + Vite
- [x] Базовая строка состояния
- [x] SignalR интеграция
- [x] API endpoint
- [x] CI/CD pipeline

### Этап 2: Расширение функциональности (1 неделя)
- [ ] История изменений статуса
- [ ] Детальная информация по клику
- [ ] Экспорт статистики
- [ ] Настройки пользователя

### Этап 3: Shared компоненты (2 недели)
- [ ] UI Kit компонентов
- [ ] Общие composables
- [ ] Централизованный API клиент
- [ ] Система нотификаций

### Этап 4: Следующий модуль (3-4 недели)
- [ ] Выбор модуля для миграции
- [ ] Разработка на Vue
- [ ] Интеграция с существующим кодом
- [ ] Тестирование

## 📊 Мониторинг и метрики

### Метрики производительности
- Время загрузки страницы
- Размер bundle
- Время отклика API
- Задержка SignalR

### Метрики надёжности
- Uptime каждого сервиса
- Количество reconnect'ов SignalR
- Частота ошибок API

### Метрики использования
- Количество пользователей
- Частота обновлений
- Использование функций

## 🔒 Безопасность

### API Security
- Rate limiting на `/api/status`
- CORS настройки для production
- Валидация входных данных

### Frontend Security
- Content Security Policy
- XSS защита
- Безопасное хранение токенов

## 📚 Документация

### Для разработчиков
- Архитектурные решения
- API документация
- Руководство по компонентам

### Для пользователей
- Описание индикаторов
- Troubleshooting guide
- FAQ

## ✅ Критерии успеха

1. **Функциональность**
   - ✅ Отображение статуса всех устройств
   - ✅ Real-time обновления через SignalR
   - ✅ Fallback на polling при сбое SignalR
   - ✅ История изменений

2. **Производительность**
   - Bundle size < 100KB
   - Время загрузки < 1 секунда
   - API response < 200ms

3. **Надёжность**
   - Uptime > 99.9%
   - Автоматическое восстановление соединений
   - Корректная обработка ошибок

4. **UX**
   - Интуитивный интерфейс
   - Мгновенная обратная связь
   - Информативные сообщения об ошибках

## 🎯 Итоги

Данный план закладывает фундамент для полной миграции TN_Doc на Vue.js, начиная с минимально рискованного компонента - строки состояния. Архитектура спроектирована с учётом масштабирования и позволит постепенно мигрировать остальные части приложения.