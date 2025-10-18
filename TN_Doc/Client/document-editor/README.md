# Document Editor - Vue SPA

Редактор документов TN_Doc, построенный на Vue 3 + TypeScript + PrimeVue.

## 📋 Описание

Это современное SPA приложение для редактирования документов через REST API. Заменяет старый подход с генерацией HTML в C# на чистую клиент-серверную архитектуру.

## 🎯 Особенности

- **Vue 3.4.21** с Composition API и TypeScript
- **PrimeVue 4.2+** для UI компонентов
- **Pinia** для управления состоянием
- **Vue Router** для маршрутизации
- **Axios** для HTTP запросов
- **Vite** для быстрой разработки и сборки

## 🚀 Быстрый старт

### Установка зависимостей

```bash
cd TN_Doc/Client
npm install
```

### Разработка

```bash
# Запустить dev сервер с hot reload
npm run dev:editor

# Приложение будет доступно на http://localhost:5174
```

### Сборка для production

```bash
# Собрать только document-editor
npm run build:editor

# Или собрать все приложения
npm run build:all
```

Собранные файлы будут в `TN_Doc/wwwroot/document-editor/`

## 📁 Структура проекта

```
document-editor/
├── src/
│   ├── components/         # Vue компоненты
│   │   └── FormField.vue   # Универсальное поле формы
│   ├── router/             # Конфигурация маршрутизации
│   │   └── index.ts
│   ├── services/           # API клиенты
│   │   └── api.service.ts  # HTTP клиент для DocumentEditController
│   ├── stores/             # Pinia stores
│   │   └── documentStore.ts
│   ├── types/              # TypeScript типы
│   │   └── document.types.ts
│   ├── views/              # Страницы приложения
│   │   ├── DocumentEditor.vue  # Главный редактор
│   │   └── ErrorPage.vue       # Страница ошибок
│   ├── App.vue             # Корневой компонент
│   └── main.ts             # Точка входа
├── index.html              # HTML шаблон
├── package.json
├── tsconfig.json
├── vite.config.ts
└── README.md
```

## 🔌 API Endpoints

Приложение использует следующие endpoints из `DocumentEditController`:

- `GET /api/documents/health` - Проверка доступности API
- `GET /api/documents/{deviceId}/{docType}/edit/{id}` - Получение конфигурации документа
- `POST /api/documents/{deviceId}/{docType}/save/{id}` - Сохранение документа

## 🌐 Маршруты

- `/document-editor/edit/:deviceId/:docType/:id` - Редактирование документа
- `/document-editor/error` - Страница ошибок

## 💾 Управление состоянием

Приложение использует Pinia store (`documentStore`) для:

- Загрузки конфигурации документа
- Управления значениями полей формы
- Отслеживания несохранённых изменений
- Сохранения документа через API

## 🎨 UI Компоненты

### FormField

Универсальный компонент для рендеринга полей формы:

- `select` - Выпадающий список (PrimeVue Dropdown)
- `text` - Текстовое поле (PrimeVue InputText)
- `number` - Числовое поле (PrimeVue InputNumber)
- `date` - Дата (PrimeVue DatePicker)
- `datetime-local` - Дата и время (PrimeVue DatePicker с showTime)

### DocumentEditor

Главный компонент редактора:

- Загрузка конфигурации документа
- Рендеринг полей формы
- Валидация
- Сохранение изменений
- Предупреждение о несохранённых изменениях

## 🔧 Конфигурация

### Vite

- Base URL: `/document-editor/`
- Output: `TN_Doc/wwwroot/document-editor/`
- Dev server: `localhost:5174`
- Proxy: API запросы проксируются на `localhost:38509`

### TypeScript

- Target: ES2022
- Module: ESNext
- Strict mode: включён
- Алиасы:
  - `@/*` → `src/*`
  - `@shared/*` → `../shared/src/*`

## 🧪 Тестирование

```bash
# Проверка типов TypeScript
npm run type-check
```

## 📝 Пример использования

```typescript
// В iframe основного приложения
const editorUrl = `/document-editor/edit/${deviceId}/Report/${docId}`;
iframe.src = editorUrl;
```

## 🔜 Планы развития

- [ ] Добавить Toast уведомления вместо alert
- [ ] Реализовать автосохранение
- [ ] Добавить горячие клавиши (Ctrl+S)
- [ ] Компонент для таблиц параметров (Passport)
- [ ] Интеграция с ELIS для Passport
- [ ] Unit тесты с Vitest
- [ ] E2E тесты с Playwright

## 📚 Документация

- [Vue 3](https://vuejs.org/)
- [PrimeVue](https://primevue.org/)
- [Pinia](https://pinia.vuejs.org/)
- [Vue Router](https://router.vuejs.org/)
- [Vite](https://vitejs.dev/)
