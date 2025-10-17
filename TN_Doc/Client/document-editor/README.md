# Document Editor - Vue SPA

## Описание

Vue SPA приложение для редактирования документов TN_Doc через REST API.

## Proof of Concept

Это proof-of-concept демонстрирует:
- ✅ REST API для получения конфигурации документов
- ✅ Vue 3 + TypeScript + Composition API
- ✅ Vue Router для маршрутизации
- ✅ Pinia для управления состоянием
- ✅ PrimeVue для UI компонентов
- ✅ Валидация полей формы
- ✅ Сохранение документов через API

## Установка

```bash
# Установка зависимостей
npm install
```

## Разработка

```bash
# Запуск dev сервера с hot-reload
npm run dev

# Приложение будет доступно по адресу:
# http://localhost:5173/document-editor/edit/{deviceId}/{docType}/{docId}

# Пример:
# http://localhost:5173/document-editor/edit/00000000-0000-0000-0000-000000000001/Jornal/123
```

## Сборка

```bash
# Сборка для production
npm run build

# Результат будет в TN_Doc/wwwroot/document-editor/
```

## Структура проекта

```
document-editor/
├── src/
│   ├── components/        # Vue компоненты
│   │   └── FormField.vue  # Универсальное поле формы
│   ├── router/            # Vue Router конфигурация
│   │   └── index.ts       # Маршруты приложения
│   ├── services/          # Сервисы
│   │   └── api.service.ts # API клиент
│   ├── stores/            # Pinia stores
│   │   └── documentStore.ts # Store для документов
│   ├── types/             # TypeScript типы
│   │   └── document.types.ts
│   ├── views/             # Страницы
│   │   ├── DocumentEditor.vue # Редактор документа
│   │   └── ErrorPage.vue      # Страница ошибки
│   ├── App.vue            # Главный компонент
│   └── main.ts            # Точка входа
├── index.html
├── package.json
├── tsconfig.json
└── vite.config.ts
```

## API Endpoints

### GET /api/documents/{deviceId}/{docType}/edit/{id}
Получение конфигурации формы редактирования

**Response:**
```json
{
  "docId": 123,
  "docType": "Jornal",
  "deviceId": "00000000-0000-0000-0000-000000000001",
  "title": "Редактирование журнала измерений",
  "fields": [
    {
      "name": "DeliveryIOF1",
      "label": "Сдал (ФИО)",
      "type": "select",
      "required": true,
      "disabled": false,
      "value": "123",
      "options": [
        { "id": 123, "name": "Иванов И.И." }
      ]
    }
  ],
  "dictionaries": {
    "deliveryUsers": [...],
    "receiveUsers": [...]
  },
  "invalidChars": ["<", ">", "\"", "'", "&"]
}
```

### POST /api/documents/{deviceId}/{docType}/save/{id}
Сохранение документа

**Request Body:**
```json
{
  "data": {
    "docID": 123,
    "values": [
      {
        "key": "DeliveryIOF1",
        "value": "123",
        "tag": "AdditionalInfo"
      }
    ]
  }
}
```

## Использование

### В iframe (текущая интеграция)

```html
<iframe
  id="editFrame"
  src="/document-editor/edit/00000000-0000-0000-0000-000000000001/Jornal/123"
  style="width: 100%; height: 600px; border: none;">
</iframe>
```

### Как отдельная страница

```
http://localhost:38509/document-editor/edit/{deviceId}/{docType}/{docId}
```

## События

Приложение отправляет postMessage события в родительское окно:

- `DocumentSaved` - документ успешно сохранен
- `CloseEditor` - запрос на закрытие редактора

## Поддерживаемые типы документов

На данный момент реализовано для:
- `Jornal` - Журнал измерений

Для добавления других типов документов:
1. Реализовать `IDocumentEditor` в классе документа
2. Добавить метод `GetEditConfig(int id)`
3. API автоматически подхватит новый тип

## Технологии

- Vue 3.4.21
- TypeScript 5.4.2
- Vue Router 4.2.5
- Pinia 2.1.7
- PrimeVue 4.2.1
- Axios 1.6.7
- Vite 5.1.5

## Преимущества подхода

1. **Полное разделение frontend/backend** - независимая разработка
2. **REST API** - можно использовать в других клиентах
3. **Современный стек** - Vue 3 + Composition API
4. **Типобезопасность** - TypeScript
5. **Масштабируемость** - легко добавлять новые типы документов
6. **Производительность** - SPA с Virtual DOM

## Следующие шаги

1. Добавить поддержку других типов документов (Passport, Act)
2. Реализовать более сложные поля (таблицы параметров)
3. Добавить Toast уведомления вместо alert
4. Реализовать автосохранение
5. Добавить валидацию на стороне backend
