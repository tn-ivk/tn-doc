# План реализации строки состояния

## 📋 Обзор задачи

Добавление строки состояния в главное окно приложения TN_Doc для отображения статуса подключений к устройствам и внешним сервисам в реальном времени.

## 🎯 Требования

### Базовый функционал
- **Индикаторы устройств**: Отображение статуса подключения к БД для всех сконфигурированных устройств (ИВК-1, ИВК-2, ИВК-РСУ)
- **Индикатор MS**: Статус подключения к hubConnection `http://localhost:5010/SignalRApp`
- **Динамическое обновление**: Цветовая индикация изменяется в реальном времени
- **Расширяемость**: Архитектура должна поддерживать добавление новых индикаторов

### Визуальные требования
- **Текстовые метки** с наименованием устройства
- **Цветовая индикация**:
  - 🟢 Зелёный - подключение активно
  - 🔴 Красный - подключение отсутствует

## 🏗️ Архитектура решения

Слои и каналы данных:
- Бэкенд:
  - REST `GET /api/status` — текущие статусы (pull, 5 сек).
  - SignalR Hub `/statusHub` — push‑обновления при изменениях.
  - Background service — периодическая проверка БД/внешних сервисов.
- Фронтенд (вариант B):
  - Мини‑проект на Vue 3 + Vite, сборка артефактов в `TN_Doc/wwwroot/statusbar/`.
  - Компонент `StatusBar.vue` — монтируется на контейнер в главном layout.

## 🎨 Расширяемость

### Планируемые индикаторы
- **OPC сервер**: Статус подключения к OPC DA/UA
- **ELIS сервис**: Доступность лабораторной системы

## 📝 Заметки по реализации

### Особенности MySQL подключений
- Использовать короткие таймауты для проверки статуса
- Не держать подключения открытыми после проверки
- Обрабатывать специфичные MySQL ошибки (Connection timeout, Access denied, etc.)

### SignalR Hub Connection
- URL: `http://localhost:5010/SignalRApp`
- Обработка automatic reconnection
- Graceful degradation при недоступности сервиса
- Индикация качества соединения (задержка)

### UX/UI соображения
- Строка состояния не должна перекрывать основной контент
- Анимации переходов состояния должны быть мягкими
- Tooltip с дополнительной информацией при hover
- Возможность скрытия строки состояния пользователем


### Сбор статистики
- Время отклика каждого устройства
- Частота разрывов подключения
- Среднее время восстановления соединения
- Загрузка CPU/памяти от мониторинга статуса

---

## ✅ Детальный план внедрения (Вариант B — Vue 3 + Vite)

### 1) Структура фронтенд‑модуля
- Создать директорию: `TN_Doc/Client/statusbar/`
- Файлы:
  - `package.json` — зависимости и скрипты Vite
  - `vite.config.ts` — сборка в `TN_Doc/wwwroot/statusbar` со стабильными именами
  - `tsconfig.json` (опц.)
  - `src/main.ts` — точка входа
  - `src/components/StatusBar.vue` — компонент строки состояния
  - `src/signalr.ts` — инициализация/подписка на SignalR
  - `src/api.ts` — запросы к `/api/status`
  - `src/types.ts` — типы DTO (devices, ms)

Шаблоны файлов (критичное):
```json
// TN_Doc/Client/statusbar/package.json
{
  "name": "status-bar",
  "private": true,
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "vite build",
    "preview": "vite preview --port 5174"
  },
  "dependencies": {
    "@microsoft/signalr": "^8.0.5",
    "vue": "^3.4.21"
  },
  "devDependencies": {
    "@vitejs/plugin-vue": "^5.0.4",
    "typescript": "^5.4.5",
    "vite": "^5.2.0",
    "vue-tsc": "^1.8.27"
  }
}
```

```ts
// TN_Doc/Client/statusbar/vite.config.ts
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'node:path';

export default defineConfig({
  plugins: [vue()],
  root: '.',
  build: {
    outDir: resolve(__dirname, '../../wwwroot/statusbar'),
    emptyOutDir: true,
    sourcemap: false,
    rollupOptions: {
      input: resolve(__dirname, 'src/main.ts'),
      output: {
        entryFileNames: 'status-bar.js',
        assetFileNames: 'status-bar.[ext]'
      }
    }
  },
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': 'http://localhost:5000',
      '/statusHub': { target: 'http://localhost:5000', ws: true }
    }
  }
});
```

### 2) Компонент и логика клиента
- Реализовать `StatusBar.vue` с реактивным состоянием: `devices[]`, `ms`, `ts`.
- При монтировании:
  - выполнить `fetchStatus()`;
  - запустить `setInterval(fetchStatus, 5000)`;
  - попытаться подключиться к SignalR и подписаться на `statusUpdated`.
- Визуализация: зелёная/красная точка, имя, latency (если есть), «Обновлено: HH:MM:SS».

Ключевые файлы (сокращено):
```ts
// src/types.ts
export type DeviceStatus = { name: string; ok: boolean; latencyMs?: number | null };
export type MsStatus = { ok: boolean; latencyMs?: number | null };
export type StatusPayload = { devices: DeviceStatus[]; ms: MsStatus; ts?: string };
```

```ts
// src/api.ts
import type { StatusPayload } from './types';
export async function fetchStatus(apiUrl = '/api/status'): Promise<StatusPayload> {
  const t0 = performance.now();
  const res = await fetch(apiUrl, { credentials: 'same-origin' });
  const data = (await res.json()) as StatusPayload;
  const t1 = performance.now();
  data.ms ??= { ok: false };
  if (data.ms.latencyMs == null) data.ms.latencyMs = Math.round(t1 - t0);
  data.ts = new Date().toLocaleTimeString();
  return data;
}
```

```ts
// src/signalr.ts
import * as signalR from '@microsoft/signalr';
import type { StatusPayload } from './types';
export async function startStatusStream(onUpdate: (p: StatusPayload) => void, hubUrl = '/statusHub') {
  try {
    const conn = new signalR.HubConnectionBuilder().withUrl(hubUrl, { withCredentials: true }).withAutomaticReconnect().build();
    conn.on('statusUpdated', (p: StatusPayload) => { p.ts = new Date().toLocaleTimeString(); onUpdate(p); });
    await conn.start();
  } catch {/* деградация на pull */}
}
```

```vue
<!-- src/components/StatusBar.vue (шаблон сокращён) -->
<template>
  <div class="status-bar__wrap">
    <div class="status-bar__item" v-for="d in devices" :key="d.name">
      <span class="dot" :style="{ backgroundColor: d.ok ? 'green' : 'red' }"></span>
      <span>{{ d.name }}</span>
      <span v-if="d.latencyMs != null" class="lat">{{ d.latencyMs }}ms</span>
    </div>
    <div class="status-bar__item">
      <span class="dot" :style="{ backgroundColor: ms.ok ? 'green' : 'red' }"></span>
      <span>MS</span>
      <span v-if="ms.latencyMs != null" class="lat">{{ ms.latencyMs }}ms</span>
    </div>
    <div class="status-bar__ts" v-if="ts">Обновлено: {{ ts }}</div>
  </div>
  </template>
```

### 3) Интеграция в Razor layout
- В `TN_Doc/Views/Shared/_Layout.cshtml` добавить контейнер и ссылки на артефакты:
```html
<div id="status-bar" class="status-bar"></div>
<link rel="stylesheet" href="/statusbar/status-bar.css?v=@ViewData["Version"]" />
<script type="module" src="/statusbar/status-bar.js?v=@ViewData["Version"]"></script>
```

### 4) Бэкенд — API, SignalR, фоновые проверки
- Контроллер `StatusController` (`GET /api/status`): возвращает текущие статусы.
- Хаб `StatusHub` и регистрация:
```csharp
builder.Services.AddSignalR();
app.MapHub<StatusHub>("/statusHub");
```
- BackgroundService (опции):
  - N секунд проверяет БД/внешние сервисы (короткие таймауты, без постоянных коннектов).
  - При изменениях отправляет `IHubContext<StatusHub>.Clients.All.SendAsync("statusUpdated", payload)`.

Формат ответа `/api/status`:
```json
{
  "devices": [ { "name": "ИВК-1", "ok": true, "latencyMs": 12 } ],
  "ms": { "ok": true, "latencyMs": 20 }
}
```

Минимальный скелет:
```csharp
[ApiController]
[Route("api/status")]
public class StatusController : ControllerBase {
  private readonly IAppConfigService _cfg;
  public StatusController(IAppConfigService cfg) => _cfg = cfg;
  [HttpGet] public IActionResult Get() {
    var devices = _cfg.GetAppCfg().Devices.Where(d => d.Use)
      .Select(d => new { name = d.Name, ok = true, latencyMs = (int?)null });
    var ms = new { ok = true, latencyMs = (int?)null };
    return Ok(new { devices, ms });
  }
}
```

### 5) Безопасность
- Доступ к `/api/status` и `/statusHub` — только авторизованным (read‑only роль).
- CORS (если раздельные домены для dev): разрешить фронт‑origin для `/statusHub` и `/api/status`.
- Заголовки безопасности: `X-Content-Type-Options`, `X-Frame-Options`, CSP в прод.

### 6) CI/CD
- В `.gitlab-ci.yml` добавить шаг сборки фронта перед сборкой .NET:
```yaml
build:statusbar:
  stage: build
  image: node:20
  script:
    - cd TN_Doc/Client/statusbar
    - npm ci
    - npm run build
  artifacts:
    paths:
      - TN_Doc/wwwroot/statusbar/
    expire_in: 7 days
```
- Основная сборка .NET использует артефакты `wwwroot/statusbar`.

### 7) Локальная разработка
- `dotnet run` (бекенд на 5000).
- `cd TN_Doc/Client/statusbar && npm i && npm run dev` — фронт на 5173, прокси на 5000.
- Открыть главное окно — виджет статусбара подключается к dev‑серверу.

### 8) Наблюдаемость и тесты
- Логировать длительность проверок, количество неуспешных попыток.
- Юнит‑тесты клиента (vue-tsc) и контрактный тест формата `/api/status`.
- Интеграционный тест: имитация падения одного канала → красная индикация.

### 9) Критерии приёмки
- Строка состояния всегда видима, не перекрывает контент, корректно реагирует на изменения.
- При деградации SignalR остаются периодические обновления (pull).
- В прод — корректные заголовки безопасности, доступ только авторизованным.