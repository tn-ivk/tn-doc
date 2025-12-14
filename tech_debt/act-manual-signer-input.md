# План: Ручной ввод ФИО подписантов в DocumentActEditor

**Дата:** 2025-12-14
**Статус:** Запланировано
**Ветка:** feature/signers-manual-input-2

## Цель

Добавить возможность ручного ввода полей "И.О. Фамилия" для представителей принимающей и сдающей стороны в редакторе Act (аналогично функционалу в Passport).

## Текущее состояние

- `Delive_IOF` / `Receive_IOF` — combobox без кнопки редактирования
- Автозаполнение связанных полей через `useActAutoFill`
- Индикаторы истории в Act **не используются**

## План реализации

### Этап 1: Создать диалог `ActManualSignerDialog.vue`

**Файл:** `TN_Doc/Client/document-editor/src/components/act/ActManualSignerDialog.vue`

Отдельный диалог для ручного ввода ФИО в Act:

```vue
<template>
  <Dialog
    :visible="visible"
    modal
    header="Ручной ввод"
    :closable="false"
    :style="{ minWidth: '400px' }"
    @update:visible="emit('update:visible', $event)"
  >
    <div class="dialog-body">
      <p class="field-label-text" v-if="fieldLabel">{{ fieldLabel }}</p>
      <div class="dialog-field">
        <input
          ref="nameInput"
          v-model="form.name"
          type="text"
          placeholder="И.О.Фамилия"
          @keyup.enter="handleConfirm"
        />
        <small v-if="validationError" class="validation-error">
          {{ validationError }}
        </small>
      </div>
    </div>

    <template #footer>
      <button class="btn btn-text" @click="handleCancel">Отмена</button>
      <button class="btn btn-primary" :disabled="!canConfirm" @click="handleConfirm">
        Сохранить
      </button>
    </template>
  </Dialog>
</template>
```

**Props:**
```ts
interface Props {
  visible: boolean;
  fieldLabel?: string;
  currentName?: string;
  invalidChars?: string[];
}
```

**Events:**
```ts
emit('update:visible', value: boolean)
emit('confirm', payload: { name: string })
```

---

### Этап 2: Создать компонент `ActSignerField.vue`

**Файл:** `TN_Doc/Client/document-editor/src/components/act/ActSignerField.vue`

Компонент поля IOF с кнопкой редактирования (без индикаторов истории):

```vue
<template>
  <div class="act-signer-field">
    <div class="select-container">
      <FormField
        :field="field"
        :modelValue="modelValue"
        :hide-label="true"
        :invalidChars="invalidChars"
        @update:modelValue="handleSelectChange"
      />

      <!-- Кнопка редактирования внутри Select -->
      <button
        class="edit-signer-btn"
        type="button"
        @click="openDialog"
        title="Ручной ввод..."
      >
        <i class="pi pi-pen-to-square"></i>
      </button>
    </div>

    <ActManualSignerDialog
      v-model:visible="isDialogVisible"
      :fieldLabel="field.label"
      :currentName="currentSignerName"
      :invalidChars="invalidChars"
      @confirm="handleManualConfirm"
    />
  </div>
</template>
```

**Props:**
```ts
interface Props {
  field: FormField;
  modelValue: any;
  invalidChars?: string[];
}
```

**Events:**
```ts
emit('update:modelValue', value: string)  // ID пользователя
emit('update:label', label: string)       // ФИО для БД
```

**Логика:**
```ts
// Получить текущее ФИО для предзаполнения диалога
const currentSignerName = computed(() => {
  const option = field.options?.find(opt => opt.value === modelValue);
  return option?.label || '';
});

// При выборе из списка
function handleSelectChange(value: string) {
  const option = field.options?.find(opt => opt.value === value);
  emit('update:modelValue', value);
  emit('update:label', option?.label || '');
}

// При ручном вводе
function handleManualConfirm(payload: { name: string }) {
  const name = payload.name.trim();

  // Проверяем дубликат
  const existing = field.options?.find(
    opt => opt.label.toLowerCase() === name.toLowerCase()
  );

  if (existing) {
    emit('update:modelValue', existing.value);
    emit('update:label', existing.label);
  } else {
    // Создаём новую опцию с префиксом manual_
    const newId = `manual_${Date.now()}`;
    field.options?.push({ value: newId, label: name, selected: false });

    emit('update:modelValue', newId);
    emit('update:label', name);
  }

  isDialogVisible.value = false;
}
```

---

### Этап 3: Модифицировать `DocumentActEditor.vue`

**Импорты:**
```ts
import ActSignerField from '@/components/act/ActSignerField.vue';
```

**Helper функция:**
```ts
const isIofField = (field: FormField): boolean => {
  return field.key === 'Delive_IOF' || field.key === 'Receive_IOF';
};
```

**Template (изменения в цикле по полям):**
```vue
<tr v-for="field in store.fields" :key="field.key">
  <td class="editor-label-cell">
    <div class="label-wrapper">
      <span class="label-text">{{ field.label }}</span>
      <span v-if="field.required" class="required-mark">*</span>
    </div>
  </td>
  <td class="editor-input-cell">
    <!-- Поля IOF с кнопкой ручного ввода -->
    <ActSignerField
      v-if="isIofField(field)"
      :field="field"
      :modelValue="store.formData[field.key]"
      :invalidChars="store.config?.invalidChars || []"
      @update:modelValue="(value) => store.updateField(field.key, value)"
      @update:label="(label) => store.updateField(`${field.key}__label`, label)"
    />

    <!-- Остальные поля -->
    <FormField
      v-else
      :field="field"
      :modelValue="store.formData[field.key]"
      :hide-label="true"
      :invalidChars="store.config?.invalidChars || []"
      @update:modelValue="(value) => store.updateField(field.key, value)"
    />
  </td>
</tr>
```

---

### Этап 4: Модифицировать `useActAutoFill.ts`

**Изменение:** при `manual_*` ID не заполнять связанные поля автоматически.

```ts
const handleDeliveIOFChange = (newValue: string) => {
  if (!newValue) {
    // Очищаем связанные поля
    store.updateField('Delive_Factory', '');
    store.updateField('Delive_FIO', '');
    store.updateField('Delive_Lic_Date', '');
    store.updateField('Delive_Lic_Number', '');
    return;
  }

  // Пропускаем автозаполнение для ручного ввода
  if (newValue.startsWith('manual_')) {
    return;
  }

  // ... остальная логика без изменений
};

const handleReceiveIOFChange = (newValue: string) => {
  if (!newValue) {
    store.updateField('Receive_Factory', '');
    store.updateField('Receive_FIO', '');
    store.updateField('Receive_Lic_Date', '');
    store.updateField('Receive_Lic_Number', '');
    return;
  }

  // Пропускаем автозаполнение для ручного ввода
  if (newValue.startsWith('manual_')) {
    return;
  }

  // ... остальная логика без изменений
};
```

---

## Структура файлов после рефакторинга

```
src/components/
├── act/
│   ├── ActSignerField.vue        # НОВЫЙ - поле IOF с кнопкой редактирования
│   └── ActManualSignerDialog.vue # НОВЫЙ - диалог ручного ввода для Act
├── FormField.vue                 # Без изменений
└── ...

src/views/
└── DocumentActEditor.vue         # Модифицирован

src/composables/
└── useActAutoFill.ts             # Модифицирован (проверка manual_*)
```

---

## Сводка изменений

| Файл | Действие |
|------|----------|
| `components/act/ActManualSignerDialog.vue` | Создать |
| `components/act/ActSignerField.vue` | Создать |
| `views/DocumentActEditor.vue` | Модифицировать |
| `composables/useActAutoFill.ts` | Модифицировать |

## Связанные файлы (для справки)

- `TN_Doc/Client/document-editor/src/components/SignerFieldGroup.vue` — аналогичный функционал в Passport
- `TN_Doc/Client/document-editor/src/components/ManualSignerDialog.vue` — диалог из Passport (не переиспользуем)
- `TN_Doc/Cfg/Act/CfgEditAct.json` — конфигурация полей Act
