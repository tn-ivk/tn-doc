# План отказа от проекта `TN_Tools` и перенос функционала в `TN.DocGeneral`

## Цели
- Исключить зависимость от `TN_Tools`/`TN_ToolsFastReport` в решении.
- Перенести уникальный функционал в `TN.DocGeneral` с единообразной реализацией и тестированием.
- Обновить CI/CD и решение, удалив сборку лишних артефактов.

## Инвентаризация текущего функционала `TN_Tools`
Источник: `tn_toolsfastreport/TN_Tools/Tools.cs`

- UnixTimestampToDatetime(long) — конвертация unix timestamp → DateTime (UTC)
- GetPropertyValue(object obj, string key, ref object result) — рекурсивный поиск значения свойства по имени
- GetNormalizedValue(object value) — замена точки на запятую в строковом представлении
- Cast<T>(object myobj) — отражательный копирующий маппер по совпадающим именам свойств

Сопоставление с `TN.DocGeneral` (см. `TN.DocGeneral/General.cs`):
- UnixTimestampToDatetime(long) — УЖЕ ЕСТЬ (DocGeneral.UnixTimestampToDatetime)
- GetPropertyValue(...) — УЖЕ ЕСТЬ (DocGeneral.GetPropertyValue)
- GetNormalizedValue(...) — НЕТ прямого аналога (локальные `.Replace(',', '.')`/`.Replace('.', ',')` встречаются точечно)
- Cast<T>(...) — НЕТ аналога (используется отражение для копирования по именам свойств)

## План работ
1) Подготовка API в `TN.DocGeneral`
- Добавить методы-утилиты в `DocGeneral` или в новый статический класс-хелпер:
  - NormalizeDecimalString(string value, char to = ',') — нормализация десятичного разделителя, безопасно для null/пустых строк.
  - MapPropertiesByName<TTarget>(object source) — безопасный маппинг по именам свойств (null-checks, совместимость по типам, skip write-only/readonly). Возвращает `TTarget`.
- Покрыть методы юнит‑тестами (позитив/edge cases: null, пустые строки, несовместимые типы, вложенные типы не трогаем).

2) Поиск и замена использования `TN_Tools`
- Поиск упоминаний `TN_ToolsFastReport`/`TN_Tools` в коде.
- Заменить вызовы:
  - UnixTimestampToDatetime/ GetPropertyValue → на DocGeneral аналоги (уже применяются в проекте).
  - GetNormalizedValue → на новый `NormalizeDecimalString(...)`.
  - Cast<T> → на `MapPropertiesByName<TTarget>(...)` (или явные маппинги там, где нужно контролировать поля).

3) Обновление решения и CI/CD
- Исключить проект `tn_toolsfastreport/TN_Tools` из `.sln` и зависимостей.
- Удалить/отключить сборку `TN_ToolsFastReport` из `.gitlab-ci.yml` (если присутствует).
- Обновить инструкции сборки/публикации (README/tech_debt) при необходимости.

4) Депрекация `TN_Tools`
- Пометить репозиторий/папку как deprecated в CHANGELOG/changes.md.
- После релиза без регрессий — удалить папку `tn_toolsfastreport` из монорепозитория.

## Детализация реализации (скетчи интерфейсов)
- NormalizeDecimalString
```csharp
public static string NormalizeDecimalString(string value, char to = ',')
{
    if (string.IsNullOrEmpty(value)) return value;
    return to == '.'
        ? value.Replace(',', '.')
        : value.Replace('.', ',');
}
```

- MapPropertiesByName
```csharp
public static TTarget MapPropertiesByName<TTarget>(object source) where TTarget : new()
{
    if (source == null) return default;
    var target = new TTarget();
    var srcProps = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
    var dstProps = typeof(TTarget).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanWrite).ToDictionary(p => p.Name);

    foreach (var sp in srcProps)
    {
        if (!dstProps.TryGetValue(sp.Name, out var dp)) continue;
        var val = sp.GetValue(source);
        if (val == null || dp.PropertyType.IsAssignableFrom(sp.PropertyType))
        {
            dp.SetValue(target, val);
        }
        // Иначе — сознательно пропускаем (без неявных конвертаций)
    }
    return target;
}
```

## Риски и меры
- Непредвиденная зависимость от `TN_ToolsFastReport.dll` (шаблоны FRX/скрипты). Мера: поиск по `frx`/скриптам, smoke-тест печати.
- Изменение вывода чисел (локали): использовать `NormalizeDecimalString` только в местах форматирования строк, не в расчётах.
- Отражательный маппинг: оставить консервативный (без авто-конверсий), при необходимости — явные маппинги.

## Критерии готовности
- Нет ссылок/использования `TN_Tools` в коде/решении.
- Все функции доступны из `TN.DocGeneral`.

## Чек‑лист выполнения
- [x] Добавлены `NormalizeDecimalString` и `MapPropertiesByName` + тесты
- [x] Создан обратно-совместимый слой `TN_ToolsFastReport` в `TN.DocGeneral`
- [x] Исключён проект `TN_Tools` из `.sln` и удалены ссылки на проект
- [x] Депрекация проекта `TN_Tools` с добавлением `[Obsolete]` атрибутов
- [ ] Удаление папки `tn_toolsfastreport` (рекомендуется после тестирования)

## Статус выполнения
✅ **ВЫПОЛНЕНО**: Все основные задачи завершены. FastReport шаблоны будут работать с обратно-совместимым слоем.

### Что реализовано:
- Методы `NormalizeDecimalString` и `MapPropertiesByName` добавлены в `DocGeneral.cs:355-388`
- Полное покрытие юнит-тестами в `Tests/Services/DocGeneralTests.cs`
- Совместимый класс `TN_ToolsFastReport.Tools` в `TN.DocGeneral/TN_ToolsFastReportCompatibility.cs`
- Удалены все ссылки на `TN_Tools` из проектных файлов
- Оригинальный `TN_Tools` помечен как `[Obsolete]` для предупреждения разработчиков

### Протестированное использование:
FastReport шаблоны (.frx) продолжают работать без изменений через совместимый слой:
```csharp
TN_ToolsFastReport.Tools tools = new TN_ToolsFastReport.Tools();
tools.GetNormalizedValue(value);           // → DocGeneral.NormalizeDecimalString()
tools.UnixTimestampToDatetime(timestamp);  // → DocGeneral.UnixTimestampToDatetime()
tools.Cast<T>(obj);                        // → DocGeneral.MapPropertiesByName<T>()
tools.GetPropertyValue(obj, key, ref result); // → DocGeneral.GetPropertyValue()
```
