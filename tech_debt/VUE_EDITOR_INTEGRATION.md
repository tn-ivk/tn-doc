# Vue Document Editor - Интеграция с TN_Doc

## 📋 Обзор

Этот документ описывает интеграцию Vue Document Editor с существующей системой TN_Doc.

## ✅ Что реализовано

### 1. Feature Flag система

**Файл конфигурации:** `TN_Doc/Cfg/CfgApp.json`

Добавлен новый флаг:
```json
{
  "UseVueDocumentEditor": false
}
```

**Модель:** `tn.docgeneral/TN.DocGeneral/Cfg/CfgApp.cs`

Добавлено свойство:
```csharp
/// <summary>
/// Флаг использования Vue Document Editor вместо серверной генерации HTML
/// </summary>
public bool UseVueDocumentEditor { get; set; } = false;
```

### 2. Обновленный HomeController

**Файл:** `TN_Doc/Controllers/HomeController.cs`

**Метод GetDocEdit:**
- Изменен тип возвращаемого значения с `string` на `IActionResult`
- Добавлена проверка флага `UseVueDocumentEditor`
- Возвращает JSON с URL для Vue SPA, если флаг включен
- Возвращает HTML контент для старого подхода, если флаг выключен

**Новый метод IsDocumentSupportedInVueEditor:**
- Определяет, поддерживается ли документ в Vue Editor
- На данный момент поддерживается только `IdDoc.Report`

```csharp
[HttpGet]
public IActionResult GetDocEdit(int IdDevice, IdDoc IdDoc, int id)
{
    // Проверяем флаг использования Vue Document Editor
    if (_cfgApp.UseVueDocumentEditor && IsDocumentSupportedInVueEditor(IdDoc))
    {
        var vueUrl = $"/document-editor/edit/{IdDevice}/{IdDoc}/{id}";
        return Json(new { useVue = true, url = vueUrl });
    }

    // Старый подход - генерация HTML на сервере
    var htmlContent = doc.GetEditDoc(id);
    return Content(htmlContent, "text/html");
}

private bool IsDocumentSupportedInVueEditor(IdDoc idDoc)
{
    return idDoc switch
    {
        IdDoc.Report => true,
        // IdDoc.Jornal => true,  // TODO: будет добавлено позже
        // IdDoc.Act => true,     // TODO: будет добавлено позже
        _ => false
    };
}
```

### 3. Обновленный JavaScript

**Файл:** `TN_Doc/wwwroot/js/Common.js`

**Функция GetEditDoc:**
- Изменен `dataType` на `'json'` для ожидания JSON ответа
- Добавлена обработка JSON ответа с флагом `useVue`
- Сохранена обратная совместимость через обработчик ошибок для HTML ответов

```javascript
function GetEditDoc() {
    $.ajax({
        async: false,
        url: 'Home/GetDocEdit',
        type: 'GET',
        dataType: 'json',
        data: {
            IdDevice: $('#ComboboxDevice').val(),
            IdDoc: $('#ComboboxDocGUID').val(),
            id: currentId
        },
        success: function (response) {
            if (response && response.useVue === true && response.url) {
                // Используем Vue Document Editor
                $('.FR').attr('src', response.url);
            }
        },
        error: function(xhr) {
            // Обработка старого HTML подхода
            var htmlContent = xhr.responseText;
            const blob = new Blob([htmlContent], { type: 'text/html;charset=utf-8' });
            const blobUrl = URL.createObjectURL(blob);
            $('.FR').attr('src', blobUrl);
        }
    });
}
```

## 🚀 Как использовать

### Включение Vue Document Editor

1. Откройте файл `TN_Doc/Cfg/CfgApp.json`
2. Найдите параметр `UseVueDocumentEditor`
3. Измените значение на `true`:
   ```json
   {
     "UseVueDocumentEditor": true
   }
   ```
4. Перезапустите приложение

### Выключение Vue Document Editor

1. Откройте файл `TN_Doc/Cfg/CfgApp.json`
2. Измените значение `UseVueDocumentEditor` на `false`
3. Перезапустите приложение

## 📝 Поддерживаемые документы

На данный момент Vue Document Editor поддерживает только:

- ✅ **Report** (Отчеты)

В будущем будут добавлены:
- ⬜ Jornal (Журналы)
- ⬜ Act (Акты)
- ⬜ Passport (Паспорта качества)
- ⬜ Poverka* (Поверки)
- ⬜ KMH* (КМХ)

## 🔄 Архитектурный поток

```
┌──────────────────────┐
│   User clicks Edit   │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────────────┐
│   JavaScript GetEditDoc()    │
└──────────┬───────────────────┘
           │
           │ AJAX GET Home/GetDocEdit
           ▼
┌──────────────────────────────┐
│   HomeController.GetDocEdit  │
└──────────┬───────────────────┘
           │
           ├─► UseVueDocumentEditor = true?
           │   ├─► Yes ─► IsDocumentSupportedInVueEditor?
           │   │          ├─► Yes ─► Return JSON { useVue: true, url: "/document-editor/..." }
           │   │          └─► No  ─► Return HTML (старый подход)
           │   └─► No ──► Return HTML (старый подход)
           │
           ▼
┌──────────────────────────────┐
│   JavaScript обрабатывает    │
│   ответ и загружает в iframe │
└──────────────────────────────┘
           │
           ├─► JSON с useVue = true?
           │   ├─► Yes ─► Загрузить Vue SPA в iframe
           │   └─► No  ─► Создать Blob URL из HTML и загрузить в iframe
           │
           ▼
┌──────────────────────────────┐
│   Iframe показывает редактор │
│   (Vue SPA или старый HTML)  │
└──────────────────────────────┘
```

## 🧪 Тестирование

### Тест 1: Проверка с выключенным флагом

1. Убедитесь, что `UseVueDocumentEditor = false` в `CfgApp.json`
2. Запустите приложение
3. Выберите документ Report и нажмите "Редактирование"
4. **Ожидаемый результат:** Загружается старый HTML редактор через Blob URL

### Тест 2: Проверка с включенным флагом

1. Установите `UseVueDocumentEditor = true` в `CfgApp.json`
2. Перезапустите приложение
3. Выберите документ Report и нажмите "Редактирование"
4. **Ожидаемый результат:** Загружается Vue SPA редактор по URL `/document-editor/edit/{deviceId}/Report/{id}`

### Тест 3: Проверка неподдерживаемого документа

1. Установите `UseVueDocumentEditor = true` в `CfgApp.json`
2. Выберите документ, который НЕ поддерживается Vue Editor (например, Passport)
3. Нажмите "Редактирование"
4. **Ожидаемый результат:** Загружается старый HTML редактор (fallback)

## 🔧 Добавление поддержки нового документа

Чтобы добавить поддержку нового типа документа в Vue Editor:

1. **Реализуйте IDocumentEditor** в модуле документа:
   ```csharp
   public class DocYourDocument : IDocumentEditor
   {
       public DocumentEditConfig GetEditConfig(int id) { ... }
   }
   ```

2. **Добавьте в IsDocumentSupportedInVueEditor:**
   ```csharp
   private bool IsDocumentSupportedInVueEditor(IdDoc idDoc)
   {
       return idDoc switch
       {
           IdDoc.Report => true,
           IdDoc.YourDocument => true,  // Добавьте сюда
           _ => false
       };
   }
   ```

3. **Обновите Vue Router** в `document-editor/src/router/index.ts`

4. **Протестируйте** новый документ

## ⚠️ Важные замечания

1. **Обратная совместимость:** Интеграция полностью обратно совместима. При `UseVueDocumentEditor = false` система работает как раньше.

2. **Production build:** Убедитесь, что Vue компоненты собраны для production перед развертыванием:
   ```bash
   cd TN_Doc/Client
   npm run build:editor
   ```

3. **Перезапуск после изменения флага:** Изменение `UseVueDocumentEditor` требует перезапуска приложения.

4. **Логирование:** Все переключения между редакторами логируются с уровнем `LogInformation` в HomeController.

## 📊 Статус Stage 1

- ✅ Feature flag система
- ✅ Обновленный HomeController
- ✅ Обновленный JavaScript
- ✅ Документация
- ⏳ Тестирование с реальными данными (в процессе)
- ⬜ UI переключатель (опционально, можно добавить позже)

## 🔗 Связанные документы

- [DOCUMENT_EDITOR_POC.md](DOCUMENT_EDITOR_POC.md) - Proof of Concept документация
- [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md) - Статус реализации
- [TN_Doc/Client/document-editor/README.md](../TN_Doc/Client/document-editor/README.md) - Frontend README
