# План реализации: Объединение методов испытаний для связанных параметров (LinkedParameters)

**Дата создания:** 2025-12-11
**Последнее обновление:** 2025-12-11
**Статус:** Планирование
**Ветка:** feature/split-method

---

## Прогресс выполнения

| Этап | Описание | Статус |
|------|----------|--------|
| 1 | Бэкенд: модели данных | ✅ Завершён |
| 2 | Бэкенд: логика построения схемы | ⬜ Не начат |
| 3 | Бэкенд: сохранение документа | ⬜ Не начат |
| 4 | Фронтенд: типы TypeScript | ⬜ Не начат |
| 5 | Фронтенд: логика usePassportEditor | ⬜ Не начат |
| 6 | Фронтенд: UI компоненты | ⬜ Не начат |
| 7 | Конфигурации JSON | ⬜ Не начат |
| 8 | Тестирование | ⬜ Не начат |
| 9 | Документация | ⬜ Не начат |

**Общий прогресс: 1/9 этапов**

---

## Обзор задачи

**Цель:** Объединить выбор метода испытаний для связанных параметров качества, когда между ними **не** установлена Slave-связь.

**Контекст:** В таблице показателей качества паспорта существуют пары параметров, представляющих одну и ту же величину в разных единицах измерения:

| Пара | Параметр 1 | Параметр 2 |
|------|------------|------------|
| 1 | `Chloride_Salts.Concentration` (мг/дм³) | `Chloride_Salts.MassFraction` (%) |
| 2 | `DNP.kPa` (кПа) | `DNP.mercury_mm` (мм рт. ст.) |
| 3 | `Yield_fraction_200` (до 200°С) | `Yield_fraction_300` (до 300°С) |

**Текущее поведение:**
- `SlaveKey` установлен → Slave-параметр скрыт (уже реализовано в GOSTR конфигурациях)
- `SlaveKey` не установлен → Параметры независимы (каждый со своим комбобоксом метода)

**Новое поведение:**
- `LinkedParameters` указан (без `SlaveKey`) → Общий комбобокс метода для группы, выбранный метод записывается во все связанные параметры

---

## Этап 1: Бэкенд — Модели данных

### Задачи

- [x] **1.1** Добавить поле `LinkedParameter` в модель `Parameter`
- [x] **1.2** Добавить поля в модель `QualityParameterSchema`
- [x] **1.3** Проверить сериализацию/десериализацию JSON

### 1.1 Модель Parameter (TN.Doc.Edit)

**Файл:** `tn.docgeneral/Passport/Models/Parameter.cs`

```csharp
/// <summary>
/// Ключ связанного параметра, использующего общий метод испытаний.
/// Используется когда параметры НЕ связаны через SlaveKey, но должны
/// использовать одинаковый метод испытаний.
/// Пример: "Chloride_Salts.MassFraction" для параметра "Chloride_Salts.Concentration"
/// </summary>
[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
public string LinkedParameter { get; set; }
```

### 1.2 Модель QualityParameterSchema

**Файл:** `tn.docgeneral/Passport/PassportEditModels.cs`

```csharp
/// <summary>
/// Ключ связанного параметра (для объединения методов испытаний).
/// Указывается на ведущем параметре группы. Null = независимый параметр.
/// </summary>
[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
public string LinkedParameter { get; set; }

/// <summary>
/// Признак того, что параметр является "ведомым" в группе LinkedParameters.
/// Если true, комбобокс метода не отображается (используется метод ведущего параметра).
/// </summary>
[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
public bool? IsLinkedFollower { get; set; }

/// <summary>
/// Ключ ведущего параметра, если текущий параметр является ведомым в группе.
/// </summary>
[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
public string LinkedLeaderKey { get; set; }
```

---

## Этап 2: Бэкенд — Логика построения схемы

### Задачи

- [ ] **2.1** Реализовать метод `ResolveLinkedParametersRoles`
- [ ] **2.2** Интегрировать вызов в `BuildQualityParametersSchema`
- [ ] **2.3** Добавить unit-тест на определение ролей

### 2.1 Метод ResolveLinkedParametersRoles

**Файл:** `tn.docgeneral/Passport/DocPassport.Editor.cs`

```csharp
/// <summary>
/// Определяет связи LinkedParameters между параметрами.
/// Вызывается после построения схемы параметров.
/// </summary>
private void ResolveLinkedParametersRoles(List<QualityParameterSchema> schemas, CfgEditPassport editCfg)
{
    // Построить карту: ключ параметра → его схема
    var keyToSchema = schemas.ToDictionary(s => s.Key, s => s, StringComparer.OrdinalIgnoreCase);

    foreach (var param in editCfg.Parameters.Where(p => p.Use && !string.IsNullOrEmpty(p.LinkedParameter)))
    {
        // Пропускаем, если есть SlaveKey (SlaveKey имеет приоритет)
        if (!string.IsNullOrEmpty(param.SlaveKey))
            continue;

        // Ведущий параметр
        if (keyToSchema.TryGetValue(param.Key, out var leaderSchema))
        {
            leaderSchema.LinkedParameter = param.LinkedParameter;
        }

        // Ведомый параметр
        if (keyToSchema.TryGetValue(param.LinkedParameter, out var followerSchema))
        {
            followerSchema.IsLinkedFollower = true;
            followerSchema.LinkedLeaderKey = param.Key;
        }
    }
}
```

---

## Этап 3: Бэкенд — Сохранение документа

### Задачи

- [ ] **3.1** Обновить логику обработки методов в `SaveDocument`
- [ ] **3.2** Добавить синхронизацию метода в связанные параметры
- [ ] **3.3** Добавить unit-тест на сохранение

### 3.1 Обновление SaveDocument

**Файл:** `tn.docgeneral/Passport/DocPassport.Editor.cs`

```csharp
// При сохранении метода ведущего параметра → записать в ведомый
foreach (var item in data.Values.Where(x => x.Tag == "Metod"))
{
    var paramKey = item.Key.Replace("method.", "");
    var paramConfig = editCfg.Parameters.FirstOrDefault(p => p.Key == paramKey);

    // Стандартная обработка метода для текущего параметра
    ProcessMethodSave(paramKey, item, dataArm, corr);

    // Если есть LinkedParameter → скопировать метод в связанный параметр
    if (!string.IsNullOrEmpty(paramConfig?.LinkedParameter) && string.IsNullOrEmpty(paramConfig.SlaveKey))
    {
        // Копируем тот же метод в связанный параметр
        ProcessMethodSave(paramConfig.LinkedParameter, item, dataArm, corr);
    }
}
```

---

## Этап 4: Фронтенд — Типы TypeScript

### Задачи

- [ ] **4.1** Добавить поля в `PassportQualityParameterSchema`
- [ ] **4.2** Обновить `PassportQualityParameter` (если нужно)
- [ ] **4.3** Запустить `npm run type-check`

### 4.1 Обновление типов

**Файл:** `TN_Doc/Client/document-editor/src/types/passport.types.ts`

```typescript
export interface PassportQualityParameterSchema {
  // ... существующие поля ...

  /**
   * Ключ связанного параметра (для объединения методов испытаний).
   * Указывается на ведущем параметре группы.
   */
  linkedParameter?: string;

  /**
   * Признак того, что параметр является "ведомым" в группе LinkedParameters.
   * Если true, комбобокс метода скрывается (используется метод ведущего).
   */
  isLinkedFollower?: boolean;

  /**
   * Ключ ведущего параметра, если текущий является ведомым.
   */
  linkedLeaderKey?: string;
}
```

---

## Этап 5: Фронтенд — Логика usePassportEditor

### Задачи

- [ ] **5.1** Обновить `handleMethodUpdate` для синхронизации методов
- [ ] **5.2** Добавить функцию `findParameterSchema`
- [ ] **5.3** Обновить логику истории изменений для связанных параметров

### 5.1 Обновление handleMethodUpdate

**Файл:** `TN_Doc/Client/document-editor/src/composables/usePassportEditor.ts`

```typescript
function handleMethodUpdate(event: MethodUpdateEvent) {
  const param = findParameter(event.paramKey);
  if (!param) {
    logger.warn(`Параметр с ключом ${event.paramKey} не найден`);
    return;
  }

  const methodOption = event.method;
  const methodJson = methodOption ? serializeMethodOption(methodOption) : '';
  const methodKey = `method.${event.paramKey}`;

  const updates: Record<string, any> = {
    [methodKey]: methodJson
  };

  trackMethodChange(methodKey, methodOption?.name);

  // Если есть linkedParameter → записать метод в связанный параметр
  const schema = findParameterSchema(event.paramKey);
  if (schema?.linkedParameter && !schema.slaveKey) {
    const linkedMethodKey = `method.${schema.linkedParameter}`;
    updates[linkedMethodKey] = methodJson;
    trackMethodChange(linkedMethodKey, methodOption?.name, 'Синхронизация метода');
  }

  store.bulkUpdateFields(updates);
}
```

---

## Этап 6: Фронтенд — UI компоненты

### Задачи

- [ ] **6.1** Создать компонент `PassportLinkedParameterGroup.vue`
- [ ] **6.2** Обновить `PassportQualityTable.vue` с группировкой
- [ ] **6.3** Добавить стили для объединённых ячеек
- [ ] **6.4** Проверить отображение в браузере

### 6.1 Компонент PassportLinkedParameterGroup

**Файл:** `TN_Doc/Client/document-editor/src/components/passport/PassportLinkedParameterGroup.vue`

```vue
<template>
  <!-- Строка ведущего параметра -->
  <tr class="parameter-row linked-group-leader">
    <td class="cell-number">{{ startIndex }}</td>
    <td class="cell-name">{{ leader.name }}</td>

    <!-- Метод испытаний с rowspan -->
    <td class="cell-method" :rowspan="totalRows">
      <PassportMethodSelectWithHistory
        :parameter="leader"
        @update:method="handleMethodUpdate"
        @manual-method="handleManualMethodRequest"
      />
    </td>

    <td v-if="isElisUsed" class="cell-documents" :rowspan="totalRows">
      <PassportDocumentField :parameter="leader" />
    </td>

    <td class="cell-measurement">
      <PassportMeasurementInputWithHistory
        :parameter="leader"
        @update:measurement="handleLeaderMeasurementUpdate"
      />
    </td>

    <td class="cell-result">
      <PassportResultCellWithHistory
        :parameter="leader"
        :isEditable="isResultEditable(leader)"
        @result-edit="handleResultEditRequest(leader.key)"
      />
    </td>
  </tr>

  <!-- Строки ведомых параметров -->
  <tr
    v-for="(follower, idx) in followers"
    :key="follower.key"
    class="parameter-row linked-group-follower"
  >
    <td class="cell-number">{{ startIndex + idx + 1 }}</td>
    <td class="cell-name">{{ follower.name }}</td>
    <td class="cell-measurement">
      <PassportMeasurementInputWithHistory
        :parameter="follower"
        @update:measurement="handleFollowerMeasurementUpdate(follower.key, $event)"
      />
    </td>
    <td class="cell-result">
      <PassportResultCellWithHistory
        :parameter="follower"
        :isEditable="isResultEditable(follower)"
        @result-edit="handleResultEditRequest(follower.key)"
      />
    </td>
  </tr>
</template>
```

### 6.2 Обновление PassportQualityTable

**Файл:** `TN_Doc/Client/document-editor/src/components/passport/PassportQualityTable.vue`

```typescript
interface ParameterGroup {
  leader: PassportQualityParameter;
  followers: PassportQualityParameter[];
  startIndex: number;
}

const parameterGroups = computed<ParameterGroup[]>(() => {
  const groups: ParameterGroup[] = [];
  const processedKeys = new Set<string>();
  let currentIndex = 1;

  for (const param of props.parameters) {
    if (processedKeys.has(param.key)) continue;
    if (param.role === 'Slave') continue;

    const group: ParameterGroup = {
      leader: param,
      followers: [],
      startIndex: currentIndex
    };

    processedKeys.add(param.key);
    currentIndex++;

    if (param.linkedParameter) {
      const follower = props.parameters.find(p => p.key === param.linkedParameter);
      if (follower && !processedKeys.has(param.linkedParameter)) {
        group.followers.push(follower);
        processedKeys.add(param.linkedParameter);
        currentIndex++;
      }
    }

    groups.push(group);
  }

  return groups;
});
```

---

## Этап 7: Конфигурации JSON

### Задачи

- [ ] **7.1** Обновить `CfgEditPassport_MI3532(13).json`
- [ ] **7.2** Обновить `CfgEditPassport_MI3532(14).json`
- [ ] **7.3** Обновить `CfgEditPassport_MI3532(15).json`
- [ ] **7.4** Обновить `CfgEditPassport_MI3532(15)_China.json`
- [ ] **7.5** Обновить `CfgEditPassportExport.json`
- [ ] **7.6** Обновить `CfgEditPassportForActNP.json`
- [ ] **7.7** Обновить `CfgEditPassport_EAC.json`
- [ ] **7.8** Валидация JSON через `jq`

### Пример изменений

```json
{
  "Id": 7,
  "Key": "Chloride_Salts.Concentration",
  "Name": "Массовая концентрация хлористых солей, мг/дм³",
  "LinkedParameter": "Chloride_Salts.MassFraction",
  "Use": true,
  "Edit": true,
  "IsBallast": true
}
```

### Пары для добавления

| Ведущий параметр | LinkedParameter |
|------------------|-----------------|
| `Chloride_Salts.Concentration` | `"Chloride_Salts.MassFraction"` |
| `DNP.kPa` | `"DNP.mercury_mm"` |
| `Yield_fraction_200` | `"Yield_fraction_300"` |

**Важно:** Для конфигураций с `SlaveKey` (GOSTR) `LinkedParameter` **не добавляется**.

---

## Этап 8: Тестирование

### Unit-тесты (C#)

- [ ] **8.1** Тест парсинга `LinkedParameters` из JSON
- [ ] **8.2** Тест приоритета `SlaveKey` над `LinkedParameters`
- [ ] **8.3** Тест `ResolveLinkedParametersRoles`
- [ ] **8.4** Тест сохранения метода в связанные параметры

**Файл:** `Tests/Configs/CfgEditPassportLinkedParametersTests.cs`

### Интеграционные тесты (Playwright)

- [ ] **8.5** Тест отображения объединённого комбобокса
- [ ] **8.6** Тест синхронизации выбора метода
- [ ] **8.7** Тест сохранения и загрузки документа

### Ручное тестирование

**Конфигурация без SlaveKey (MI3532):**
- [ ] Комбобокс метода объединён для пар параметров
- [ ] При выборе метода он записывается в оба параметра
- [ ] Сохранение документа корректно записывает метод в оба параметра
- [ ] Загрузка существующего документа отображает метод корректно

**Конфигурация со SlaveKey (GOSTR50):**
- [ ] LinkedParameters игнорируется
- [ ] Slave-параметр скрыт (существующее поведение)

**ELIS интеграция:**
- [ ] При загрузке данных из ELIS метод записывается в оба параметра
- [ ] Флаги elisFilled корректно устанавливаются для обоих параметров

---

## Этап 9: Документация

### Задачи

- [ ] **9.1** Обновить `docs/configs/passport.md`
- [ ] **9.2** Добавить раздел о LinkedParameters
- [ ] **9.3** Обновить CHANGELOG.md

---

## План внедрения

### Фаза 1: Разработка (без изменения конфигураций)

- [ ] Этапы 1-6 выполнены
- [ ] Unit-тесты проходят
- [ ] Параметры без `LinkedParameters` работают как раньше

### Фаза 2: Обновление конфигураций

- [ ] Этап 7 выполнен
- [ ] Ручное тестирование на dev-стенде
- [ ] Code review

### Фаза 3: Релиз

- [ ] Интеграционные тесты проходят
- [ ] Тестирование на staging
- [ ] Документация обновлена
- [ ] Merge в master

---

## Риски и митигация

| Риск | Митигация | Статус |
|------|-----------|--------|
| Конфликт SlaveKey и LinkedParameters | SlaveKey имеет приоритет (явная проверка) | ⬜ |
| Неправильный порядок параметров | Проверка наличия follower в списке | ⬜ |
| Потеря данных при сохранении | Unit-тесты на SaveDocument | ⬜ |
| UI баги с rowspan | E2E тесты на отображение таблицы | ⬜ |

---

## Ожидаемый результат

**До:**
```
| № | Наименование                        | Метод      | Измер. | Результат |
|----|-------------------------------------|------------|--------|-----------|
| 7  | Хлористые соли, мг/дм³              | [Комбо 1]  | 100    | 100       |
| 8  | Хлористые соли, %                   | [Комбо 2]  | 0.01   | 0.01      |
```

**После:**
```
| № | Наименование                        | Метод      | Измер. | Результат |
|----|-------------------------------------|------------|--------|-----------|
| 7  | Хлористые соли, мг/дм³              | [Общий     | 100    | 100       |
| 8  | Хлористые соли, %                   |  комбо]    | 0.01   | 0.01      |
```

---

## Связанные файлы

### Бэкенд (C#)
- [ ] `tn.docgeneral/Passport/Models/Parameter.cs`
- [ ] `tn.docgeneral/Passport/PassportEditModels.cs`
- [ ] `tn.docgeneral/Passport/DocPassport.Editor.cs`

### Фронтенд (TypeScript/Vue)
- [ ] `TN_Doc/Client/document-editor/src/types/passport.types.ts`
- [ ] `TN_Doc/Client/document-editor/src/composables/usePassportEditor.ts`
- [ ] `TN_Doc/Client/document-editor/src/components/passport/PassportQualityTable.vue`
- [ ] `TN_Doc/Client/document-editor/src/components/passport/PassportLinkedParameterGroup.vue` (новый)

### Конфигурации
- [ ] `TN_Doc/Cfg/Passport/CfgEditPassport_MI3532(13).json`
- [ ] `TN_Doc/Cfg/Passport/CfgEditPassport_MI3532(14).json`
- [ ] `TN_Doc/Cfg/Passport/CfgEditPassport_MI3532(15).json`
- [ ] `TN_Doc/Cfg/Passport/CfgEditPassport_MI3532(15)_China.json`
- [ ] `TN_Doc/Cfg/Passport/CfgEditPassportExport.json`
- [ ] `TN_Doc/Cfg/Passport/CfgEditPassportForActNP.json`
- [ ] `TN_Doc/Cfg/Passport/CfgEditPassport_EAC.json`

### Документация
- [ ] `docs/configs/passport.md`
- [ ] `CHANGELOG.md`

---

## История изменений плана

| Дата | Изменение |
|------|-----------|
| 2025-12-11 | Создание плана |
| 2025-12-11 | Добавлены чекбоксы для отслеживания выполнения |
| 2025-12-11 | Этап 1 завершён: добавлены поля LinkedParameters в модели данных |
