# Инструкция по тестированию Document Editor

## 🚀 Подготовка к тестированию

### Шаг 1: Установка зависимостей

```bash
cd /mnt/c/dev/dotnet/ivk/tn_doc/TN_Doc/Client
npm install
```

### Шаг 2: Запуск backend

```bash
cd /mnt/c/dev/dotnet/ivk/tn_doc/TN_Doc
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

Backend будет доступен на `http://localhost:38509`

### Шаг 3: Запуск frontend dev сервера

В новом терминале:

```bash
cd /mnt/c/dev/dotnet/ivk/tn_doc/TN_Doc/Client
npm run dev:editor
```

Frontend dev сервер запустится на `http://localhost:5174`

## 🧪 Тесты API

### Тест 1: Health Check

```bash
curl http://localhost:38509/api/documents/health
```

**Ожидаемый ответ:**
```json
{
  "status": "healthy",
  "service": "DocumentEditAPI",
  "timestamp": "2025-..."
}
```

### Тест 2: Получение конфигурации Report

Вам нужно знать:
- `{deviceId}` - GUID устройства из CfgApp.json
- `{docId}` - ID документа Report в БД

```bash
curl "http://localhost:38509/api/documents/{deviceId}/Report/edit/{docId}"
```

**Пример:**
```bash
curl "http://localhost:38509/api/documents/00000000-0000-0000-0000-000000000001/Report/edit/123"
```

**Ожидаемый ответ:**
```json
{
  "docId": 123,
  "docType": "Report",
  "title": "Редактирование отчёта",
  "fields": [
    {
      "key": "SDal_IOF1",
      "label": "Сдал (ФИО) 1",
      "type": "select",
      "required": false,
      "editable": true,
      "options": [...]
    }
  ],
  "initialValues": {
    "SDal_IOF1": "12",
    ...
  },
  "deviceId": "00000000-0000-0000-0000-000000000001"
}
```

## 🌐 Тесты в браузере

### Тест 3: Открытие редактора

Откройте в браузере:
```
http://localhost:5174/edit/{deviceId}/Report/{docId}
```

**Пример:**
```
http://localhost:5174/edit/00000000-0000-0000-0000-000000000001/Report/123
```

**Что должно отображаться:**

1. ✅ Заголовок "Редактирование отчёта"
2. ✅ Поля формы с выпадающими списками пользователей
3. ✅ Кнопка "Сохранить" (должна быть disabled, пока нет изменений)
4. ✅ Кнопка "Отмена"

### Тест 4: Редактирование и сохранение

1. Выберите пользователя из одного из выпадающих списков
2. Проверьте, что:
   - ✅ Появился badge "Несохранённые изменения"
   - ✅ Кнопка "Сохранить" стала активной
3. Нажмите "Сохранить"
4. Проверьте:
   - ✅ Появился alert "Документ успешно сохранён"
   - ✅ Badge "Несохранённые изменения" исчез
   - ✅ Кнопка "Сохранить" снова disabled

### Тест 5: Предупреждение о несохранённых изменениях

1. Измените какое-либо поле
2. Попробуйте закрыть вкладку браузера
3. Проверьте:
   - ✅ Браузер показывает предупреждение о несохранённых изменениях

### Тест 6: Отмена изменений

1. Измените какое-либо поле
2. Нажмите кнопку "Отмена"
3. Проверьте:
   - ✅ Появился confirm "У вас есть несохранённые изменения..."
   - ✅ При подтверждении страница закрывается/возврат назад

## 🔧 Отладка

### Логи в браузере

Откройте DevTools (F12) → Console

Вы должны видеть логи:
```
[API Request] GET /api/documents/...
[API Response] /api/documents/... {...}
[DocumentStore] Конфигурация загружена: {...}
[DocumentStore] Поле "SDal_IOF1" обновлено: "15"
[DocumentStore] Документ успешно сохранён
```

### Логи на backend

Проверьте терминал с `dotnet run`, должны быть:
```
[TRACE] API запрос конфигурации редактирования: deviceId=..., docType=Report, id=123
[INFO] Конфигурация редактирования успешно получена: Report (id=123)
[TRACE] API запрос сохранения документа: deviceId=..., docType=Report, id=123
[INFO] Документ успешно сохранён: Report (id=123)
```

## 📊 Production Build Test

### Шаг 1: Сборка

```bash
cd /mnt/c/dev/dotnet/ivk/tn_doc/TN_Doc/Client
npm run build:editor
```

Проверьте, что файлы созданы в `TN_Doc/wwwroot/document-editor/`

### Шаг 2: Тест через ASP.NET Core

1. Остановите dev сервер Vite (если запущен)
2. Убедитесь, что backend работает: `dotnet run`
3. Откройте в браузере:
   ```
   http://localhost:38509/document-editor/edit/{deviceId}/Report/{docId}
   ```

4. Проверьте, что приложение работает как в dev режиме

## ❌ Возможные проблемы

### "Device with GUID ... not found"

- Проверьте, что GUID устройства существует в CfgApp.json
- Убедитесь, что передан правильный GUID

### "Document type 'Report' does not support Vue editor"

- Проверьте, что DocReport реализует IDocumentEditor
- Пересоберите backend: `dotnet build`

### "Failed to create document instance"

- Проверьте, что библиотека DocReport скомпилирована
- Проверьте логи NLog для подробностей

### Frontend не загружается

- Проверьте, что dev сервер запущен на порту 5174
- Проверьте консоль браузера на наличие ошибок
- Убедитесь, что все зависимости установлены: `npm install`

### CORS ошибки

- В dev режиме Vite проксирует запросы на backend
- В production режиме проверьте CORS настройки в Startup.cs

## ✅ Чеклист успешного тестирования

- [ ] Health check API возвращает "healthy"
- [ ] GET конфигурации возвращает корректный JSON
- [ ] Редактор загружается в браузере
- [ ] Поля формы отображаются корректно
- [ ] Выпадающие списки заполнены данными пользователей
- [ ] Изменения полей отслеживаются (badge "Несохранённые изменения")
- [ ] Сохранение работает (появляется alert об успехе)
- [ ] Валидация обязательных полей работает
- [ ] Предупреждение при закрытии с несохранёнными изменениями
- [ ] Production build работает корректно
- [ ] Логи в консоли браузера и backend информативны

## 🎓 Следующие шаги после успешного тестирования

1. Интегрировать с существующим iframe в Index.cshtml
2. Добавить обработку результата сохранения (Toast вместо alert)
3. Написать unit тесты для компонентов
4. Добавить E2E тесты с Playwright
5. Реализовать автосохранение (debounce)
6. Добавить горячие клавиши (Ctrl+S)
