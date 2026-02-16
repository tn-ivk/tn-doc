# UI дизайн (текущий интерфейс)

## Обзор

TN_Doc — веб‑приложение ASP.NET Core MVC с Razor‑представлениями и статическими JS/CSS ресурсами.
UI реализован на **Bootstrap + jQuery + DataTables** без SPA‑фреймворков.

## Основные экраны

### Главная страница
`TN_Doc/Views/Home/Index.cshtml`

- Левая панель: фильтры (устройство, документ, диапазон дат), таблица результатов
- Правая панель: просмотр PDF (iframe `.FR`) и панель действий (экспорт/печать)
- Кнопка **«Справочники»** открывает модальный редактор

### Модальный редактор справочников
- Вкладки: «Персонал» и «Методы испытаний»
- Реализован в `DirEditorComponentScript.js`
- Сохраняет данные через `DirEditorController`

### Редактирование документов
- Режим редактирования открывает HTML‑форму в iframe (`wwwroot/HTML/DocEdit*.html`)
- Сохранение через `Home/SaveDoc` и `Home/UpdateDoc` (для паспорта)

## Стили и ассеты

- `TN_Doc/wwwroot/css/` — стили
- `TN_Doc/wwwroot/js/` — логика UI
- `TN_Doc/wwwroot/lib/` — библиотечные зависимости

## Последние изменения UI (январь 2026)

- `TN_Doc/wwwroot/css/site.css`:
  - для `select#ComboboxTemplateDoc` установлен `max-width: 450px` (ограничение разъезжания верхней панели)
  - удалено дублирование CSS-блока sticky footer
- `TN_Doc/wwwroot/css/LeftPanel.css`:
  - ширина кнопки **«Получить данные»** увеличена `40% → 45%`
  - добавлен `white-space: nowrap` у `.filterButton`
  - добавлен `overflow: hidden` для контейнера `.cont`
- `TN_Doc/Views/Home/Index.cshtml`:
  - удалён лишний HTML-тег `<body>` внутри Razor-представления
  - ширина ячейки кнопки режима редактирования уменьшена `250px → 200px`

## Скриншоты

Скрипты и ассеты для документации: `docs/ui-screenshots/`.
