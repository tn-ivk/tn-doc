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
  "Key": "Laboratory",
  "Name": "Лаборатория предприятия",
  "Type": "text",
  "ElisAlias": ["labName"]
}
```

**Как это работает**:
- ELIS отправляет: `{ "labInfo": { "labName": "ООО 'Тестовая лаборатория'" } }`
- Vue компонент ищет в конфигурации поле с `ElisAlias: ["labName"]`
- Перебирает массив алиасов и ищет первый найденный ключ
- Заполняет поле `Laboratory` значением `"ООО 'Тестовая лаборатория'"`
- Добавляет зеленую подсветку (`highlightColor`)

### Пример 2: Представитель лаборатории (форматирование ФИО)

```json
{
  "Key": "Laboratory_IOF",
  "Name": "Представитель испытательной лаборатории (ИОФ)",
  "Type": "list",
  "ElisAlias": ["chiefLabShortSign"]
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

**Результат**: `Laboratory_IOF = "И. П. Сидоров"`

### Пример 3: Должность и организация

```json
{
  "Key": "Laboratory_Post",
  "Name": "Представитель испытательной лаборатории (должность)",
  "Type": "text",
  "ElisAlias": ["chiefLabPosition"]
},
{
  "Key": "Laboratory_Factory",
  "Name": "Представитель испытательной лаборатории (предприятие)",
  "Type": "text",
  "ElisAlias": ["chiefLabOrganization"]
}
```

### Пример 4: Дата и время

```json
{
  "Key": "PassportPeriodDT.Begin",
  "Name": "Дата и время отбора пробы (начало)",
  "Type": "datetime-local",
  "ElisAlias": ["startPeriodTime"]
}
```

**ELIS данные**: `{ "startPeriodTime": "2025-11-06T14:30:00Z" }`
**Результат**: Автоматически конвертируется в формат datetime-local

### Пример 5: Множественные алиасы (fallback) ⭐

```json
{
  "Key": "AccrSertifNumber",
  "Name": "Номер аттестата аккредитации",
  "Type": "text",
  "ElisAlias": ["accreditationNumber", "accreditationCertificate", "attestNumber"]
}
```

**Как это работает (fallback механизм)**:
- Vue компонент перебирает массив `ElisAlias` по порядку
- Пытается найти `accreditationNumber` в данных ELIS
- Если не найдено, пытается `accreditationCertificate`
- Если не найдено, пытается `attestNumber`
- Использует первое найденное значение
- Логирует в консоль, какой именно алиас сработал

## Примеры качественных параметров (Edit.Parameters)

### Пример 1: Базовый параметр качества (русское название) ⭐

```json
{
  "Id": 6,
  "Key": "MassWaterFracCorrection",
  "Name": "Массовая доля воды, %",
  "ElisAlias": ["Массовая доля воды(%)"],
  "RequiredFill": true,
  "RoundValue": 2,
  "Edit": true
}
```

**⚠️ ВАЖНО**: Parameters используют **русские полные названия** в `ElisAlias`, не camelCase!

**ELIS данные**:
```json
{
  "parameters": {
    "Массовая доля воды(%)": {
      "value": 0.3,
      "valueString": "Менее 0,5",
      "testMethodName": "ГОСТ 2477-2014"
    }
  }
}
```

**Результат**:
- `value.MassWaterFracCorrection = 0.3` (зеленый фон)
- `method.MassWaterFracCorrection = { "Name": "ГОСТ 2477-2014", ... }` (зеленый фон)
- `result.MassWaterFracCorrection = "Менее 0,5"` (зеленый фон)

### Пример 2: Параметр с множественными алиасами (fallback) ⭐

```json
{
  "Id": 8,
  "Key": "Chloride_Salts.MassFraction",
  "Name": "Массовая концентрация хлористых солей, %",
  "ElisAlias": [
    "Массовая концентрация хлористых солей(%)",
    "Массовая доля хлористых солей(%)"
  ],
  "Edit": true
}
```

**Как работает fallback**:
- Сначала ищет `"Массовая концентрация хлористых солей(%)"` в `elisData.parameters`
- Если не найдено, пробует `"Массовая доля хлористых солей(%)"`
- Использует первое найденное значение

**ELIS данные** (один из вариантов):
```json
{
  "parameters": {
    "Массовая доля хлористых солей(%)": {
      "value": 15.0,
      "valueString": "15",
      "testMethodName": "ГОСТ 6318-92"
    }
  }
}
```

**Результат**: Используется второй алиас, т.к. первый не найден.

### Пример 3: Параметр с экзотическими символами

```json
{
  "Id": 17,
  "Key": "Mass_fraction_of_hydrogen_sulfide",
  "Name": "Массовая доля сероводорода, млнˉ¹ (ppm)",
  "ElisAlias": [
    "Массовая доля сероводорода(млн⁻¹)",
    "Массовая доля сероводорода(млн⁻¹ (ppm))"
  ],
  "RequiredFill": true,
  "RoundValue": 1,
  "Edit": true
}
```

**⚠️ Внимание**: Разные символы для "минус один":
- `⁻` (U+207B) - верхний индекс минус
- `ˉ` (U+02C9) - макрон

Fallback массив учитывает эти вариации.

### Пример 4: Параметр без ELIS (остается пустым)

```json
{
  "Id": 4,
  "Key": "SIKN_Number",
  "Name": "СИКН №",
  "Type": "text"
}
```

**Результат**: Нет `ElisAlias` → поле не будет заполнено из ELIS, доступно только для ручного ввода.

## Маппинг ключей ELIS API ↔ ElisAlias

### Стандартные ключи из ELIS API

| Категория | ELIS ключ | Рекомендуемый ElisAlias | Описание |
|-----------|-----------|-------------------------|----------|
| **Лаборатория** | `labName` | `["labName"]` | Название лаборатории |
| | `labAddress` | `["labAddress"]` | Адрес лаборатории |
| | `accreditationNumber` | `["accreditationNumber"]` | Номер аттестата аккредитации |
| **Протокол** | `protocolNumber` | `["protocolNumber"]` | Номер протокола испытаний |
| | `startPeriodTime` | `["startPeriodTime"]` | Начало периода |
| | `endPeriodTime` | `["endPeriodTime"]` | Конец периода |
| **Представитель** | `signers.laboratory.givenName` | - | Имя (автоматически в chiefLabShortSign) |
| | `signers.laboratory.middleName` | - | Отчество (автоматически) |
| | `signers.laboratory.familyName` | - | Фамилия (автоматически) |
| | `signers.laboratory.post` | `["chiefLabPosition"]` | Должность |
| | `signers.laboratory.company` | `["chiefLabOrganization"]` | Организация |
| **Параметры качества** | `parameters["{Русское название}"].value` | `["Русское название"]` | Числовое значение ⚠️ |
| | `parameters["{Русское название}"].valueString` | - | Текстовое представление |
| | `parameters["{Русское название}"].testMethodName` | - | Метод испытаний |

**⚠️ КРИТИЧНО**:
- **AdditionalInfo** использует **camelCase** в `ElisAlias` (`["labName"]`)
- **Parameters** использует **русские полные названия** в `ElisAlias` (`["Массовая доля воды(%)"]`)

### Примеры конкретных параметров качества

| Параметр | ELIS ключ | ElisAlias | Название |
|----------|-----------|-----------|----------|
| Вода | `Массовая доля воды(%)` | `["Массовая доля воды(%)"]` | Массовая доля воды, % |
| Хлористые соли (концентр.) | `Массовая концентрация хлористых солей(мг/дм³)` | `["Массовая концентрация хлористых солей(мг/дм³)"]` | Массовая концентрация хлористых солей, мг/дм³ |
| Хлористые соли (%) | `Массовая концентрация хлористых солей(%)` | `["Массовая концентрация хлористых солей(%)", "Массовая доля хлористых солей(%)"]` | Массовая концентрация хлористых солей, % |
| Механические примеси | `Массовая доля механических примесей(%)` | `["Массовая доля механических примесей(%)"]` | Массовая доля механических примесей, % |
| Сера | `Массовая доля серы(%)` | `["Массовая доля серы(%)"]` | Массовая доля серы, % |
| ДНП (кПа) | `Давление насыщенных паров(кПа)` | `["Давление насыщенных паров(кПа)"]` | Давление насыщенных паров, кПа |
| ДНП (мм рт. ст.) | `Давление насыщенных паров(мм рт. ст.)` | `["Давление насыщенных паров(мм рт. ст.)"]` | Давление насыщенных паров, мм рт. ст. |
| Сероводород | `Массовая доля сероводорода(млн⁻¹)` | `["Массовая доля сероводорода(млн⁻¹)", "Массовая доля сероводорода(млн⁻¹ (ppm))"]` | Массовая доля сероводорода, млнˉ¹ (ppm) |

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

- [ ] `ElisAlias` это **массив строк** `["key1", "key2"]`, не строка с разделителем
- [ ] **AdditionalInfo**: все ключи в `ElisAlias` написаны в **camelCase** (`labName`, не `LabName`)
- [ ] **Parameters**: все ключи в `ElisAlias` написаны **русскими полными названиями** (`"Массовая доля воды(%)"`)
- [ ] Ключи соответствуют данным из ELIS API (запросить примеры данных у backend)
- [ ] Для fallback используется массив: `["key1", "key2", "key3"]`
- [ ] Обязательные поля имеют `RequiredFill: true`
- [ ] Параметры качества с ELIS интеграцией имеют массив `ElisAlias`
- [ ] Конфигурация валидна (проверить через JSON validator)

### Команда для валидации JSON

```bash
# Проверить синтаксис JSON
cat TN_Doc/Cfg/Passport/CfgEditPassport_GOSTR50.2.040\(I\).json | jq . > /dev/null && echo "✅ Valid JSON" || echo "❌ Invalid JSON"

# Извлечь все ElisAlias из AdditionalInfo
cat TN_Doc/Cfg/Passport/CfgEditPassport_GOSTR50.2.040\(I\).json | jq '.AdditionalInfo[] | select(.ElisAlias) | {Key, ElisAlias}'

# Извлечь все ElisAlias из Parameters
cat TN_Doc/Cfg/Passport/CfgEditPassport_GOSTR50.2.040\(I\).json | jq '.Parameters[] | select(.ElisAlias) | {Key, ElisAlias}'

# Подсчитать количество полей с ELIS интеграцией
echo "AdditionalInfo с ELIS: $(cat TN_Doc/Cfg/Passport/CfgEditPassport_GOSTR50.2.040\(I\).json | jq '[.AdditionalInfo[] | select(.ElisAlias)] | length')"
echo "Parameters с ELIS: $(cat TN_Doc/Cfg/Passport/CfgEditPassport_GOSTR50.2.040\(I\).json | jq '[.Parameters[] | select(.ElisAlias)] | length')"
```

## Отладка маппинга

### Скрипт для проверки маппинга

Добавьте в DevTools Console (iframe Vue компонента):

```javascript
// 1. Проверить конфигурацию полей AdditionalInfo
console.group('📋 AdditionalInfo с ELIS');
console.table(
  store.fields
    .filter(f => f.elisAlias)
    .map(f => ({
      Key: f.key,
      Label: f.label,
      ElisAlias: Array.isArray(f.elisAlias) ? f.elisAlias.join(', ') : f.elisAlias,
      Type: f.type,
      IsArray: Array.isArray(f.elisAlias)
    }))
);
console.groupEnd();

// 2. Проверить конфигурацию параметров качества
console.group('⚗️ Parameters с ELIS');
console.table(
  store.config.qualityParametersSchema
    .filter(p => p.elisAlias)
    .map(p => ({
      Key: p.key,
      Name: p.name,
      ElisAlias: Array.isArray(p.elisAlias) ? p.elisAlias.join(', ') : p.elisAlias,
      IsArray: Array.isArray(p.elisAlias)
    }))
);
console.groupEnd();

// 3. Проверить данные ELIS
const elisData = JSON.parse(localStorage.getItem('dataPassport'));
if (elisData) {
  console.group('📦 ELIS данные');
  console.log('labInfo keys:', elisData.labInfo ? Object.keys(elisData.labInfo) : 'отсутствует');
  console.log('parameters keys:', elisData.parameters ? Object.keys(elisData.parameters) : 'отсутствует');
  console.log('signers:', elisData.signers ? 'есть' : 'отсутствует');
  console.groupEnd();
} else {
  console.warn('⚠️ ELIS данные не найдены в localStorage');
}

// 4. Проверить несоответствия (ключи в конфиге, но нет в ELIS данных)
if (elisData) {
  console.group('🔍 Проверка несоответствий');

  // AdditionalInfo
  const missingAdditionalInfo = store.fields
    .filter(f => f.elisAlias)
    .filter(f => {
      const aliases = Array.isArray(f.elisAlias) ? f.elisAlias : [f.elisAlias];
      return !aliases.some(alias =>
        elisData.labInfo?.[alias] ||
        elisData[alias] ||
        elisData.signers?.laboratory?.[alias]
      );
    });

  if (missingAdditionalInfo.length > 0) {
    console.warn('Поля AdditionalInfo без данных в ELIS:', missingAdditionalInfo.map(f => f.key));
  }

  // Parameters
  const missingParameters = store.config.qualityParametersSchema
    .filter(p => p.elisAlias)
    .filter(p => {
      const aliases = Array.isArray(p.elisAlias) ? p.elisAlias : [p.elisAlias];
      return !aliases.some(alias => elisData.parameters?.[alias]);
    });

  if (missingParameters.length > 0) {
    console.warn('Параметры без данных в ELIS:', missingParameters.map(p => p.key));
  }

  console.groupEnd();
}
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

## Использование highlightColor в FormField

### Базовое использование (без подсветки)

```vue
<FormField
  :field="field"
  :modelValue="store.formData[field.key]"
  @update:modelValue="(value) => store.updateField(field.key, value)"
/>
```

**Результат**: Стандартный белый фон элемента.

### С подсветкой ELIS данных (зелёный фон)

```vue
<FormField
  :field="field"
  :modelValue="store.formData[field.key]"
  :highlightColor="store.formData[`${field.key}__elisFilled`] ? 'var(--md-elis-highlight)' : undefined"
  @update:modelValue="(value) => store.updateField(field.key, value)"
/>
```

**Результат**: Зелёный фон (#d4edda) для полей, заполненных из ELIS.

### С кастомной подсветкой (любой цвет)

```vue
<!-- Жёлтая подсветка для предупреждений -->
<FormField
  :field="field"
  :modelValue="store.formData[field.key]"
  :highlightColor="hasWarning ? '#fff3cd' : undefined"
  @update:modelValue="(value) => store.updateField(field.key, value)"
/>

<!-- Голубая подсветка для изменённых полей -->
<FormField
  :field="field"
  :modelValue="store.formData[field.key]"
  :highlightColor="isModified ? 'rgb(217, 237, 247)' : undefined"
  @update:modelValue="(value) => store.updateField(field.key, value)"
/>
```

### Управление подсветкой через computed

```typescript
// В компоненте DocumentPassportEditor.vue
const getFieldHighlight = (fieldKey: string) => {
  // Зелёный фон для ELIS данных
  if (store.formData[`${fieldKey}__elisFilled`]) {
    return 'var(--md-elis-highlight)';
  }

  // Жёлтый фон для изменённых полей
  if (store.formData[`${fieldKey}__modified`]) {
    return '#fff3cd';
  }

  // Нет подсветки
  return undefined;
};
```

```vue
<FormField
  :field="field"
  :modelValue="store.formData[field.key]"
  :highlightColor="getFieldHighlight(field.key)"
  @update:modelValue="(value) => store.updateField(field.key, value)"
/>
```

### Доступные CSS переменные для подсветки

```css
/* TN_Doc/wwwroot/css/material3.css */

--md-elis-highlight: #d4edda;        /* Зелёный (ELIS данные) */
--md-primary-light: #e3f2fd;         /* Голубой (primary) */
--md-error-light: #f8d7da;           /* Красный (ошибки) */
--md-gray-light: #f8f9fa;            /* Серый (disabled) */
```

## Рекомендации

1. **Используйте единообразное именование** в ElisAlias (camelCase)
2. **Документируйте маппинг** в комментариях конфигурации
3. **Тестируйте с реальными данными** ELIS перед deploy
4. **Используйте fallback** (`"key1|key2"`) для параметров с разными именами
5. **Логируйте несопоставленные ключи** для отладки
6. **Используйте CSS переменные** для цветов подсветки вместо hardcoded HEX
7. **Сбрасывайте highlightColor** при ручном редактировании полей

---

## История изменений

- **2025-11-06**: Обновлена документация на основе реальной конфигурации
  - ⚠️ **КРИТИЧНО**: `ElisAlias` это массив строк `["key1", "key2"]`, не строка `"key1|key2"`
  - Обновлены все примеры с учётом массивов
  - Добавлены примеры из реального файла `CfgEditPassport_GOSTR50.2.040(I).json`
  - Документирована смешанная номенклатура: camelCase для AdditionalInfo, русские названия для Parameters
  - Обновлены команды для валидации конфигурации
  - Добавлены расширенные скрипты отладки маппинга
  - Добавлен пример с экзотическими символами (⁻ vs ˉ)

**Последнее обновление**: 2025-11-06
