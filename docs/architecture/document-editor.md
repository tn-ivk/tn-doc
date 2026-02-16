# Document Editor Architecture

## Обзор

Document Editor — Vue 3 SPA для редактирования документов в браузере. Редактор используется для паспортов качества, актов и других типов документов, которые реализуют `IDocumentEditor`.

- **Исходники**: `TN_Doc/Client/document-editor/`
- **Production build**: `TN_Doc/wwwroot/document-editor/`
- **Base URL**: `/document-editor/`
- **Dev server**: `npm run dev:editor` (порт 5174, общий с Configurator)

## Маршруты

```text
/document-editor/edit/:deviceId/Passport/:id
/document-editor/edit/:deviceId/Act/:id
/document-editor/edit/:deviceId/:docType/:id
/document-editor/error
```

Маршруты определены в `TN_Doc/Client/document-editor/src/router/index.ts`.

## Интеграция с backend

Document Editor использует REST API контроллера `DocumentEditController`:

- `GET /api/documents/{deviceId}/{docType}/edit/{id}` — получить `DocumentEditConfig`.
- `POST /api/documents/{deviceId}/{docType}/save/{id}` — сохранить документ.
- `POST /api/documents/{deviceId}/{docType}/update/{id}` — обновить паспорт после подтверждения от ИВК.

Документные библиотеки загружаются через `IDocModuleLoader` и должны реализовывать:

```csharp
public interface IDocumentEditor
{
    DocumentEditConfig GetEditConfig(int id);
    bool SaveDocument(int id, Dictionary<string, object> values);
}
```

Для `/update` требуется реализация `IDocUpdater` (используется только для `Passport`).

## Структура фронтенда

### Представления

- `DocumentPassportEditor.vue` — паспорт качества.
- `DocumentActEditor.vue` — акт.
- `DocumentEditor.vue` — универсальный редактор для остальных типов.
- `ErrorPage.vue` — вывод ошибок маршрутизации/загрузки.

### Store

`TN_Doc/Client/document-editor/src/stores/documentStore.ts` хранит:

- `config` (`DocumentEditConfig`), `fields`, `formData`
- `formHistory` (история изменений)
- флаги `isLoading`, `isSaving`, `isDirty`, `canSave`

### Composables

- `useDocumentEditor` — загрузка/сохранение документа, общие хелперы.
- `usePassportEditor` — логика таблицы качества, методов, связанных параметров.
- `useFieldHistory` — запись истории (`Manual`, `ELIS`, `IVK`, `Auto`).
- `useElisIntegration` — приём данных ELIS через `postMessage`.
- `useActAutoFill` — автозаполнение полей подписантов в акте.

## Паспорт качества: ключевые механизмы

- **LinkedParameter**: объединение выбора метода для пары параметров.
  - Компонент: `PassportLinkedParameterGroup.vue`.
- **SlaveKey**: мастер-слейв параметры, ведомые скрыты в таблице.
- **IsBallast**: балластные параметры синхронизируют `result` с `value` и скрывают ручное редактирование результата.
- **Field History**: индикаторы источника данных через `FieldHistoryIndicator.vue`.
  - История возвращается в `initialValues` с суффиксом `__history`.
  - Визуальная подсказка отображается через `v-tooltip`.

## Акт: особенности

- Используется табличный редактор с `FormField` и `ActSignerField`.
- `ActSignerField` поддерживает ручной ввод ФИО и автозаполнение через словари.
- Логика автозаполнения реализована в `useActAutoFill`.
- Для Export_Rus шаблона акта (`Act_N_GOSTR50.2.040(G)(Export_Rus).frx`) поддержан посменный вывод массы нетто прописью:
  - если по сменам различаются обозначения нефти по ГОСТ Р 51858, выводится чередование блоков "масса прописью + обозначение";
  - если обозначение единое, используется общий вывод массы из `Common.ValueInWords.WholePart`.

## Интеграция с ELIS

Document Editor ожидает данные ELIS через `postMessage` из родительского окна:

```js
{ type: 'ELIS_DATA', payload: { ... } }
```

Компосабл `useElisIntegration` обрабатывает событие и заполняет `formData`, выставляет `__elisFilled` и создаёт записи истории.

## Сборка и запуск

```bash
# Dev server
cd TN_Doc/Client
npm run dev:editor

# Production build
npm run build:editor
```

Configurator и Document Editor используют один порт 5174, запускайте их по очереди или меняйте `server.port` в `vite.config.ts`.

## Ключевые файлы

- `TN_Doc/Client/document-editor/src/views/DocumentPassportEditor.vue`
- `TN_Doc/Client/document-editor/src/views/DocumentActEditor.vue`
- `TN_Doc/Client/document-editor/src/stores/documentStore.ts`
- `TN_Doc/Client/document-editor/src/composables/useDocumentEditor.ts`
- `TN_Doc/Client/document-editor/src/composables/usePassportEditor.ts`
- `TN_Doc/Client/document-editor/src/composables/useFieldHistory.ts`
- `TN_Doc/Client/document-editor/src/composables/useElisIntegration.ts`
