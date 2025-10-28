# Интеграция OPC тегов в редактор документов

## Обзор

Данный документ описывает интеграцию функциональности записи OPC тегов в новый Vue-редактор документов, реализуя логику, аналогичную старой версии (`EditDoc.js`).

## Изменения в архитектуре

### 1. Новый OPC API клиент (`opc.service.ts`)

**Расположение:** `TN_Doc/Client/document-editor/src/services/opc.service.ts`

**Функциональность:**
- Чтение и запись OPC тегов через TN_MessagingService (порт 5010)
- Polling механизм для ожидания изменения значения тега
- Формирование полных имен тегов с префиксом устройства

**Основные методы:**
```typescript
// Чтение значения тега
readTag(deviceName, tagName, namespaceIndex, indexArray): Promise<any>

// Запись значения в тег
writeTag(deviceName, tagName, value, namespaceIndex, indexArray): Promise<void>

// Polling тега с ожиданием изменения
pollTag(deviceName, tagName, expectedChange, maxDuration, pollInterval): Promise<boolean>

// Формирование полного имени тега
getFullTagName(tagName, prefix): string
```

**Пример использования:**
```typescript
import { opcApi } from '@/services/opc.service';

// Запись в тег
await opcApi.writeTag('guid-device', 'IVK.ARM.ARM_FillActAndPassport', true, 2, 0);

// Polling тега (ожидание увеличения значения)
const success = await opcApi.pollTag(
  'guid-device',
  'IVK.ARM.ARM_FillActAndPassportResult',
  (current, initial) => current > initial,
  5000, // 5 секунд
  500   // интервал 500 мс
);
```

---

### 2. Композабл для сохранения с OPC (`usePassportSave.ts`)

**Расположение:** `TN_Doc/Client/document-editor/src/composables/usePassportSave.ts`

**Функциональность:**
- Реализует логику сохранения паспортов с записью в OPC теги
- Поддерживает три стратегии сохранения в зависимости от типа документа

**Логика сохранения для паспортов качества (DocType = "Passport"):**

1. **Сохранить документ** через `documentApi.saveDocument()`
2. **Записать в OPC тег** `ARM.ARM_FillActAndPassport = true`
3. **Polling тега** `ARM.ARM_FillActAndPassportResult` (ожидание увеличения значения)
   - Максимальное время: 5000 мс
   - Интервал опроса: 500 мс
4. **Объединить данные** из `localStorage.getItem('dataPassport')`
5. **Обновить документ** через `documentApi.updateDocument()`

**Логика сохранения для актов (DocType = "Act"):**

1. **Сохранить документ** через `documentApi.saveDocument()`
2. **Записать в OPC тег** `ARM.ARM_FillActAndPassport = true`
3. **Без polling** - только запись в тег

**Для остальных документов (Report, Jornal и т.д.):**

Обычное сохранение без OPC логики.

**Основные методы:**
```typescript
// Сохранение паспорта с полной OPC логикой (включая polling)
savePassportWithOpc(): Promise<boolean>

// Сохранение акта с записью в тег (без polling)
saveActWithOpc(): Promise<boolean>

// Универсальная функция (автоматический выбор стратегии)
saveDocumentWithOpc(): Promise<boolean>
```

---

### 3. Обновление API сервиса

**Файл:** `TN_Doc/Client/document-editor/src/services/api.service.ts`

**Добавлен новый метод:**
```typescript
// Обновление документа после успешной записи в OPC тег
updateDocument(deviceId, docType, id, data): Promise<SaveDocumentResponse>
```

**Маршрут:** `POST /api/documents/{deviceId}/{docType}/update/{id}`

---

### 4. Обновление типов

**Файл:** `TN_Doc/Client/document-editor/src/types/document.types.ts`

**Добавлены новые поля в `DocumentEditConfig`:**
```typescript
export interface DocumentEditConfig {
  // ... существующие поля ...

  /** GUID устройства для OPC тегов */
  deviceGuid?: string;

  /** Имя устройства */
  deviceName?: string;

  /** Префикс тега для OPC (например: "IVK", "IVK_2") */
  tagPrefix?: string;
}
```

---

### 5. Интеграция в useDocumentEditor

**Файл:** `TN_Doc/Client/document-editor/src/composables/useDocumentEditor.ts`

**Изменения:**
- Импортирован `usePassportSave`
- Метод `handleSave()` теперь использует `saveDocumentWithOpc()`
- Добавлено уведомление при таймауте polling

```typescript
const { saveDocumentWithOpc } = usePassportSave();

const handleSave = async (): Promise<boolean> => {
  // ... валидация ...

  const success = await saveDocumentWithOpc();

  if (!success) {
    // Предупреждение о таймауте polling
    toast.add({
      severity: 'warn',
      summary: 'Предупреждение',
      detail: 'Документ сохранен, но ИВК не подтвердил запись в течение 5 секунд'
    });
  }

  return success;
};
```

---

## Требования к бэкенду

### 1. Обновление API контроллера

**Необходимо реализовать новый endpoint:**

```csharp
[HttpPost("{deviceId}/{docType}/update/{id}")]
public async Task<IActionResult> UpdateDocument(
    int deviceId,
    string docType,
    int id,
    [FromBody] Dictionary<string, object> data)
{
    // Логика обновления документа после успешной записи в OPC тег
    // Аналогично методу SaveDoc, но для обновления существующего документа
}
```

**Отличие от `SaveDoc`:**
- `SaveDoc` - первичное сохранение (до записи в тег)
- `UpdateDoc` - финальное обновление (после подтверждения от ИВК)

---

### 2. Добавление OPC параметров в конфигурацию

**В методе `GetEditConfig` необходимо добавить поля:**

```csharp
public class DocumentEditConfigDto
{
    // ... существующие поля ...

    public string DeviceGuid { get; set; }      // GUID устройства для OPC
    public string DeviceName { get; set; }      // Имя устройства
    public string TagPrefix { get; set; }       // Префикс тега (IVK, IVK_2 и т.д.)
}
```

**Пример ответа:**
```json
{
  "docId": 123,
  "docType": "Passport",
  "deviceId": 1,
  "deviceGuid": "550e8400-e29b-41d4-a716-446655440000",
  "deviceName": "IVK-1",
  "tagPrefix": "IVK",
  "fields": [...],
  "initialValues": {...}
}
```

---

### 3. Настройка CORS для TN_MessagingService

**Если TN_MessagingService работает на другом порту (5010), необходимо настроить CORS:**

```csharp
// В Startup.cs TN_MessagingService
services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder
            .WithOrigins("http://localhost:38509", "http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// В Configure
app.UseCors("AllowFrontend");
```

---

## Схема работы для паспортов

```
┌─────────────────────────────────────────────────────────────────┐
│                    Новый Vue-редактор                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 1. SaveDoc (POST /api/documents/{deviceId}/Passport/save/{id}) │
│    - Сохранение основных данных паспорта                        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. WriteTag (PUT http://localhost:5010/api/Values/)            │
│    - Запись ARM.ARM_FillActAndPassport = true                  │
│    - Триггер для ИВК на обновление данных                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. Polling ARM.ARM_FillActAndPassportResult                     │
│    - Чтение тега каждые 500 мс                                 │
│    - Ожидание увеличения значения (max 5000 мс)                │
└─────────────────────────────────────────────────────────────────┘
                              │
                ┌─────────────┴─────────────┐
                │                           │
         Успех (тег изменился)    Таймаут (5 секунд)
                │                           │
                ▼                           ▼
┌──────────────────────────────┐  ┌──────────────────────┐
│ 4. Объединение данных        │  │ Предупреждение       │
│    - localStorage            │  │ пользователю         │
│      ('dataPassport')        │  └──────────────────────┘
└──────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. UpdateDoc (POST /api/documents/{deviceId}/Passport/update/{id}) │
│    - Финальное обновление с объединенными данными               │
└─────────────────────────────────────────────────────────────────┘
```

---

## Схема работы для актов

```
┌─────────────────────────────────────────────────────────────────┐
│                    Новый Vue-редактор                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 1. SaveDoc (POST /api/documents/{deviceId}/Act/save/{id})      │
│    - Сохранение данных акта                                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. WriteTag (PUT http://localhost:5010/api/Values/)            │
│    - Запись ARM.ARM_FillActAndPassport = true                  │
│    - Уведомление ИВК (без ожидания подтверждения)              │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                         ✓ Готово
```

---

## Обработка ошибок

### 1. Таймаут polling
**Сценарий:** ИВК не ответил в течение 5 секунд

**Поведение:**
- Документ **сохранен** в базе
- Пользователь видит предупреждение
- Возвращается `false` из `saveDocumentWithOpc()`

### 2. Ошибка записи в OPC тег
**Сценарий:** TN_MessagingService недоступен или ошибка записи тега

**Поведение:**
- Документ **сохранен** в базе
- Выброс исключения с сообщением: "Документ сохранен, но не удалось записать в ИВК"
- Пользователь видит ошибку в Toast

### 3. Отсутствие OPC параметров
**Сценарий:** В конфигурации отсутствуют `deviceGuid` или `tagPrefix`

**Поведение:**
- Логируется предупреждение
- OPC логика **пропускается**
- Выполняется обычное сохранение

---

## Тестирование

### Чек-лист для тестирования

#### Паспорт качества
- [ ] Сохранение паспорта при работающем ИВК
- [ ] Сохранение паспорта при недоступном ИВК (таймаут polling)
- [ ] Сохранение паспорта при недоступном TN_MessagingService
- [ ] Объединение данных из localStorage
- [ ] Вызов UpdateDoc после успешного polling

#### Акт приема-сдачи
- [ ] Сохранение акта при работающем ИВК
- [ ] Сохранение акта при недоступном TN_MessagingService
- [ ] Запись в тег без ожидания

#### Другие документы (Report, Jornal)
- [ ] Сохранение без OPC логики
- [ ] Отсутствие запросов к TN_MessagingService

---

## Дополнительные замечания

### localStorage для паспортов

В старой логике используется `localStorage.getItem('dataPassport')` для объединения дополнительных данных протокола испытаний.

**Формат данных:**
```javascript
{
  // Дополнительные данные протокола ELIS или других источников
  "additionalField1": "value1",
  "additionalField2": "value2"
}
```

**Когда записывается:**
- При загрузке данных из ELIS
- При выборе методов испытаний

**Рекомендация:** Убедитесь, что эти данные корректно записываются в localStorage в новой реализации.

---

### Конфигурация TN_MessagingService

По умолчанию OPC API клиент использует:
- **Базовый URL:** `http://localhost:5010`
- **Endpoint:** `/api/Values`

Если TN_MessagingService работает на другом хосте/порту, передайте базовый URL в конструктор:

```typescript
// В opc.service.ts
export const opcApi = new OpcApiService('http://192.168.1.100:5010');
```

---

## Резюме изменений

✅ **Создано:**
1. `opc.service.ts` - API клиент для работы с OPC тегами
2. `usePassportSave.ts` - Композабл для сохранения с OPC логикой

✅ **Изменено:**
1. `api.service.ts` - добавлен метод `updateDocument()`
2. `document.types.ts` - добавлены поля `deviceGuid`, `deviceName`, `tagPrefix`
3. `useDocumentEditor.ts` - интегрирован `usePassportSave`

🔄 **Требуется на бэкенде:**
1. Реализовать endpoint `POST /api/documents/{deviceId}/{docType}/update/{id}`
2. Добавить OPC параметры в `GetEditConfig` ответ
3. Настроить CORS для TN_MessagingService (если требуется)

---

## Контакты и поддержка

При возникновении вопросов обращайтесь к документации:
- `CLAUDE.md` - общая информация о проекте
- `README.md` - инструкции по запуску
- `TN_Doc/changes.md` - история изменений
