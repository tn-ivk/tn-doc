# Исправление дублирования данных в LabInfo при сохранении паспортов качества

**Дата:** 2025-11-01
**Версия:** 1.4.3
**Файл:** `tn.docgeneral/Passport/DocPassport.cs`
**Коммит:** faef13a (в основном репозитории), 3ea5af9 (финальное исправление)
**Статус:** ✅ Исправлено и протестировано

---

## Краткое резюме

**Проблема:** При сохранении паспорта качества через Document Editor для каждого параметра создавалось 3 дубликата в массиве `LabInfo` с префиксами `method.*`, `value.*`, `result.*`.

**Решение:** Добавлен вызов `.Replace()` для удаления префиксов перед сохранением в БД - минимальные изменения кода (3 строки), полная обратная совместимость.

**Результат:**
- ✅ Размер JSON в БД сократился в ~3 раза
- ✅ Один элемент LabInfo содержит всю информацию о параметре
- ✅ Протестировано на реальных данных (18 параметров качества)
- ✅ Работает как со старым, так и с новым редактором
- ✅ Критически важно для системы истории изменений (v1.4.4)

**Методы изменены:**
- `CorrectMethod()` (строка 599)
- `DocUpdate(string jsonData, QualityPassport? elisProtocol)` (строка 244)
- `SaveDocument()` (строка 892)

---

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

### 1. Метод `CorrectMethod` (строка 599)

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

### 2. Метод `DocUpdate(string jsonData, QualityPassport? elisProtocol)` (строка 244)

Добавлена обработка префиксов для всех типов данных. Метод также поддерживает сохранение полного ELIS протокола и системы истории изменений (v1.4.4):

#### Обработка методов (Tag = "Metod")
```csharp
var metod = JsonDeserializeObject<Metod>(item.Value);

// Убираем префикс "method." из ключа для сохранения в старом формате
var parameterKey = item.Key.Replace("method.", "");
var existingLabInfo = dataArm.LabInfo.FirstOrDefault(x => x.ParameterKey == parameterKey);

if (existingLabInfo != null)
{
    // Обновляем существующий метод
    existingLabInfo.Metod = metod;
}
else
{
    // Создаём новую запись
    existingLabInfo = new LabInfo { ParameterKey = parameterKey, Metod = metod };
    dataArm.LabInfo.Add(existingLabInfo);
}

// Добавляем записи истории ТОЛЬКО если ELIS включен
if (isElisUsed && item.History != null && item.History.Count > 0)
{
    foreach (var historyEntry in item.History)
    {
        dataArm.AddFieldHistoryEntry(item.Key, historyEntry);
    }
}
```

#### Обработка значений (Tag = "Value")
```csharp
// Убираем префикс "value." из ключа
var parameterKey1 = item.Key.Replace("value.", "");
var existingLabInfo = dataArm.LabInfo.FirstOrDefault(x => x.ParameterKey == parameterKey1);

if (existingLabInfo != null)
{
    if (string.IsNullOrEmpty(existingLabInfo.Value))
    {
        existingLabInfo.Value = item.Value;
    }
}
else
{
    existingLabInfo = new LabInfo
    {
        ParameterKey = parameterKey1,
        Value = item.Value
    };
    dataArm.LabInfo.Add(existingLabInfo);
}

// Добавляем записи истории ТОЛЬКО если ELIS включен
if (isElisUsed && item.History != null && item.History.Count > 0)
{
    foreach (var historyEntry in item.History)
    {
        dataArm.AddFieldHistoryEntry(item.Key, historyEntry);
    }
}

// Пересчитываем ElisFilled на основе последнего источника ТОЛЬКО если ELIS включен
if (isElisUsed)
{
    existingLabInfo.ElisFilled = dataArm.GetLastSourceForControl(item.Key) == DataSource.ELIS;
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

### 3. Метод `SaveDocument` (старый редактор, строка 892)

Аналогичные изменения внесены в обработку блока `PrintValue` для обратной совместимости со старым редактором:

```csharp
// Убираем префикс "result." из ключа
var parameterKey4 = item.Key.Replace("result.", "");
var existingLabInfo = dataArm.LabInfo.FirstOrDefault(x => x.ParameterKey == parameterKey4);
if (existingLabInfo != null)
{
    existingLabInfo.Value = item.Value;
    existingLabInfo.ElisFilled = item.ElisFilled;
}
else
{
    dataArm.LabInfo.Add(new LabInfo
    {
        ParameterKey = parameterKey4,
        Value = item.Value,
        ElisFilled = item.ElisFilled
    });
}
```

## Преимущества нового формата

1. **Компактность данных:** размер JSON сокращен в ~3 раза
2. **Единообразие:** один элемент LabInfo содержит всю информацию о параметре
3. **Обратная совместимость:** формат соответствует старой версии редактора
4. **Упрощение логики:** проще искать и обновлять данные по ключу параметра
5. **Целостность данных:** вся информация о параметре хранится в одном месте

## Структура элемента LabInfo

```csharp
public partial class LabInfo
{
    public string ParameterKey { get; set; } = string.Empty;     // Чистое имя параметра (без префиксов)
    public Metod Metod { get; set; } = new();                    // Информация о методе измерения
    public LabDocumentInfo Document { get; set; } = new();       // Информация о документе ELIS
    public string Value { get; set; } = string.Empty;            // Значение результата для печати
    public bool ElisFilled { get; set; } = false;                // Флаг заполнения из ELIS (вычисляется динамически в v1.4.4)
}
```

**Файл:** `tn.docgeneral/Passport/DataIVKDoc.cs`

**Важные замечания:**
- `ParameterKey` содержит **только** имя параметра без префиксов (`TempCorrection`, а не `value.TempCorrection`)
- `Value` содержит результат для печати (может быть как из ELIS `ValueString`, так и из HAL/IVK измерений)
- `ElisFilled` в v1.4.4+ вычисляется динамически на основе истории изменений через `DataARM.GetLastSourceForControl()`

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
3. ✅ **Протестировано на реальных данных (ноябрь 2025):**
   - Сохранение паспорта через Vue Document Editor работает корректно
   - Загрузка сохраненного паспорта отображает данные правильно
   - Генерация PDF отчета использует данные из единого элемента LabInfo
   - Интеграция с ELIS сохраняет данные в правильном формате
   - Проверена работа с ~18 параметрами качества
   - Размер JSON в БД сократился в ~3 раза по сравнению со старым форматом

## Миграция данных

Существующие записи в БД с тройным дублированием останутся рабочими благодаря логике поиска по префиксам при чтении. Новые записи будут сохраняться в оптимизированном формате.

**Миграция старых данных не требуется** - система автоматически обрабатывает оба формата при чтении.

## Влияние на систему истории изменений (v1.4.4)

Исправление дублирования LabInfo является **критически важным** для корректной работы системы истории изменений полей паспорта качества:

1. **Раздельное отслеживание источников данных:**
   - `value.{ParameterKey}` - история изменений значения измерения
   - `method.{ParameterKey}` - история изменений метода испытания
   - `result.{ParameterKey}` - история изменений результата для печати
   - `document.{ParameterKey}` - история изменений номера документа ELIS

2. **Единая запись LabInfo:**
   - Один элемент LabInfo содержит всю информацию о параметре
   - Флаг `ElisFilled` теперь вычисляется динамически на основе истории изменений
   - Вызов `dataArm.GetLastSourceForControl(item.Key)` определяет последний источник данных

3. **Пример работы истории:**
   ```csharp
   // Проверка последнего источника для значения
   var lastSourceForValue = dataArm.GetLastSourceForControl("value.TempCorrection");
   // Возвращает: DataSource.ELIS | DataSource.Manual | DataSource.IVK | DataSource.Unknown

   // Обновление флага ElisFilled на основе истории
   existingLabInfo.ElisFilled = lastSourceForValue == DataSource.ELIS;
   ```

4. **Обратная совместимость:**
   - При ELIS выключен (`IsUsedElis = false`) история НЕ сохраняется
   - Старый механизм с флагом `ElisFilled` продолжает работать
   - Миграция из старого флага в историю происходит автоматически при первом сохранении

**ВАЖНО:** Без исправления дублирования LabInfo система истории изменений не смогла бы корректно работать из-за конфликтов между тремя копиями данных одного параметра.

## Уроки и выводы (Lessons Learned)

### 1. Проблемы, выявленные при разработке

**Проблема:** Тройное дублирование данных возникло из-за фронтенд-логики, которая отправляла отдельные поля для `value.{Key}`, `method.{Key}` и `result.{Key}`.

**Причина:** Изначально предполагалось, что каждый префикс создаст отдельную запись LabInfo для хранения соответствующих данных.

**Решение:** Использование `.Replace()` для удаления префиксов перед сохранением позволило объединить все данные параметра в одну запись.

### 2. Важность обратной совместимости

**Решение с `.Replace()`** оказалось оптимальным:
- Минимальные изменения в коде (3 строки на метод)
- Полная обратная совместимость со старыми данными
- Не требуется миграция БД
- Работает как со старым, так и с новым редактором

**Альтернативные подходы (отклонены):**
- Изменение фронтенд-логики отправки данных - потребовало бы больших изменений
- Создание отдельных таблиц для method/value/result - избыточная сложность
- Миграция данных в БД - риск потери данных

### 3. Связь с системой истории изменений

Исправление дублирования было **необходимым условием** для внедрения системы истории изменений:

**Без исправления:**
- История для `value.TempCorrection`, `method.TempCorrection`, `result.TempCorrection` хранилась бы в трёх разных элементах LabInfo
- Невозможно отследить полную картину изменений параметра
- Конфликты при определении флага `ElisFilled`

**После исправления:**
- Один элемент LabInfo для `TempCorrection` содержит всю информацию
- История изменений хранится в `DataARM.FieldHistoryMap` с раздельными ключами (`value.*`, `method.*`, `result.*`, `document.*`)
- Флаг `ElisFilled` вычисляется динамически из истории через `GetLastSourceForControl()`

### 4. Оптимизация хранения данных

**Измеренные результаты:**
- Размер JSON в поле `DataARM` сократился **в ~3 раза**
- Для паспорта с 18 параметрами качества:
  - Старый формат: ~54 элемента LabInfo (3 × 18)
  - Новый формат: ~18 элементов LabInfo
- Упрощена логика поиска и обновления данных

### 5. Рекомендации для будущих разработок

1. **При проектировании форматов данных:**
   - Избегать дублирования информации на уровне хранения
   - Использовать префиксы только на уровне UI/API, но не в БД
   - Продумывать структуру данных с учетом будущего расширения функциональности

2. **При работе с префиксами:**
   - Документировать соглашения об именовании ключей
   - Использовать `.Replace()` для нормализации ключей перед сохранением
   - Проверять отсутствие дубликатов при загрузке данных

3. **При тестировании:**
   - Проверять реальный размер данных в БД
   - Тестировать работу с существующими данными (обратная совместимость)
   - Использовать логирование для отладки структуры данных

## Связанные файлы

- `tn.docgeneral/Passport/DocPassport.cs` - основная логика (строки 244-536, 599-610, 892-1132)
- `TN_Doc/Client/document-editor/src/views/DocumentPassportEditor.vue` - Vue компонент редактора
- `TN_Doc/Client/document-editor/src/composables/usePassportEditor.ts` - логика редактора
- `TN.DocGeneral/Models/DataARM.cs` - модель данных с поддержкой истории изменений (v1.4.4)
- `TN.DocGeneral/Models/LabInfo.cs` - модель элемента качественного параметра
- `docs/features/field-history.md` - документация системы истории изменений (v1.4.4)

## Авторы и история

- **Исправление дублирования:** 2025-11-01 (коммит faef13a, 3ea5af9)
- **Интеграция с системой истории изменений:** 2025-11-17 (в разработке v1.4.4)
- **Оригинальная реализация DocPassport:** см. git history
