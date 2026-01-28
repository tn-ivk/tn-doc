# StatusBar Architecture

## Обзор

StatusBar — это real-time компонент мониторинга здоровья системы, построенный на **Vue 3 + TypeScript + PrimeVue** с использованием **SignalR** для двусторонней связи.

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
    StatusBar --> DeviceIndicators[Device Indicators]
    StatusBar --> ServiceIndicators[Service Indicators]

    DeviceIndicators --> StatusIndicator1[StatusIndicator: ИВК-1]
    DeviceIndicators --> StatusIndicator2[StatusIndicator: ИВК-2]
    ServiceIndicators --> StatusIndicator3[StatusIndicator: MS]
    ServiceIndicators --> StatusIndicator4[StatusIndicator: ELIS]
```

### Vue Component Structure

**StatusBar.vue:**
```vue
<template>
  <div class="status-bar">
    <div class="status-bar__container">
      <!-- Device Indicators -->
      <div class="status-bar__section">
        <StatusIndicator
          v-for="device in devices"
          :key="device.id"
          :label="device.name"
          :status="getDeviceStatus(device)"
        />
      </div>

      <!-- Service Indicators -->
      <div class="status-bar__section">
        <StatusIndicator label="MS" :status="msStatus" />
        <StatusIndicator label="ELIS" :status="elisStatus" />
      </div>
    </div>
  </div>
</template>
```

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

| State | Color | Icon | Description |
|-------|-------|------|-------------|
| **online** | Зеленый | `pi-link` | Устройство/сервис подключено |
| **offline** | Красный (мигание) | `pi-times-circle` | Устройство/сервис не на связи |
| **ndv** | Серый | `pi-question-circle` | Недостоверно (нет связи с сервером) |
| **warning** | Желтый | `pi-exclamation-triangle` | Предаварийная ситуация |

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

    Vue->>SignalR: Initialize connection
    SignalR->>Hub: Connect to /statusHub
    Hub-->>SignalR: Connection established

    loop Every 30 seconds
        Service->>Provider: CheckAllStatuses()
        Provider->>Provider: Query databases
        Provider->>Provider: Check OPC servers
        Provider->>Provider: Check ELIS
        Provider->>Provider: Check MessagingService
        Provider-->>Service: StatusResponse

        Service->>Hub: BroadcastStatusUpdate(data)
        Hub->>SignalR: Invoke "statusUpdated"
        SignalR->>Vue: on("statusUpdated", callback)
        Vue->>Vue: Update store & UI
    end

    Note over Vue: User clicks refresh
    Vue->>Provider: GET /api/status
    Provider-->>Vue: StatusResponse (HTTP)
    Vue->>Vue: Update store & UI
```

### useSignalR Composable

```typescript
export function useSignalR(hubUrl: string) {
  const connectionState = ref<ConnectionState>('disconnected');
  const connection = new HubConnectionBuilder()
    .withUrl(hubUrl)
    .withAutomaticReconnect()
    .build();

  const on = (eventName: string, callback: Function) => {
    connection.on(eventName, callback);
  };

  const start = async () => {
    connectionState.value = 'connecting';
    await connection.start();
    connectionState.value = 'connected';
  };

  return { connectionState, on, start };
}
```

## Backend Architecture

### StatusMonitoringService (Background Service)

```mermaid
flowchart TD
    Start([Application Start]) --> Init[Initialize Service]
    Init --> Wait[Wait 30s]
    Wait --> Check[CheckAllStatuses]

    Check --> QueryDB[Query Databases]
    Check --> QueryOPC[Check OPC Servers]
    Check --> QueryELIS[Check ELIS]
    Check --> QueryMS[Check MessagingService]

    QueryDB --> Aggregate[Aggregate Results]
    QueryOPC --> Aggregate
    QueryELIS --> Aggregate
    QueryMS --> Aggregate

    Aggregate --> Broadcast[Broadcast via SignalR]
    Broadcast --> Wait

    Stop([Application Stop]) -.-> Cancel[Cancel Token]
    Cancel -.-> End([Service Stopped])
```

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

| Метрика | Значение | Цель |
|---------|----------|------|
| Initial Load | ~300KB | Bundle size |
| Update Interval | 60s | Background check (StatusMonitoringService) |
| Response Time | <100ms | UI update |
| Timeout (MS) | 2s | MessagingService |
| Timeout (ELIS) | 5s | ELIS Lab System |

> **Примечание (v1.4.2+)**: Удалена кнопка ручного обновления статусов, удалён индикатор SignalR соединения, убрано отображение времени последнего обновления.

## Error Handling

```mermaid
flowchart TD
    Error[Error Occurred] --> Type{Error Type}

    Type -->|Network| Retry[Auto-retry 3x]
    Type -->|Server| ServerError[Show error state]
    Type -->|Timeout| TimeoutError[Mark as ndv]

    Retry -->|Success| Update[Update UI]
    Retry -->|Fail| NDV[Mark as ndv]

    ServerError --> Log[Log to console]
    TimeoutError --> Log

    NDV --> Fallback[Fallback to polling]
    Log --> Notify[Optional: Notify user]
```

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

## См. также

- [Vue 3 Documentation](https://vuejs.org/)
- [PrimeVue Components](https://primevue.org/)
- [SignalR Client](https://docs.microsoft.com/aspnet/core/signalr/)
- [Pinia State Management](https://pinia.vuejs.org/)
