# Баг: Переключение вкладок в Конфигураторе при клике на паспорт

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
