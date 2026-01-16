# StatusBar Architecture

## Обзор

StatusBar — это real-time компонент мониторинга здоровья системы, построенный на **Vue 3 + TypeScript + PrimeVue** с использованием **SignalR** для двусторонней связи.

**Статус**: Production (с версии 1.4.2+)
**Последнее обновление**: v1.4.4+
**Ключевые особенности**:
- Автоматическое переподключение SignalR с умной стратегией retry
- Централизованное логирование через `@tn-doc/shared/logger`
- Fallback на HTTP polling при проблемах с WebSocket
- 4 визуальных состояния индикаторов (online, offline, ndv, warning)

## Архитектура компонента

```mermaid
graph TB
    subgraph "Frontend - Vue 3"
        App[App.vue]
        StatusBar[StatusBar.vue]
        Indicator[StatusIndicator.vue]
        Store[Pinia Store]
        SignalRComposable[useSignalR Composable]
    end

    subgraph "Backend - ASP.NET Core"
        StatusHub[StatusHub]
        MonitoringService[StatusMonitoringService]
        StatusProvider[StatusProvider]
    end

    subgraph "External Systems"
        DB[(Databases)]
        OPC[OPC Servers]
        ELIS[ELIS]
        MS[MessagingService]
    end

    App --> StatusBar
    StatusBar --> Indicator
    StatusBar --> Store
    StatusBar --> SignalRComposable

    SignalRComposable <-->|WebSocket| StatusHub
    StatusHub <--> MonitoringService
    MonitoringService --> StatusProvider

    StatusProvider --> DB
    StatusProvider --> OPC
    StatusProvider --> ELIS
    StatusProvider --> MS
```

## Frontend Architecture

### Component Hierarchy

```mermaid
graph TD
    Main[main.ts] --> App[App.vue]
    App --> StatusBar[StatusBar.vue]
    StatusBar --> Store[Pinia StatusStore]
    StatusBar --> SignalR[useSignalR Composable]
    StatusBar --> Logger[Shared Logger]
    StatusBar --> DeviceIndicators[Device Indicators]
    StatusBar --> ServiceIndicators[Service Indicators]

    DeviceIndicators --> StatusIndicator1[StatusIndicator: ИВК-1]
    DeviceIndicators --> StatusIndicator2[StatusIndicator: ИВК-2]
    ServiceIndicators --> StatusIndicator3[StatusIndicator: MS]
    ServiceIndicators --> StatusIndicator4[StatusIndicator: ELIS]

    Logger --> ServerAPI[POST /api/ClientLog/logging]
```

### Vue Component Structure

**StatusBar.vue** (упрощенная версия):
```vue
<template>
  <div class="status-bar">
    <div class="status-bar__container">
      <!-- Device Indicators -->
      <div v-if="store.devices.length > 0" class="status-bar__section">
        <StatusIndicator
          v-for="device in store.devices"
          :key="device.id"
          :label="device.name"
          :status="getDeviceStatus(device)"
          :tooltip="`${device.name}: ${device.isConnected ? 'Подключено' : 'Отключено'}`"
          @click="handleDeviceClick(device)"
        />
      </div>

      <!-- Service Indicators -->
      <div class="status-bar__section status-bar__section--services">
        <StatusIndicator
          label="MS"
          :status="getServiceStatus(store.services.messagingService)"
          tooltip="Messaging Service"
        />
        <StatusIndicator
          v-if="store.services.elis"
          label="ELIS"
          :status="getServiceStatus(store.services.elis)"
          tooltip="Лабораторная система"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { logger } from '@tn-doc/shared'; // Централизованный логгер
import { useStatusStore } from '../stores/statusStore';
import { useSignalR } from '../composables/useSignalR';
import { useIntervalFn } from '@vueuse/core';

const store = useStatusStore();
const { connectionState, on } = useSignalR('/statusHub');

// Fallback polling каждые 10 секунд если SignalR не подключен
const { pause, resume } = useIntervalFn(() => {
  if (connectionState.value !== 'connected') {
    store.fetchStatus();
  }
}, 10000);

// SignalR real-time обновления
on('statusUpdated', (data: StatusResponse) => {
  store.updateFromSignalR(data);
  logger.trace('StatusBar: получено обновление через SignalR', {
    deviceCount: data.devices?.length || 0
  });
});

// Начальная загрузка с задержкой 2 секунды
onMounted(() => {
  logger.debug('StatusBar: компонент монтирован');
  setTimeout(() => store.fetchStatus(), 2000);
});
</script>
```

**Ключевые изменения в v1.4.4+**:
- Использование `logger` из `@tn-doc/shared` вместо console.log
- Fallback HTTP polling с интервалом 10 секунд
- Задержка начальной загрузки 2 секунды для готовности сервера
- Условный рендеринг ELIS индикатора (опциональный сервис)

### State Management (Pinia)

```mermaid
classDiagram
    class StatusStore {
        +devices: DeviceStatus[]
        +services: ServiceStatus
        +lastUpdate: Date
        +isLoading: boolean
        +error: string
        +fetchStatus() Promise
        +updateFromSignalR(data) void
        +overallHealth: HealthStatus
    }

    class DeviceStatus {
        +id: string
        +name: string
        +type: string
        +isConnected: boolean
        +latencyMs: number
        +error: string
    }

    class ServiceStatus {
        +messagingService: ConnectionStatus
        +elis: ConnectionStatus
    }

    StatusStore --> DeviceStatus
    StatusStore --> ServiceStatus
```

## Indicator Status States

```mermaid
stateDiagram-v2
    [*] --> online: Device/Service Connected
    [*] --> offline: Device/Service Disconnected
    [*] --> ndv: Server Unreachable
    [*] --> warning: Pre-alert Condition

    online --> offline: Connection Lost
    online --> warning: Degraded Performance
    offline --> online: Connection Restored
    ndv --> online: Server Back Online
    warning --> online: Issue Resolved
    offline --> ndv: Server Down
```

### Status States

| State | Color | Icon | Description | CSS Variables |
|-------|-------|------|-------------|---------------|
| **online** | Зеленый | `pi-link` | Устройство/сервис подключено | `--p-green-100` (bg), `--p-green-700` (text) |
| **offline** | Красный (мигание) | `pi-times-circle` | Устройство/сервис не на связи | `--p-red-100` (bg), `--p-red-700` (text) |
| **ndv** | Серый | `pi-question-circle` | Недостоверно (нет связи с сервером) | `--p-surface-100` (bg), `--p-surface-700` (text) |
| **warning** | Желтый | `pi-exclamation-triangle` | Предаварийная ситуация | `--p-yellow-100` (bg), `--p-yellow-700` (text) |

**Логика определения состояния** (TN_Doc/Client/statusbar/src/components/StatusBar.vue:81-102):
```typescript
function getDeviceStatus(device: DeviceStatus): IndicatorStatus {
  if (device.isConnected) {
    return 'online';
  }
  // Если есть ошибка "Нет связи с сервером", то это ndv
  if (device.error?.includes('Нет связи с сервером')) {
    return 'ndv';
  }
  // Иначе устройство просто не на связи
  return 'offline';
}
```

**Цвета индикаторов**:
- Все цвета определены через CSS переменные в `/TN_Doc/wwwroot/css/material3.css:84-94`
- Использует PrimeVue совместимую палитру для консистентного UI
- Анимация мигания для состояния `offline`: `@keyframes blink` (opacity 1 → 0.3 → 1, период 1.5s)

### Visual States Animation

```mermaid
sequenceDiagram
    participant Component
    participant CSS
    participant Animation

    Note over Component: Status = offline
    Component->>CSS: Apply .status-indicator--offline
    CSS->>Animation: Apply .status-indicator__icon--blink

    loop Every 1.5s
        Animation->>Animation: opacity 1 → 0.3 → 1
    end

    Note over Component: Status changed to online
    Component->>CSS: Remove blink animation
    Component->>CSS: Apply .status-indicator--online
```

## Real-time Communication

### SignalR Flow

```mermaid
sequenceDiagram
    participant Vue as Vue StatusBar
    participant SignalR as SignalR Client
    participant Hub as StatusHub
    participant Service as StatusMonitoringService
    participant Provider as StatusProvider

    Vue->>SignalR: Initialize connection (auto onMounted)
    SignalR->>Hub: Connect to /statusHub
    Hub-->>SignalR: Connection established

    Note over Vue: Задержка 2 секунды после монтирования
    Vue->>Provider: GET /api/status (initial load)
    Provider-->>Vue: StatusResponse (HTTP)
    Vue->>Vue: Update store & UI

    loop Every 60 seconds (background service)
        Service->>Provider: GetStatusAsync()
        Provider->>Provider: Query databases
        Provider->>Provider: Check OPC servers
        Provider->>Provider: Check ELIS
        Provider->>Provider: Check MessagingService
        Provider-->>Service: StatusResponse

        alt Status Changed
            Service->>Hub: BroadcastStatusUpdate(data)
            Hub->>SignalR: Invoke "statusUpdated"
            SignalR->>Vue: on("statusUpdated", callback)
            Vue->>Vue: Update store via updateFromSignalR()
            Vue->>Logger: logger.trace("StatusBar: получено обновление")
        else No Changes
            Note over Service: Skip broadcast
        end
    end

    alt SignalR Disconnected
        loop Every 10 seconds (fallback polling)
            Vue->>Provider: GET /api/status
            Provider-->>Vue: StatusResponse (HTTP)
            Vue->>Vue: Update store & UI
        end
    end
```

**Ключевые изменения в v1.4.4+**:
- **Backend интервал**: 60 секунд (изменено с 30 секунд) - см. TN_Doc/Services/StatusMonitoringService.cs:24
- **Fallback polling**: 10 секунд при потере SignalR соединения
- **Начальная задержка**: 2 секунды перед первым запросом статуса
- **Оптимизация broadcast**: отправка только при изменении статуса (метод `HasStatusChanged`)
- **Пропуск проверок**: если нет активных SignalR клиентов (оптимизация нагрузки)

### useSignalR Composable

```typescript
export function useSignalR(hubUrl: string) {
  const connection = ref<signalR.HubConnection | null>(null);
  const connectionState = ref<ConnectionState>('disconnected');
  const error = ref<string | null>(null);
  const pendingSubscriptions = new Map<string, ((...args: any[]) => void)[]>();

  async function connect() {
    try {
      connectionState.value = 'connecting';
      logger.debug(`SignalR: попытка подключения к хабу`, { hubUrl });

      connection.value = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          withCredentials: true,
          skipNegotiation: false
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            // 5 секунд в первую минуту, 30 секунд после
            const delay = retryContext.elapsedMilliseconds < 60000 ? 5000 : 30000;
            logger.warn(`SignalR: попытка переподключения #${retryContext.previousRetryCount + 1}`);
            return delay;
          }
        })
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      // Обработчики событий переподключения
      connection.value.onreconnecting((err) => {
        connectionState.value = 'connecting';
        logger.warn('SignalR: соединение потеряно, попытка переподключения');
      });

      connection.value.onreconnected((connectionId) => {
        connectionState.value = 'connected';
        logger.debug('SignalR: успешно переподключен', { connectionId });
      });

      connection.value.onclose((err) => {
        connectionState.value = 'disconnected';
        if (err) {
          logger.error('SignalR: соединение закрыто с ошибкой', { error: err.message });
        }
      });

      await connection.value.start();
      connectionState.value = 'connected';
      logger.debug('SignalR: успешно подключен', { hubUrl, connectionId: connection.value.connectionId });

      // Применяем отложенные подписки
      if (pendingSubscriptions.size > 0) {
        pendingSubscriptions.forEach((callbacks, eventName) => {
          callbacks.forEach(callback => connection.value!.on(eventName, callback));
        });
        pendingSubscriptions.clear();
      }
    } catch (err) {
      connectionState.value = 'disconnected';
      logger.error('SignalR: ошибка подключения', { error: err.message });
    }
  }

  function on(eventName: string, callback: (...args: any[]) => void) {
    // Отложенная подписка если соединение еще не готово
    if (!connection.value || connectionState.value !== 'connected') {
      if (!pendingSubscriptions.has(eventName)) {
        pendingSubscriptions.set(eventName, []);
      }
      pendingSubscriptions.get(eventName)!.push(callback);
      logger.debug(`SignalR: подписка на событие ${eventName} отложена`);
      return;
    }

    connection.value.on(eventName, callback);
    logger.debug(`SignalR: подписка на событие ${eventName} активна`);
  }

  onMounted(() => connect());
  onUnmounted(() => disconnect());

  return { connection, connectionState, error, connect, disconnect, on, off };
}
```

**Ключевые особенности** (TN_Doc/Client/statusbar/src/composables/useSignalR.ts):
- **Автоматическое переподключение**: умная стратегия retry (5s первую минуту, затем 30s)
- **Отложенные подписки**: события можно подписывать до установки соединения
- **Централизованное логирование**: все события логируются через shared logger
- **Graceful degradation**: при проблемах автоматически переключается на HTTP polling
- **Автоподключение**: connect() вызывается в onMounted() автоматически

## Backend Architecture

### StatusMonitoringService (Background Service)

```mermaid
flowchart TD
    Start([Application Start]) --> Init[Initialize Service]
    Init --> Delay[Задержка 5 секунд - инициализация]
    Delay --> CheckClients{Есть активные<br/>SignalR клиенты?}

    CheckClients -->|Нет| Wait[Wait 60s]
    CheckClients -->|Да| Check[GetStatusAsync]

    Check --> QueryDB[Query Databases]
    Check --> QueryOPC[Check OPC Servers]
    Check --> QueryELIS[Check ELIS]
    Check --> QueryMS[Check MessagingService]

    QueryDB --> Aggregate[Aggregate Results]
    QueryOPC --> Aggregate
    QueryELIS --> Aggregate
    QueryMS --> Aggregate

    Aggregate --> Compare{HasStatusChanged?}
    Compare -->|Да| Broadcast[Broadcast via SignalR]
    Compare -->|Нет| Skip[Skip broadcast]

    Broadcast --> ResetErrors[Reset error counter]
    Skip --> ResetErrors

    ResetErrors --> Wait
    Wait --> CheckClients

    Stop([Application Stop]) -.-> Cancel[Cancel Token]
    Cancel -.-> End([Service Stopped])

    Check -.->|Exception| ErrorHandler[Error Handler]
    ErrorHandler --> IncrementErrors[Increment consecutive errors]
    IncrementErrors --> CheckErrorCount{Errors > 3?}
    CheckErrorCount -->|Да| ExtendedWait[Wait 60s × multiplier]
    CheckErrorCount -->|Нет| Wait
    ExtendedWait --> CheckClients
```

**Оптимизации в v1.4.4+** (TN_Doc/Services/StatusMonitoringService.cs):
- **Интервал проверки**: 60 секунд (строка 24: `TimeSpan.FromSeconds(60)`)
- **Пропуск при отсутствии клиентов**: проверка `AppClientTracker.HasActiveClients` (строки 56-61)
- **Broadcast только при изменениях**: метод `HasStatusChanged()` (строки 135-177)
- **Умная обработка ошибок**: экспоненциальный backoff при повторяющихся ошибках (строки 116-121)
- **Начальная задержка**: 5 секунд после старта приложения (строка 49)

### StatusProvider Implementation

```csharp
public class StatusProvider : IStatusProvider
{
    public async Task<StatusResponse> GetAllStatuses()
    {
        var devices = await CheckDatabaseConnections();
        var services = new ServiceStatus
        {
            MessagingService = await CheckMessagingService(),
            Elis = await CheckElisService()
        };

        return new StatusResponse
        {
            Devices = devices,
            Services = services,
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task<List<DeviceStatus>> CheckDatabaseConnections()
    {
        var devices = new List<DeviceStatus>();
        foreach (var deviceConfig in _appConfig.Devices)
        {
            var status = new DeviceStatus
            {
                Id = deviceConfig.IdDevice,
                Name = deviceConfig.Name,
                Type = "database"
            };

            var sw = Stopwatch.StartNew();
            try
            {
                await _dbContext.Database.CanConnectAsync();
                status.IsConnected = true;
                status.LatencyMs = sw.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                status.IsConnected = false;
                status.Error = ex.Message;
            }

            devices.Add(status);
        }
        return devices;
    }
}
```

## HTTP Clients Configuration

```mermaid
graph LR
    subgraph "HTTP Clients"
        MSClient[MessagingService Client<br/>Timeout: 2s]
        ELISClient[ELIS Client<br/>Timeout: 5s]
    end

    subgraph "Services"
        MS[TN_MessagingService]
        ELIS[ELIS LabHub]
    end

    MSClient -->|GET /api/health| MS
    ELISClient -->|GET /health| ELIS

    MS -.->|200 OK| MSClient
    MS -.->|Timeout| MSClient

    ELIS -.->|200 OK| ELISClient
    ELIS -.->|Timeout| ELISClient
```

## Build & Integration

### Build Process

```mermaid
flowchart LR
    Source[Vue Source Files] --> TypeScript[TypeScript Compiler]
    TypeScript --> Vite[Vite Bundler]

    Vite --> CSS[status-bar.css]
    Vite --> JS[status-bar.js]
    Vite --> Fonts[Font Files]

    CSS --> Output[wwwroot/statusbar/]
    JS --> Output
    Fonts --> Output

    Output --> ASPNet[ASP.NET Core Static Files]
```

### Integration with ASP.NET Core

```html
<!-- _Layout.cshtml -->
<link rel="stylesheet" href="~/statusbar/status-bar.css" />
<div id="status-bar-app"></div>
<script src="~/statusbar/status-bar.js"></script>
```

## Performance Considerations

### Optimization Strategy

```mermaid
graph TB
    subgraph "Frontend Optimizations"
        Debounce[Debounce Manual Refresh]
        Lazy[Lazy Load Icons]
        Memo[Memoize Status Computations]
    end

    subgraph "Backend Optimizations"
        Cache[Cache DB Schema]
        Parallel[Parallel Health Checks]
        Timeout[Short Timeouts 2-5s]
    end

    subgraph "Network Optimizations"
        SignalR[SignalR Compression]
        MinJS[Minified JS/CSS]
        HTTP2[HTTP/2]
    end
```

### Metrics

| Метрика | Значение | Примечание |
|---------|----------|------------|
| **Bundle size** | ~300KB | Initial load (minified + gzipped) |
| **Backend check interval** | 60s | StatusMonitoringService (v1.4.4+) |
| **Fallback polling** | 10s | HTTP polling при отсутствии SignalR |
| **Initial delay** | 2s | Задержка перед первым запросом |
| **Backend startup delay** | 5s | Задержка старта фонового сервиса |
| **UI update time** | <100ms | Время обновления индикаторов |
| **Timeout (MS)** | 2s | MessagingService health check |
| **Timeout (ELIS)** | 5s | ELIS Lab System health check |
| **SignalR retry (initial)** | 5s | Первая минута после разрыва |
| **SignalR retry (extended)** | 30s | После первой минуты |
| **Error backoff** | 60s × (errors - 2) | Max 5x multiplier |

**Оптимизации производительности** (v1.4.4+):
- Пропуск проверок при отсутствии активных SignalR клиентов
- Broadcast только при изменении статуса (экономия трафика)
- История обновлений: хранение последних 10 статусов в Pinia store
- Умная обработка ошибок с экспоненциальным backoff

## Централизованное логирование (v1.4.4+)

StatusBar использует shared logger из пакета `@tn-doc/shared` для унифицированного логирования клиентских событий.

### Logger API

```typescript
import { logger } from '@tn-doc/shared';

// Доступные уровни логирования
logger.trace('Детальная трассировка', { data: {} });  // Самый подробный
logger.debug('Отладочная информация', { step: 1 });   // Разработка
logger.info('Информационное сообщение');               // Основные события
logger.warn('Предупреждение', { reason: 'timeout' }); // Потенциальные проблемы
logger.error('Ошибка выполнения', { error: err });    // Ошибки

// Логирование исключений с stack trace
logger.exception(new Error('Критическая ошибка'), 'Контекст ошибки', { userId: 123 });
```

### Конфигурация

**TN_Doc/Client/shared/src/logger.ts**:
- **Endpoint**: `POST /api/ClientLog/logging`
- **Console logging**: автоматически включен в dev режиме (`import.meta.env.DEV`)
- **Global context**: можно добавить глобальный контекст (userId, app version)
- **Server fallback**: при ошибке отправки логирует в консоль

### Примеры использования в StatusBar

```typescript
// StatusBar.vue
import { logger } from '@tn-doc/shared';

onMounted(() => {
  logger.debug('StatusBar: компонент монтирован, загрузка статусов через 2 секунды');
  setTimeout(() => store.fetchStatus(), 2000);
});

on('statusUpdated', (data: StatusResponse) => {
  store.updateFromSignalR(data);
  logger.trace('StatusBar: получено обновление через SignalR', {
    deviceCount: data.devices?.length || 0
  });
});

// StatusStore.ts
logger.info('StatusStore: статусы успешно загружены', {
  deviceCount: response.devices.length,
  connectedDevices: response.devices.filter(d => d.isConnected).length
});

logger.error('StatusStore: ошибка загрузки статусов', {
  error: err instanceof Error ? err.message : String(err)
});

// useSignalR.ts
logger.debug('SignalR: успешно подключен', {
  hubUrl,
  connectionId: connection.value.connectionId
});

logger.warn(`SignalR: попытка переподключения #${retryContext.previousRetryCount + 1}`);
```

### Архитектура логирования

```mermaid
flowchart LR
    subgraph "Frontend Vue Components"
        StatusBar[StatusBar.vue]
        Store[statusStore.ts]
        SignalR[useSignalR.ts]
    end

    subgraph "Shared Logger"
        Logger[logger.ts]
        Console[Console Output<br/>dev mode]
        API[POST /api/ClientLog/logging]
    end

    subgraph "Backend"
        Controller[ClientLogController]
        NLog[NLog Logger]
        LogFiles[logs/client-*.log]
    end

    StatusBar --> Logger
    Store --> Logger
    SignalR --> Logger

    Logger --> Console
    Logger --> API
    API --> Controller
    Controller --> NLog
    NLog --> LogFiles
```

**Преимущества централизованного логирования**:
- Единый формат логов для всех Vue компонентов
- Автоматическая отправка на сервер с контекстом
- Возможность отслеживания клиентских ошибок в production
- Унифицированное логирование в dev и prod окружениях
- Типобезопасность через TypeScript

## Error Handling

```mermaid
flowchart TD
    Error[Error Occurred] --> Type{Error Type}

    Type -->|Network| Retry[SignalR Auto-reconnect]
    Type -->|Server| ServerError[Show error state]
    Type -->|Timeout| TimeoutError[Mark as ndv]

    Retry -->|Success| Update[Update UI]
    Retry -->|Fail| Fallback[Fallback to HTTP polling]

    ServerError --> Log[logger.error]
    TimeoutError --> Log

    Fallback --> PollingLoop[10s polling interval]
    Log --> ServerLogging[POST /api/ClientLog/logging]
```

**Обработка ошибок в v1.4.4+**:
- **SignalR разрыв**: автоматический retry с умной стратегией (5s → 30s)
- **HTTP ошибки**: fallback на polling каждые 10 секунд
- **Нет связи с сервером**: все индикаторы переходят в состояние `ndv`
- **Логирование**: все ошибки отправляются на сервер через shared logger
- **Backend ошибки**: экспоненциальный backoff (до 5x увеличение интервала)

## Development Workflow

```mermaid
flowchart LR
    Edit[Edit Vue Files] --> Watch[npm run dev]
    Watch --> HMR[Hot Module Reload]
    HMR --> Browser[Browser Auto-refresh]

    Build[npm run build] --> Compile[TypeScript + Vite]
    Compile --> Output[wwwroot/statusbar/]
    Output --> DotNet[dotnet run]
    DotNet --> Test[Test in Browser]
```

## Responsive Design

```mermaid
graph TD
    subgraph "Desktop >1024px"
        D1[Horizontal Layout]
        D2[All Indicators Visible]
        D3[Padding: 0.5rem]
    end

    subgraph "Tablet 768px - 1024px"
        T1[Vertical Layout]
        T2[Sections Stacked]
        T3[Padding: 0.45rem]
    end

    subgraph "Mobile <768px"
        M1[Compact Layout]
        M2[Reduced Gaps]
        M3[Padding: 0.25rem]
    end
```

## История изменений

### v1.4.4+ (Текущая версия)

**Основные улучшения**:
- ✅ Интервал backend проверки увеличен с 30s до 60s (оптимизация нагрузки)
- ✅ Централизованное логирование через `@tn-doc/shared/logger`
- ✅ Умная стратегия переподключения SignalR (5s → 30s)
- ✅ Fallback HTTP polling с интервалом 10s
- ✅ Оптимизация: пропуск проверок при отсутствии клиентов
- ✅ Broadcast только при изменении статуса (экономия трафика)
- ✅ История обновлений (последние 10) в Pinia store
- ✅ Экспоненциальный backoff при ошибках backend
- ✅ Все цвета через CSS переменные (material3.css)

**Удаленные элементы**:
- ❌ Кнопка ручного обновления (автоматизация через SignalR + polling)
- ❌ Индикатор состояния SignalR (упрощение UI)

### v1.4.2 (Production Release)

**Первый production release**:
- ✅ Базовая функциональность мониторинга
- ✅ 4 состояния индикаторов (online, offline, ndv, warning)
- ✅ SignalR real-time обновления
- ✅ Интеграция с PrimeVue
- ✅ Responsive design

## Известные ограничения

1. **Визуальные индикаторы**: состояние `warning` в настоящее время не используется (зарезервировано для будущих функций)
2. **Детализация устройств**: клик по индикатору пока не показывает модальное окно с деталями (будущая функция)
3. **ELIS зависимость**: индикатор ELIS отображается только если `Elis.Use = true` в конфигурации
4. **Latency метрики**: latency отображается только для устройств, но не для сервисов

## Планы развития

- [ ] Модальное окно с детальной информацией об устройстве при клике на индикатор
- [ ] Графики исторических данных latency и uptime
- [ ] Уведомления при изменении критических статусов
- [ ] Настраиваемые пороги для состояния `warning`
- [ ] Поддержка дополнительных сервисов (OPC, KMH)
- [ ] Экспорт истории статусов в CSV/JSON

## См. также

- [Vue 3 Documentation](https://vuejs.org/)
- [PrimeVue Components](https://primevue.org/)
- [SignalR Client](https://docs.microsoft.com/aspnet/core/signalr/)
- [Pinia State Management](https://pinia.vuejs.org/)
- [Configurator Architecture](./configurator.md) - конфигурация приложения
- [Document Editor Architecture](./document-editor.md) - редактирование документов
