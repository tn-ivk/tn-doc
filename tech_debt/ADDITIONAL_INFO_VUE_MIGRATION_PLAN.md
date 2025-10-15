## Миграция таблицы AdditionalInfo на Vue-компонент

Цель: заменить статическую таблицу `#AdditionalInfo` в формах редактирования документов на Vue-компонент, сохранив текущие API, валидации, интеграцию с ЕЛИС и `SaveDoc`, а также привести стили к требованиям дизайн-документа (`tn.docgeneral/DESIGN_DOCUMENTATION.md`). 

### 1. Область охвата
- Файлы форм:
  - `TN_Doc/wwwroot/HTML/DocEdit.html`
  - `TN_Doc/wwwroot/HTML/DocEditAct.html`
  - `TN_Doc/wwwroot/HTML/DocEditPassport.html`
- Скрипты и функции, функционал которых должен быть повторен:
  - `GetUsers()`, `GetInvalideChars()` из `Common.js`
  - Валидации: `InputStringCheck`, `InputValueCheck`, `CheckEmpty`, `CheckInvalidChars`
  - Сохранение: `SaveDoc`, `SaveDocPassport`
  - Интеграция с ЕЛИС: заполнение элементов с `data-tag="AdditionalInfo"`, атрибуты `data-edit`, `data-elis-filled`, 
    инициализация tooltip

### 2. Архитектура решения
- Подключение Vue 3 через CDN локально в страницах форм (без bundler).
- Создание компонента `AdditionalInfoTable` в `TN_Doc/wwwroot/js/components/additional-info-table.js` (ES-модуль):
  - Пропсы: `fields`, `dictionaries`, `invalidChars`, `designTokens`.
  - Эмиссии событий: `change` (key, value), `initialized`.
  - Рендер: `<table id="AdditionalInfo"><tbody>...</tbody></table>`; строки на основании `fields`.
  - Генерация нативных `input/select/textarea` с обязательными атрибутами:
    - `data-edit="1"`, `data-tag="AdditionalInfo"`, `data-key` (ключ поля), `id`, `name` (совместимость с `UserChangeEvent`),
    - классы состояний: `correct-value`/`incorrect-value`, `elis-data` где требуется.
  - Для select сотрудников: опции из `dictionaries.Users`, значения `Id`, отображение текста `FIO`/`IOF` по типу формы.
  - Автопривязка обработчиков: `oninput="InputStringCheck(this)"`, `onchange="UserChangeEvent(this)"` при наличии правил.
  - Первичная валидация при mount: вызов `element.oninput()` если задан.
- Инициализация приложения на странице (вставка `<div id="additional-info-root">` перед таблицей, замена HTML таблицы на компонент).

### 3. Формат данных для компонента
- `fields: Array<AdditionalInfoField>` из backend (или текущей инициализации), каждый элемент:
  - `Key` (обяз.), `Caption` (подпись в левой колонке), `Control` (`input|select|textarea|datetime-local|number`), 
    `Edit` (bool -> `data-edit`), `RequiredFill` (-> атрибут `fill-required`), `Value`, 
    специфичные атрибуты: `data-roundvalue`, `data-elis-filled`, `ElisAlias`, `Name` (для `UserChangeEvent`).
- Если исходная форма генерируется сервером, требуется этап адаптации подготовки `fields` на клиенте (минимум: прокси-обёртка над уже вставленным HTML для извлечения и повторной инициализации) или расширение API контроллера для выдачи AdditionalInfo как JSON (рекомендуется, но не обязательно на первом шаге).

### 4. Интеграция по страницам
4.1 `DocEdit.html`
- Поля: `Delive_IOF`, `Delive_Post`, `Delive_Factory`, `Receive_IOF`, `Receive_Post`, `Receive_Factory`.
- Логика: `UserChangeEvent` заполняет Post/Factory по выбранному IOF (должны сохраняться `name/id`).

4.2 `DocEditAct.html`
- Поля сдающей/принимающей стороны: `*_IOF`, `*_FIO`, `*_Factory`, доверенности `*_Lic_Number`, `*_Lic_Date`.
- Логика: аналогично, плюс валидация даты/номера и интеграция с ЕЛИС (атрибуты `data-elis-filled` не ломать).

4.3 `DocEditPassport.html`
- Поля лаборатории: `Laboratory_IOF`, `Laboratory_Post`, `Laboratory_Factory`;
- Поля сдающей/принимающей стороны: аналогично `DocEdit.html`.

### 5. Стили и соответствие дизайн-документу
- Базироваться на:
  - `TN_Doc/wwwroot/css/material3.css` — цвета/токены состояний (primary, hover, outline, disabled);
  - `TN_Doc/wwwroot/css/commonEditForm.css` — существующая сетка и поведение `#AdditionalInfo`.
- Проверить и обеспечить:
  - Высота элементов 35px, скругление 8px, отступы 6px/10px;
  - Hover: граница `#B0BEC5`;
  - Focus: граница `--md-primary` и тень `rgba(30,136,229,0.35)`;
  - Disabled: фон `--md-surface-variant`, текст `--md-text-secondary`, курсор `not-allowed`.
- Не дублировать стили — только использовать существующие классы/переменные.

### 6. Совместимость и неблокирующие изменения
- Сохранить атрибуты, которые читает существующий код (`data-tag`, `data-edit`, `data-key`, `title`, `fill-required`, `data-roundvalue`).
- Не менять сигнатуры `GetUsers`, `GetInvalideChars` и работу `SaveDoc`/`SaveDocPassport` (сбор данных из DOM по селекторам остается валидным).
- Для tooltip использовать jQuery UI: после рендера вызвать `$("[title]").tooltip()` и обновлять `updateTooltip` при валидации.

### 7. Пошаговый план внедрения
1) Добавить контейнер `#additional-info-root` на каждой странице рядом с текущим `#AdditionalInfo`.
2) Подключить Vue 3 (CDN) только на этих трёх страницах.
3) Создать `additional-info-table.js` (ES-модуль) и инициализировать компонент в `document.ready`.
4) Прокинуть данные `fields` из текущих страниц (временная сборка с DOM/скриптов) или расширить backend, чтобы отдать JSON AdditionalInfo.
5) Вывести компонент и скрыть старую таблицу (или полностью заменить DOM-узел `#AdditionalInfo`).
6) Пройти чек-лист визуальных состояний по `DESIGN_DOCUMENTATION.md`.
7) Регрессионно проверить сценарии: валидация, автозаполнение по `UserChangeEvent`, ЕЛИС, сохранение, тултипы.

### 8. Риски и меры
- Несоответствие данных `fields` и ожиданий `SaveDoc` — проверять наличие `data-edit`/`data-key`/`data-tag`.
- Потеря инициализаторов (oninput/onchange) — принудительно навешивать/вызвать после mount.
- Конфликт стилей — использовать централизованные `material3.css` и `commonEditForm.css`.

### 9. Критерии готовности (DoD)
- На всех трёх страницах рендерится Vue-компонент вместо старой таблицы.
- Валидация и тултипы работают как раньше; статусы сохраняются.
- Интеграция с ЕЛИС заполняет значения и подсветку.
- `SaveDoc`/`SaveDocPassport` собирают корректные значения.
- Соответствие `DESIGN_DOCUMENTATION.md` по цветам/состояниям подтверждено визуальной проверкой.


