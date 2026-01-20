# Редактор справочников (DirEditor)

## Обзор

В текущей версии TN_Doc нет отдельного SPA‑конфигуратора. Вместо этого используется встроенный **редактор справочников** (DirEditor), который открывается модальным окном на главной странице приложения.

**Где находится UI:** `TN_Doc/Views/Home/Index.cshtml` (кнопка **«Справочники»**)

**Основной скрипт:** `TN_Doc/wwwroot/js/DirEditorComponentScript.js`

## Назначение

Редактор предназначен для правки:
- справочников персонала (группы пользователей, пользователи, доверенности)
- конфигурации методов испытаний паспорта качества (QP‑конфиги)

## Архитектура взаимодействия

```mermaid
sequenceDiagram
    participant User
    participant UI as DirEditor UI (modal)
    participant API as DirEditorController
    participant Cfg as AppConfigService

    User->>UI: Открыть «Справочники»
    UI->>API: GET /DirEditor/GetDir
    API->>Cfg: GetDictionariesJsonAsync()
    Cfg-->>API: JSON справочников
    API-->>UI: dirJsonRaw

    UI->>API: GET /DirEditor/GetQpConfigs
    API->>Cfg: GetQualityPassportConfigs()
    Cfg-->>API: JSON конфигураций
    API-->>UI: qpCfgJsonRaw

    User->>UI: Сохранить изменения
    UI->>API: POST /DirEditor/SetDir
    UI->>API: POST /DirEditor/SetQpConfigs
    API->>Cfg: SetDirectoriesJsonAsync / SetQpConfigFromJsonAsync
```

## Табличные разделы

### Вкладка «Персонал»
- **Группы пользователей** (`UsersGroup`)
- **Пользователи** (`Users`)
- **Доверенности** (`Licenses`)

### Вкладка «Методы испытаний»
- Методы/настройки, используемые для заполнения паспортов качества

## Формат данных

Запросы/ответы передают JSON‑строки в полях:
- `dirJsonRaw` — справочники
- `qpCfgJsonRaw` — конфигурации паспортов качества

## Эндпойнты

См. `docs/api/endpoints.md`:
- `GET /DirEditor/GetDir`
- `POST /DirEditor/SetDir`
- `GET /DirEditor/GetQpConfigs`
- `POST /DirEditor/SetQpConfigs`

