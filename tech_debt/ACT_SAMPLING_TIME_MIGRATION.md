# План миграции актов: время пробоотбора вместо времени смены

## 📋 Обзор задачи

Необходимо доработать 5 шаблонов актов для отображения времени пробоотбора вместо времени смены.

**Цель:** Заменить расчетное время смен на фактическое время пробоотбора из данных PassportPeriodDT.

## 🎯 Файлы для доработки

### Список шаблонов актов (5 файлов):
1. `Act_N_GOSTR50.2.040(G).frx` - основной ГОСТ шаблон
2. `Act_N_MI3532(18).frx` - шаблон по МИ 3532-2018
3. `Act_N_GOSTR50.2.040(G)_ShiftTime.frx` - специализированный для смен
4. `Act_N_GOSTR50.2.040(G)_ShiftTime_China.frx` - китайская версия
5. `Act_N_GOSTR50.2.040(G)(Export_Rus).frx` - экспортная версия

## 🔧 Детальный план изменений

### Этап 1: Удаление кода расчета времени смены

**Удалить следующий блок кода:**
```csharp
string shiftCountStr = DataIVK.TableResultActAndPassport.ResultActAndPassport.Common.UsedShift;
if (!int.TryParse(shiftCountStr, out int shiftCount) || shiftCount < 1)
    shiftCount = 1;
double hoursPerShift = 24.0 / shiftCount;
```

**Местоположение:** В секции скриптов каждого шаблона, обычно в начале метода обработки данных.

### Этап 2: Объявление новых переменных

**Добавить перед циклом for:**
```csharp
string begin = string.Empty;
string end = string.Empty;
int samplingBeginTs = 0;
int samplingEndTs = 0;

for (int i = 0; i < (listShiftsData.Count); i++)
```

**Назначение переменных:**
- `begin` - время начала пробоотбора в формате HH:mm
- `end` - время окончания пробоотбора в формате HH:mm
- `samplingBeginTs` - Unix timestamp начала пробоотбора
- `samplingEndTs` - Unix timestamp окончания пробоотбора

### Этап 3: Замена логики расчета времени

**Заменить существующий код:**
```csharp
// СТАРЫЙ КОД (удалить)
string begin = FormatTime(i * hoursPerShift);
string end = FormatTime((i + 1) * hoursPerShift);
```

**На новый код:**
```csharp
// НОВЫЙ КОД
begin = string.Empty;
end = string.Empty;
if(i < DataIVK.TableResultActAndPassport.ResultActAndPassport.PassportPeriodDT.Count)
{
    samplingBeginTs = DataIVK.TableResultActAndPassport.ResultActAndPassport.PassportPeriodDT[i].Begin;
    samplingEndTs = DataIVK.TableResultActAndPassport.ResultActAndPassport.PassportPeriodDT[i].End;
}

if(samplingBeginTs > 0 && samplingEndTs > 0)
{
    begin = tools.UnixTimestampToDatetime(samplingBeginTs).ToString("HH:mm");
    end = tools.UnixTimestampToDatetime(samplingEndTs).ToString("HH:mm").Replace("00:00", "24:00");
}
```

### Этап 4: Удаление функции FormatTime

**Удалить функцию:**
```csharp
private string FormatTime(double totalHours)
{
    int hours = (int)totalHours;
    int minutes = (int)((totalHours - hours) * 60);

    if (hours == 24)
        return "24:00";

    return $"{hours:00}:{minutes:00}";
}
```

**Обоснование:** Функция больше не нужна, так как время берется из фактических данных.

## 🔍 Технические детали

### Источник данных
- **Старый:** Расчет на основе `UsedShift` и формулы `24.0 / shiftCount`
- **Новый:** Фактические данные из `PassportPeriodDT[i].Begin` и `PassportPeriodDT[i].End`

### Формат времени
- **Входные данные:** Unix timestamp (int)
- **Выходной формат:** "HH:mm" строка
- **Специальная обработка:** "00:00" заменяется на "24:00" для корректного отображения

### Обработка граничных случаев
1. **Отсутствие данных:** `begin` и `end` остаются пустыми
2. **Некорректные timestamp:** Проверка `> 0` перед обработкой
3. **Выход за границы массива:** Проверка `i < PassportPeriodDT.Count`

## 📋 Пошаговый план выполнения

### Модификация всех шаблонов
Применить изменения к каждому из 5 шаблонов:

1. **Act_N_GOSTR50.2.040(G).frx**
   - Удаление кода расчета смен
   - Добавление новых переменных
   - Замена логики времени
   - Удаление FormatTime

2. **Act_N_MI3532(18).frx**
   - Аналогичные изменения

3. **Act_N_GOSTR50.2.040(G)_ShiftTime.frx**
   - Полная замена логики смен на пробоотбор

4. **Act_N_GOSTR50.2.040(G)_ShiftTime_China.frx**
   - Изменения с учетом китайской локализации

5. **Act_N_GOSTR50.2.040(G)(Export_Rus).frx**
   - Изменения в экспортной версии

## ⚠️ Важные особенности

### Обработка данных
1. **Отсутствие данных PassportPeriodDT** - документ покажет пустое время
2. **Некорректные Unix timestamps** - время не отображается
3. **Изменение семантики документа** - акты теперь показывают время пробоотбора

## 📝 Документирование изменений

### Изменения в CLAUDE.md
Добавить в раздел "Recent Changes":
```markdown
### v1.4.2 (планируемая)
- 🔄 Заменено время смены на время пробоотбора в актах
- ✅ Обновлены 5 шаблонов актов для отображения фактического времени
- 🗑️ Удален устаревший код расчета времени смен
- ➕ Добавлена поддержка данных PassportPeriodDT
```

### Техническая документация
- Обновить описание шаблонов актов
- Документировать новый источник данных времени
- Описать формат данных PassportPeriodDT

## 🎯 Ожидаемые результаты

### После внедрения
1. **Точность данных:** Акты будут показывать фактическое время пробоотбора
2. **Соответствие стандартам:** Документы будут соответствовать требованиям метрологии
3. **Устранение расчетов:** Убрана необходимость в расчете времени смен
4. **Улучшенная трассируемость:** Прямая связь с данными измерений

### Преимущества изменений
- ✅ Более точная информация в документах
- ✅ Упрощение логики шаблонов
- ✅ Соответствие фактическим данным измерений
- ✅ Устранение потенциальных ошибок расчета

---

**Статус:** К реализации
**Приоритет:** Высокий
**Объем работ:** Модификация 5 шаблонов FastReport