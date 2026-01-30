# Добавление нового модуля документа

Ниже — минимальный рабочий маршрут для добавления нового модуля документа.

## 1. Создайте проект модуля

- Разместите новый проект в `tn.docgeneral/<ModuleName>/`.
- Цель: `net8.0`.
- Добавьте ссылку на `tn.docgeneral/TN.DocGeneral/TN.DocGeneral.csproj`.

## 2. Реализуйте класс модуля

Модуль должен наследоваться от `DocGeneral` и иметь конструктор с ожидаемой сигнатурой:

```csharp
public class NewDocModule : DocGeneral
{
    public NewDocModule(DbContextOptions<DocGeneral> options,
        IAppConfigService appConfig,
        IConfigurationCacheService configCache,
        int idDevice,
        IdDoc idDoc,
        string path)
        : base(options, appConfig, configCache, idDevice, idDoc, path) { }

    public override object GetViewDoc(int id)
    {
        // Извлечение данных и подготовка JSON
        return new { };
    }

    public override string GetEditDoc(int id)
    {
        // HTML форма (если используется)
        return string.Empty;
    }
}
```

> Если модуль поддерживает сохранение/обновление данных — используйте `IDocUpdater` (по аналогии с существующими модулями).

## 3. Добавьте шаблон FastReport

- Поместите `.frx` в `TN_Doc/Doc/<DocType>/`.
- Укажите путь к шаблону в конфигурации (`PathToDocTemplateFile`).

## 4. Создайте конфиги документа

Минимальный набор:

- `TN_Doc/Cfg/Cfg<DocType>.json`
- `TN_Doc/Cfg/CfgEdit<DocType>.json` (если нужна форма редактирования)

## 5. Зарегистрируйте модуль в `CfgApp.json`

Добавьте документ в список `Devices[*].Docs`:

```json
{
  "Use": true,
  "IdDoc": 999,
  "Name": "Новый документ",
  "Description": "",
  "PathToDocDll": "/Dll/NewDoc.dll",
  "PathToDocConfigFile": "/Cfg/CfgNewDoc.json",
  "PathToDocEditConfigFile": "/Cfg/CfgEditNewDoc.json",
  "LastUsedTemplateId": 0,
  "TemplateDocs": [
    {
      "Use": true,
      "Id": 0,
      "Name": "Основной шаблон",
      "Description": "",
      "PathToDocTemplateFile": "/Doc/NewDoc/NewDoc.frx",
      "PathToDocEditConfigFile": ""
    }
  ]
}
```

> `PathToDocDll` и пути к шаблонам задаются относительно корня приложения.

## 6. Сборка и размещение DLL

- Соберите модуль и разместите DLL в `TN_Doc/Dll/` (или в `/opt/TN_Doc/Dll/` для Linux-пакета).
- Запустите приложение и проверьте генерацию документа.

## См. также

- [FastReport Templates Guide](fastreport-templates.md)
- [Архитектура модулей документов](../architecture/document-modules.md)
