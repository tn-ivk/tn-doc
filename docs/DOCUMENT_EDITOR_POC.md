# Document Editor - Proof of Concept

## 📋 Обзор

Этот документ описывает полноценный proof-of-concept Vue SPA приложения для редактирования документов TN_Doc через REST API.

## 🎯 Что реализовано

### Backend (C#)

1. **API Controller** (`TN_Doc/Controllers/DocumentEditController.cs`)
   - `GET /api/documents/{deviceId}/{docType}/edit/{id}` - получение конфигурации
   - `POST /api/documents/{deviceId}/{docType}/save/{id}` - сохранение документа
   - `GET /api/documents/health` - проверка доступности API

2. **Интерфейс IDocumentEditor** (`tn.docgeneral/TN.DocGeneral/IDocumentEditor.cs`)
   - Контракт для документов с поддержкой API редактирования

3. **Реализация в DocJornal** (`tn.docgeneral/Jornal/DocJornal.cs`)
   - Метод `GetEditConfig()` - возвращает JSON конфигурацию вместо HTML

### Frontend (Vue 3 + TypeScript)

1. **Vue SPA приложение** (`TN_Doc/Client/document-editor/`)
   - Vue 3 с Composition API
   - TypeScript для типобезопасности
   - Vue Router для маршрутизации
   - Pinia для управления состоянием
   - PrimeVue для UI компонентов

2. **Компоненты**
   - `FormField.vue` - универсальное поле формы с валидацией
   - `DocumentEditor.vue` - главная страница редактирования
   - `ErrorPage.vue` - страница ошибок

3. **Сервисы**
   - `api.service.ts` - HTTP клиент для взаимодействия с API
   - `documentStore.ts` - Pinia store для управления состоянием документа

## 🚀 Инструкция по запуску

### Шаг 1: Установка зависимостей

```bash
cd /home/snafu/projects/ivk/tn_doc/TN_Doc/Client/document-editor
npm install
```

### Шаг 2: Запуск backend (ASP.NET Core)

```bash
cd /home/snafu/projects/ivk/tn_doc/TN_Doc
dotnet run
```

Backend будет доступен на `http://localhost:38509`

### Шаг 3: Запуск frontend dev сервера

```bash
cd /home/snafu/projects/ivk/tn_doc/TN_Doc/Client/document-editor
npm run dev
```

Frontend dev сервер запустится на `http://localhost:5173`

### Шаг 4: Тестирование

#### Тест 1: Проверка API

```bash
# Проверка health check
curl http://localhost:38509/api/documents/health

# Ожидаемый ответ:
# {"status":"healthy","service":"DocumentEditAPI","timestamp":"2025-..."}
```

#### Тест 2: Получение конфигурации документа

```bash
# Замените {deviceId} и {docId} на реальные значения из вашей БД
curl http://localhost:38509/api/documents/{deviceId}/Jornal/edit/{docId}

# Пример:
curl http://localhost:38509/api/documents/00000000-0000-0000-0000-000000000001/Jornal/edit/123
```

#### Тест 3: Открытие редактора в браузере

```
http://localhost:5173/document-editor/edit/{deviceId}/Jornal/{docId}

# Пример:
http://localhost:5173/document-editor/edit/00000000-0000-0000-0000-000000000001/Jornal/123
```

**Что вы должны увидеть:**
1. Заголовок "Редактирование журнала измерений"
2. Таблицу с 4 полями:
   - Сдал (ФИО) 1
   - Сдал (ФИО) 2
   - Принял (ФИО) 1
   - Принял (ФИО) 2
3. Кнопку "Сохранить"

#### Тест 4: Редактирование и сохранение

1. Выберите пользователей из выпадающих списков
2. Нажмите "Сохранить"
3. Проверьте в консоли браузера логи сохранения
4. Проверьте в БД, что данные обновились

### Шаг 5: Сборка для production

```bash
cd /home/snafu/projects/ivk/tn_doc/TN_Doc/Client/document-editor
npm run build
```

Файлы будут собраны в `TN_Doc/wwwroot/document-editor/`

После сборки приложение будет доступно по адресу:
```
http://localhost:38509/document-editor/edit/{deviceId}/Jornal/{docId}
```

## 📁 Структура файлов

```
TN_Doc/
├── Controllers/
│   └── DocumentEditController.cs       # API endpoints
├── Client/
│   └── document-editor/                # Vue SPA
│       ├── src/
│       │   ├── components/
│       │   │   └── FormField.vue       # Универсальное поле
│       │   ├── router/
│       │   │   └── index.ts            # Маршрутизация
│       │   ├── services/
│       │   │   └── api.service.ts      # HTTP клиент
│       │   ├── stores/
│       │   │   └── documentStore.ts    # Pinia store
│       │   ├── types/
│       │   │   └── document.types.ts   # TypeScript типы
│       │   ├── views/
│       │   │   ├── DocumentEditor.vue  # Редактор
│       │   │   └── ErrorPage.vue       # Ошибки
│       │   ├── App.vue
│       │   └── main.ts
│       ├── package.json
│       ├── vite.config.ts
│       └── README.md
└── wwwroot/
    └── document-editor/                # Production build

tn.docgeneral/
├── TN.DocGeneral/
│   └── IDocumentEditor.cs              # Интерфейс для API
└── Jornal/
    └── DocJornal.cs                    # Реализация для Jornal
```

## 🔄 Архитектурный поток данных

```
┌─────────────┐
│   Browser   │
│  (Vue SPA)  │
└──────┬──────┘
       │
       │ HTTP Request
       │ GET /api/documents/{deviceId}/{docType}/edit/{id}
       ▼
┌──────────────────────┐
│ DocumentEditController│
│    (ASP.NET Core)     │
└──────────┬───────────┘
           │
           │ GetDocumentClass()
           ▼
┌──────────────────────┐
│  IAppConfigService   │
└──────────┬───────────┘
           │
           │ Returns DocJornal instance
           ▼
┌──────────────────────┐
│     DocJornal        │
│  (IDocumentEditor)   │
└──────────┬───────────┘
           │
           │ GetEditConfig(id)
           │  - Loads document from DB
           │  - Loads CfgEditJornal.json
           │  - Builds field configurations
           │  - Returns JSON object
           ▼
┌──────────────────────┐
│   JSON Response      │
│   {                  │
│     docId: 123,      │
│     fields: [...],   │
│     ...              │
│   }                  │
└──────────┬───────────┘
           │
           │ HTTP Response
           ▼
┌──────────────────────┐
│   Vue SPA            │
│   - Displays form    │
│   - User edits       │
│   - Validates        │
│   - Saves via POST   │
└──────────────────────┘
```

## 🎨 Преимущества подхода

### 1. **Полное разделение frontend/backend**
- Frontend и backend разрабатываются независимо
- API можно использовать из других клиентов (мобильные приложения, desktop)

### 2. **Современный стек**
- Vue 3 с Composition API
- TypeScript для типобезопасности
- PrimeVue для enterprise UI

### 3. **Масштабируемость**
- Легко добавлять новые типы документов
- Просто расширять функциональность

### 4. **Лучшая производительность**
- SPA с Virtual DOM
- Hot Module Replacement в dev режиме
- Оптимизированная production сборка

### 5. **Удобство разработки**
- Hot reload для мгновенных изменений
- TypeScript подсказки в IDE
- Компонентная архитектура

## 🆚 Сравнение с текущим подходом

| Аспект | Текущий подход (HTML в C#) | Vue SPA PoC |
|--------|----------------------------|-------------|
| **Разметка** | HTML строки в C# | Vue компоненты |
| **Стилизация** | Inline CSS в строках | Scoped CSS + PrimeVue |
| **Логика** | jQuery в HTML строках | TypeScript + Composition API |
| **Валидация** | JavaScript функции в строках | Реактивные computed + Pinia |
| **Поддержка** | Сложно найти и изменить | Понятная структура файлов |
| **Тестирование** | Сложно тестировать | Unit/E2E тесты для компонентов |
| **Dev Experience** | Без подсказок, без hot reload | TypeScript + Hot Reload |

## 📝 Следующие шаги для полной миграции

### Этап 1: Расширение PoC (1-2 недели)
1. Добавить Toast уведомления вместо alert
2. Реализовать автосохранение через debounce
3. Добавить индикатор несохраненных изменений
4. Реализовать горячие клавиши (Ctrl+S для сохранения)

### Этап 2: Поддержка сложных документов (2-3 недели)
1. Реализовать `GetEditConfig` для Passport
   - Таблица параметров с методами испытаний
   - Логика TogglePrintCellEditable
   - ELIS интеграция
2. Создать компонент ParametersTable.vue
3. Реализовать сложную валидацию

### Этап 3: Миграция остальных документов (1-2 месяца)
1. Act, Report - простые документы
2. Все варианты Poverka
3. Все варианты KMH

### Этап 4: Полная замена (1 месяц)
1. Удалить старые HTML-генераторы
2. Обновить все вызовы на новый API
3. Финальное тестирование

## 🐛 Известные ограничения PoC

1. ❌ Пока только для Jornal (простой документ)
2. ❌ Нет поддержки таблиц параметров
3. ❌ Нет ELIS интеграции
4. ❌ Нет сложной валидации (округление, зависимости полей)
5. ❌ Используется alert вместо Toast уведомлений

Но все это легко добавляется, когда архитектура уже на месте!

## ✅ Заключение

Этот PoC демонстрирует, что **переход на API + Vue SPA является правильным решением** для системы TN_Doc:

- ✅ Чистая архитектура с разделением ответственностей
- ✅ Современный стек технологий
- ✅ Удобство разработки и поддержки
- ✅ Масштабируемость и расширяемость
- ✅ Лучшая производительность

**Рекомендация**: Продолжать развитие в этом направлении, постепенно мигрируя документы от простых к сложным.
