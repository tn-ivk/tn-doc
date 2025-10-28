# Интеграция OPC тегов в редактор документов

## Обзор

Данный документ описывает интеграцию функциональности записи OPC тегов в новый Vue-редактор документов, реализуя логику, аналогичную старой версии (`EditDoc.js`).

**Архитектурное решение:** OPC параметры передаются из главного окна через URL query params при открытии iframe с редактором, а НЕ получаются на бэкенде. Это сохраняет разделение ответственности и соответствует оригинальной архитектуре.

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

### 4. Композабл для получения OPC параметров из URL

**Файл:** `TN_Doc/Client/document-editor/src/composables/useOpcParams.ts`

**Функциональность:**
- Получает OPC параметры из URL query params
- Валидирует наличие всех обязательных параметров
- Возвращает интерфейс `OpcDeviceParams` или `null`

**Интерфейс OPC параметров:**
```typescript
export interface OpcDeviceParams {
  deviceGuid: string;  // IdDevice в виде строки
  deviceName: string;  // Имя устройства
  tagPrefix: string;   // Префикс для OPC тегов
}
```

**Пример использования:**
```typescript
const { opcParams, hasOpcParams } = useOpcParams();

// opcParams.value будет содержать параметры из URL или null
if (opcParams.value) {
  await saveDocumentWithOpc(opcParams.value);
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

## Требования к фронтенду (главное окно)

### Передача OPC параметров через URL

**Главное окно должно передавать OPC параметры при открытии iframe с редактором:**

```javascript
// Пример из старого Common.js
const deviceId = $('#ComboboxDevice').val();           // Например: 1
const deviceName = $('#ComboboxDevice :selected').text(); // Например: "ИВК-1"
const tagPrefix = 'IVK';  // Из конфигурации устройства

// Формируем URL с параметрами
const editorUrl = `/document-editor/${deviceId}/Passport/${id}?deviceGuid=${deviceId}&deviceName=${encodeURIComponent(deviceName)}&tagPrefix=${tagPrefix}`;

// Открываем iframe
document.getElementsByClassName('FR')[0].src = editorUrl;
```

**Обязательные query параметры:**
- `deviceGuid` - IdDevice в виде строки (используется для OPC запросов)
- `deviceName` - имя устройства
- `tagPrefix` - префикс тега из OPC настроек устройства

**Откуда брать `tagPrefix`:**
```javascript
// Нужно получить из конфигурации устройства
// Например, через новый API endpoint или при загрузке списка устройств
$.ajax({
    url: '/Home/GetDeviceOpcPrefix',
    data: { IdDevice: deviceId },
    success: function(tagPrefix) {
        // Используем полученный префикс
    }
});
```

---

## Требования к бэкенду

### 1. Обновление API контроллера

**Необходимо реализовать endpoint UpdateDocument** (уже реализован):

```csharp
[HttpPost("{deviceId}/{docType}/update/{id}")]
public IActionResult UpdateDocument(
    int deviceId,
    string docType,
    int id,
    [FromBody] JsonElement data)
{
    // Логика обновления документа после успешной записи в OPC тег
    // Аналогично методу SaveDoc, но для обновления существующего документа
}
```

**Отличие от `SaveDoc`:**
- `SaveDoc` - первичное сохранение (до записи в тег)
- `UpdateDoc` - финальное обновление (после подтверждения от ИВК)

---

### 2. (Опционально) Endpoint для получения OPC префикса устройства

**Для удобства главного окна можно добавить:**

```csharp
[HttpGet("device/{deviceId}/opc-prefix")]
public IActionResult GetDeviceOpcPrefix(int deviceId)
{
    var device = _appConfig.GetDeviceCfg(deviceId);
    if (device?.OpcConnectionSettings == null)
    {
        return Ok(new { tagPrefix = "" });
    }

    var prefix = device.OpcConnectionSettings.Type == OpcType.DA
        ? device.OpcConnectionSettings.DaSettings?.StartPrefix
        : device.OpcConnectionSettings.UaSettings?.StartPrefix;

    return Ok(new { tagPrefix = prefix ?? "" });
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
