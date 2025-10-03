# Status Bar - Инструкция по установке и разработке

## Первоначальная установка

### 1. Установка зависимостей

```bash
cd TN_Doc/Client
npm install
```

### 2. Сборка для production

```bash
npm run build
```

Скомпилированные файлы будут размещены в `TN_Doc/wwwroot/statusbar/`.

## Разработка

### Запуск dev сервера (hot reload)

```bash
cd TN_Doc/Client
npm run dev
```

Dev сервер запустится на `http://localhost:5173` с проксированием запросов к backend на `http://localhost:38509`.

### Проверка типов

```bash
npm run type-check
```

## Структура проекта

```
TN_Doc/Client/
├── statusbar/              # Модуль строки состояния
│   ├── src/
│   │   ├── components/     # Vue компоненты
│   │   ├── stores/         # Pinia stores
│   │   ├── composables/    # Vue composables
│   │   ├── types/          # TypeScript типы
│   │   ├── App.vue         # Корневой компонент
│   │   └── main.ts         # Точка входа
│   ├── package.json
│   ├── vite.config.ts
│   └── tsconfig.json
├── shared/                 # Общие модули
│   └── src/
│       └── api/            # API клиенты
└── package.json            # Корневой package.json (workspaces)
```

## Backend интеграция

Status bar интегрирован с существующей архитектурой TN_Doc:

- **IAppConfigService**: Используется для получения конфигурации устройств
- **NLog**: Интеграция для серверного и клиентского логирования
- **SignalR Hub**: `/statusHub` для real-time обновлений
- **API endpoint**: `/api/status` для получения статуса

## Проверка работы

1. Убедитесь что backend запущен:
   ```bash
   cd TN_Doc
   dotnet run
   ```

2. Откройте браузер на `http://localhost:38509`

3. В нижней части страницы должен появиться status bar с индикаторами состояния устройств и сервисов

## Troubleshooting

### Status bar не отображается

1. Проверьте что файлы собраны: `ls TN_Doc/wwwroot/statusbar/`
2. Проверьте консоль браузера на ошибки JavaScript
3. Убедитесь что container `<div id="status-bar"></div>` присутствует в _Layout.cshtml

### SignalR не подключается

1. Проверьте что `services.AddSignalR()` добавлен в Startup.cs
2. Проверьте что маршрут `/statusHub` зарегистрирован
3. Проверьте логи сервера на наличие ошибок подключения

### Устройства показывают статус "offline"

1. Проверьте конфигурацию устройств в `CfgApp.json`
2. Проверьте подключение к MySQL/MariaDB
3. Проверьте логи StatusProvider в NLog

## Дальнейшая разработка

Status bar - это первый Vue.js модуль в TN_Doc. Архитектура спроектирована для последующей миграции других частей интерфейса:

- Используйте `shared/` для общих компонентов и утилит
- Следуйте паттерну workspace для новых модулей
- Интегрируйтесь с существующими сервисами TN_Doc (IAppConfigService, NLog)