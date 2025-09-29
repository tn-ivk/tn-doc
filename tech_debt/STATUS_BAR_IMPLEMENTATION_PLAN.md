# План реализации строки состояния и начала миграции на Vue.js

## 📋 Обзор задачи

Реализация строки состояния как первого этапа полной миграции фронтенда TN_Doc на Vue.js. Строка состояния станет MVP для новой архитектуры и заложит фундамент для поэтапной миграции всего интерфейса.

**Адаптация под TN_Doc v1.4.2**: План адаптирован под существующую архитектуру TN_Doc, включая интеграцию с IAppConfigService, NLog и существующими паттернами разработки.

## 🎯 Цели и требования

### Стратегические цели
- **Пилотный проект** для миграции на Vue.js
- **Установка инфраструктуры** для будущих Vue-компонентов
- **Минимальный риск** для существующего функционала
- **Основа для масштабирования** на весь проект

### Функциональные требования
- **Индикаторы устройств**: Статус подключения к БД для всех сконфигурированных устройств из CfgApp.json
- **Индикатор MS**: Статус подключения к TN_MessagingService (`http://localhost:5010`)
- **OPC сервер**: Статус подключения к OPC DA/UA (через TN_MessagingService)
- **ELIS сервис**: Доступность лабораторной системы (через TN.ElisConnector)
- **Реальное время**: Push через SignalR + fallback на polling
- **Визуализация**: Цветовая индикация, иконки состояния, время отклика
- **UX**: Возможность ручного обновления, история событий, tooltips
- **Логирование**: Полная интеграция с существующим NLog для серверной и клиентской диагностики

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
│   │       │   ├── StatusIndicator.vue
│   │       │   └── icons/
│   │       │       ├── IconRefresh.vue
│   │       │       ├── IconWifi.vue
│   │       │       └── IconWarning.vue
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
- **Backend**: ASP.NET Core 8.0 (существующая архитектура TN_Doc)
- **Интеграция**: IAppConfigService, NLog, существующие сервисы DI
- **База данных**: MySQL/MariaDB через Pomelo.EntityFrameworkCore.MySql
- **Внешние системы**: TN_MessagingService, TN.ElisConnector

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

#### 2.1 Конфигурация модуля (совместимость с TN_Doc)
```json
// TN_Doc/Client/statusbar/package.json
{
  "name": "@tn-doc/statusbar",
  "version": "1.4.2",
  "private": true,
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "vue-tsc --noEmit && vite build",
    "type-check": "vue-tsc --noEmit",
    "preview": "vite preview",
    "test": "echo \"Status bar tests not implemented yet\""
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
  },
  "engines": {
    "node": ">=18.0.0",
    "npm": ">=8.0.0"
  },
  "repository": {
    "type": "git",
    "url": "http://192.168.100.100/orpovy/ivk/tn_doc.git"
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
        chunkFileNames: 'status-bar.chunk.js',
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
import type { StatusResponse, DeviceStatus, ServiceStatus } from '../types/status.types';
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
import { ref, onMounted, onUnmounted } from 'vue'
import * as signalR from '@microsoft/signalr'
import type { StatusResponse } from '../types/status.types'
import { useStatusLogging } from './useStatusLogging'

export function useSignalR(hubUrl: string) {
  const connection = ref<signalR.HubConnection | null>(null)
  const connectionState = ref<'disconnected' | 'connecting' | 'connected'>('disconnected')
  const error = ref<string | null>(null)
  const { log } = useStatusLogging()

  async function connect() {
    try {
      connectionState.value = 'connecting'
      log('info', `Attempting to connect to SignalR hub: ${hubUrl}`)

      connection.value = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, { withCredentials: true })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            const delay = retryContext.elapsedMilliseconds < 60000 ? 5000 : 30000
            log('warn', `SignalR reconnection attempt #${retryContext.previousRetryCount + 1} in ${delay}ms`)
            return delay
          }
        })
        .configureLogging(signalR.LogLevel.Warning)
        .build()

      connection.value.onreconnecting((error) => {
        connectionState.value = 'connecting'
        log('warn', 'SignalR connection lost, attempting to reconnect', error)
      })

      connection.value.onreconnected((connectionId) => {
        connectionState.value = 'connected'
        log('info', `SignalR reconnected successfully with ID: ${connectionId}`)
      })

      connection.value.onclose((error) => {
        connectionState.value = 'disconnected'
        if (error) {
          log('error', 'SignalR connection closed with error', error)
        } else {
          log('info', 'SignalR connection closed normally')
        }
      })

      await connection.value.start()
      connectionState.value = 'connected'
      error.value = null
      log('info', `SignalR connected successfully to ${hubUrl} with ID: ${connection.value.connectionId}`)
    } catch (err) {
      connectionState.value = 'disconnected'
      error.value = err instanceof Error ? err.message : 'Connection failed'
      log('error', 'SignalR connection failed', err)
    }
  }

  function on(eventName: string, callback: (...args: any[]) => void) {
    connection.value?.on(eventName, callback)
    log('debug', `Subscribed to SignalR event: ${eventName}`)
  }

  function off(eventName: string, callback?: (...args: any[]) => void) {
    if (callback) {
      connection.value?.off(eventName, callback)
    } else {
      connection.value?.off(eventName)
    }
    log('debug', `Unsubscribed from SignalR event: ${eventName}`)
  }

  async function disconnect() {
    if (connection.value) {
      await connection.value.stop()
      connection.value = null
      connectionState.value = 'disconnected'
      log('info', 'SignalR connection manually disconnected')
    }
  }

  onMounted(() => {
    connect()
  })

  onUnmounted(() => {
    disconnect()
  })

  return {
    connection,
    connectionState,
    error,
    connect,
    disconnect,
    on,
    off
  }
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
          <IconRefresh :spinning="store.isLoading" />
        </button>

        <span
          class="status-bar__connection"
          :class="`status-bar__connection--${signalRState}`"
          :title="`SignalR: ${signalRState}`"
        >
          <IconWifi />
        </span>
      </div>
    </div>

    <!-- Error notification -->
    <div v-if="store.error" class="status-bar__error">
      <IconWarning />
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
import IconRefresh from './icons/IconRefresh.vue';
import IconWifi from './icons/IconWifi.vue';
import IconWarning from './icons/IconWarning.vue';
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
    /* timestamp disabled */
    display: none;
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
  .spin { animation: spin 1s linear infinite; }
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}
@keyframes spin { from { transform: rotate(0deg);} to { transform: rotate(360deg);} }
</style>
```

#### 3.6 SVG Иконки компоненты

```vue
<!-- TN_Doc/Client/statusbar/src/components/icons/IconRefresh.vue -->
<template>
  <svg :width="size" :height="size" viewBox="0 0 24 24" :class="{ 'spin': spinning }">
    <path
      fill="currentColor"
      :d="spinning ? spinPath : refreshPath"
    />
  </svg>
</template>

<script setup lang="ts">
interface Props {
  size?: number;
  spinning?: boolean;
}

withDefaults(defineProps<Props>(), {
  size: 16,
  spinning: false
});

const refreshPath = "M12 6V3L8 7l4 4V8c2.76 0 5 2.24 5 5a5 5 0 0 1-9.9 1H5.02A7.002 7.002 0 0 0 19 13c0-3.87-3.13-7-7-7z";
const spinPath = "M12 4V2A10 10 0 1 0 22 12h-2a8 8 0 1 1-8-8z";
</script>

<style scoped>
.spin {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}
</style>
```

```vue
<!-- TN_Doc/Client/statusbar/src/components/icons/IconWifi.vue -->
<template>
  <svg :width="size" :height="size" viewBox="0 0 24 24">
    <path
      fill="currentColor"
      d="M12 21l-4-4h8l-4 4zm-8-8l-4-4h24l-4 4H4zm4-8L4 1h16l-4 4H8z"
    />
  </svg>
</template>

<script setup lang="ts">
interface Props {
  size?: number;
}

withDefaults(defineProps<Props>(), {
  size: 16
});
</script>
```

```vue
<!-- TN_Doc/Client/statusbar/src/components/icons/IconWarning.vue -->
<template>
  <svg :width="size" :height="size" viewBox="0 0 24 24">
    <path
      fill="currentColor"
      d="M12 2L2 20h20L12 2zm0 3.5L19.5 18.5h-15L12 5.5zM11 10v4h2v-4h-2zm0 6v2h2v-2h-2z"
    />
  </svg>
</template>

<script setup lang="ts">
interface Props {
  size?: number;
}

withDefaults(defineProps<Props>(), {
  size: 16
});
</script>
```

#### 3.7 Индикатор компонент
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

#### 4.1 Startup.cs изменения (интеграция с существующей структурой TN_Doc)
```csharp
// TN_Doc/Startup.cs - ДОПОЛНЕНИЯ к существующим сервисам
public void ConfigureServices(IServiceCollection services)
{
    // ... существующие сервисы TN_Doc ...

    // Status Bar сервисы - интегрируем с существующими
    services.AddSignalR();
    // services.AddMemoryCache(); // Возможно уже есть в TN_Doc

    // HTTP клиенты для проверки внешних сервисов
    services.AddHttpClient("MessagingService", client =>
    {
        client.BaseAddress = new Uri("http://localhost:5010");
        client.Timeout = TimeSpan.FromSeconds(2);
        client.DefaultRequestHeaders.Add("User-Agent", "TN_Doc-StatusChecker/1.4.2");
    });

    services.AddHttpClient("Elis", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.Add("User-Agent", "TN_Doc-StatusChecker/1.4.2");
    });

    // Rate limiting для API
    services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("api-status", config =>
        {
            config.PermitLimit = 10;
            config.Window = TimeSpan.FromSeconds(10);
            config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            config.QueueLimit = 2;
        });
    });

    // Наши сервисы - используют уже существующий IAppConfigService
    services.AddScoped<IStatusProvider, StatusProvider>();
    services.AddHostedService<StatusMonitoringService>();

    // ... остальные существующие сервисы TN_Doc ...
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ... существующие настройки TN_Doc ...

    app.UseRateLimiter();

    // ... остальные middleware ...

    app.UseEndpoints(endpoints =>
    {
        // Существующие маршруты TN_Doc
        endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        // Добавляем SignalR Hub
        endpoints.MapHub<StatusHub>("/statusHub");
    });
}
```

#### 4.1.1 Health Checks реализация
```csharp
// TN_Doc/HealthChecks/DatabaseHealthCheck.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MySqlConnector;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IAppConfigService _configService;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(IAppConfigService configService, ILogger<DatabaseHealthCheck> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = _configService.GetAppCfg();
            var devices = config.Devices.Where(d => d.Use).ToList();

            if (!devices.Any())
            {
                return HealthCheckResult.Unhealthy("No active devices configured");
            }

            var tasks = devices.Select(async device =>
            {
                try
                {
                    var csb = new MySqlConnectionStringBuilder(device.ConnString)
                    {
                        ConnectionTimeout = 1,
                        DefaultCommandTimeout = 1
                    };

                    using var connection = new MySqlConnection(csb.ConnectionString);
                    await connection.OpenAsync(cancellationToken);

                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT 1";
                    await command.ExecuteScalarAsync(cancellationToken);

                    return (device.Name, Success: true, Error: (string?)null);
                }
                catch (Exception ex)
                {
                    return (device.Name, Success: false, Error: ex.Message);
                }
            });

            var results = await Task.WhenAll(tasks);
            var failed = results.Where(r => !r.Success).ToList();

            if (failed.Count == devices.Count)
            {
                return HealthCheckResult.Unhealthy(
                    "All database connections failed",
                    data: failed.ToDictionary(f => f.Name, f => (object)f.Error));
            }

            if (failed.Any())
            {
                return HealthCheckResult.Degraded(
                    $"{failed.Count} of {devices.Count} database connections failed",
                    data: failed.ToDictionary(f => f.Name, f => (object)f.Error));
            }

            return HealthCheckResult.Healthy(
                $"All {devices.Count} database connections are healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return HealthCheckResult.Unhealthy("Health check execution failed", ex);
        }
    }
}

// TN_Doc/HealthChecks/MessagingServiceHealthCheck.cs
public class MessagingServiceHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MessagingServiceHealthCheck> _logger;

    public MessagingServiceHealthCheck(
        IHttpClientFactory httpClientFactory,
        ILogger<MessagingServiceHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient("MessagingService");
            var response = await client.GetAsync("/health", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Messaging service is responding");
            }

            return HealthCheckResult.Unhealthy(
                $"Messaging service returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Messaging service health check failed");
            return HealthCheckResult.Unhealthy("Messaging service is unreachable", ex);
        }
    }
}

// TN_Doc/HealthChecks/ElisHealthCheck.cs
public class ElisHealthCheck : IHealthCheck
{
    private readonly IAppConfigService _configService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ElisHealthCheck> _logger;

    public ElisHealthCheck(
        IAppConfigService configService,
        IHttpClientFactory httpClientFactory,
        ILogger<ElisHealthCheck> logger)
    {
        _configService = configService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = _configService.GetAppCfg();

            if (config.Elis?.Use != true)
            {
                return HealthCheckResult.Healthy("ELIS is disabled");
            }

            if (string.IsNullOrEmpty(config.Elis.LabHubUrl))
            {
                return HealthCheckResult.Unhealthy("ELIS URL is not configured");
            }

            using var client = _httpClientFactory.CreateClient("Elis");
            client.BaseAddress = new Uri(config.Elis.LabHubUrl);
            var response = await client.GetAsync("/health", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("ELIS service is responding");
            }

            return HealthCheckResult.Unhealthy(
                $"ELIS service returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ELIS health check failed");
            return HealthCheckResult.Unhealthy("ELIS service is unreachable", ex);
        }
    }
}
```

#### 4.2 Status Controller (интегрирован с TN_Doc)
```csharp
// TN_Doc/Controllers/StatusController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TN_Doc.Services;

namespace TN_Doc.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly IStatusProvider _statusProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<StatusController> _logger;
    private readonly IAppConfigService _appConfigService;

    private const string CacheKey = "status_data";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromSeconds(5);

    public StatusController(
        IStatusProvider statusProvider,
        IMemoryCache cache,
        ILogger<StatusController> logger,
        IAppConfigService appConfigService)
    {
        _statusProvider = statusProvider;
        _cache = cache;
        _logger = logger;
        _appConfigService = appConfigService;
    }

    /// <summary>
    /// Получить текущий статус всех устройств и сервисов
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(StatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStatus()
    {
        var requestStart = DateTime.UtcNow;
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogDebug("Status request from {ClientIP}", clientIp);

        try
        {
            // Проверяем кэш
            if (_cache.TryGetValue(CacheKey, out StatusResponse cachedData))
            {
                var cacheHitDuration = DateTime.UtcNow - requestStart;
                _logger.LogTrace("Status served from cache in {Duration}ms to {ClientIP}",
                    cacheHitDuration.TotalMilliseconds, clientIp);
                return Ok(cachedData);
            }

            // Генерируем новый статус
            var response = await _statusProvider.GetStatusAsync(HttpContext.RequestAborted);

            // Кэшируем результат
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration,
                Priority = CacheItemPriority.High
            };
            _cache.Set(CacheKey, response, cacheOptions);

            var totalDuration = DateTime.UtcNow - requestStart;
            _logger.LogInformation("Status request completed in {Duration}ms for {ClientIP}. " +
                "Devices: {HealthyDevices}/{TotalDevices}, Services: {HealthyServices}",
                totalDuration.TotalMilliseconds,
                clientIp,
                response.Devices.Count(d => d.IsConnected),
                response.Devices.Count,
                CountHealthyServices(response.Services));

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Status request from {ClientIP} was cancelled", clientIp);
            return StatusCode(499, "Request was cancelled");
        }
        catch (Exception ex)
        {
            var errorDuration = DateTime.UtcNow - requestStart;
            _logger.LogError(ex, "Failed to retrieve status for {ClientIP} after {Duration}ms: {ErrorMessage}",
                clientIp, errorDuration.TotalMilliseconds, ex.Message);

            return StatusCode(500, new { error = "Failed to retrieve system status", details = ex.Message });
        }
    }

    /// <summary>
    /// Принудительное обновление кэша статусов
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(StatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshStatus()
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        _logger.LogInformation("Status refresh requested by {ClientIP}", clientIp);

        try
        {
            _cache.Remove(CacheKey);
            var response = await _statusProvider.GetStatusAsync(HttpContext.RequestAborted);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration,
                Priority = CacheItemPriority.High
            };
            _cache.Set(CacheKey, response, cacheOptions);

            _logger.LogInformation("Status refreshed by {ClientIP}. New status: Devices {HealthyDevices}/{TotalDevices}",
                clientIp,
                response.Devices.Count(d => d.IsConnected),
                response.Devices.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh status for {ClientIP}: {ErrorMessage}", clientIp, ex.Message);
            return StatusCode(500, new { error = "Failed to refresh status", details = ex.Message });
        }
    }

    private static int CountHealthyServices(ServiceStatus services)
    {
        var healthy = 0;
        if (services.MessagingService?.IsConnected == true) healthy++;
        if (services.Elis?.IsConnected == true) healthy++;
        if (services.OpcDa?.IsConnected == true) healthy++;
        if (services.OpcUa?.IsConnected == true) healthy++;
        return healthy;
    }
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

#### 4.4 Background Service (с полной интеграцией NLog)
```csharp
// TN_Doc/Services/StatusMonitoringService.cs
using Microsoft.AspNetCore.SignalR;
using TN_Doc.Hubs;

namespace TN_Doc.Services;

public class StatusMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<StatusHub> _hubContext;
    private readonly ILogger<StatusMonitoringService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);
    private StatusResponse _lastStatus;
    private int _consecutiveErrors = 0;
    private const int MAX_CONSECUTIVE_ERRORS = 5;

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
        _logger.LogInformation("Status monitoring background service started with {CheckInterval}s interval",
            _checkInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            var cycleStart = DateTime.UtcNow;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<IStatusProvider>();
                var currentStatus = await provider.GetStatusAsync(stoppingToken);

                var hasChanges = HasStatusChanged(currentStatus);
                var connectedClients = await GetConnectedClientsCount();

                if (hasChanges)
                {
                    _lastStatus = currentStatus;

                    _logger.LogInformation("Status changed detected, broadcasting to {ClientCount} clients. " +
                        "Devices: {HealthyDevices}/{TotalDevices}, Services: MS={MsStatus}, ELIS={ElisStatus}",
                        connectedClients,
                        currentStatus.Devices.Count(d => d.IsConnected),
                        currentStatus.Devices.Count,
                        currentStatus.Services.MessagingService?.IsConnected ?? false,
                        currentStatus.Services.Elis?.IsConnected);

                    await _hubContext.Clients.All.SendAsync(
                        "statusUpdated",
                        currentStatus,
                        stoppingToken);
                }
                else
                {
                    _logger.LogTrace("No status changes detected in monitoring cycle");
                }

                // Сброс счетчика ошибок при успешном выполнении
                _consecutiveErrors = 0;

                // Лог производительности каждые 10 циклов
                if (DateTime.UtcNow.Minute % 10 == 0 && DateTime.UtcNow.Second < 10)
                {
                    var cycleDuration = DateTime.UtcNow - cycleStart;
                    _logger.LogInformation("Status monitoring performance: cycle took {CycleDurationMs}ms, " +
                        "{ConnectedClients} connected clients",
                        cycleDuration.TotalMilliseconds, connectedClients);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Status monitoring service is shutting down");
                break;
            }
            catch (Exception ex)
            {
                _consecutiveErrors++;
                var cycleDuration = DateTime.UtcNow - cycleStart;

                if (_consecutiveErrors <= MAX_CONSECUTIVE_ERRORS)
                {
                    _logger.LogWarning(ex, "Error #{ErrorCount} in status monitoring cycle (took {CycleDurationMs}ms): {ErrorMessage}",
                        _consecutiveErrors, cycleDuration.TotalMilliseconds, ex.Message);
                }
                else
                {
                    _logger.LogError(ex, "Critical: {ErrorCount} consecutive errors in status monitoring. " +
                        "Latest cycle took {CycleDurationMs}ms. Service stability compromised.",
                        _consecutiveErrors, cycleDuration.TotalMilliseconds);
                }

                // Увеличиваем интервал при множественных ошибках
                if (_consecutiveErrors > 3)
                {
                    var delayMultiplier = Math.Min(_consecutiveErrors - 2, 5);
                    await Task.Delay(_checkInterval.Multiply(delayMultiplier), stoppingToken);
                    continue;
                }
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Status monitoring background service stopped");
    }

    private async Task<int> GetConnectedClientsCount()
    {
        try
        {
            // В реальности это может потребовать дополнительной логики для подсчета клиентов
            return await Task.FromResult(1);
        }
        catch
        {
            return 0;
        }
    }

    private bool HasStatusChanged(StatusResponse current)
    {
        if (_lastStatus == null)
        {
            _logger.LogDebug("First status check, marking as changed");
            return true;
        }

        var changes = new List<string>();

        // Сравниваем устройства
        foreach (var device in current.Devices)
        {
            var lastDevice = _lastStatus.Devices.FirstOrDefault(d => d.Id == device.Id);
            if (lastDevice == null)
            {
                changes.Add($"New device: {device.Name}");
            }
            else if (lastDevice.IsConnected != device.IsConnected)
            {
                changes.Add($"Device {device.Name}: {(device.IsConnected ? "connected" : "disconnected")}");
            }
        }

        // Сравниваем сервисы
        if (current.Services.MessagingService?.IsConnected != _lastStatus.Services.MessagingService?.IsConnected)
        {
            changes.Add($"MessagingService: {(current.Services.MessagingService?.IsConnected == true ? "connected" : "disconnected")}");
        }

        if (current.Services.Elis?.IsConnected != _lastStatus.Services.Elis?.IsConnected)
        {
            changes.Add($"ELIS: {(current.Services.Elis?.IsConnected == true ? "connected" : "disconnected")}");
        }

        if (current.Services.OpcDa?.IsConnected != _lastStatus.Services.OpcDa?.IsConnected)
        {
            changes.Add($"OPC DA: {(current.Services.OpcDa?.IsConnected == true ? "connected" : "disconnected")}");
        }

        if (current.Services.OpcUa?.IsConnected != _lastStatus.Services.OpcUa?.IsConnected)
        {
            changes.Add($"OPC UA: {(current.Services.OpcUa?.IsConnected == true ? "connected" : "disconnected")}");
        }

        if (changes.Any())
        {
            _logger.LogInformation("Status changes detected: {Changes}", string.Join(", ", changes));
            return true;
        }

        return false;
    }
}
```

#### 4.5 StatusProvider (адаптирован под TN_Doc архитектуру)
```csharp
// TN_Doc/Services/IStatusProvider.cs
public interface IStatusProvider
{
    Task<StatusResponse> GetStatusAsync(CancellationToken ct = default);
}

// TN_Doc/Services/StatusProvider.cs - ИНТЕГРИРОВАН С TN_DOC
public class StatusProvider : IStatusProvider
{
    private readonly IAppConfigService _appConfigService; // Используем существующий сервис
    private readonly ILogger<StatusProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public StatusProvider(
        IAppConfigService appConfigService,
        ILogger<StatusProvider> logger,
        IHttpClientFactory httpClientFactory)
    {
        _appConfigService = appConfigService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<StatusResponse> GetStatusAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting status check for all devices and services");

        try
        {
            // Используем существующий метод получения конфигурации
            var appConfig = _appConfigService.GetAppCfg();

            _logger.LogDebug("Retrieved app configuration with {DeviceCount} devices, ELIS: {ElisEnabled}, OPC settings: {OpcCount}",
                appConfig.Devices?.Count ?? 0,
                appConfig.Elis?.Use ?? false,
                appConfig.OpcConnectionSettings?.Count ?? 0);

            var devices = new List<DeviceStatus>();
            var healthyDevices = 0;
            var totalLatency = 0;

            // Проверка устройств с использованием существующей конфигурации
            if (appConfig.Devices?.Any() == true)
            {
                var deviceTasks = appConfig.Devices
                    .Where(d => d.Use) // Используем флаг Use из конфигурации
                    .Select(device => CheckDeviceAsync(device, ct));

                var deviceResults = await Task.WhenAll(deviceTasks);
                devices.AddRange(deviceResults);

                healthyDevices = devices.Count(d => d.IsConnected);
                totalLatency = devices.Where(d => d.LatencyMs.HasValue).Sum(d => d.LatencyMs.Value);

                _logger.LogInformation("Device status check completed: {HealthyCount}/{TotalCount} devices healthy, total latency: {TotalLatency}ms",
                    healthyDevices, devices.Count, totalLatency);
            }

            // Проверка сервисов
            var services = await CheckServicesAsync(appConfig, ct);

            var response = new StatusResponse
            {
                Devices = devices,
                Services = services,
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            _logger.LogInformation("Status check completed in {ElapsedMs}ms - Devices: {HealthyDevices}/{TotalDevices}, MS: {MsStatus}, ELIS: {ElisStatus}",
                stopwatch.ElapsedMilliseconds,
                healthyDevices,
                devices.Count,
                services.MessagingService?.IsConnected ?? false,
                services.Elis?.IsConnected);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve status information after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task<DeviceStatus> CheckDeviceAsync(Device device, CancellationToken ct)
    {
        var deviceStopwatch = Stopwatch.StartNew();
        var status = new DeviceStatus
        {
            Id = device.IdDevice.ToString(),
            Name = device.Name,
            Type = "database",
            IsConnected = false
        };

        _logger.LogDebug("Checking device {DeviceName} (ID: {DeviceId})", device.Name, device.IdDevice);

        try
        {
            // Используем существующую логику получения строки подключения
            var connectionString = device.ConnString;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Connection string is not configured for device {device.Name}");
            }

            var csb = new MySqlConnectionStringBuilder(connectionString)
            {
                ConnectionTimeout = 2,
                DefaultCommandTimeout = 2
            };

            using var connection = new MySqlConnection(csb.ConnectionString);
            await connection.OpenAsync(ct);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 2;
            await command.ExecuteScalarAsync(ct);

            status.IsConnected = true;
            status.LatencyMs = (int)deviceStopwatch.ElapsedMilliseconds;

            _logger.LogDebug("Device {DeviceName} connection successful in {LatencyMs}ms",
                device.Name, status.LatencyMs);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Device {DeviceName} connection check was cancelled", device.Name);
            status.Error = "Operation cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Device {DeviceName} connection check failed in {ElapsedMs}ms: {ErrorMessage}",
                device.Name, deviceStopwatch.ElapsedMilliseconds, ex.Message);
            status.Error = ex.Message;
        }
        finally
        {
            status.LastChecked = DateTime.Now;
        }

        return status;
    }

    // Остальные методы проверки сервисов...
    // (код сокращен для краткости, полная реализация в рекомендациях)
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
    <link rel="stylesheet" href="/statusbar/status-bar.css" asp-append-version="true" />
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
    <script type="module" src="/statusbar/status-bar.js" asp-append-version="true"></script>
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

### Этап 1: MVP строки состояния (3-4 недели) - РЕАЛИСТИЧНАЯ ОЦЕНКА
- [ ] **Неделя 1-2**: Изучение архитектуры и инфраструктура
  - [ ] Анализ существующей архитектуры TN_Doc (IAppConfigService, NLog, DI)
  - [ ] Настройка workspace структуры `TN_Doc/Client/`
  - [ ] Конфигурация Vite + TypeScript + Pinia с интеграцией в существующую сборку
  - [ ] Адаптация под существующие паттерны TN_Doc
- [ ] **Неделя 3**: Базовая реализация
  - [ ] StatusProvider с интеграцией IAppConfigService
  - [ ] Базовый StatusBar компонент с Vue 3
  - [ ] Создание SVG компонентов иконок
  - [ ] API endpoint StatusController
- [ ] **Неделя 4**: Интеграция и полировка
  - [ ] SignalR интеграция и fallback на polling
  - [ ] Полная интеграция с NLog (сервер + клиент)
  - [ ] Интеграция в существующий Layout
  - [ ] Тестирование с реальными данными TN_Doc

### Этап 2: Расширение функциональности (1-2 недели)
- [ ] История изменений статуса
- [ ] Детальная информация по клику
- [ ] Экспорт статистики
- [ ] Настройки пользователя

### Этап 3: Shared компоненты и CI/CD (2 недели)
- [ ] UI Kit компонентов для будущих модулей
- [ ] Общие composables
- [ ] Централизованный API клиент
- [ ] Система нотификаций
- [ ] CI/CD pipeline обновление для Vue сборки
- [ ] Документация архитектуры для команды

### Этап 4: Следующий модуль (3-4 недели)
- [ ] Выбор модуля для миграции
- [ ] Разработка на Vue
- [ ] Интеграция с существующим кодом
- [ ] Тестирование

## 📊 Мониторинг и метрики

### Метрики производительности
- Время загрузки страницы (`performance.mark`)
- Размер bundle статус-бара (< 100KB)
- Время отклика API `/api/status` (< 200ms)
- Задержка SignalR соединения
- Memory usage Vue приложения

### Метрики надёжности
- Uptime каждого сервиса (БД, MS, ELIS, OPC)
- Количество reconnect'ов SignalR за час
- Частота ошибок API (HTTP 4xx, 5xx)
- Success rate проверок статуса
- Частота fallback на polling

### Метрики использования
- Количество активных пользователей
- Частота ручных обновлений
- Количество кликов по индикаторам
- Время сессии со строкой состояния

### Интеграция с существующим NLog TN_Doc
```xml
<!-- Добавить в TN_Doc/nlog.config -->
<targets>
  <!-- Существующие targets TN_Doc -->

  <!-- Специальный файл для Status Bar логов -->
  <target xsi:type="File" name="statusFile"
          fileName="${basedir}/logs/status-${shortdate}.log"
          layout="${longdate} ${uppercase:${level}} ${logger} ${message} ${exception:format=tostring}" />
</targets>

<rules>
  <!-- Существующие правила TN_Doc -->

  <!-- Правила для Status Bar компонентов -->
  <logger name="TN_Doc.Services.StatusProvider" minlevel="Debug" writeTo="statusFile" />
  <logger name="TN_Doc.Services.StatusMonitoringService" minlevel="Info" writeTo="statusFile" />
  <logger name="TN_Doc.Controllers.StatusController" minlevel="Info" writeTo="statusFile" />
  <logger name="TN_Doc.Hubs.StatusHub" minlevel="Info" writeTo="statusFile" />
</rules>
```

```csharp
// TN_Doc/Services/StatusProvider.cs - Примеры интеграции с NLog
_logger.LogInformation("Status check completed: {DeviceCount} devices, {HealthyCount} healthy, {Duration}ms",
    devices.Count,
    devices.Count(d => d.IsConnected),
    stopwatch.ElapsedMilliseconds);

// Структурированное логирование для мониторинга
_logger.LogInformation("StatusBar performance metrics: {ApiResponseTime}ms, {DeviceCount} devices, {HealthyDevices} healthy, {Timestamp}",
    stopwatch.ElapsedMilliseconds,
    devices.Count,
    devices.Count(d => d.IsConnected),
    DateTime.UtcNow);
```

```typescript
// TN_Doc/Client/statusbar/src/composables/useStatusLogging.ts
export function useStatusLogging() {
  async function logToServer(entry: LogEntry) {
    try {
      const logData = {
        level: entry.level.toUpperCase(),
        message: `[StatusBar] ${entry.message}`,
        data: entry.data ? JSON.stringify(entry.data) : null,
        timestamp: entry.timestamp || new Date().toISOString(),
        source: 'StatusBar'
      }

      // Интеграция с существующим ClientLogController TN_Doc
      await fetch('/api/ClientLog', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(logData)
      })
    } catch (error) {
      console.error('Failed to send log to TN_Doc server:', error)
    }
  }

  return { logToServer, /* другие методы */ }
}
```

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
   - ✅ Отображение статуса всех устройств из CfgApp.json
   - ✅ Real-time обновления через SignalR
   - ✅ Fallback на polling при сбое SignalR
   - ✅ История изменений
   - ✅ Интеграция с существующим IAppConfigService
   - ✅ Полное логирование через NLog

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

**Адаптированный план** учитывает специфику существующей архитектуры TN_Doc v1.4.2 и обеспечивает:

### ✅ Ключевые преимущества адаптации
- **Полная интеграция** с существующими сервисами (IAppConfigService, NLog, DI)
- **Использование проверенных паттернов** TN_Doc для конфигурации и логирования
- **Минимальные изменения** в существующем коде
- **Совместимость** с 43 документными модулями
- **Реалистичные временные оценки** с учетом изучения архитектуры

### 📋 Готовность к реализации
План готов к реализации и содержит:
- Детальные примеры кода с интеграцией TN_Doc
- Правильное использование существующих сервисов
- Полную интеграцию с NLog и ClientLogController
- Реалистичные временные оценки (5-7 недель вместо 2)
- Пошаговый план миграции с минимальными рисками

Этот подход позволит создать качественную основу для поэтапной миграции всего TN_Doc на Vue.js архитектуру.