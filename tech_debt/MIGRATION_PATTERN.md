# Паттерн миграции документов на Vue Editor

Этот документ описывает пошаговый процесс добавления поддержки Vue редактора для нового типа документа.

## Обзор

Vue Document Editor - это универсальный компонент, который работает с любым типом документа через интерфейс `IDocumentEditor`. Чтобы добавить поддержку нового типа документа, необходимо:

1. Реализовать интерфейс `IDocumentEditor` в классе документа
2. Проверить, что `DocumentEditController` поддерживает новый тип (обычно не требуется)

## Шаг 1: Добавить интерфейс IDocumentEditor

### 1.1. Добавить using

В файле класса документа (например, `DocAct.cs`, `DocJornal.cs`):

```csharp
using TN_DocGeneral.Interfaces;
```

### 1.2. Реализовать интерфейс

```csharp
public class DocYourDocument : DocGeneral, IDocumentEditor
```

## Шаг 2: Реализовать метод GetEditConfig

Это основной метод, который возвращает конфигурацию формы редактирования.

```csharp
/// <summary>
/// Получить конфигурацию формы редактирования для Vue компонента
/// </summary>
public DocumentEditConfig GetEditConfig(int id)
{
    _logger.Trace($"Получение конфигурации редактирования документа {IdDoc} с идентификатором {id}");
    GetViewDoc(id); // Загружаем данные документа

    try
    {
        var config = new DocumentEditConfig
        {
            DocId = id,
            DocType = "YourDocType", // Имя типа документа
            DeviceId = _deviceId,
            Fields = BuildFieldsFromConfig(),
            InitialValues = ExtractInitialValues()
        };

        _logger.Trace($"Конфигурация редактирования документа {IdDoc} (id={id}) создана с {config.Fields.Count} полями");
        return config;
    }
    catch (Exception ex)
    {
        _logger.Error(ex, $"Ошибка при создании конфигурации редактирования документа {IdDoc} с ID {id}: {ex.Message}");
        throw;
    }
}
```

## Шаг 3: Реализовать BuildFieldsFromConfig

Метод строит список полей формы на основе конфигурации.

### Пример 1: Простые поля (текст + select)

```csharp
private List<FormField> BuildFieldsFromConfig()
{
    var fields = new List<FormField>();
    var editDoc = LoadCfg<CfgEdit>(PathToDocEditConfigFile);

    foreach (var item in editDoc.AdditionalInfo.Where(x => x.Use).ToList())
    {
        var field = new FormField
        {
            Key = item.Key,
            Label = item.Name,
            Type = MapFieldType(item.Type), // "text" или "select"
            Required = item.RequiredFill,
            Editable = item.Edit,
            Tag = "AdditionalInfo"
        };

        // Для list полей добавляем опции
        if (item.Type == "list")
        {
            field.Options = GetUserOptionsForField(item.Key);
        }

        fields.Add(field);
    }

    return fields;
}

private string MapFieldType(string configType)
{
    return configType switch
    {
        "list" => "select",
        "text" => "text",
        _ => "text"
    };
}
```

### Пример 2: Только select поля (как в Jornal)

```csharp
private List<FormField> BuildFieldsFromConfig()
{
    var fields = new List<FormField>();
    var editDoc = LoadCfg<CfgEdit>(PathToDocEditConfigFile);

    foreach (var item in editDoc.AdditionalInfo.Where(x => x.Use).ToList())
    {
        var field = new FormField
        {
            Key = item.Key,
            Label = item.Name,
            Type = "select",
            Required = false,
            Editable = item.Edit,
            Tag = "AdditionalInfo",
            Options = GetUserOptionsForField(item.Key)
        };

        fields.Add(field);
    }

    return fields;
}
```

## Шаг 4: Реализовать GetUserOptionsForField

Метод возвращает список опций для select полей.

### Пример 1: Разные группы пользователей (Act)

```csharp
private List<SelectOption> GetUserOptionsForField(string fieldKey)
{
    var options = new List<SelectOption>
    {
        new SelectOption { Value = "", Label = "" } // Пустая опция
    };

    var users = fieldKey switch
    {
        "Delive_IOF" => Doc.Doc?.Settings?.Dictionarys?.Users?.Where(x => x.IdGroup == 2)?.ToList() ?? new List<Users>(),
        "Receive_IOF" => Doc.Doc?.Settings?.Dictionarys?.Users?.Where(x => x.IdGroup == 3)?.ToList() ?? new List<Users>(),
        _ => new List<Users>()
    };

    options.AddRange(users.Select(user => new SelectOption
    {
        Value = user.Id.ToString(),
        Label = user.IOF
    }));

    return options;
}
```

### Пример 2: Множественные поля одной группы (Jornal)

```csharp
private List<SelectOption> GetUserOptionsForField(string fieldKey)
{
    var options = new List<SelectOption>
    {
        new SelectOption { Value = "", Label = "" }
    };

    var users = fieldKey switch
    {
        "DeliveryIOF1" or "DeliveryIOF2" => Doc.Doc?.Settings?.Dictionarys?.Users?.Where(x => x.IdGroup == 2)?.ToList() ?? new List<Users>(),
        "ReceiveIOF1" or "ReceiveIOF2" => Doc.Doc?.Settings?.Dictionarys?.Users?.Where(x => x.IdGroup == 3)?.ToList() ?? new List<Users>(),
        _ => new List<Users>()
    };

    options.AddRange(users.Select(user => new SelectOption
    {
        Value = user.Id.ToString(),
        Label = user.IOF
    }));

    return options;
}
```

## Шаг 5: Реализовать ExtractInitialValues

Метод извлекает текущие значения полей из документа.

### Пример 1: Текстовые поля + пользователи (Act)

```csharp
private Dictionary<string, object> ExtractInitialValues()
{
    var additionalInfo = ((DataIVKDoc)Doc.Doc.DataIVK).TableResultActAndPassport.AdditionalInfo ?? new AdditionalInfo();
    var values = new Dictionary<string, object>();

    // Текстовые поля - напрямую
    if (!string.IsNullOrEmpty(additionalInfo.ActNumber))
        values["ActNumber"] = additionalInfo.ActNumber;
    if (!string.IsNullOrEmpty(additionalInfo.DelivePoint))
        values["DelivePoint"] = additionalInfo.DelivePoint;
    if (!string.IsNullOrEmpty(additionalInfo.Factory))
        values["Factory"] = additionalInfo.Factory;
    if (!string.IsNullOrEmpty(additionalInfo.SIKN_Number))
        values["SIKN_Number"] = additionalInfo.SIKN_Number;

    // Поля пользователей - находим ID по ФИО
    if (!string.IsNullOrEmpty(additionalInfo.Delive_IOF))
    {
        var user = Doc.Doc?.Settings?.Dictionarys?.Users?.FirstOrDefault(x => x.IOF == additionalInfo.Delive_IOF);
        if (user != null)
            values["Delive_IOF"] = user.Id.ToString();
    }

    if (!string.IsNullOrEmpty(additionalInfo.Receive_IOF))
    {
        var user = Doc.Doc?.Settings?.Dictionarys?.Users?.FirstOrDefault(x => x.IOF == additionalInfo.Receive_IOF);
        if (user != null)
            values["Receive_IOF"] = user.Id.ToString();
    }

    return values;
}
```

### Пример 2: Только пользователи (Jornal, Report)

```csharp
private Dictionary<string, object> ExtractInitialValues()
{
    var dataArm = ((DataIVKDoc)Doc.Doc.DataIVK).TableMeasurementJornal.DataARM ?? new DataARM { AdditionalData = new AdditionalData() };
    var values = new Dictionary<string, object>();

    // Находим ID пользователей по их ФИО
    if (!string.IsNullOrEmpty(dataArm.AdditionalData.DeliveryIOF1))
    {
        var user = Doc.Doc?.Settings?.Dictionarys?.Users?.FirstOrDefault(x => x.IOF == dataArm.AdditionalData.DeliveryIOF1);
        if (user != null)
            values["DeliveryIOF1"] = user.Id.ToString();
    }

    // Повторить для всех полей пользователей...

    return values;
}
```

## Шаг 6: Проверка конфигурационных файлов

### 6.1. Структура CfgEdit{DocumentType}.json

Убедитесь, что существует конфигурационный файл в `/TN_Doc/Cfg/{DocumentType}/CfgEdit{DocumentType}.json` или `/TN_Doc/Cfg/CfgEdit{DocumentType}.json`:

```json
{
  "AdditionalInfo": [
    {
      "Id": 1,
      "Use": true,
      "Edit": true,
      "Key": "FieldKey",
      "Type": "text",  // или "list"
      "Name": "Отображаемое имя поля",
      "RequiredFill": false
    }
  ]
}
```

### 6.2. Типы полей

- `"text"` - текстовое поле (InputText в PrimeVue)
- `"list"` - выпадающий список (Select в PrimeVue)

## Шаг 7: Проверка работы контроллера

`DocumentEditController` уже универсален и не требует изменений. Он автоматически работает с любым документом, реализующим `IDocumentEditor`.

### Проверка маршрутизации

Убедитесь, что тип документа правильно определён в `IdDoc` enum:

```csharp
public enum IdDoc
{
    Report = 1,
    Act = 2,
    Jornal = 3,
    Passport = 4,
    // ... другие типы
}
```

## Шаг 8: Сборка и тестирование

### 8.1. Компиляция backend

```bash
dotnet build
```

### 8.2. Сборка frontend

```bash
cd TN_Doc/Client
npm run build:editor
```

### 8.3. Тестирование через API

```bash
# Health check
curl http://localhost:5000/api/documents/health

# Получить конфигурацию редактирования
curl http://localhost:5000/api/documents/1/YourDocType/edit/123

# Сохранить документ
curl -X POST http://localhost:5000/api/documents/1/YourDocType/save/123 \
  -H "Content-Type: application/json" \
  -d '{"FieldKey": "NewValue"}'
```

## Примеры реализаций

### 1. Report (простой случай)

- **Поля**: Только select для выбора подписантов
- **Конфигурация**: На основе `Signers` в `FooterDoc`
- **Данные**: `DataARM.AdditionalData`

**Файл**: `/tn.docgeneral/Report/DocReport.cs:214-328`

### 2. Act (текст + select)

- **Поля**: Текстовые поля (ActNumber, DelivePoint, Factory, SIKN_Number) + select для пользователей
- **Конфигурация**: `CfgEditAct.json` с 25 полями (только часть активна)
- **Данные**: `TableResultActAndPassport.AdditionalInfo`

**Файл**: `/tn.docgeneral/Act/DocAct.cs:425-560`

### 3. Jornal (только select)

- **Поля**: 4 select поля для выбора операторов
- **Конфигурация**: `CfgEditJornal.json`
- **Данные**: `TableMeasurementJornal.DataARM.AdditionalData`

**Файл**: `/tn.docgeneral/Jornal/DocJornal.cs:302-422`

## Общие рекомендации

### Логирование

Добавляйте логи на всех этапах:

```csharp
_logger.Trace($"Получение конфигурации редактирования документа {IdDoc} с идентификатором {id}");
_logger.Trace($"Конфигурация редактирования документа {IdDoc} (id={id}) создана с {config.Fields.Count} полями");
_logger.Error(ex, $"Ошибка при создании конфигурации редактирования документа {IdDoc} с ID {id}: {ex.Message}");
```

### Обработка ошибок

Оборачивайте код в try-catch и проверяйте nullable значения:

```csharp
var users = Doc.Doc?.Settings?.Dictionarys?.Users?.Where(x => x.IdGroup == 2)?.ToList() ?? new List<Users>();
```

### Пустые опции

Всегда добавляйте пустую опцию в начало списка select:

```csharp
var options = new List<SelectOption>
{
    new SelectOption { Value = "", Label = "" }
};
```

## Следующие шаги

После успешной реализации IDocumentEditor:

1. Обновите `IMPLEMENTATION_STATUS.md`
2. Протестируйте через dev server: `npm run dev:editor`
3. Соберите production build: `npm run build:editor`
4. Обновите документацию по необходимости

## Часто задаваемые вопросы

**Q: Нужно ли изменять DocumentEditController?**
A: Нет, контроллер универсален и работает через интерфейс IDocumentEditor.

**Q: Как добавить новый тип поля?**
A: Расширьте метод `MapFieldType()` и добавьте соответствующий компонент в `FormField.vue`.

**Q: Что делать, если структура данных сложная?**
A: Посмотрите на реализацию Report как пример. Для более сложных случаев (например, таблицы параметров в Passport) может потребоваться создание нового компонента.

**Q: Как протестировать без реальной БД?**
A: Используйте endpoint `/api/documents/health` для проверки API и моки данных в тестах.

## Контрольный список миграции

- [ ] Добавлен `using TN_DocGeneral.Interfaces;`
- [ ] Класс реализует `IDocumentEditor`
- [ ] Реализован метод `GetEditConfig(int id)`
- [ ] Реализован метод `BuildFieldsFromConfig()`
- [ ] Реализован метод `GetUserOptionsForField()` (если есть select)
- [ ] Реализован метод `MapFieldType()` (если есть разные типы)
- [ ] Реализован метод `ExtractInitialValues()`
- [ ] Проверен конфигурационный файл `CfgEdit*.json`
- [ ] Добавлено логирование
- [ ] Проверена обработка ошибок
- [ ] Проект скомпилирован без ошибок
- [ ] Frontend собран (`npm run build:editor`)
- [ ] API протестирован
- [ ] Обновлена документация

## Ссылки

- [IDocumentEditor Interface](/tn.docgeneral/TN.DocGeneral/IDocumentEditor.cs)
- [DocumentEditController](/TN_Doc/Controllers/DocumentEditController.cs)
- [FormField.vue](/TN_Doc/Client/document-editor/src/components/FormField.vue)
- [DocumentEditor.vue](/TN_Doc/Client/document-editor/src/views/DocumentEditor.vue)
- [IMPLEMENTATION_STATUS.md](/tech_debt/IMPLEMENTATION_STATUS.md)
