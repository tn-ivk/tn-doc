---
title: Дизайн TN_Doc — обзор, палитра, элементы, скриншоты и рекомендации
date: 2026-01-16
---

## Обзор интерфейса

TN_Doc — веб‑приложение ASP.NET Core (Razor Views) c Bootstrap‑гридом и jQuery‑компонентами, с отдельными SPA‑модулями на Vue 3:
- **StatusBar** — индикаторы статуса устройств/сервисов (Vue 3 + SignalR)
- **Configurator** — веб-интерфейс настроек приложения (Vue 3 + PrimeVue)
- **Document Editor** — редактор документов в браузере (Vue 3 + PrimeVue)

Главная страница (`Home/Index`) содержит левую панель фильтров и правую панель просмотра документа (PDF в iframe), верхнее меню действий и модальные окна для «Справочников» и «Конфигуратора» (в iframe).

## Централизация цветов (v1.4.3+)

**Все цвета приложения централизованы в `/TN_Doc/wwwroot/css/material3.css`** с использованием CSS переменных. Это упрощает изменение темы оформления и обеспечивает единообразие визуального стиля.

### Основные CSS переменные

#### Основные цвета (Primary)
- `--md-primary: #1E88E5` — основной синий (Blue 600)
- `--md-primary-hover: #1565C0` — синий при наведении (Blue 700)
- `--md-primary-active: #0D47A1` — синий при нажатии (Blue 800)
- `--md-primary-light: #E7F1FF` — светлый фон для подсветки

#### Вторичные и статусные цвета
- `--md-secondary: #43A047` — вторичный зелёный
- `--md-error: #E53935` — цвет ошибок
- `--md-error-bootstrap: #DC3545` — Bootstrap danger color
- `--md-error-light: #FFF8F8` — светлый фон ошибок
- `--md-warning: #F9A825` — цвет предупреждений

#### Фоны и поверхности
- `--md-surface: #FAFAFA` — основной фон
- `--md-surface-variant: #F1F3F4` — карточки, заголовки таблиц
- `--md-white: #FFFFFF` — белый цвет
- `--md-gray-light: #CCCCCC` — светло-серый

#### Границы (Borders)
- `--md-outline: #CFD8DC` — основные границы
- `--md-outline-light: #E0E0E0` — светлые границы
- `--md-border: #CED4DA` — границы полей ввода
- `--md-border-light: #E5E5E5` — очень светлые границы

#### Текст
- `--md-text: #212121` — основной текст
- `--md-text-secondary: #5F6368` — вторичный текст
- `--md-text-tertiary: #333333` — третичный текст
- `--md-text-muted: #495057` — приглушённый текст
- `--md-text-light: #666666` — светло-серый текст

#### Серые кнопки
- `--md-gray-600: #6C757D` — серые кнопки
- `--md-gray-700: #5A6268` — серые кнопки при наведении
- `--md-gray-800: #545B62` — серые кнопки при нажатии

#### Интеграция с ELIS
- `--md-elis-highlight: #8FD19E` — подсветка данных из ELIS

#### Disabled состояния
- `--md-disabled-bg: #EDEFF2` — фон отключённых элементов
- `--md-disabled-text: #6B7280` — текст отключённых элементов
- `--md-disabled-border: #BFCAD6` — границы отключённых элементов

### История централизации (v1.4.3)

В октябре 2025 года была проведена полная централизация цветов. **Заменено 44 hardcoded HEX цвета** в следующих файлах:

| Файл | Количество замен |
|------|------------------|
| `elisRequestWindow.css` | 10 |
| `newstyle.css` | 9 |
| `menu-dropdown.css` | 6 |
| `errorDialogWindow.css` | 6 |
| `site.css` | 6 |
| `LeftPanel.css` | 4 |
| `elisEditForm.css` | 2 |
| `commonEditForm.css` | 1 |
| **Всего** | **44** |

**Результат:** Все цвета определены в одном месте (`material3.css`), что упрощает изменение темы оформления и обеспечивает визуальную согласованность.

## Палитра и стили (устаревшая информация)

> ⚠️ **Внимание:** Эта секция содержит устаревшую информацию о цветовой палитре до централизации (до v1.4.3). Актуальная информация о цветах находится в разделе "Централизация цветов" выше.

<details>
<summary>Устаревшая информация о цветах (до v1.4.3)</summary>

- **Брендовый синий:** `#1b6ec2` (кнопки/hover), `#155a9e` (hover), `#1861ac` (бордеры)
- **Ссылки:** `#0366d6`
- **Сообщения об ошибках/валидатор:** `#dc3545` (фон/бордюр, тултипы)
- **Серые границы/фон:** `#ced4da`, `#e5e5e5`, `#e0e6ef`, `#f5faff`, `#fafbfc`, `#e6f0ff`
- **Текст:** `#495057`, базовый `#222` для списков

</details>

### Источники стилей

- **Основной файл с CSS переменными:** `wwwroot/css/material3.css` (все цвета и токены)
- **Базовые стили:** `wwwroot/css/site.css` — Bootstrap‑совместимые
- **Компоненты интерфейса:**
  - `wwwroot/css/LeftPanel.css`, `wwwroot/css/newstyle.css` — таблицы, кнопки, поля
  - `wwwroot/css/menu-dropdown.css` — меню действий
  - `wwwroot/css/elisRequestWindow.css` — окно запроса ELIS
  - `wwwroot/css/errorDialogWindow.css` — диалог ошибок
  - `wwwroot/css/elisEditForm.css` — форма редактирования ELIS
  - `wwwroot/css/commonEditForm.css` — общая форма редактирования
- **Справочники:** `wwwroot/css/dictionaries/*` — UI справочников (меню слева, таблицы, элементы выбора)
- **StatusBar:** `wwwroot/statusbar/status-bar.css` — визуальные состояния индикаторов статуса
- **Vue компоненты (v1.4.4+):**
  - `TN_Doc/Client/statusbar/src/` — StatusBar (Vue 3 + PrimeVue)
  - `TN_Doc/Client/configurator/src/` — Configurator (Vue 3 + PrimeVue)
  - `TN_Doc/Client/document-editor/src/` — Document Editor (Vue 3 + PrimeVue)
  - `TN_Doc/Client/document-editor/src/components/history/` — компоненты истории изменений полей

## Ключевые графические элементы

### Кнопки действий
- **Основные (Primary):** фон `var(--md-primary)`, hover `var(--md-primary-hover)`, active `var(--md-primary-active)`
- **Серые (Secondary):** фон `var(--md-gray-600)`, hover `var(--md-gray-700)`, active `var(--md-gray-800)`
- **Disabled:** фон `var(--md-disabled-bg)`, текст `var(--md-disabled-text)`, граница `var(--md-disabled-border)`
- **Высота:** `var(--md-control-height)` (35px)
- **Радиус скругления:** `var(--md-radius)` (8px)

### Dropdown меню
- **Фон:** `var(--md-surface-variant)`
- **Граница:** `var(--md-outline)`
- **Hover:** фон `var(--md-primary)` с белым текстом
- **Выбранный элемент:** фон `var(--md-primary-light)`

### Индикаторы статуса (StatusBar)
- **Online:** зелёный (`var(--p-green-700)` на фоне `var(--p-green-100)`)
- **Offline:** красный с миганием (`var(--p-red-700)` на фоне `var(--p-red-100)`)
- **NDV (нет данных):** серый (`var(--md-text-secondary)`)
- **Warning:** жёлтый (`var(--p-yellow-700)` на фоне `var(--p-yellow-100)`)

### Окно просмотра протокола ELIS
- Файл стилей: `wwwroot/css/elisRequestWindow.css`
- Актуальная ширина модального окна:
  - `min-width: 1050px`
  - `max-width: 1350px`
- Изменение внесено для более комфортного чтения длинных протоколов без горизонтальной прокрутки на широких экранах.

### Валидация форм/полей
- **Бордер ошибки:** `var(--md-error-bootstrap)`
- **Фон ошибки:** `var(--md-error-light)`
- **Текст ошибки:** `var(--md-error)`

### Табличные списки
- **Заголовки:** фон `var(--md-surface-variant)`, текст `var(--md-text)`
- **Границы:** `var(--md-outline)`
- **Hover:** смесь `var(--md-primary)` с `var(--md-surface)` (7% прозрачность)
- **Выбранная строка:** фон `var(--md-primary)`, текст белый

### Индикаторы истории полей (v1.4.4+)
- **Позиция:** компактные значки в правом верхнем углу поля ввода
- **Размеры:** 6px для текстовых меток (ЕЛИС/ИВК), 14px для иконок (Manual/Unknown)
- **Цвета источников:**
  - ELIS: `#4CAF50` (зелёный)
  - Manual: `#2196F3` (синий)
  - IVK: `#FF9800` (оранжевый)
  - Unknown: `#9E9E9E` (серый)

## Навигация и окна

### Главная страница
- **Левая панель:** фильтры (устройство, документ, даты), таблица найденных документов
- **Правая панель:** просмотр PDF документа в iframe
- **Верхнее меню:** кнопки действий (Экспорт, Печать, Отправить, Справочники, Конфигуратор)
- **StatusBar:** индикаторы статуса устройств и сервисов в нижней части

### Модальные окна
- **Справочники:** вкладки, левое меню категорий, таблицы данных
- **Конфигуратор:** встраивается через iframe (`/configurator`)
  - Отдельный SPA на Vue 3 + PrimeVue
  - Вкладки: Настройки СИ, ELIS, OPC, Приложение

### Document Editor (v1.4.4+, активная разработка)
- **Назначение:** Редактирование документов в браузере через Vue SPA
- **Технологии:** Vue 3 + TypeScript + PrimeVue
- **Основные компоненты:**
  - `DocumentPassportEditor` — главный редактор паспорта
  - `PassportQualityTable` — таблица параметров качества
  - `FormFieldWithHistory` — поля с индикатором истории
  - Компоненты истории: `FieldHistoryIndicator` (tooltip)
- **Функции:**
  - Редактирование параметров качества (value/method/result/document)
  - Интеграция с ELIS (загрузка данных из лабораторных протоколов)
  - История изменений полей (отслеживание источников: ELIS/Manual/IVK)
  - Автозаполнение зависимых параметров
  - Валидация диапазонов значений
- **Статус:** Активная разработка на ветке `developWork`, используется в production сборках

## Скриншоты

Скриншоты генерируются Playwright и сохраняются в `docs/assets/ui/`:
- `home/home-full.png` — главная
- `home/mainTable.png`, `home/leftPanel.png`, `home/rightPanel.png`, `home/dataTable.png`
- `home/menu-dropdown.png`
- `dictionaries/modal.png`, `dictionaries/side-menu.png`, `dictionaries/content.png`
- `configurator/modal.png`, `configurator/iframe-full.png`, `configurator/frame-*.png`
- `statusbar/*.png`
- `home/pdf-frame.png`

См. раздел «Как воспроизвести», чтобы обновить снимки.

## Как воспроизвести и обновить скриншоты

1. Запустите бэкенд TN_Doc локально (Development), чтобы был доступен `http://localhost:5000` или задайте `TN_DOC_BASE_URL`.
2. Установите зависимости в `docs/ui-screenshots`:
   ```bash
   cd docs/ui-screenshots
   npm i
   npx playwright install --with-deps
   npm run screenshots
   ```
3. Скриншоты появятся в `docs/assets/ui/`.

Переменная окружения: `TN_DOC_BASE_URL=http://localhost:38509` (или ваш порт IIS Express / Kestrel).

## Рекомендации по улучшению дизайна

### Выполненные улучшения (v1.4.3+)

✅ **Централизация цветов (v1.4.3):** Все цвета вынесены в CSS переменные в `material3.css`. Заменено 44 hardcoded HEX цвета в 8 файлах.

✅ **Дизайн-токены (v1.4.3):** Вынесены размеры, радиусы, шрифты в `:root` CSS-переменные:
- `--md-control-height`, `--md-control-height-sm`, `--md-control-height-lg`
- `--md-radius`, `--md-spacing-1`, `--md-spacing-2`
- `--md-font-family`, `--md-font-size-base`, `--md-font-weight-*`

✅ **Disabled состояния (v1.4.3):** Централизованы стили для disabled элементов через `--md-disabled-bg/text/border`

✅ **Серые кнопки (v1.4.3):** Кнопки Отмена/Удаление используют серый цвет вместо красного через `--md-gray-600/700/800`

### Планируемые улучшения

1. **Консолидация стилей:** Объединить дробные стилевые файлы, убрать дубли (`.invalid-cell-content`, повторяющиеся селекторы)
2. **Контраст и доступность:**
   - Проверить контраст ссылок на светлом фоне
   - Добавить focus-ring для клавиатурной навигации
   - Тестирование с WCAG 2.1 AA
3. **Единые состояния элементов:** Согласовать hover/active для всех dropdown меню, выбрать один паттерн
4. **Сетка и отступы:** Унифицировать вертикальные ритмы (8px-шкала), выровнять отступы в фильтрах и панели действий
5. **Иконки:** Перейти на современный набор (`@tabler/icons` или PrimeIcons) и убрать смешение FontAwesome версий
6. **Компоненты таблиц:** Применить единый Vue-компонент таблицы с заголовком, сортировкой и пустыми состояниями
7. **Перенос модалок в частичные вьюхи:** Вынести большие модальные разметки в отдельные `.cshtml` partials для читаемости, добавить ARIA-атрибуты
8. **Темизация:** Добавить светлую/тёмную темы через CSS-переменные (переключатель в меню), сохранить брендовые оттенки
9. **StatusBar оптимизация:** Добавить компактный режим и tooltip-описания статусов
10. **Document Editor UI полировка:** Финализация стилей для production-релиза (v1.4.4)

## Компоненты истории изменений полей (v1.4.4+)

### FieldHistoryIndicator - Индикатор источника данных

**Назначение:** Визуальная индикация источника данных в правом верхнем углу поля ввода

**Расположение:** `TN_Doc/Client/document-editor/src/components/history/FieldHistoryIndicator.vue`

**Визуальные характеристики:**
- **Позиция:** Абсолютное позиционирование (top: 4px, right: 4px) внутри родительского элемента `position: relative`
- **Размер текстовых меток (ЕЛИС/ИВК):**
  - Font-size: **6px** (очень компактный размер)
  - Line-height: 1
  - Font-weight: 600 (полужирный)
  - Padding: 2px 4px
  - Border-radius: 3px
- **Размер иконок (Manual/Unknown):**
  - Font-size: **14px**
  - Padding: 2px
  - Border-radius: 50% (круглая форма)
- **Фон:** rgba(255, 255, 255, 0.9) с полупрозрачностью
- **Тень:** box-shadow 0 1px 3px rgba(0, 0, 0, 0.1)

**Цветовая схема по источникам:**

| Источник | Цвет | Отображение | Описание |
|----------|------|-------------|----------|
| **ELIS** | `#4CAF50` (зелёный) | Текст "ЕЛИС" 6px | Данные загружены из протокола ЕЛИС |
| **Manual** | `#2196F3` (синий) | Иконка `pi-user-edit` 14px | Ручное редактирование пользователем |
| **IVK** | `#FF9800` (оранжевый) | Текст "ИВК" 6px | Округление системой ИВК |
| **Unknown** | `#9E9E9E` (серый) | Иконка `pi-question-circle` 14px | Неизвестный источник (старые данные) |

**Триггер:** Наведение курсора (hover) показывает tooltip с описанием источника данных

### FormFieldWithHistory - Обёртка для полей AdditionalInfo

**Назначение:** Интеграция истории изменений в обычные поля формы

**Расположение:** `TN_Doc/Client/document-editor/src/components/FormFieldWithHistory.vue`

**Функциональность:**
- Использует существующий компонент FormField
- Добавляет FieldHistoryIndicator (tooltip)
- Автоматически отслеживает ручные изменения через `useFieldHistory.trackManualChange()`
- Поддерживает подсветку ELIS через highlightColor prop

### Компоненты для таблицы параметров качества

**PassportMeasurementInputWithHistory:**
- Обёртка для измеренных значений (ключ `value.{ParameterKey}`)
- Отслеживает изменения числовых полей

**PassportMethodSelectWithHistory:**
- Обёртка для выбора методов испытаний (ключ `method.{ParameterKey}`)
- Отслеживает изменения выбранного метода

**PassportResultCellWithHistory:**
- Обёртка для результирующих значений для печати (ключ `result.{ParameterKey}`)
- Отслеживает финальное значение после округления

**Важно:** Каждый тип поля (`value.*`, `method.*`, `result.*`) имеет раздельную историю изменений

### Технические детали

**Composable useFieldHistory:**
- Централизованная логика управления историей
- Функции: `trackManualChange()`, `trackElisLoad()`, `trackIVKRounding()`
- Нормализация значений для корректного сравнения (точка vs запятая)
- Автоматический FIFO при превышении 10 записей

**Интеграция с documentStore:**
- Хранилище `formHistory: Record<string, FieldHistoryEntry[]>`
- Передача на бэкенд через поле `__history` в payload
- Загрузка истории из fieldHistory при открытии документа

## Приложение: маршруты для скриншотов

- `/` — Главная страница с фильтрами и просмотром документов
- `/configurator` — Конфигуратор (SPA внутри iframe на главной и отдельный путь)
- (Планируется) `/document-editor` — Document Editor для редактирования паспортов качества

## Источники стилей (обновлено v1.4.3+)

### Централизованные стили
- **`wwwroot/css/material3.css`** — **ГЛАВНЫЙ ФАЙЛ**: все CSS переменные, токены цветов, размеров, шрифтов

### Базовые компоненты интерфейса
- `wwwroot/css/site.css` — базовые стили, Bootstrap-совместимые
- `wwwroot/css/LeftPanel.css` — левая панель фильтров
- `wwwroot/css/newstyle.css` — таблицы, кнопки, поля ввода
- `wwwroot/css/menu-dropdown.css` — меню действий
- `wwwroot/css/elisRequestWindow.css` — окно запроса ELIS
- `wwwroot/css/errorDialogWindow.css` — диалог ошибок
- `wwwroot/css/elisEditForm.css` — форма редактирования ELIS
- `wwwroot/css/commonEditForm.css` — общая форма редактирования

### Модули и функциональность
- `wwwroot/css/dictionaries/*` — UI справочников (меню слева, таблицы, элементы выбора)
- `wwwroot/statusbar/status-bar.css` — индикаторы статуса (старый CSS, постепенно мигрирует в Vue)

### Vue SPA модули (v1.4.2+)
- **StatusBar:** `TN_Doc/Client/statusbar/src/`
  - Индикаторы статуса устройств и сервисов
  - Vue 3 + TypeScript + PrimeVue
- **Configurator:** `TN_Doc/Client/configurator/src/`
  - Веб-интерфейс настроек приложения
  - Vue 3 + TypeScript + PrimeVue
- **Document Editor (v1.4.4+):** `TN_Doc/Client/document-editor/src/`
  - Редактор паспортов качества в браузере
  - Vue 3 + TypeScript + PrimeVue
  - Компоненты истории изменений: `src/components/history/`

### Правило использования цветов
⚠️ **ВАЖНО:** С версии v1.4.3+ **НЕ используйте hardcoded HEX цвета** в новых/обновляемых CSS файлах. Все цвета должны ссылаться на CSS переменные из `material3.css` (например, `var(--md-primary)`, `var(--md-text)`, `var(--md-border)`).

---

## Итоги

Документ содержит актуальную информацию о дизайн-системе TN_Doc на базе Material Design 3:

1. **Централизация цветов (v1.4.3+):** Все цвета в `material3.css` с CSS переменными
2. **Компоненты истории изменений (v1.4.4+):** Визуальные индикаторы источников данных
3. **Document Editor:** Готовый к production SPA-модуль для редактирования паспортов
4. **Единообразие:** Применение Material Design 3 токенов во всех модулях
5. **Доступность:** Планируется тестирование WCAG 2.1 AA

**Для разработчиков:** При работе с UI всегда используйте CSS переменные из `material3.css`, не создавайте новые hardcoded цвета.
