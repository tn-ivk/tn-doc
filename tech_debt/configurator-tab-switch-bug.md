# Баг: Переключение вкладок в Конфигураторе при клике на паспорт

> **СТАТУС: ИСПРАВЛЕН (2026-01-16)**
>
> Исправление применено через Вариант 2: Hash в имени entry file + ViteManifestService.
> См. раздел "Применённое исправление" в конце документа.

## Описание проблемы

При клике на любой паспорт в дереве документов (Конфигуратор → Документы → Паспорта → любой паспорт) активная вкладка неожиданно переключается с "Документы" на "Общие".

**Воспроизведение:**
1. Открыть `/configurator`
2. Дождаться загрузки (5 сек)
3. Перейти на вкладку "Документы"
4. Раскрыть "Паспорта"
5. Кликнуть на любой паспорт (например, "Паспорт для нефтепродукта")
6. **Результат:** Вкладка переключается на "Общие"
7. **Ожидание:** Вкладка остаётся на "Документы", справа отображается редактор паспорта

## Корневая причина

**Vue приложение пересоздаётся при загрузке lazy-loaded chunk.**

### Техническая цепочка событий:

1. Пользователь кликает на паспорт
2. Срабатывает `loadVisualEditor()` в `DocumentConfigEditor.vue`
3. Загружается lazy chunk `configurator.Bmbxp-7o.js`
4. Chunk импортирует зависимости: `from"./configurator.js"`
5. **ПРОБЛЕМА:** Браузер выполняет `configurator.js` (main.ts) ПОВТОРНО
6. Создаётся новый экземпляр Vue приложения
7. Pinia store пересоздаётся (currentConfig = null)
8. Срабатывает `onMounted` в App.vue
9. После 2-секундной задержки вызывается `loadConfig()`
10. Tabs сбрасывается на значение по умолчанию "0" (вкладка "Общие")

### Почему main.ts выполняется повторно?

**Несоответствие URL модулей:**

| Источник | URL |
|----------|-----|
| HTML (Razor view) | `/configurator/configurator.js?v=nd4S60Dheadn3cz5665ubCOUzcGZS91ihjvp0oBwdsk` |
| Lazy chunk import | `./configurator.js` → `/configurator/configurator.js` (БЕЗ ?v=) |

ASP.NET добавляет `asp-append-version="true"` к URL в Razor view, но Vite генерирует lazy chunks с относительными импортами без версии.

**ES Module Registry использует полный URL как ключ.** Браузер воспринимает эти URL как РАЗНЫЕ модули и выполняет main.ts повторно.

## Доказательства

### Серверные логи показывают два вызова `/api/configurator/config`:

```
13:38:52.2109 - GET /api/configurator/config (первая загрузка страницы)
13:40:57.1856 - GET /api/configurator/document-config (клик на паспорт)
13:40:57.1856 - GET /configurator/configurator.Bmbxp-7o.js (lazy chunk)
13:40:57.2695 - "Configurator: инициализация приложения" (main.ts выполняется ПОВТОРНО!)
13:40:59.3577 - GET /api/configurator/config (ВТОРОЙ вызов loadConfig!)
```

Разница 2 секунды между загрузкой chunk и вторым config - это `setTimeout(..., 2000)` в `onMounted`.

### DOM refs меняются:

До клика: `ref=e7, e8, e11...`
После клика: `ref=e506, e507, e510...`

Это подтверждает полное пересоздание Vue компонентов.

---

## Результаты диагностики (2026-01-16)

### Добавлены диагностические логи в файлы:

- `main.ts` - отслеживание инициализации с уникальным ID
- `App.vue` - отслеживание создания компонента и состояния store
- `useVisualEditor.ts` - отслеживание загрузки lazy chunk
- `configStore.ts` - отслеживание создания store и вызовов loadConfig

### Консольные логи при воспроизведении бага:

#### 1. Первоначальная загрузка страницы:
```
[ДИАГНОСТИКА] main.ts выполняется (инициализация #1)
ID текущей инициализации: init_1768592142269_vrcx0b
URL модуля (import.meta.url): http://localhost:5000/configurator/configurator.js?v=WcZ1xtOpQIu...
                                                                                  ↑↑↑↑↑↑↑↑↑↑↑↑
                                                                                  С ВЕРСИЕЙ
[ДИАГНОСТИКА] App.vue создаётся (экземпляр #1)
[ДИАГНОСТИКА] configStore: factory function выполняется (экземпляр #1)
Store ID: store_1768592142353_kbdqv2
```

#### 2. Клик на паспорт - загрузка lazy chunk:
```
[ДИАГНОСТИКА] useVisualEditor: начинаем загрузку lazy chunk
Init ID ДО загрузки: init_1768592142269_vrcx0b
Init Count ДО загрузки: 1
```

#### 3. КРИТИЧЕСКИЙ МОМЕНТ - main.ts выполняется ПОВТОРНО:
```
[ДИАГНОСТИКА] main.ts выполняется (инициализация #2)
ID текущей инициализации: init_1768592181951_qlnv1j
ID предыдущей инициализации: init_1768592142269_vrcx0b
Флаг __CONFIGURATOR_INITIALIZED__: true
URL модуля (import.meta.url): http://localhost:5000/configurator/configurator.js
                                                                               ↑
                                                                         БЕЗ ВЕРСИИ!

[ДИАГНОСТИКА] ОШИБКА: main.ts выполняется ПОВТОРНО! Это вызывает пересоздание Vue приложения.
```

#### 4. Новые экземпляры App.vue и store:
```
[ДИАГНОСТИКА] App.vue создаётся (экземпляр #2)
Component ID: app_1768592182050_s7rgi0
[ДИАГНОСТИКА] configStore: factory function выполняется (экземпляр #2)
Store ID: store_1768592182052_0l37bc
  currentConfig: null    ← СБРОШЕН!
  isLoading: false
```

#### 5. После загрузки chunk:
```
!!! КРИТИЧЕСКАЯ ПРОБЛЕМА: Init ID изменился во время загрузки chunk !!!
Это означает, что main.ts был выполнен повторно!
Init ID до: init_1768592142269_vrcx0b
Init ID после: init_1768592181951_qlnv1j
```

### Подтверждённая корневая причина:

**Несоответствие URL модулей ES Module:**

| Момент | URL модуля |
|--------|------------|
| Загрузка страницы (Razor) | `configurator.js?v=WcZ1xtOpQIuVcVrcmTfZ...` |
| Import из lazy chunk | `configurator.js` (без версии) |

ES Module Registry в браузере использует **полный URL как ключ**. Браузер воспринимает эти URL как **разные модули** и выполняет main.ts повторно.

### Цепочка событий (подтверждено):

1. Lazy chunk `configurator.Cc9kyeaj.js` загружается
2. Chunk содержит: `import{...}from"./configurator.js"` (относительный путь)
3. Браузер резолвит путь: `/configurator/configurator.js` (БЕЗ `?v=`)
4. ES Module Registry не находит модуль с таким URL
5. Браузер загружает и выполняет `configurator.js` как новый модуль
6. `main.ts` выполняется повторно → новый Vue app, новый Pinia store
7. `onMounted` в App.vue срабатывает → через 2 сек. вызывается `loadConfig()`
8. Tabs сбрасывается на "0" (Общие) - дефолтное значение в шаблоне

## Неудачные попытки исправления

1. **`v-if` → `v-show` для loading spinner** - не помогло, проблема глубже
2. **Controlled Tabs с `v-model:value`** - не помогло, ref сбрасывается при пересоздании
3. **Early return в onMounted если config загружен** - не помогло, store пересоздаётся
4. **Защита через `window.__CONFIGURATOR_INITIALIZED__`** - не работает (причина неясна)

## Возможные решения

### 1. Исправить URL несоответствие (рекомендуется)

**Вариант A:** Убрать `asp-append-version` из Razor view
```html
<script type="module" src="~/configurator/configurator.js"></script>
```
*Минус:* Сломает cache busting

**Вариант B:** Настроить Vite для генерации абсолютных путей с версией
```typescript
// vite.config.ts
build: {
  rollupOptions: {
    output: {
      // Использовать хэш в имени файла вместо ?v=
      entryFileNames: 'configurator.[hash].js',
    }
  }
}
```
И обновить Razor view для загрузки файла с хэшем.

**Вариант C:** Использовать import maps в HTML
```html
<script type="importmap">
{
  "imports": {
    "./configurator.js": "/configurator/configurator.js?v=..."
  }
}
</script>
```

### 2. Отказаться от lazy loading для визуальных редакторов

```typescript
// useVisualEditor.ts - заменить динамический импорт на статический
import PassportConfigEditor from '../components/visual-editors/PassportConfigEditor.vue';

const VISUAL_EDITOR_PATTERNS = [
  {
    pattern: /CfgEditPassport.*\.json$/i,
    component: PassportConfigEditor, // вместо () => import(...)
    label: 'Паспорт качества'
  }
];
```
*Минус:* Увеличит размер основного бандла

### 3. Вынести инициализацию Pinia store наружу

Создать store до mount приложения и передать его извне, чтобы он не пересоздавался.

## Затронутые файлы

- `TN_Doc/Client/configurator/src/main.ts` - точка входа
- `TN_Doc/Client/configurator/src/App.vue` - главный компонент
- `TN_Doc/Client/configurator/src/composables/useVisualEditor.ts` - lazy loading
- `TN_Doc/Client/configurator/src/components/DocumentConfigEditor.vue` - использует lazy loading
- `TN_Doc/Client/configurator/vite.config.ts` - конфигурация сборки
- `TN_Doc/Views/ConfiguratorView/Index.cshtml` - Razor view с asp-append-version

## Приоритет

**Высокий** - баг делает невозможным редактирование конфигураций паспортов через визуальный редактор.

## Дата обнаружения

2026-01-16

---

## Рекомендуемое решение (на основе диагностики)

### Вариант 1: Убрать `asp-append-version` (быстрое решение)

**Файл:** `TN_Doc/Views/ConfiguratorView/Index.cshtml`

```diff
- <script type="module" src="~/configurator/configurator.js" asp-append-version="true"></script>
+ <script type="module" src="~/configurator/configurator.js"></script>
```

**Плюсы:**
- Исправляет баг мгновенно
- Минимальные изменения

**Минусы:**
- Теряется cache busting для entry point (но lazy chunks уже имеют hash в имени)

### Вариант 2: Использовать hash в имени entry file (правильное решение)

**Файл:** `TN_Doc/Client/configurator/vite.config.ts`

```typescript
build: {
  rollupOptions: {
    output: {
      entryFileNames: 'configurator.[hash].js',  // ← добавить hash
      chunkFileNames: 'configurator.[hash].js',
      // ...
    }
  }
}
```

**Файл:** `TN_Doc/Views/ConfiguratorView/Index.cshtml`

Потребуется механизм для подстановки актуального имени файла с хешем (например, через manifest.json).

**Плюсы:**
- Сохраняется cache busting
- URL модулей будут совпадать

**Минусы:**
- Требует доработки Razor view для чтения manifest.json

### Вариант 3: Отказаться от lazy loading (временное решение)

**Файл:** `TN_Doc/Client/configurator/src/composables/useVisualEditor.ts`

```typescript
// Заменить динамический импорт на статический
import PassportConfigEditor from '../components/visual-editors/PassportConfigEditor.vue';

const VISUAL_EDITOR_PATTERNS = [
  {
    pattern: /CfgEditPassport.*\.json$/i,
    component: PassportConfigEditor,  // вместо () => import(...)
    label: 'Паспорт качества'
  }
];
```

**Плюсы:**
- Исправляет баг
- Простая реализация

**Минусы:**
- Увеличивает размер основного бандла (~6KB)
- Не масштабируется при добавлении новых визуальных редакторов

---

## Применённое исправление (2026-01-16)

### Выбран Вариант 2: Hash в имени entry file + ViteManifestService

#### Изменённые файлы:

1. **`TN_Doc/Client/configurator/vite.config.ts`**
   - Добавлен `manifest: true` для генерации `.vite/manifest.json`
   - Изменён `entryFileNames` на `configurator.[hash].js`
   - Изменён `assetFileNames` для CSS на `configurator.[hash].css`

2. **`TN_Doc/Services/ViteManifestService.cs`** (новый файл)
   - Сервис для чтения Vite manifest.json
   - Методы `GetEntryFile()` и `GetCssFile()` для получения путей с хэшами
   - Кэширование manifest в памяти

3. **`TN_Doc/Extensions/IServiceCollectionExtensions.cs`**
   - Добавлен метод `AddViteManifest()` для регистрации сервиса

4. **`TN_Doc/Startup.cs`**
   - Добавлен вызов `services.AddViteManifest()`

5. **`TN_Doc/Views/ConfiguratorView/Index.cshtml`**
   - Инжектируется `IViteManifestService`
   - Пути к JS и CSS файлам получаются из manifest
   - Убран `asp-append-version` (хэш теперь в имени файла)

#### Результат сборки:

```
configurator.Leouq4GF.js     - entry point (с хэшем)
configurator.CoBeEF1v.js     - lazy chunk (с хэшем)
configurator.BW08Wrts.css    - стили (с хэшем)
.vite/manifest.json          - маппинг файлов
```

#### Lazy chunk теперь корректно импортирует:

```javascript
// БЫЛО (вызывало повторную загрузку main.ts):
import{...}from"./configurator.js"

// СТАЛО (URL совпадают):
import{...}from"./configurator.Leouq4GF.js"
```

#### Результат тестирования:

```
Init ID ДО загрузки lazy chunk: init_1768593837763_f6ptd8
Init ID ПОСЛЕ загрузки lazy chunk: init_1768593837763_f6ptd8
Init Count: 1 (не изменился!)

✓ Init ID не изменился (ОК)
✓ Вкладка осталась на "Документы"
✓ Визуальный редактор загрузился корректно
✓ DOM refs не пересоздались
```

#### Преимущества решения:

- Сохранён cache busting (хэш в имени файла)
- URL модулей совпадают при загрузке из HTML и из lazy chunks
- Централизованный сервис для всех Vue приложений
- Легко расширить на statusbar и document-editor
