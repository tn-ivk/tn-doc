# Status Bar - Итоговая реализация

## ✅ Что реализовано

Полнофункциональный статус-бар для TN_Doc v1.4.2 согласно плану из `tech_debt/STATUS_BAR_IMPLEMENTATION_PLAN.md`.

### Frontend (Vue.js 3.4 + TypeScript 5.4)

**Структура:**
```
TN_Doc/Client/
├── statusbar/                      # Модуль строки состояния
│   ├── src/
│   │   ├── components/
│   │   │   ├── StatusBar.vue       # Главный компонент
│   │   │   ├── StatusIndicator.vue # Индикатор устройства/сервиса
│   │   │   └── icons/              # SVG иконки (Refresh, Wifi, Warning)
│   │   ├── stores/statusStore.ts   # Pinia store
│   │   ├── composables/
│   │   │   ├── useSignalR.ts       # SignalR интеграция
│   │   │   └── useStatusLogging.ts # Логирование
│   │   ├── types/status.types.ts   # TypeScript типы
│   │   ├── App.vue
│   │   └── main.ts
│   ├── package.json
│   ├── vite.config.ts
│   └── tsconfig.json
├── shared/
│   └── src/api/ApiClient.ts        # HTTP клиент с кэшированием
├── package.json                     # npm workspaces
├── vite.config.base.ts
└── STATUS_BAR_SETUP.md
```

**Возможности:**
- ✅ Real-time обновления через SignalR
- ✅ Fallback на polling при отключении WebSocket
- ✅ Pinia для управления состоянием
- ✅ Интеграция с NLog для клиентского логирования
- ✅ TypeScript для безопасности типов
- ✅ Responsive дизайн

### Backend (ASP.NET Core 8.0)

**Файлы:**
```
TN_Doc/
├── Models/Status/
│   ├── DeviceStatus.cs           # Модель статуса устройства
│   ├── ConnectionStatus.cs       # Модель подключения
│   ├── ServiceStatus.cs          # Модель статуса сервисов
│   └── StatusResponse.cs         # API ответ
├── Controllers/
│   └── StatusController.cs       # API endpoint /api/status
├── Hubs/
│   └── StatusHub.cs              # SignalR Hub /statusHub
├── Services/
│   ├── IStatusProvider.cs        # Интерфейс
│   ├── StatusProvider.cs         # Проверка статуса (интеграция с IAppConfigService)
│   └── StatusMonitoringService.cs # Background service
├── Startup.cs                     # Обновлен (SignalR, DI, HttpClient)
└── Views/Shared/_Layout.cshtml    # Интеграция UI
```

**Возможности:**
- ✅ Интеграция с существующим `IAppConfigService`
- ✅ Проверка подключения к MySQL для каждого устройства
- ✅ Проверка Messaging Service (http://localhost:5010)
- ✅ Проверка конфигурации ELIS
- ✅ Memory cache (5 сек) для снижения нагрузки
- ✅ NLog логирование
- ✅ Background service для мониторинга и broadcast через SignalR
- ✅ Graceful error handling

## 🚀 Инструкция по запуску

### 1. Установка Node.js зависимостей

```bash
cd TN_Doc/Client
npm install
```

### 2. Сборка frontend

```bash
# Production build
npm run build

# Или для разработки с hot reload:
npm run dev
```

После сборки файлы будут в `TN_Doc/wwwroot/statusbar/`:
- `status-bar.js` - JavaScript bundle
- `status-bar.css` - Стили
- `status-bar.[hash].js` - Chunk файлы

### 3. Сборка и запуск backend

```bash
cd ../..
dotnet restore
dotnet build TN_Doc/TN_Doc.csproj
cd TN_Doc
dotnet run
```

### 4. Открыть в браузере

```
http://localhost:38509
```

Status bar должен появиться внизу страницы.

## 🔍 Проверка работы

### Визуальная проверка

1. **Status bar виден** внизу страницы
2. **Индикаторы устройств** показывают статус подключения к БД
3. **Индикаторы сервисов** (MS, ELIS если настроен)
4. **Кнопка refresh** работает
5. **SignalR индикатор** показывает статус WebSocket
6. **Время последнего обновления** отображается

### Проверка API

```bash
# Проверить статус через API
curl http://localhost:38509/api/status

# Принудительное обновление
curl -X POST http://localhost:38509/api/status/refresh
```

### Проверка SignalR

Откройте консоль браузера (F12) и найдите логи:
```
[StatusBar] Attempting to connect to SignalR hub: /statusHub
[StatusBar] SignalR connected successfully
```

### Проверка логов сервера

Логи в:
- **Linux**: `/opt/TN_Doc/logs/`
- **Windows**: `TN_Doc/logs/`

Ищите записи:
```
Status monitoring background service started with 10s interval
StatusHub: Client connected - ConnectionId: ...
Status request completed in Xms
```

## ⚠️ Известные ограничения

### ELIS проверка

Проверка ELIS упрощена - проверяется только наличие конфигурации (`ClientName`, `OstKey`).
Реальная проверка через TN.ElisConnector требует:
1. Развертывание TN.ElisConnector сервиса
2. Известный health check endpoint
3. Обновление `StatusProvider.CheckElisServiceAsync()`

### OPC проверка

OPC DA/UA проверка не реализована, требует:
1. Интеграция с TN_MessagingService API
2. Health check endpoint для OPC серверов
3. Обновление `StatusProvider.CheckServicesAsync()`

## 🔧 Настройка

### Изменение интервала мониторинга

`TN_Doc/Services/StatusMonitoringService.cs`:
```csharp
private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);
```

### Изменение времени кэширования

`TN_Doc/Controllers/StatusController.cs`:
```csharp
private static readonly TimeSpan CacheExpiration = TimeSpan.FromSeconds(5);
```

### Изменение polling интервала (fallback)

`TN_Doc/Client/statusbar/src/components/StatusBar.vue`:
```typescript
const { pause, resume } = useIntervalFn(() => {
  if (connectionState.value !== 'connected') {
    store.fetchStatus();
  }
}, 10000); // 10 секунд
```

## 📝 Дальнейшая разработка

Status bar - это MVP для постепенной миграции TN_Doc на Vue.js.

### Следующие шаги:

1. **Добавить OPC проверку** когда будет доступен API TN_MessagingService
2. **Реализовать ELIS health check** через TN.ElisConnector
3. **Детальная информация** по клику на индикатор (модальное окно)
4. **История событий** изменения статуса
5. **Новые Vue модули** используя структуру workspace

### Добавление нового Vue модуля:

```bash
cd TN_Doc/Client
mkdir new-module
cd new-module
npm init
# Установить зависимости, настроить vite.config.ts аналогично statusbar
```

### Использование shared компонентов:

```typescript
import { apiClient } from '@shared/api/ApiClient';
```

## 🐛 Troubleshooting

### Frontend не собирается

```bash
cd TN_Doc/Client
rm -rf node_modules
rm -rf */node_modules
npm install
npm run build
```

### Backend ошибки компиляции

Проверьте что установлены пакеты:
- `Microsoft.AspNetCore.SignalR`
- `Microsoft.Extensions.Caching.Memory`
- `MySqlConnector`

```bash
dotnet restore
dotnet build
```

### Status bar не отображается

1. Проверьте `TN_Doc/wwwroot/statusbar/` содержит файлы
2. Проверьте `_Layout.cshtml` содержит:
   ```html
   <div id="status-bar"></div>
   <script type="module" src="~/statusbar/status-bar.js" asp-append-version="true"></script>
   ```
3. Проверьте консоль браузера на ошибки

### SignalR не подключается

1. Проверьте `Startup.cs` содержит:
   ```csharp
   services.AddSignalR();
   endpoints.MapHub<TN_Doc.Hubs.StatusHub>("/statusHub");
   ```
2. Проверьте CORS настройки
3. Проверьте логи сервера

### Устройства показывают offline

1. Проверьте `CfgApp.json` - устройства должны иметь `Use: true`
2. Проверьте строки подключения к MySQL
3. Проверьте логи `StatusProvider`

## 📚 Документация

- **План реализации**: `tech_debt/STATUS_BAR_IMPLEMENTATION_PLAN.md`
- **Инструкция разработчика**: `TN_Doc/Client/STATUS_BAR_SETUP.md`
- **CLAUDE.md**: Общая документация по проекту

## ✅ Checklist готовности

- [x] Frontend собран успешно
- [x] Backend компилируется
- [x] Status bar отображается в UI
- [x] API `/api/status` работает
- [x] SignalR подключается
- [ ] Проверено на production окружении
- [ ] OPC проверка реализована
- [ ] ELIS health check реализован
- [ ] Документация актуализирована в `changes.md`

## 🎯 Итог

Status bar успешно реализован как первый Vue.js модуль в TN_Doc. Архитектура позволяет поэтапно мигрировать остальные части интерфейса, используя workspace структуру и shared компоненты.