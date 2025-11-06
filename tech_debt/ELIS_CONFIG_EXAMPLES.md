# Примеры конфигурации ElisAlias

Этот файл содержит примеры правильной конфигурации `ElisAlias` для интеграции с системой ELIS.

## Структура конфигурации CfgEditPassport.json

```json
{
  "DocType": "Passport",
  "IsElisUsed": true,
  "AdditionalInfo": {
    "Fields": [
      // ... поля с ElisAlias
    ]
  },
  "Edit": {
    "Parameters": [
      // ... параметры с ElisAlias
    ]
  }
}
```

## Примеры полей AdditionalInfo

### Пример 1: Простое текстовое поле

```json
{
  "Key": "LabName",
  "Label": "Лаборатория",
  "Tag": "AdditionalInfo",
  "Type": "text",
  "Required": false,
  "Editable": true,
  "ElisAlias": "labName"
}
```

**Как это работает**:
- ELIS отправляет: `{ "labInfo": { "labName": "ООО 'Тестовая лаборатория'" } }`
- Vue компонент ищет в конфигурации поле с `ElisAlias: "labName"`
- Заполняет поле `LabName` значением `"ООО 'Тестовая лаборатория'"`
- Добавляет зеленую подсветку

### Пример 2: Представитель лаборатории (форматирование ФИО)

```json
{
  "Key": "ChiefLabShortSign",
  "Label": "Представитель лаборатории",
  "Type": "text",
  "ElisAlias": "chiefLabShortSign"
}
```

**Особенность**: ELIS отправляет отдельные поля `givenName`, `middleName`, `familyName`, а Vue компонент автоматически форматирует их в "И. О. Фамилия".

**ELIS данные**:
```json
{
  "signers": {
    "laboratory": {
      "givenName": "Иван",
      "middleName": "Петрович",
      "familyName": "Сидоров",
      "post": "Начальник лаборатории",
      "company": "ООО 'Тестовая лаборатория'"
    }
  }
}
```

**Результат**: `ChiefLabShortSign = "И. П. Сидоров"`

### Пример 3: Должность и организация

```json
{
  "Key": "ChiefLabPosition",
  "Label": "Должность",
  "Type": "text",
  "ElisAlias": "chiefLabPosition"
},
{
  "Key": "ChiefLabOrganization",
  "Label": "Организация",
  "Type": "text",
  "ElisAlias": "chiefLabOrganization"
}
```

### Пример 4: Дата и время

```json
{
  "Key": "DocumentDate",
  "Label": "Дата документа",
  "Type": "datetime-local",
  "ElisAlias": "documentDate"
}
```

**ELIS данные**: `{ "documentDate": "2025-11-06T14:30:00Z" }`
**Результат**: Автоматически конвертируется в формат datetime-local

### Пример 5: Множественные алиасы (fallback)

```json
{
  "Key": "LabAddress",
  "Label": "Адрес лаборатории",
  "Type": "text",
  "ElisAlias": "labAddress|address|laboratoryAddress"
}
```

**Как это работает**:
- Vue компонент пытается найти `labAddress` в данных ELIS
- Если не найдено, пытается `address`
- Если не найдено, пытается `laboratoryAddress`
- Использует первое найденное значение

## Примеры качественных параметров (Edit.Parameters)

### Пример 1: Базовый параметр качества

```json
{
  "Id": 1,
  "Key": "Density",
  "Name": "Плотность при 20°C, кг/м³",
  "Tag": "Value",
  "Editable": true,
  "RequiredFill": true,
  "MethodRequiredFill": true,
  "RoundValue": 1,
  "ElisAlias": "density",
  "Methods": [
    {
      "Id": 1,
      "Name": "ГОСТ 3900-85",
      "Use": true,
      "IsDefault": true,
      "LimitValueActivate": false
    }
  ]
}
```

**ELIS данные**:
```json
{
  "parameters": {
    "density": {
      "value": 853.5,
      "valueString": "853,5",
      "testMethodName": "ГОСТ 3900-85"
    }
  }
}
```

**Результат**:
- `value.Density = "853.5"` (зеленый фон)
- `method.Density = { "Name": "ГОСТ 3900-85", ... }` (зеленый фон)
- `result.Density = "853,5"` (зеленый фон)

### Пример 2: Параметр с лимитом

```json
{
  "Key": "WaterContent",
  "Name": "Массовая доля воды, %",
  "ElisAlias": "waterContent",
  "Methods": [
    {
      "Name": "ГОСТ 2477-2014",
      "LimitValueActivate": true,
      "LimitValue": 0.5,
      "LimitValueString": "Не обнаружено"
    }
  ]
}
```

**ELIS данные с текстовым представлением**:
```json
{
  "parameters": {
    "waterContent": {
      "value": 0.3,
      "valueString": "Менее 0,5",
      "testMethodName": "ГОСТ 2477-2014"
    }
  }
}
```

**Парсинг метода**:
- Vue компонент парсит `"Менее 0,5"` → `limitValue: 0.5, operator: 'less'`
- Создается метод с `LimitValueActivate: true, LimitValue: 0.5`

### Пример 3: Параметр с множественными алиасами

```json
{
  "Key": "FractionalComposition",
  "Name": "Фракционный состав",
  "ElisAlias": "fractionalComposition|fractionComp|fractComp"
}
```

### Пример 4: Параметр без ELIS (остается пустым)

```json
{
  "Key": "ManualParameter",
  "Name": "Ручной параметр (только для ручного ввода)",
  "ElisAlias": null
}
```

**Результат**: Поле не будет заполнено из ELIS, доступно только для ручного ввода.

## Маппинг ключей ELIS API ↔ ElisAlias

### Стандартные ключи из ELIS API

| Категория | ELIS ключ | Рекомендуемый ElisAlias | Описание |
|-----------|-----------|-------------------------|----------|
| **Лаборатория** | `labName` | `"labName"` | Название лаборатории |
| | `labAddress` | `"labAddress"` | Адрес лаборатории |
| | `accreditationCertificate` | `"accreditationCertificate"` | Аттестат аккредитации |
| **Протокол** | `protocolNumber` | `"protocolNumber"` | Номер протокола испытаний |
| | `startPeriodTime` | `"startPeriodTime"` | Начало периода |
| | `endPeriodTime` | `"endPeriodTime"` | Конец периода |
| **Представитель** | `signers.laboratory.givenName` | - | Имя (автоматически в chiefLabShortSign) |
| | `signers.laboratory.middleName` | - | Отчество (автоматически) |
| | `signers.laboratory.familyName` | - | Фамилия (автоматически) |
| | `signers.laboratory.post` | `"chiefLabPosition"` | Должность |
| | `signers.laboratory.company` | `"chiefLabOrganization"` | Организация |
| **Параметры качества** | `parameters.{key}.value` | ElisAlias параметра | Числовое значение |
| | `parameters.{key}.valueString` | - | Текстовое представление |
| | `parameters.{key}.testMethodName` | - | Метод испытаний |

### Примеры конкретных параметров качества

| Параметр | ELIS ключ | ElisAlias | Название |
|----------|-----------|-----------|----------|
| Плотность | `density` | `"density"` | Плотность при 20°C |
| Вязкость | `viscosity` | `"viscosity\|kinematicViscosity"` | Кинематическая вязкость |
| Сера | `sulfur` | `"sulfur\|sulfurContent"` | Массовая доля серы |
| Вода | `water` | `"water\|waterContent"` | Массовая доля воды |
| Механические примеси | `mechanicalImpurities` | `"mechanicalImpurities"` | Механические примеси |
| Температура застывания | `pourPoint` | `"pourPoint"` | Температура застывания |
| Хлористые соли | `chlorideSalts` | `"chlorideSalts"` | Хлористые соли |

## Специальные случаи

### Форматирование ФИО

Vue компонент автоматически обрабатывает следующие поля:

```javascript
// Если в ELIS данных есть:
{
  "signers": {
    "laboratory": {
      "givenName": "Иван",
      "middleName": "Петрович",
      "familyName": "Сидоров"
    }
  }
}

// То автоматически создаются:
chiefLabShortSign = "И. П. Сидоров"
chiefLabPosition = signers.laboratory.post
chiefLabOrganization = signers.laboratory.company
```

### Парсинг текстовых представлений методов

Vue компонент умеет парсить следующие форматы:

| Формат ELIS | Результат парсинга |
|-------------|-------------------|
| `"Менее 4,0"` | `limitValue: 4.0, operator: 'less'` |
| `"Более 10,5"` | `limitValue: 10.5, operator: 'more'` |
| `"Не более 5"` | `limitValue: 5.0, operator: 'less_equal'` |
| `"Не менее 3,5"` | `limitValue: 3.5, operator: 'more_equal'` |
| `"До 8"` | `limitValue: 8.0, operator: 'less'` |
| `"От 2"` | `limitValue: 2.0, operator: 'more_equal'` |

## Проверка конфигурации

### Чек-лист для проверки ElisAlias

- [ ] Все ключи `ElisAlias` написаны в camelCase (не PascalCase!)
- [ ] Ключи соответствуют данным из ELIS API
- [ ] Для fallback используется синтаксис `"key1|key2|key3"`
- [ ] Обязательные поля имеют `Required: true`
- [ ] Параметры качества имеют `ElisAlias`
- [ ] Конфигурация валидна (проверить через JSON validator)

### Команда для валидации JSON

```bash
# Проверить синтаксис JSON
cat TN_Doc/Cfg/CfgEditPassport.json | jq . > /dev/null && echo "Valid JSON" || echo "Invalid JSON"

# Извлечь все ElisAlias
cat TN_Doc/Cfg/CfgEditPassport.json | jq '.. | objects | select(.ElisAlias) | .ElisAlias' | sort | uniq
```

## Отладка маппинга

### Скрипт для проверки маппинга

Добавьте в DevTools Console (iframe Vue компонента):

```javascript
// Проверить конфигурацию полей
console.table(
  store.fields
    .filter(f => f.elisAlias)
    .map(f => ({
      Key: f.key,
      Label: f.label,
      ElisAlias: f.elisAlias,
      Type: f.type
    }))
);

// Проверить конфигурацию параметров
console.table(
  store.config.qualityParametersSchema
    .filter(p => p.elisAlias)
    .map(p => ({
      Key: p.key,
      Name: p.name,
      ElisAlias: p.elisAlias
    }))
);

// Проверить данные ELIS
const elisData = JSON.parse(localStorage.getItem('dataPassport'));
console.log('ELIS parameters:', Object.keys(elisData.parameters));
```

## Примеры несоответствий и их решение

### Проблема 1: Поле не заполняется

**Симптом**: Поле остается пустым после применения ELIS.

**Возможные причины**:
1. Неправильный `ElisAlias` (не соответствует ELIS API)
2. Ключ написан в PascalCase вместо camelCase
3. Данные отсутствуют в ELIS протоколе

**Решение**:
```javascript
// Проверить доступные ключи в ELIS
const elisData = JSON.parse(localStorage.getItem('dataPassport'));
console.log('Available keys:', Object.keys(elisData));
console.log('Parameters:', Object.keys(elisData.parameters || {}));

// Сравнить с ElisAlias в конфигурации
```

### Проблема 2: Метод испытаний не парсится

**Симптом**: Метод создается с неправильным `limitValue`.

**Решение**: Проверить формат `valueString` в ELIS данных, добавить новый паттерн в `useElisIntegration.ts:createMethodFromElisData()`.

## Рекомендации

1. **Используйте единообразное именование** в ElisAlias (camelCase)
2. **Документируйте маппинг** в комментариях конфигурации
3. **Тестируйте с реальными данными** ELIS перед deploy
4. **Используйте fallback** (`"key1|key2"`) для параметров с разными именами
5. **Логируйте несопоставленные ключи** для отладки

---

**Последнее обновление**: 2025-11-06
