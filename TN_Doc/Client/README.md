# TN_Doc Client Applications

Vue.js приложения для TN_Doc, организованные как npm workspaces.

## Структура

- `statusbar/` - Модуль строки состояния
- `configurator/` - Конфигуратор настроек (SPA)
- `shared/` - Общие компоненты, утилиты и API клиенты
- `e2e/` - E2E тесты (Playwright)

## Требования

- Node.js >= 18.0.0
- npm >= 8.0.0

## Разработка

```bash
# Установка зависимостей для всех workspace
npm install

# Запуск dev сервера для statusbar
npm run dev

# Запуск dev сервера для configurator
npm run dev:configurator

# Сборка для production (statusbar)
npm run build

# Сборка configurator
npm run build:configurator

# Сборка всех приложений
npm run build:all

# Проверка типов
npm run type-check

# E2E тесты
npm run test:e2e
```

## Сборка

Скомпилированные файлы размещаются в:
- `TN_Doc/wwwroot/statusbar/`
- `TN_Doc/wwwroot/configurator/`

Оба клиента автоматически подключаются к ASP.NET Core приложению.
