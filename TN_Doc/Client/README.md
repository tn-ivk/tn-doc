# TN_Doc Client Applications

Vue.js приложения для TN_Doc, организованные как npm workspaces.

## Структура

- `statusbar/` - Модуль строки состояния (MVP для миграции на Vue.js)
- `shared/` - Общие компоненты, утилиты и API клиенты

## Требования

- Node.js >= 18.0.0
- npm >= 8.0.0

## Разработка

```bash
# Установка зависимостей для всех workspace
npm install

# Запуск dev сервера для statusbar
npm run dev

# Сборка для production
npm run build

# Проверка типов
npm run type-check
```

## Сборка

Скомпилированные файлы размещаются в `TN_Doc/wwwroot/statusbar/` и автоматически подключаются к ASP.NET Core приложению.