# План: Ручной ввод подписанта только при включённом ELIS

## Задача

Кнопка ручного ввода подписанта в форме редактирования Act должна быть доступна только при `IsUsedElis == true`.

## Вариант реализации

Передаём флаг на уровне поля (`allowManualInput` в `FormField`) — это более гибкое решение, позволяющее управлять кнопкой для каждого IOF-поля отдельно.

---

## Этап 1: Бэкенд — расширение модели FormField

**Файл:** `tn.docgeneral/TN.DocGeneral/IDocumentEditor.cs`

Добавить новое свойство в класс `FormField`:

```csharp
/// <summary>
/// Разрешён ли ручной ввод для поля (показывается кнопка редактирования)
/// Используется для IOF-полей в Act при включённом ELIS
/// </summary>
public bool AllowManualInput { get; set; }
```

---

## Этап 2: Бэкенд — установка флага в DocAct

**Файл:** `tn.docgeneral/Act/DocAct.cs`

### 2.1. В методе `GetEditConfig` получить флаг ELIS:

```csharp
public DocumentEditConfig GetEditConfig(int id)
{
    _logger.Trace($"...");
    GetViewDoc(id);

    var isElisUsed = _appConfig.IsUsedElis(_deviceId); // ← добавить

    try
    {
        var config = new DocumentEditConfig
        {
            ...
            Fields = BuildFieldsFromConfig(isElisUsed), // ← передать флаг
            ...
        };
```

### 2.2. Изменить сигнатуру метода `BuildFieldsFromConfig`:

```csharp
private List<FormField> BuildFieldsFromConfig(bool isElisUsed)
```

### 2.3. Установить `AllowManualInput` для IOF-полей:

```csharp
if (item.Type == "list")
{
    // ... существующий код ...

    // Ручной ввод разрешён только при включённом ELIS
    if (item.Key == "Delive_IOF" || item.Key == "Receive_IOF")
    {
        field.AllowManualInput = isElisUsed;
    }
}
```

---

## Этап 3: Фронтенд — расширение типа FormField

**Файл:** `TN_Doc/Client/document-editor/src/types/document.types.ts`

Добавить свойство в интерфейс `FormField`:

```typescript
export interface FormField {
  // ... существующие поля ...

  /** Разрешён ли ручной ввод (кнопка редактирования) */
  allowManualInput?: boolean;
}
```

---

## Этап 4: Фронтенд — условный рендеринг кнопки

**Файл:** `TN_Doc/Client/document-editor/src/components/act/ActSignerField.vue`

### 4.1. Добавить `allowManualInput` в props:

```typescript
interface Props {
  field: FormFieldType;
  modelValue: any;
  invalidChars?: string[];
  allowManualInput?: boolean; // ← добавить
}
```

### 4.2. Показывать кнопку условно:

```vue
<button
  v-if="allowManualInput"
  class="edit-signer-btn"
  type="button"
  @click="openDialog"
  title="Ручной ввод..."
>
  <i class="pi pi-pen-to-square"></i>
</button>
```

---

## Этап 5: Фронтенд — передача флага в компонент

**Файл:** `TN_Doc/Client/document-editor/src/views/DocumentActEditor.vue`

```vue
<ActSignerField
  v-if="isIofField(field)"
  :field="field"
  :modelValue="store.formData[field.key]"
  :invalidChars="store.config?.invalidChars || []"
  :allowManualInput="field.allowManualInput"
  @update:modelValue="..."
  @update:label="..."
/>
```

---

## Итоговый флоу

```
CfgApp.json (IsUsedElis: true/false)
    ↓
_appConfig.IsUsedElis(_deviceId)
    ↓
DocAct.GetEditConfig() → BuildFieldsFromConfig(isElisUsed)
    ↓
FormField { Key: "Delive_IOF", AllowManualInput: true/false }
    ↓
Vue: ActSignerField :allowManualInput="field.allowManualInput"
    ↓
v-if="allowManualInput" → показать/скрыть кнопку
```

---

## Изменяемые файлы

| Файл | Изменения |
|------|-----------|
| `tn.docgeneral/TN.DocGeneral/IDocumentEditor.cs` | +1 свойство в FormField |
| `tn.docgeneral/Act/DocAct.cs` | +3 строки в GetEditConfig, +5 строк в BuildFieldsFromConfig |
| `TN_Doc/Client/document-editor/src/types/document.types.ts` | +1 поле в FormField |
| `TN_Doc/Client/document-editor/src/components/act/ActSignerField.vue` | +1 prop, +1 v-if |
| `TN_Doc/Client/document-editor/src/views/DocumentActEditor.vue` | +1 атрибут в компоненте |

---

## Статус

- [x] Этап 1: Бэкенд — расширение модели FormField
- [x] Этап 2: Бэкенд — установка флага в DocAct
- [x] Этап 3: Фронтенд — расширение типа FormField
- [x] Этап 4: Фронтенд — условный рендеринг кнопки
- [x] Этап 5: Фронтенд — передача флага в компонент
- [x] Тестирование (сборка успешна)
