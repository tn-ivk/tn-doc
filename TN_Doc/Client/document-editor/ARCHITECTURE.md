# Document Editor - Архитектура

## Обзор

Document Editor - это Vue 3 SPA для редактирования документов с поддержкой специфичной логики для разных типов документов.

## Структура компонентов

### Базовые компоненты

**`DocumentEditor.vue`** - Базовый редактор для всех документов
- Использует композабл `useDocumentEditor` для общей логики
- Без специфичной логики автозаполнения
- Маршрут: `/edit/:deviceId/:docType/:id` (кроме Act)

**`DocumentActEditor.vue`** - Специализированный редактор для Актов
- Наследует всю логику базового редактора через композаблы
- Добавляет автозаполнение связанных полей (Factory, FIO, License)
- Маршрут: `/edit/:deviceId/Act/:id`

### Композаблы (Composables)

**`composables/useDocumentEditor.ts`** - Общая логика редактирования
- ✅ Загрузка документа (`loadDocument`)
- ✅ Сохранение документа (`handleSave`)
- ✅ Экспонирование `SaveDoc()` для главного окна
- ✅ Уведомление родительского окна о состоянии кнопки
- ✅ Обработка beforeunload для несохраненных изменений

**`composables/useActAutoFill.ts`** - Логика автозаполнения для Актов
- ✅ Автозаполнение полей при выборе подписанта сдающей стороны (`Delive_IOF`)
- ✅ Автозаполнение полей при выборе подписанта принимающей стороны (`Receive_IOF`)
- ✅ Связанные поля: Factory, FIO, Lic_Date, Lic_Number
- ✅ Поиск лицензий по ID в справочнике

## Маршрутизация

```typescript
// Специальный маршрут для Актов (обрабатывается первым)
{
  path: '/edit/:deviceId/Act/:id',
  component: DocumentActEditor
}

// Общий маршрут для остальных документов
{
  path: '/edit/:deviceId/:docType/:id',
  component: DocumentEditor
}
```

## Переиспользование кода

### Стили
Оба компонента (`DocumentEditor.vue` и `DocumentActEditor.vue`) используют идентичные CSS стили:
- Material Design 3 переменные (`var(--md-*)`)
- Responsive table layout
- Единая визуальная структура

### Логика
- **Общая логика**: вынесена в `useDocumentEditor`
- **Специфичная логика**: вынесена в отдельные композаблы (например, `useActAutoFill`)

## Добавление новых документов со специфичной логикой

### Вариант 1: Простая логика
Если логика простая, добавьте композабл и используйте в `DocumentEditor.vue`:

```typescript
// В DocumentEditor.vue
const { setupAutoFill } = usePassportAutoFill(); // новый композабл
if (route.params.docType === 'Passport') {
  setupAutoFill();
}
```

### Вариант 2: Сложная логика
Если логика сложная, создайте отдельный компонент:

1. Создайте `DocumentXXXEditor.vue`
2. Используйте `useDocumentEditor` для базовой логики
3. Создайте `useXXXAutoFill` для специфичной логики
4. Добавьте маршрут в `router/index.ts`

```typescript
{
  path: '/edit/:deviceId/XXX/:id',
  component: DocumentXXXEditor
}
```

## Автозаполнение полей (на примере Актов)

### Backend (C#)

Расширение API для передачи метаданных пользователей и справочников:

```csharp
// DocAct.cs - GetUserOptionsForField()
options.AddRange(users.Select(user => new SelectOption {
    Value = user.Id.ToString(),
    Label = user.IOF,
    Data = new Dictionary<string, object> {
        { "factory", user.Factory },
        { "fio", user.FIO },
        { "licId", user.LicId }
    }
}));

// GetEditConfig() - добавление справочников
Dictionaries = new DocumentDictionaries {
    Licenses = Doc.Doc?.Settings?.Dictionarys?.Licenses?.Select(...)
}
```

### Frontend (Vue/TypeScript)

Реактивная обработка изменений через Vue watchers:

```typescript
// useActAutoFill.ts
watch(() => store.formData['Delive_IOF'], (newValue) => {
  const option = findFieldOption('Delive_IOF', newValue);
  const userData = option.data as UserData;

  // Автозаполнение связанных полей
  store.updateField('Delive_Factory', userData.factory);
  store.updateField('Delive_FIO', userData.fio);

  // Поиск и заполнение лицензии
  const license = findLicense(userData.licId);
  store.updateField('Delive_Lic_Date', license.licensesDate);
  store.updateField('Delive_Lic_Number', license.licensesNumber);
});
```

## Преимущества архитектуры

✅ **DRY принцип**: Общая логика в одном месте (`useDocumentEditor`)
✅ **Гибкость**: Легко добавить новые документы с уникальной логикой
✅ **Переиспользование**: Стили и базовая логика переиспользуются
✅ **Типобезопасность**: TypeScript интерфейсы для всех данных
✅ **Тестируемость**: Композаблы легко тестировать изолированно
✅ **Читаемость**: Разделение ответственности между композаблами
