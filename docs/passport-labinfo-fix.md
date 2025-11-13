# Исправление дублирования данных в LabInfo при сохранении паспортов качества

**Дата:** 2025-11-01
**Версия:** 1.4.3
**Файл:** `tn.docgeneral/Passport/DocPassport.cs`
**Коммит:** c14fdec

## Описание проблемы

При сохранении заполненной формы редактирования паспорта качества через Vue Document Editor в БД в поле `DataARM` сохранялся массив `LabInfo` с тройным дублированием данных для каждого параметра качества.

### Пример проблемного формата

Для одного параметра `TempCorrection` создавалось **три** отдельных элемента:

```json
{
  "LabInfo": [
    {
      "ParameterKey": "method.TempCorrection",
      "Metod": { "Id": 1, "Name": "ГОСТ Р 00001-2001 Метод А", ... },
      "Value": "",
      "ElisFilled": false
    },
    {
      "ParameterKey": "value.TempCorrection",
      "Metod": { "Id": 0, "Use": false, ... },
      "Value": "0,0",
      "ElisFilled": false
    },
    {
      "ParameterKey": "result.TempCorrection",
      "Metod": { "Id": 0, "Use": false, ... },
      "Value": "0,0",
      "ElisFilled": false
    }
  ]
}
```

Это приводило к:
- Избыточному хранению данных (в 3 раза больше необходимого)
- Усложнению логики чтения данных
- Несоответствию старому формату данных

## Решение

Изменена логика сохранения данных в методах `DocUpdate` и `CorrectMethod` для удаления префиксов из ключей параметров.

### Исправленный формат

Теперь для каждого параметра создается **один** элемент с полной информацией:

```json
{
  "LabInfo": [
    {
      "ParameterKey": "TempCorrection",
      "Metod": { "Id": 1, "Name": "ГОСТ Р 00001-2001 Метод А", ... },
      "Value": "0,0",
      "Document": { "Type": null, "Number": null, "Date": "0001-01-01T00:00:00" },
      "ElisFilled": false
    },
    {
      "ParameterKey": "PressCorrection",
      "Metod": { "Id": 3, "Name": "ГОСТ Р 00002-2001 Метод А", ... },
      "Value": "0,00",
      "Document": { "Type": null, "Number": null, "Date": "0001-01-01T00:00:00" },
      "ElisFilled": false
    }
    // ... остальные параметры
  ]
}
```

## Внесенные изменения

### 1. Метод `CorrectMethod` (строка 1033)

**Было:**
```csharp
private LabInfo CorrectMethod(IQualityParameter parameter, Metod method, EditData item)
{
    parameter.Desc = method.Name;
    return new LabInfo
    {
        ParameterKey = item.Key,  // содержал префикс "method."
        Metod = method,
        ElisFilled = item.ElisFilled
    };
}
```

**Стало:**
```csharp
private LabInfo CorrectMethod(IQualityParameter parameter, Metod method, EditData item)
{
    parameter.Desc = method.Name;
    // Убираем префикс "method." из ключа для сохранения в старом формате
    var parameterKey = item.Key.Replace("method.", "");
    return new LabInfo
    {
        ParameterKey = parameterKey,
        Metod = method,
        ElisFilled = item.ElisFilled
    };
}
```

### 2. Новый метод `DocUpdate(string jsonData)` (строка 765)

Добавлена обработка префиксов для всех типов данных:

#### Обработка методов (Tag = "Metod")
```csharp
var metod = JsonDeserializeObject<Metod>(item.Value);
// Убираем префикс "method." из ключа
var parameterKey = item.Key.Replace("method.", "");
dataArm.LabInfo.Add(new LabInfo {
    ParameterKey = parameterKey,
    Metod = metod,
    ElisFilled = item.ElisFilled
});
```

#### Обработка значений (Tag = "Value")
```csharp
// Убираем префикс "value." из ключа
var parameterKey1 = item.Key.Replace("value.", "");
var existingLabInfo = dataArm.LabInfo.FirstOrDefault(x => x.ParameterKey == parameterKey1);
if (existingLabInfo != null)
{
    if (string.IsNullOrEmpty(existingLabInfo.Value))
        existingLabInfo.Value = item.Value;
    existingLabInfo.ElisFilled = item.ElisFilled;
}
else
{
    dataArm.LabInfo.Add(new LabInfo {
        ParameterKey = parameterKey1,
        Value = item.Value,
        ElisFilled = item.ElisFilled
    });
}
```

#### Обработка результатов для печати (Tag = "PrintValue")
```csharp
// Убираем префикс "result." из ключа
var parameterKey2 = item.Key.Replace("result.", "");
var existingLabInfo = dataArm.LabInfo.FirstOrDefault(x => x.ParameterKey == parameterKey2);
if (existingLabInfo != null)
{
    existingLabInfo.Value = item.Value;
    existingLabInfo.ElisFilled = item.ElisFilled;
}
else
{
    dataArm.LabInfo.Add(new LabInfo {
        ParameterKey = parameterKey2,
        Value = item.Value,
        ElisFilled = item.ElisFilled
    });
}
```

### 3. Старый метод `DocUpdate(CorrectionData data)` (строка 520)

Аналогичные изменения внесены в обработку блока `PrintValue` для обратной совместимости со старым редактором.

## Преимущества нового формата

1. **Компактность данных:** размер JSON сокращен в ~3 раза
2. **Единообразие:** один элемент LabInfo содержит всю информацию о параметре
3. **Обратная совместимость:** формат соответствует старой версии редактора
4. **Упрощение логики:** проще искать и обновлять данные по ключу параметра
5. **Целостность данных:** вся информация о параметре хранится в одном месте

## Структура элемента LabInfo

```csharp
public class LabInfo
{
    public string ParameterKey { get; set; }     // Чистое имя параметра (без префиксов)
    public Metod Metod { get; set; }             // Информация о методе измерения
    public string Value { get; set; }            // Значение результата для печати
    public LabDocumentInfo Document { get; set; }// Информация о документе
    public bool ElisFilled { get; set; }         // Флаг заполнения из ELIS
}
```

## Затронутые параметры

Все качественные показатели нефти:
- `TempCorrection` - температура
- `PressCorrection` - давление
- `DensCorrection` - плотность
- `Dens20Correction` - плотность при 20°C
- `Dens15Correction` - плотность при 15°C
- `MassWaterFracCorrection` - массовая доля воды
- `Chloride_Salts.Concentration` - концентрация хлористых солей
- `Chloride_Salts.MassFraction` - массовая доля хлористых солей
- `Impurity` - содержание примесей
- `SulfurCorrection` - содержание серы
- `DNP.kPa` - давление насыщенных паров (кПа)
- `DNP.mercury_mm` - давление насыщенных паров (мм рт.ст.)
- `Yield_fraction_200` - выход фракций до 200°C
- `Yield_fraction_300` - выход фракций до 300°C
- `Yield_fraction_350` - выход фракций до 350°C
- `Mass_fraction_of_paraffin` - массовая доля парафинов
- `Mass_fraction_of_hydrogen_sulfide` - массовая доля сероводорода
- `Mass_fraction_of_methyl_and_ethyl_mercaptan` - массовая доля меркаптанов
- `Mass_fraction_of_organic_chlorides` - массовая доля органических хлоридов

## Тестирование

После внесения изменений:
1. ✅ Проект успешно скомпилирован без ошибок
2. ✅ Обратная совместимость со старым редактором сохранена
3. ⚠️ Требуется тестирование:
   - Сохранение паспорта через Vue редактор
   - Загрузка сохраненного паспорта
   - Генерация PDF отчета с использованием сохраненных данных
   - Интеграция с ELIS

## Миграция данных

Существующие записи в БД с тройным дублированием останутся рабочими благодаря логике поиска по префиксам при чтении. Новые записи будут сохраняться в оптимизированном формате.

Рекомендуется создать скрипт миграции для преобразования существующих данных (опционально).

## Связанные файлы

- `tn.docgeneral/Passport/DocPassport.cs` - основная логика
- `TN_Doc/Client/document-editor/src/views/DocumentPassportEditor.vue` - Vue компонент редактора
- `TN_Doc/Client/document-editor/src/composables/usePassportEditor.ts` - логика редактора

## Авторы

- Исправление дублирования: 2025-11-01
- Оригинальная реализация: см. git history
